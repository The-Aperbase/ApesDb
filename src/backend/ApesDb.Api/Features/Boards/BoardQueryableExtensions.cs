using ApesDb.Domain.Entities.Boards;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards;

internal static class BoardQueryableExtensions
{
    public static Task<Board?> FindOwnedForUpdateAsync(
        this DbSet<Board> boards,
        Guid boardId,
        Guid ownerUserId,
        CancellationToken ct
    )
    {
        return boards
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM "public"."Boards"
                WHERE "Id" = {boardId} AND "OwnerUserId" = {ownerUserId}
                FOR UPDATE
                """
            )
            .SingleOrDefaultAsync(ct);
    }
}
