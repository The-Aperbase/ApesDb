ALTER TABLE "public"."BoardEntries"
    ADD COLUMN "Position" integer;

WITH ranked_entries AS (
    SELECT
        "BoardId",
        "GameId",
        row_number() OVER (
            PARTITION BY "BoardId", "StateId"
            ORDER BY "AddedAt", "GameId"
        ) - 1 AS "Position"
    FROM "public"."BoardEntries"
)
UPDATE "public"."BoardEntries" AS entries
SET "Position" = ranked_entries."Position"
FROM ranked_entries
WHERE entries."BoardId" = ranked_entries."BoardId"
  AND entries."GameId" = ranked_entries."GameId";

ALTER TABLE "public"."BoardEntries"
    ALTER COLUMN "Position" SET NOT NULL,
    ADD CONSTRAINT "CK_BoardEntries_Position_NonNegative" CHECK ("Position" >= 0),
    ADD CONSTRAINT "UQ_BoardEntries_BoardId_StateId_Position"
        UNIQUE ("BoardId", "StateId", "Position")
        DEFERRABLE INITIALLY DEFERRED;
