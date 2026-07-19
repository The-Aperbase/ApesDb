ALTER TABLE "public"."GamesListEntries"
    ADD COLUMN IF NOT EXISTS "State" integer NOT NULL DEFAULT 0;

ALTER TABLE "public"."GamesListEntries"
    ADD CONSTRAINT "CK_GamesListEntries_State" CHECK ("State" IN (0, 1, 2));
