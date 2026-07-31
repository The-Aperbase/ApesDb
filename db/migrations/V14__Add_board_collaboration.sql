CREATE TABLE "public"."BoardInvitationStatuses" (
    "Id" integer NOT NULL,
    "Name" character varying(16) NOT NULL,
    CONSTRAINT "PK_BoardInvitationStatuses" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_BoardInvitationStatuses_Name"
    ON "public"."BoardInvitationStatuses" ("Name");

INSERT INTO "public"."BoardInvitationStatuses" ("Id", "Name")
VALUES (0, 'pending'), (1, 'accepted'), (2, 'declined'), (3, 'cancelled');

CREATE TABLE "public"."BoardInvitations" (
    "Id" uuid NOT NULL DEFAULT uuidv7(),
    "BoardId" uuid NOT NULL,
    "InviteeUserId" uuid,
    "InviteeEmail" character varying(256) NOT NULL,
    "StatusId" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "ResolvedAt" timestamp with time zone,
    CONSTRAINT "PK_BoardInvitations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_BoardInvitations_Boards_BoardId" FOREIGN KEY ("BoardId")
        REFERENCES "public"."Boards" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BoardInvitations_Users_InviteeUserId" FOREIGN KEY ("InviteeUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BoardInvitations_BoardInvitationStatuses_StatusId" FOREIGN KEY ("StatusId")
        REFERENCES "public"."BoardInvitationStatuses" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_BoardInvitations_Resolution" CHECK (
        ("StatusId" = 0 AND "ResolvedAt" IS NULL)
        OR ("StatusId" <> 0 AND "ResolvedAt" IS NOT NULL)
    )
);

CREATE UNIQUE INDEX "IX_BoardInvitations_BoardId_InviteeEmail_Pending"
    ON "public"."BoardInvitations" ("BoardId", "InviteeEmail")
    WHERE "StatusId" = 0;
CREATE INDEX "IX_BoardInvitations_InviteeUserId_StatusId"
    ON "public"."BoardInvitations" ("InviteeUserId", "StatusId");

CREATE TABLE "public"."BoardCollaborators" (
    "BoardId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "JoinedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_BoardCollaborators" PRIMARY KEY ("BoardId", "UserId"),
    CONSTRAINT "FK_BoardCollaborators_Boards_BoardId" FOREIGN KEY ("BoardId")
        REFERENCES "public"."Boards" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BoardCollaborators_Users_UserId" FOREIGN KEY ("UserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_BoardCollaborators_UserId"
    ON "public"."BoardCollaborators" ("UserId");
