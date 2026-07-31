using ApesDb.Domain;
using ApesDb.Domain.Entities.Boards;
using Microsoft.EntityFrameworkCore;

namespace ApesDb.Api.Features.Boards;

internal static class BoardQueryableExtensions
{
    public static IQueryable<Board> WhereAccessibleTo(
        this IQueryable<Board> boards,
        ApplicationDbContext dbContext,
        Guid userId
    )
    {
        return boards.Where(board =>
            board.OwnerUserId == userId
            || dbContext.BoardCollaborators.Any(collaborator =>
                collaborator.BoardId == board.Id && collaborator.UserId == userId
            )
        );
    }

    public static Task<Board?> FindAccessibleForUpdateAsync(
        this DbSet<Board> boards,
        Guid boardId,
        Guid userId,
        CancellationToken ct
    )
    {
        return boards
            .FromSqlInterpolated(
                $"""
                SELECT board.*
                FROM "public"."Boards" AS board
                WHERE board."Id" = {boardId}
                  AND (
                    board."OwnerUserId" = {userId}
                    OR EXISTS (
                        SELECT 1
                        FROM "public"."BoardCollaborators" AS collaborator
                        WHERE collaborator."BoardId" = board."Id"
                          AND collaborator."UserId" = {userId}
                    )
                  )
                FOR UPDATE
                """
            )
            .SingleOrDefaultAsync(ct);
    }

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
