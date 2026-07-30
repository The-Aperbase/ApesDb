using System.Security.Claims;
using ApesDb.Common;
using ApesDb.Domain;
using ApesDb.Domain.Entities.Calendar;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Auth.Services.UserProvisioning;

public sealed class UserProvisioningService : IUserProvisioningService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UserProvisioningService(ApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ProvisionedUser> EnsureUserFromPrincipalAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default
    )
    {
        var subject =
            principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing subject claim.");
        var email =
            principal.FindFirstValue(ClaimTypes.Email) ?? throw new InvalidOperationException("Missing email claim.");
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? "Unknown Soldier";
        var pictureUrl = principal.FindFirstValue("picture");
        var now = _dateTimeProvider.UtcNow;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var provisionedUserId = await _dbContext
            .Database.SqlQuery<Guid>(
                $"""
                INSERT INTO "public"."Users" (
                    "Auth0Subject", "Email", "Name", "PictureUrl", "CreatedAt", "UpdatedAt"
                )
                VALUES ({subject}, {normalizedEmail}, {name}, {pictureUrl}, {now}, {now})
                ON CONFLICT ("Auth0Subject") DO UPDATE SET
                    "Email" = EXCLUDED."Email",
                    "Name" = EXCLUDED."Name",
                    "PictureUrl" = EXCLUDED."PictureUrl",
                    "UpdatedAt" = EXCLUDED."UpdatedAt"
                RETURNING "Id" AS "Value"
                """
            )
            .AsAsyncEnumerable()
            .SingleAsync(cancellationToken);

        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "public"."CalendarInvitations"
            SET "InviteeUserId" = {provisionedUserId}
            WHERE "InviteeUserId" IS NULL
                AND "InviteeEmail" = {normalizedEmail}
                AND "StatusId" = {CalendarInvitationStatus.Pending}
            """,
            cancellationToken
        );
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "public"."Notifications" (
                "Id", "UserId", "Type", "ResourceId", "IsActionable", "CreatedAt"
            )
            SELECT uuidv7(), {provisionedUserId}, 'CalendarInvite', invitation."Id", true, {now}
            FROM "public"."CalendarInvitations" AS invitation
            WHERE invitation."InviteeUserId" = {provisionedUserId}
                AND invitation."StatusId" = {CalendarInvitationStatus.Pending}
            ON CONFLICT ("UserId", "Type", "ResourceId") DO NOTHING
            """,
            cancellationToken
        );
        await transaction.CommitAsync(cancellationToken);

        return new ProvisionedUser(provisionedUserId);
    }
}
