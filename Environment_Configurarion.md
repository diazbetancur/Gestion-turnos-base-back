## Environment Configuration

### Variables de entorno

# Estas variables de entorno se pueden configurar en el entorno de producción para sobreescribir los valores del archivo appsettings.Production.json

# Ejemplo de configuración de variables de entorno:

# dotnet user-secrets set "ConnectionStrings:PgSQL" "Host=database;Port=5432;Username=postgres;Password=MyStrongPassword!123\_;Database=dataBase;Include Error Detail=true; SSL Mode=Require;Trust Server Certificate=true"

"ConnectionStrings:PgSQL"="..."

"jwtKey"="..."

"EmailService:smtpServer"="..."
"EmailService:smtpPort"="587"
"EmailService:smtpUser"="..."
"EmailService:smtpPassword"="..."
"EmailService:EnableSsl"="true"

"Genearls:UrlForgot"="https://nuevo-restaurante.com/auth"

"PythonApiSettings:BaseUrl"="..."
"PythonApiSettings:FunctionName"="..."
"PythonApiSettings:Timeout"="1520"

"Encryption:Key"="..."
"Encryption:IV"="..."

"MigrationSettings:EnableAutoMigrate"="false"
