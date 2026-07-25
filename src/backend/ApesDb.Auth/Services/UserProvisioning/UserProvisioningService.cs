using System.Security.Claims;
using ApesDb.Common;
using ApesDb.Domain;
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
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? "Unknown Soldier";
        var pictureUrl = principal.FindFirstValue("picture");
        var now = _dateTimeProvider.UtcNow;
        var provisionedUserId = await _dbContext
            .Database.SqlQuery<Guid>(
                $"""
                INSERT INTO "public"."Users" (
                    "Auth0Subject", "Email", "Name", "PictureUrl", "CreatedAt", "UpdatedAt"
                )
                VALUES ({subject}, {email}, {name}, {pictureUrl}, {now}, {now})
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

        return new ProvisionedUser(provisionedUserId);
    }
}
