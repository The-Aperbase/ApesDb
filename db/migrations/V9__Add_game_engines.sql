CREATE TABLE IF NOT EXISTS "public"."GameEngines" (
    "Id" bigint NOT NULL,
    "Name" character varying(256) NOT NULL,
    "Checksum" uuid,
    "IgdbUpdatedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT now(),
    "LastSyncedAt" timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT "PK_GameEngines" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "public"."GameGameEngines" (
    "GameId" bigint NOT NULL,
    "GameEngineId" bigint NOT NULL,
    CONSTRAINT "PK_GameGameEngines" PRIMARY KEY ("GameId", "GameEngineId"),
    CONSTRAINT "FK_GameGameEngines_Games_GameId"
        FOREIGN KEY ("GameId") REFERENCES "public"."Games" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_GameGameEngines_GameEngines_GameEngineId"
        FOREIGN KEY ("GameEngineId") REFERENCES "public"."GameEngines" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_GameGameEngines_GameEngineId"
    ON "public"."GameGameEngines" ("GameEngineId");
