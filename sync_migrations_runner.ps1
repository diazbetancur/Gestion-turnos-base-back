# Script PowerShell para ejecutar el SQL de sincronización de migraciones
# Asegúrate de tener instalado Npgsql

$connectionString = "Host=database-gandarias.ct6gmyi80fdr.eu-central-1.rds.amazonaws.com;Port=5432;Username=postgres;Password=MyStrongPassword!123_;Database=postgres;SSL Mode=Require;Trust Server Certificate=true"

$sql = @"
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
('20251113125107_ChangeDateTimeToDateOnlyInUserAbsenteeism', '8.0.10')
ON CONFLICT ("MigrationId") DO NOTHING;
"@

try {
    Add-Type -AssemblyName "System.Data"
    
    # Intentar cargar Npgsql
    $npgsqlPath = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\npgsql" -Recurse -Filter "Npgsql.dll" | Select-Object -First 1
    
    if ($npgsqlPath) {
        Add-Type -Path $npgsqlPath.FullName
        Write-Host "? Npgsql cargado desde: $($npgsqlPath.FullName)"
        
        $conn = New-Object Npgsql.NpgsqlConnection($connectionString)
        $conn.Open()
        
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        $rowsAffected = $cmd.ExecuteNonQuery()
        
        Write-Host "? Script ejecutado exitosamente. Filas afectadas: $rowsAffected"
        
        $conn.Close()
    } else {
        Write-Host "? No se encontró Npgsql. Ejecuta el SQL manualmente en pgAdmin o DBeaver."
    }
} catch {
    Write-Host "? Error: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Por favor, ejecuta el archivo sync_migrations.sql manualmente en tu cliente PostgreSQL (pgAdmin, DBeaver, etc.)"
}
