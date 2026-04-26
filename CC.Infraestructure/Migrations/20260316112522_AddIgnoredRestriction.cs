using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CC.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIgnoredRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Management"".""IgnoredRestrictions"" (
                    ""Id"" uuid NOT NULL DEFAULT gen_random_uuid(),
                    ""Date"" date NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""Observation"" text NOT NULL,
                    ""DateCreated"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_IgnoredRestrictions"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_IgnoredRestrictions_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Management"".""AspNetUsers"" (""Id"") ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS ""Management"".""ScheduleGaps"" (
                    ""Id"" uuid NOT NULL DEFAULT gen_random_uuid(),
                    ""Date"" date NOT NULL,
                    ""WorkstationId"" uuid NOT NULL,
                    ""StartTime"" time without time zone NOT NULL,
                    ""EndTime"" time without time zone NOT NULL,
                    ""GapExplanation"" text NULL,
                    ""GapCategory"" text NULL,
                    ""Token"" text NULL,
                    ""IsPostAi"" boolean NOT NULL DEFAULT false,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_ScheduleGaps"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_ScheduleGaps_Workstations_WorkstationId"" FOREIGN KEY (""WorkstationId"") REFERENCES ""Management"".""Workstations"" (""Id"") ON DELETE RESTRICT
                );

                CREATE TABLE IF NOT EXISTS ""Management"".""ScheduleQualityShiftScores"" (
                    ""Id"" uuid NOT NULL,
                    ""Token"" text NOT NULL,
                    ""DemandId"" uuid NULL,
                    ""Date"" date NOT NULL,
                    ""WorkstationName"" text NULL,
                    ""StartTime"" interval NOT NULL,
                    ""EndTime"" interval NOT NULL,
                    ""Demanded"" numeric(10,2) NOT NULL DEFAULT 0,
                    ""Covered"" numeric(10,2) NOT NULL DEFAULT 0,
                    ""Unmet"" numeric(10,2) NOT NULL DEFAULT 0,
                    ""CoverageScore"" numeric(5,2) NOT NULL DEFAULT 100,
                    ""FairnessScore"" numeric(5,2) NOT NULL DEFAULT 100,
                    ""RulesScore"" numeric(5,2) NOT NULL DEFAULT 100,
                    ""Score"" numeric(5,2) NOT NULL DEFAULT 100,
                    ""Metrics"" jsonb NOT NULL DEFAULT '{}'::jsonb,
                    ""ConfigSnapshot"" jsonb NULL,
                    ""IsPostAi"" boolean NOT NULL DEFAULT false,
                    ""DateCreated"" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_ScheduleQualityShiftScores"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""Management"".""ScheduleSuggestions"" (
                    ""Id"" uuid NOT NULL,
                    ""Token"" character varying(100) NOT NULL,
                    ""WeekStart"" date NOT NULL,
                    ""WeekEnd"" date NOT NULL,
                    ""Tipo"" character varying(50) NOT NULL,
                    ""Prioridad"" character varying(20) NOT NULL,
                    ""Titulo"" character varying(200) NOT NULL,
                    ""Descripcion"" text NULL,
                    ""ImpactoEsperado"" jsonb NULL,
                    ""MejoraEstimada"" numeric(5,2) NULL,
                    ""Detalles"" jsonb NULL,
                    ""EmpleadosInvolucrados"" jsonb NULL,
                    ""WorkstationsAfectadas"" jsonb NULL,
                    ""DiasAfectados"" jsonb NULL,
                    ""AccionesConcretas"" jsonb NULL,
                    ""Implementada"" boolean NULL DEFAULT false,
                    ""FechaImplementacion"" timestamp without time zone NULL,
                    ""IsPostAi"" boolean NOT NULL DEFAULT false,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT ""PK_ScheduleSuggestions"" PRIMARY KEY (""Id"")
                );

                CREATE INDEX IF NOT EXISTS ""IX_IgnoredRestrictions_UserId"" ON ""Management"".""IgnoredRestrictions"" (""UserId"");
                CREATE INDEX IF NOT EXISTS ""IX_ScheduleGaps_WorkstationId"" ON ""Management"".""ScheduleGaps"" (""WorkstationId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""Management"".""IgnoredRestrictions"";
                DROP TABLE IF EXISTS ""Management"".""ScheduleGaps"";
                DROP TABLE IF EXISTS ""Management"".""ScheduleQualityShiftScores"";
                DROP TABLE IF EXISTS ""Management"".""ScheduleSuggestions"";
            ");
        }
    }
}
