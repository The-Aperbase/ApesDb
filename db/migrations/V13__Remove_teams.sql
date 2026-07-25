DELETE FROM "public"."Notifications";

ALTER TABLE "public"."Notifications"
    DROP CONSTRAINT IF EXISTS "CK_Notifications_Type";

ALTER TABLE "public"."Notifications"
    ALTER COLUMN "Type" TYPE varchar(100) USING "Type"::text;

INSERT INTO "public"."Boards" (
    "Id", "OwnerUserId", "Name", "Picture", "CreatedAt", "UpdatedAt"
)
SELECT lists."Id",
       teams."OwnerUserId",
       left(lists."Name", 128),
       lists."Picture",
       lists."CreatedAt",
       lists."UpdatedAt"
FROM "public"."GamesLists" lists
JOIN "public"."Teams" teams ON teams."Id" = lists."TeamId";

INSERT INTO "public"."BoardEntries" ("BoardId", "GameId", "State", "AddedAt")
SELECT "GamesListId", "GameId", "State", "AddedAt"
FROM "public"."GamesListEntries";

DROP TABLE IF EXISTS "public"."GamesListEntries";
DROP TABLE IF EXISTS "public"."GamesLists";
DROP TABLE IF EXISTS "public"."TeamMemberships";
DROP TABLE IF EXISTS "public"."Teams";
