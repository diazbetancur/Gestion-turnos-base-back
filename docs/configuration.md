# Configuracion Segura por Ambiente

Este backend debe cargar configuracion en este orden:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. `dotnet user-secrets` en `Development`
4. Variables de entorno del proveedor de despliegue

Reglas:

- No subir secretos al repositorio.
- El valor anterior de `jwtKey` debe considerarse comprometido y debe rotarse antes de cualquier despliegue.
- No reutilizar secretos, dominios ni endpoints del proyecto anterior.
- No registrar connection strings, `jwtKey`, credenciales SMTP ni claves de cifrado en logs.

## Variables requeridas

Secretas:

- `ConnectionStrings__PgSQL`
- `jwtKey`
- `EmailService__smtpPassword`
- `Encryption__Key`
- `Encryption__IV`

Configuracion operativa:

- `AllowedOrigins`
- `EmailService__smtpServer`
- `EmailService__smtpPort`
- `EmailService__smtpUser`
- `EmailService__EnableSsl`
- `Genearls__UrlForgot`
- `PythonApiSettings__BaseUrl`
- `PythonApiSettings__FunctionName`
- `PythonApiSettings__Timeout`
- `MigrationSettings__EnableAutoMigrate`
- `MigrationSettings__BaselineMigrationId`
- `JwtTokenSettings__DefaultHours`
- `JwtTokenSettings__CoordinatorHours`

## Development con user-secrets

Inicializacion:

```bash
cd Api-Gandarias
dotnet user-secrets init
```

Ejemplos seguros:

```bash
dotnet user-secrets set "ConnectionStrings:PgSQL" "Host=localhost;Port=5432;Username=app_user;Password=CHANGE_ME;Database=gestion_turnos;Include Error Detail=true"
dotnet user-secrets set "jwtKey" "CHANGE_ME_WITH_A_RANDOM_SECRET_OF_AT_LEAST_32_CHARACTERS"
dotnet user-secrets set "EmailService:smtpServer" "smtp.example.com"
dotnet user-secrets set "EmailService:smtpPort" "587"
dotnet user-secrets set "EmailService:smtpUser" "app@example.com"
dotnet user-secrets set "EmailService:smtpPassword" "CHANGE_ME"
dotnet user-secrets set "EmailService:EnableSsl" "true"
dotnet user-secrets set "Genearls:UrlForgot" "http://localhost:4200/auth"
dotnet user-secrets set "PythonApiSettings:BaseUrl" "http://localhost:8000"
dotnet user-secrets set "PythonApiSettings:FunctionName" "local-python-api"
dotnet user-secrets set "PythonApiSettings:Timeout" "1520"
dotnet user-secrets set "Encryption:Key" "BASE64_ENCRYPTION_KEY"
dotnet user-secrets set "Encryption:IV" "BASE64_ENCRYPTION_IV"
dotnet user-secrets set "MigrationSettings:EnableAutoMigrate" "true"
```

## QA y Production con variables de entorno

Variables soportadas por `AddEnvironmentVariables()`:

```text
ConnectionStrings__PgSQL
jwtKey
AllowedOrigins__0
AllowedOrigins__1
EmailService__smtpServer
EmailService__smtpPort
EmailService__smtpUser
EmailService__smtpPassword
EmailService__EnableSsl
Genearls__UrlForgot
PythonApiSettings__BaseUrl
PythonApiSettings__FunctionName
PythonApiSettings__Timeout
Encryption__Key
Encryption__IV
MigrationSettings__EnableAutoMigrate
MigrationSettings__BaselineMigrationId
JwtTokenSettings__DefaultHours
JwtTokenSettings__CoordinatorHours
```

Ejemplo:

```bash
export ConnectionStrings__PgSQL="Host=db.internal;Port=5432;Username=app_user;Password=CHANGE_ME;Database=gestion_turnos"
export jwtKey="CHANGE_ME_WITH_A_RANDOM_SECRET_OF_AT_LEAST_32_CHARACTERS"
export AllowedOrigins__0="https://app.example.com"
export AllowedOrigins__1="https://admin.example.com"
export EmailService__smtpServer="smtp.example.com"
export EmailService__smtpPort="587"
export EmailService__smtpUser="app@example.com"
export EmailService__smtpPassword="CHANGE_ME"
export EmailService__EnableSsl="true"
export Genearls__UrlForgot="https://app.example.com/auth"
export PythonApiSettings__BaseUrl="https://python-api.example.com"
export PythonApiSettings__FunctionName="agenda-worker"
export PythonApiSettings__Timeout="1520"
export Encryption__Key="BASE64_ENCRYPTION_KEY"
export Encryption__IV="BASE64_ENCRYPTION_IV"
export MigrationSettings__EnableAutoMigrate="false"
export MigrationSettings__BaselineMigrationId="20260111162601_AddAuditLogTable"
```

## Comportamiento de validacion al arranque

- `ConnectionStrings:PgSQL` y `jwtKey` son obligatorias en todos los ambientes.
- `AllowedOrigins` es obligatoria fuera de `Development`.
- `Encryption:Key` y `Encryption:IV` bloquean el arranque fuera de `Development` porque el cifrado se usa en la generacion y lectura de tokens QR.
- `Genearls:UrlForgot` bloquea el arranque fuera de `Development` porque se usa para links de recuperacion de password.
- `EmailService` y `PythonApiSettings` generan warnings cuando faltan por completo, y errores si quedan parcialmente configuradas fuera de `Development`.

## Checklist de despliegue

- Rotar el `jwtKey` heredado del proyecto anterior.
- Confirmar que no existen dominios ni endpoints del cliente anterior en `AllowedOrigins`, `Genearls` o `PythonApiSettings`.
- Cargar secretos desde el proveedor de despliegue y no desde archivos versionados.
- Verificar que `AllowedOrigins` tenga al menos un dominio valido en QA y Production.
- Mantener `MigrationSettings__EnableAutoMigrate=false` salvo despliegues controlados.
