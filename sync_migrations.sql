-- Script para sincronizar el historial de migraciones con el estado actual de la BD
-- Ejecuta este script en tu base de datos PostgreSQL

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES
('20250509014501_creationBase', '8.0.10'),
('20250510153555_hybridWorkStation', '8.0.10'),
('20250510155913_updateUserWorkStation', '8.0.10'),
('20250512140714_addUserRestrictions', '8.0.10'),
('20250513014437_addRestriction', '8.0.10'),
('20250516141417_addPropertiesUser', '8.0.10'),
('20250521000717_updateUserAddExtraHours', '8.0.10'),
('20250521001143_updateUserWorskStationPreference', '8.0.10'),
('20250521135043_addEmployeeRestrictionWeek', '8.0.10'),
('20250521162408_addAbsenteeismType', '8.0.10'),
('20250521164423_addUserAbsenteeism', '8.0.10'),
('20250525144638_employeeExceptions', '8.0.10'),
('20250603010124_addPropertiesUserCant', '8.0.10'),
('20250603012050_addLawRestriction', '8.0.10'),
('20250605001530_addTemplateWorkStation', '8.0.10'),
('20250707115304_updateShiftType', '8.0.10'),
('20250708155347_shiftTypeRestriction', '8.0.10'),
('20250719174245_addForgot', '8.0.10'),
('20250720224236_updateWorkstations', '8.0.10'),
('20250720232016_schedule', '8.0.10'),
('20250725014551_userShiftResolve', '8.0.10'),
('20250728215711_alterSchedule', '8.0.10'),
('20250731125231_addfieldsData', '8.0.10'),
('20250815015403_addPropertiesSchedule', '8.0.10'),
('20250815200751_addNullableRelation', '8.0.10'),
('20250819165824_addColorWorkArea', '8.0.10'),
('20250828030910_addSigning', '8.0.10'),
('20250831192305_updateSigning', '8.0.10'),
('20251113125107_ChangeDateTimeToDateOnlyInUserAbsenteeism', '8.0.10'),
('20260111162601_AddAuditLogTable', '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verifica que todas las migraciones fueron registradas
SELECT * FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";
