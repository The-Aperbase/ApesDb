DELETE FROM "public"."Notifications";

ALTER TABLE "public"."Notifications"
    DROP CONSTRAINT IF EXISTS "CK_Notifications_Type";

ALTER TABLE "public"."Notifications"
    ALTER COLUMN "Type" TYPE varchar(100) USING "Type"::text;

DROP TABLE IF EXISTS "public"."TeamMemberships";
DROP TABLE IF EXISTS "public"."Teams";
