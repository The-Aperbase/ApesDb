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

CREATE TABLE "public"."CalendarInvitations" (
    "Id" uuid NOT NULL DEFAULT uuidv7(),
    "InviterUserId" uuid NOT NULL,
    "InviteeUserId" uuid,
    "InviteeEmail" character varying(256) NOT NULL,
    "Status" character varying(16) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ResolvedAt" timestamp with time zone,
    CONSTRAINT "PK_CalendarInvitations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CalendarInvitations_Users_InviterUserId" FOREIGN KEY ("InviterUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CalendarInvitations_Users_InviteeUserId" FOREIGN KEY ("InviteeUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_CalendarInvitations_Status"
        CHECK ("Status" IN ('Pending', 'Accepted', 'Declined', 'Cancelled')),
    CONSTRAINT "CK_CalendarInvitations_Resolution" CHECK (
        ("Status" = 'Pending' AND "ResolvedAt" IS NULL)
        OR
        ("Status" <> 'Pending' AND "ResolvedAt" IS NOT NULL)
    )
);

CREATE INDEX "IX_CalendarInvitations_InviteeUserId_Status"
    ON "public"."CalendarInvitations" ("InviteeUserId", "Status");
CREATE UNIQUE INDEX "UX_CalendarInvitations_InviterUserId_InviteeEmail_Pending"
    ON "public"."CalendarInvitations" ("InviterUserId", "InviteeEmail")
    WHERE "Status" = 'Pending';

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
