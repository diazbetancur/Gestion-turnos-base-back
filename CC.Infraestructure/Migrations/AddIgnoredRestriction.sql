START TRANSACTION;

CREATE TABLE "Management"."IgnoredRestrictions" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "Date" date NOT NULL,
    "UserId" uuid NOT NULL,
    "Observation" text NOT NULL,
    "DateCreated" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_IgnoredRestrictions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_IgnoredRestrictions_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "Management"."AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Management"."ScheduleGaps" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "Date" date NOT NULL,
    "WorkstationId" uuid NOT NULL,
    "StartTime" time without time zone NOT NULL,
    "EndTime" time without time zone NOT NULL,
    "GapExplanation" text,
    "GapCategory" text,
    "Token" text,
    "IsPostAi" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_ScheduleGaps" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ScheduleGaps_Workstations_WorkstationId" FOREIGN KEY ("WorkstationId") REFERENCES "Management"."Workstations" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Management"."ScheduleQualityShiftScores" (
    "Id" uuid NOT NULL,
    "Token" text NOT NULL,
    "DemandId" uuid,
    "Date" date NOT NULL,
    "WorkstationName" text,
    "StartTime" interval NOT NULL,
    "EndTime" interval NOT NULL,
    "Demanded" numeric(10,2) NOT NULL DEFAULT 0.0,
    "Covered" numeric(10,2) NOT NULL DEFAULT 0.0,
    "Unmet" numeric(10,2) NOT NULL DEFAULT 0.0,
    "CoverageScore" numeric(5,2) NOT NULL DEFAULT 100.0,
    "FairnessScore" numeric(5,2) NOT NULL DEFAULT 100.0,
    "RulesScore" numeric(5,2) NOT NULL DEFAULT 100.0,
    "Score" numeric(5,2) NOT NULL DEFAULT 100.0,
    "Metrics" jsonb NOT NULL DEFAULT ('{}'::jsonb),
    "ConfigSnapshot" jsonb,
    "IsPostAi" boolean NOT NULL DEFAULT FALSE,
    "DateCreated" timestamp without time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_ScheduleQualityShiftScores" PRIMARY KEY ("Id")
);

CREATE TABLE "Management"."ScheduleSuggestions" (
    "Id" uuid NOT NULL,
    "Token" character varying(100) NOT NULL,
    "WeekStart" date NOT NULL,
    "WeekEnd" date NOT NULL,
    "Tipo" character varying(50) NOT NULL,
    "Prioridad" character varying(20) NOT NULL,
    "Titulo" character varying(200) NOT NULL,
    "Descripcion" text,
    "ImpactoEsperado" jsonb,
    "MejoraEstimada" numeric(5,2),
    "Detalles" jsonb,
    "EmpleadosInvolucrados" jsonb,
    "WorkstationsAfectadas" jsonb,
    "DiasAfectados" jsonb,
    "AccionesConcretas" jsonb,
    "Implementada" boolean DEFAULT FALSE,
    "FechaImplementacion" timestamp without time zone,
    "IsPostAi" boolean NOT NULL DEFAULT FALSE,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_ScheduleSuggestions" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_IgnoredRestrictions_UserId" ON "Management"."IgnoredRestrictions" ("UserId");

CREATE INDEX "IX_ScheduleGaps_WorkstationId" ON "Management"."ScheduleGaps" ("WorkstationId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260316112522_AddIgnoredRestriction', '8.0.11');

COMMIT;

