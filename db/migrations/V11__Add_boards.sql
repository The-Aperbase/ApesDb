CREATE TABLE "public"."Boards" (
    "Id" uuid NOT NULL DEFAULT uuidv7(),
    "OwnerUserId" uuid NOT NULL,
    "Name" character varying(128) NOT NULL,
    "Picture" bytea,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_Boards" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Boards_Users_OwnerUserId" FOREIGN KEY ("OwnerUserId")
        REFERENCES "public"."Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Boards_OwnerUserId" ON "public"."Boards" ("OwnerUserId");

CREATE TABLE "public"."BoardEntries" (
    "BoardId" uuid NOT NULL,
    "GameId" bigint NOT NULL,
    "State" integer NOT NULL DEFAULT 0,
    "AddedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_BoardEntries" PRIMARY KEY ("BoardId", "GameId"),
    CONSTRAINT "FK_BoardEntries_Boards_BoardId" FOREIGN KEY ("BoardId")
        REFERENCES "public"."Boards" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BoardEntries_Games_GameId" FOREIGN KEY ("GameId")
        REFERENCES "public"."Games" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_BoardEntries_State" CHECK ("State" IN (0, 1, 2, 3))
);

CREATE INDEX "IX_BoardEntries_GameId" ON "public"."BoardEntries" ("GameId");
