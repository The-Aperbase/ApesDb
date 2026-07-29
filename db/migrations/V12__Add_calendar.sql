CREATE TABLE "public"."CalendarEvents" (
    "Id" uuid NOT NULL DEFAULT uuidv7(),
    "OwnerUserId" uuid NOT NULL,
    "Title" character varying(128) NOT NULL,
    "StartAt" timestamp with time zone NOT NULL,
    "EndAt" timestamp with time zone NOT NULL,
    "AllDay" boolean NOT NULL DEFAULT false,
    "TimeZoneId" character varying(128) NOT NULL,
    "RecurrenceJson" jsonb,
    "RecurrenceUntil" timestamp with time zone,
    "RecurringEventId" uuid,
    "OriginalStartAt" timestamp with time zone,
    "IsCancelled" boolean NOT NULL DEFAULT false,
    "TitleOverridden" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_CalendarEvents" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CalendarEvents_Users_OwnerUserId" FOREIGN KEY ("OwnerUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CalendarEvents_CalendarEvents_RecurringEventId" FOREIGN KEY ("RecurringEventId")
        REFERENCES "public"."CalendarEvents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_CalendarEvents_Duration" CHECK ("EndAt" > "StartAt"),
    CONSTRAINT "CK_CalendarEvents_Exception" CHECK (
        ("RecurringEventId" IS NULL AND "OriginalStartAt" IS NULL AND "IsCancelled" = false)
        OR
        ("RecurringEventId" IS NOT NULL AND "OriginalStartAt" IS NOT NULL AND "RecurrenceJson" IS NULL)
    )
);

CREATE INDEX "IX_CalendarEvents_OwnerUserId_StartAt_EndAt"
    ON "public"."CalendarEvents" ("OwnerUserId", "StartAt", "EndAt");
CREATE INDEX "IX_CalendarEvents_RecurringEventId"
    ON "public"."CalendarEvents" ("RecurringEventId");
CREATE UNIQUE INDEX "UX_CalendarEvents_RecurringEventId_OriginalStartAt"
    ON "public"."CalendarEvents" ("RecurringEventId", "OriginalStartAt")
    WHERE "RecurringEventId" IS NOT NULL;

CREATE TABLE "public"."CalendarInvitationStatuses" (
    "Id" integer NOT NULL,
    "Name" character varying(16) NOT NULL,
    CONSTRAINT "PK_CalendarInvitationStatuses" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_CalendarInvitationStatuses_Name"
    ON "public"."CalendarInvitationStatuses" ("Name");

INSERT INTO "public"."CalendarInvitationStatuses" ("Id", "Name")
VALUES
    (0, 'Pending'),
    (1, 'Accepted'),
    (2, 'Declined'),
    (3, 'Cancelled');

CREATE TABLE "public"."CalendarInvitations" (
    "Id" uuid NOT NULL DEFAULT uuidv7(),
    "InviterUserId" uuid NOT NULL,
    "InviteeUserId" uuid,
    "InviteeEmail" character varying(256) NOT NULL,
    "StatusId" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ResolvedAt" timestamp with time zone,
    CONSTRAINT "PK_CalendarInvitations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CalendarInvitations_Users_InviterUserId" FOREIGN KEY ("InviterUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CalendarInvitations_Users_InviteeUserId" FOREIGN KEY ("InviteeUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CalendarInvitations_CalendarInvitationStatuses_StatusId" FOREIGN KEY ("StatusId")
        REFERENCES "public"."CalendarInvitationStatuses" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_CalendarInvitations_Resolution" CHECK (
        ("StatusId" = 0 AND "ResolvedAt" IS NULL)
        OR
        ("StatusId" <> 0 AND "ResolvedAt" IS NOT NULL)
    )
);

CREATE INDEX "IX_CalendarInvitations_InviteeUserId_StatusId"
    ON "public"."CalendarInvitations" ("InviteeUserId", "StatusId");
CREATE INDEX "IX_CalendarInvitations_StatusId"
    ON "public"."CalendarInvitations" ("StatusId");
CREATE UNIQUE INDEX "UX_CalendarInvitations_InviterUserId_InviteeEmail_Pending"
    ON "public"."CalendarInvitations" ("InviterUserId", "InviteeEmail")
    WHERE "StatusId" = 0;

CREATE TABLE "public"."CalendarConnections" (
    "Id" uuid NOT NULL DEFAULT uuidv7(),
    "FirstUserId" uuid NOT NULL,
    "SecondUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_CalendarConnections" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CalendarConnections_Users_FirstUserId" FOREIGN KEY ("FirstUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CalendarConnections_Users_SecondUserId" FOREIGN KEY ("SecondUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_CalendarConnections_DistinctUsers" CHECK ("FirstUserId" <> "SecondUserId")
);

CREATE INDEX "IX_CalendarConnections_FirstUserId" ON "public"."CalendarConnections" ("FirstUserId");
CREATE INDEX "IX_CalendarConnections_SecondUserId" ON "public"."CalendarConnections" ("SecondUserId");
CREATE UNIQUE INDEX "UX_CalendarConnections_UserPair"
    ON "public"."CalendarConnections" (
        LEAST("FirstUserId", "SecondUserId"),
        GREATEST("FirstUserId", "SecondUserId")
    );
