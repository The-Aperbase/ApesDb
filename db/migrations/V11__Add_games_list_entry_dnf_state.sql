ALTER TABLE "public"."GamesListEntries"
    DROP CONSTRAINT "CK_GamesListEntries_State";

ALTER TABLE "public"."GamesListEntries"
    ADD CONSTRAINT "CK_GamesListEntries_State" CHECK ("State" IN (0, 1, 2, 3));
