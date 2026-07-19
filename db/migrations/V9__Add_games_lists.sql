CREATE TABLE IF NOT EXISTS "public"."GamesLists" (
    "Id" uuid NOT NULL DEFAULT uuidv7(),
    "TeamId" uuid NOT NULL,
    "Name" character varying(256) NOT NULL,
    "Picture" bytea,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_GamesLists" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GamesLists_Teams_TeamId" FOREIGN KEY ("TeamId")
        REFERENCES "public"."Teams" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_GamesLists_TeamId" ON "public"."GamesLists" ("TeamId");

CREATE TABLE IF NOT EXISTS "public"."GamesListEntries" (
    "GamesListId" uuid NOT NULL,
    "GameId" bigint NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_GamesListEntries" PRIMARY KEY ("GamesListId", "GameId"),
    CONSTRAINT "FK_GamesListEntries_GamesLists_GamesListId" FOREIGN KEY ("GamesListId")
        REFERENCES "public"."GamesLists" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_GamesListEntries_Games_GameId" FOREIGN KEY ("GameId")
        REFERENCES "public"."Games" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_GamesListEntries_GameId" ON "public"."GamesListEntries" ("GameId");
