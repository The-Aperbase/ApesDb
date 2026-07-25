ALTER TABLE "public"."BoardEntries"
    DROP CONSTRAINT "CK_BoardEntries_State";

ALTER TABLE "public"."BoardEntries"
    ALTER COLUMN "State" DROP DEFAULT,
    ALTER COLUMN "State" TYPE character varying(16)
        USING CASE "State"
            WHEN 0 THEN 'Todo'
            WHEN 1 THEN 'InProgress'
            WHEN 2 THEN 'Completed'
            WHEN 3 THEN 'Dnf'
        END,
    ALTER COLUMN "State" SET DEFAULT 'Todo';

ALTER TABLE "public"."BoardEntries"
    ADD CONSTRAINT "CK_BoardEntries_State"
        CHECK ("State" IN ('Todo', 'InProgress', 'Completed', 'Dnf'));
