# 🏢 Gandarias - Sistema de Gestión de Horarios y Personal

Sistema integral de gestión de horarios, fichajes y recursos humanos para Restaurante Gandarias.

## 📋 Tabla de Contenidos

- [Arquitectura del Sistema](#arquitectura-del-sistema)
- [Tecnologías](#tecnologías)
- [Requisitos Previos](#requisitos-previos)
- [Guía de Instalación Completa](#guía-de-instalación-completa)
- [Seguridad Implementada](#seguridad-implementada)
- [Esquema de Base de Datos](#esquema-de-base-de-datos)
- [Documentación de APIs](#documentación-de-apis)
- [Despliegue](#despliegue)
- [Variables de Entorno](#variables-de-entorno)

---

## 🏗️ Arquitectura del Sistema

### Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│                         FRONTEND                                 │
│                    (Angular/React)                               │
│                  http://gandarias.s3...                          │
└────────────────────────┬────────────────────────────────────────┘
                         │ HTTPS (443)
                         │ JWT Authentication
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    AWS App Runner / Docker                       │
│                         Port: 8080                               │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │              API GATEWAY (ASP.NET Core 8.0)               │  │
│  │  • JWT Authentication & Authorization                     │  │
│  │  • Swagger/OpenAPI Documentation                          │  │
│  │  • CORS Configuration                                     │  │
│  │  • Health Checks (/health)                                │  │
│  └────────────────────┬──────────────────────────────────────┘  │
│                       │                                          │
│  ┌────────────────────▼──────────────────────────────────────┐  │
│  │            CAPA DE PRESENTACIÓN (Controllers)             │  │
│  │  • UserController          • ScheduleController           │  │
│  │  • WorkAreaController      • SigningController            │  │
│  │  • WorkstationController   • AbsenteeismController        │  │
│  │  • ReportsController       • UserShiftController          │  │
│  └────────────────────┬──────────────────────────────────────┘  │
│                       │                                          │
│  ┌────────────────────▼──────────────────────────────────────┐  │
│  │        CAPA DE APLICACIÓN (CC.Application)                │  │
│  │  • Services (Business Logic)                              │  │
│  │  • AutoMapper (DTO ↔ Entity)                              │  │
│  │  • Validaciones de Negocio                                │  │
│  └────────────────────┬──────────────────────────────────────┘  │
│                       │                                          │
│  ┌────────────────────▼──────────────────────────────────────┐  │
│  │         CAPA DE DOMINIO (CC.Domain)                       │  │
│  │  • Entities (Modelos de Datos)                            │  │
│  │  • DTOs (Data Transfer Objects)                           │  │
│  │  • Interfaces (Contratos)                                 │  │
│  └────────────────────┬──────────────────────────────────────┘  │
│                       │                                          │
│  ┌────────────────────▼──────────────────────────────────────┐  │
│  │    CAPA DE INFRAESTRUCTURA (CC.Infrastructure)            │  │
│  │  • Repositories (Data Access)                             │  │
│  │  • EF Core DbContext                                      │  │
│  │  • Migrations                                             │  │
│  │  • Email Service (SMTP)                                   │  │
│  └────────────────────┬──────────────────────────────────────┘  │
└────────────────────────┼──────────────────────────────────────┘
                         │ Npgsql (Port: 5432)
                         │ SSL/TLS Connection
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│              AWS RDS PostgreSQL Database                         │
│         database-gandarias.ct6gmyi80fdr.eu-central-1            │
│                                                                  │
│  Schema: Management                                              │
│  • AspNetUsers (Identity)       • Schedules                     │
│  • AspNetRoles                  • Signings                      │
│  • WorkAreas                    • UserAbsenteeisms              │
│  • Workstations                 • EmployeeScheduleExceptions    │
│  • UserWorkstations             • LawRestrictions               │
│  • ShiftTypes                   • WorkstationDemands            │
└─────────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    SERVICIOS EXTERNOS                            │
│                                                                  │
│  • AWS Lambda (Python API - Algoritmo de Horarios)              │
│    https://63rzi23yidsam4lv2cmnhs3uuq0vpxkh.lambda-url...      │
│                                                                  │
│  • SMTP Server (Correos)                                        │
│    restaurantegandarias-com.correoseguro.dinaserver.com:587    │
└─────────────────────────────────────────────────────────────────┘
```

### Flujo de Datos

```
┌──────────┐    JWT Token    ┌─────────────┐   Validación  ┌──────────┐
│  Client  │ ──────────────> │   API       │ ────────────> │ Identity │
│ (Browser)│                 │ Controller  │               │ Service  │
└──────────┘                 └─────────────┘               └──────────┘
     │                              │                            │
     │        JSON Response         │        DTOs                │
     │ <────────────────────────────┤                            │
     │                              │                            │
     │                              ▼                            │
     │                       ┌─────────────┐                     │
     │                       │  Services   │                     │
     │                       │  (Business  │                     │
     │                       │   Logic)    │                     │
     │                       └─────────────┘                     │
     │                              │                            │
     │                              ▼                            │
     │                       ┌─────────────┐                     │
     │                       │ Repository  │                     │
     │                       │   Pattern   │                     │
     │                       └─────────────┘                     │
     │                              │                            │
     │                              ▼                            │
     │                       ┌─────────────┐                     │
     │                       │   EF Core   │                     │
     │                       │  DbContext  │                     │
     │                       └─────────────┘                     │
     │                              │                            │
     │                              ▼                            │
     │                       ┌─────────────┐                     │
     └───────────────────────│ PostgreSQL  │─────────────────────┘
                             │  Database   │
                             └─────────────┘
```

---

## 🛠️ Tecnologías

### Backend Stack
- **Framework**: ASP.NET Core 8.0
- **Lenguaje**: C# 12.0
- **ORM**: Entity Framework Core 8.0
- **Base de Datos**: PostgreSQL 15+ (AWS RDS)
- **Autenticación**: ASP.NET Core Identity + JWT
- **Documentación**: Swagger/OpenAPI
- **Logging**: Serilog

### Paquetes NuGet Principales
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="AutoMapper" Version="13.0.*" />
<PackageReference Include="Serilog" Version="4.0.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.*" />
```

### Arquitectura del Proyecto
- **Clean Architecture** / **Onion Architecture**
- **Repository Pattern**
- **Unit of Work Pattern**
- **Dependency Injection**
- **DTO Pattern**

### Infraestructura
- **Cloud Provider**: AWS
- **Compute**: AWS App Runner / Docker
- **Database**: AWS RDS PostgreSQL
- **Storage**: AWS S3 (Frontend estático)
- **Serverless**: AWS Lambda (Python - Algoritmos)
- **Email**: SMTP Server (Dinaserver)

---

## 📦 Requisitos Previos

### Software Requerido

| Software | Versión Mínima | Propósito |
|----------|----------------|-----------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0.x | Framework de desarrollo |
| [PostgreSQL](https://www.postgresql.org/download/) | 15.x | Base de datos |
| [Git](https://git-scm.com/) | 2.x | Control de versiones |
| [Docker](https://www.docker.com/) (Opcional) | 20.x | Contenedorización |

### IDEs Recomendados

- **[Visual Studio 2022](https://visualstudio.microsoft.com/)** - IDE completo (Recomendado)
  - Workloads: ASP.NET y desarrollo web, Desarrollo de .NET Desktop
- **[Visual Studio Code](https://code.visualstudio.com/)** - Editor ligero
  - Extensiones: C#, C# Dev Kit, Docker, PostgreSQL
- **[JetBrains Rider](https://www.jetbrains.com/rider/)** - IDE alternativo

### Herramientas Adicionales

- **Cliente PostgreSQL**: [pgAdmin 4](https://www.pgadmin.org/), [DBeaver](https://dbeaver.io/), o extensión VS Code
- **Cliente API**: [Postman](https://www.postman.com/) o [Insomnia](https://insomnia.rest/)
- **Cliente Git**: [Git Bash](https://git-scm.com/), [GitHub Desktop](https://desktop.github.com/), o integrado en IDE

### Requisitos del Sistema

**Mínimo:**
- **CPU**: 2 cores
- **RAM**: 4 GB
- **Disco**: 2 GB libres
- **OS**: Windows 10/11, macOS 10.15+, Linux (Ubuntu 20.04+)

**Recomendado:**
- **CPU**: 4+ cores
- **RAM**: 8 GB
- **Disco**: 10 GB libres (SSD)
- **OS**: Windows 11, macOS 12+, Ubuntu 22.04+

---

## 🚀 Guía de Instalación Completa

### Paso 1: Verificar Requisitos Previos

Antes de comenzar, verifica que todas las herramientas estén instaladas correctamente:

```bash
# Verificar .NET SDK
dotnet --version
# Debe mostrar: 8.0.x

# Verificar Git
git --version
# Debe mostrar: git version 2.x.x

# Verificar PostgreSQL
psql --version
# Debe mostrar: psql (PostgreSQL) 15.x

# Verificar Docker (Opcional)
docker --version
# Debe mostrar: Docker version 20.x.x
```

### Paso 2: Clonar el Repositorio

```bash
# Clonar desde GitHub
git clone https://github.com/diazbetancur/Gandarias-back.git

# Navegar al directorio del proyecto
cd Gandarias-back

# Verificar estructura del proyecto
ls -la
# Deberías ver: Api-Gandarias/, CC.Application/, CC.Domain/, CC.Infraestructure/
```

### Paso 3: Configurar PostgreSQL

#### Opción A: Instalación Local

1. **Crear base de datos:**

```bash
# Conectar a PostgreSQL
psql -U postgres

# Crear base de datos
CREATE DATABASE gandarias;

# Crear usuario (opcional)
CREATE USER gandarias_user WITH PASSWORD 'tu_password_seguro';
GRANT ALL PRIVILEGES ON DATABASE gandarias TO gandarias_user;

# Salir
\q
```

2. **Verificar conexión:**

```bash
psql -U postgres -d gandarias -c "SELECT version();"
```

#### Opción B: Docker (Recomendado para desarrollo)

```bash
# Crear contenedor PostgreSQL
docker run --name gandarias-postgres \
  -e POSTGRES_DB=gandarias \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=Gandarias123! \
  -p 5432:5432 \
  -v gandarias-data:/var/lib/postgresql/data \
  -d postgres:15

# Verificar que está corriendo
docker ps

# Ver logs
docker logs gandarias-postgres

# Conectar al contenedor
docker exec -it gandarias-postgres psql -U postgres -d gandarias
```

### Paso 4: Configurar Variables de Entorno

#### Windows (PowerShell):

```powershell
# Copiar archivo de configuración de ejemplo
Copy-Item Api-Gandarias\appsettings.json Api-Gandarias\appsettings.Development.json

# Editar con tu editor favorito
notepad Api-Gandarias\appsettings.Development.json
```

#### Linux/macOS:

```bash
# Copiar archivo de configuración
cp Api-Gandarias/appsettings.json Api-Gandarias/appsettings.Development.json

# Editar con tu editor favorito
nano Api-Gandarias/appsettings.Development.json
# o
code Api-Gandarias/appsettings.Development.json
```

**Configuración mínima requerida** (`appsettings.Development.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "PgSQL": "Host=localhost;Port=5432;Username=postgres;Password=TU_PASSWORD;Database=gandarias;SSL Mode=Disable"
  },
  "jwtKey": "GENERA_UNA_CLAVE_SECRETA_DE_AL_MENOS_32_CARACTERES_AQUI_MUY_SEGURA",
  "EmailService": {
    "smtpServer": "smtp.gmail.com",
    "smtpPort": "587",
    "smtpUser": "tu_email@gmail.com",
    "smtpPassword": "tu_app_password",
    "EnableSsl": true
  },
  "Genearls": {
    "UrlForgot": "http://localhost:4200/auth"
  },
  "PythonApiSettings": {
    "BaseUrl": "https://tu-lambda-url.amazonaws.com",
    "FunctionName": "gandarias-agenda-api",
    "Timeout": 1520
  },
  "Encryption": {
    "Key": "BASE64_KEY_32_BYTES",
    "IV": "BASE64_IV_16_BYTES"
  }
}
```

**Generar claves de encriptación:**

```bash
# En PowerShell o Bash
dotnet run --project Api-Gandarias -- generate-keys

# O manualmente (PowerShell):
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))  # Key
[Convert]::ToBase64String((1..16 | ForEach-Object { Get-Random -Maximum 256 }))  # IV

# O manualmente (Bash):
openssl rand -base64 32  # Key
openssl rand -base64 16  # IV
```

### Paso 5: Restaurar Dependencias

```bash
# Navegar al directorio raíz del proyecto
cd Gandarias-back

# Restaurar paquetes NuGet para todos los proyectos
dotnet restore

# Verificar que no hay errores
dotnet build

# Resultado esperado: Build succeeded. 0 Warning(s). 0 Error(s).
```

### Paso 6: Aplicar Migraciones de Base de Datos

```bash
# Opción 1: Usando dotnet CLI (Recomendado)
dotnet ef database update \
  --project CC.Infraestructure \
  --startup-project Api-Gandarias

# Opción 2: Usando Visual Studio Package Manager Console
# En VS 2022: Tools > NuGet Package Manager > Package Manager Console
Update-Database -Project CC.Infraestructure -StartupProject Api-Gandarias

# Verificar que las tablas se crearon correctamente
psql -U postgres -d gandarias -c "\dt \"Management\".*"
```

**Si hay problemas con las migraciones:**

```bash
# Ver lista de migraciones
dotnet ef migrations list --project CC.Infraestructure --startup-project Api-Gandarias

# Eliminar base de datos y recrear
dotnet ef database drop --project CC.Infraestructure --startup-project Api-Gandarias --force
dotnet ef database update --project CC.Infraestructure --startup-project Api-Gandarias
```

### Paso 7: Sembrar Datos Iniciales (Seed)

Los datos iniciales (roles, permisos, usuario admin) se cargan automáticamente al iniciar la aplicación por primera vez.

**Usuarios por defecto creados:**

| Usuario | Password | Rol | Email |
|---------|----------|-----|-------|
| Admin | `Gandarias1.` | Admin | - |
| Employee | `Gandarias1.` | Employee | - |
| Fichaje | `FichajeGandarias1.` | Coordinator | horarios@restaurantegandarias.com |

**⚠️ IMPORTANTE**: Cambiar estas contraseñas en producción.

### Paso 8: Compilar y Ejecutar

#### Opción A: Ejecutar con dotnet CLI

```bash
# Navegar al proyecto API
cd Api-Gandarias

# Ejecutar en modo Development
dotnet run

# O con watch (recarga automática)
dotnet watch run

# La aplicación estará disponible en:
# - HTTP: http://localhost:8080
# - Swagger: http://localhost:8080/swagger
```

#### Opción B: Ejecutar con Visual Studio

1. Abrir `Gandarias-back.sln` en Visual Studio 2022
2. Configurar `Api-Gandarias` como proyecto de inicio (clic derecho → Set as Startup Project)
3. Presionar **F5** o clic en el botón **▶ Start**
4. El navegador abrirá automáticamente Swagger UI

#### Opción C: Ejecutar con Visual Studio Code

```bash
# Abrir el proyecto en VS Code
code .

# Presionar F5 o ir a Run > Start Debugging
# Seleccionar ".NET Core" si se solicita
```

### Paso 9: Verificar Instalación

**1. Health Check:**

```bash
curl http://localhost:8080/health
# Respuesta esperada: Healthy
```

**2. Swagger UI:**

Abrir en navegador: `http://localhost:8080/swagger`

**3. Probar login:**

```bash
curl -X POST http://localhost:8080/api/User/login \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "Admin",
    "password": "Gandarias1."
  }'
```

Respuesta esperada:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2025-01-14T10:00:00Z"
}
```

**4. Verificar logs:**

Los logs se generan en `Api-Gandarias/logs/log-YYYY-MM-DD.txt`

```bash
# Ver últimas líneas del log
tail -f Api-Gandarias/logs/log-$(date +%Y-%m-%d).txt

# En Windows PowerShell:
Get-Content "Api-Gandarias\logs\log-$(Get-Date -Format 'yyyy-MM-dd').txt" -Tail 50 -Wait
```

### Paso 10: Configuración de HTTPS (Opcional para desarrollo)

```bash
# Generar certificado de desarrollo
dotnet dev-certs https --trust

# Ejecutar con HTTPS
dotnet run --urls="https://localhost:5001;http://localhost:8080"
```

### Solución de Problemas Comunes

#### Error: "Unable to resolve service for type 'DbContextOptions'"

**Solución:**
```bash
# Verificar que la cadena de conexión sea correcta
# Verificar que PostgreSQL esté corriendo
docker ps  # Si usas Docker
# o
sudo systemctl status postgresql  # Linux
# o
Get-Service postgresql*  # Windows PowerShell
```

#### Error: "JWT secret key not configured"

**Solución:**
```bash
# Asegurarse de que jwtKey tenga al menos 32 caracteres en appsettings.Development.json
# O configurar variable de entorno
export JWT_SECRET_KEY="tu_clave_de_al_menos_32_caracteres"  # Linux/macOS
$env:JWT_SECRET_KEY="tu_clave_de_al_menos_32_caracteres"    # Windows PowerShell
```

#### Error: "Failed executing DbCommand... relation does not exist"

**Solución:**
```bash
# Eliminar base de datos y recrear
dotnet ef database drop --project CC.Infraestructure --startup-project Api-Gandarias --force
dotnet ef database update --project CC.Infraestructure --startup-project Api-Gandarias
```

#### Error de compilación: "The type or namespace name 'X' could not be found"

**Solución:**
```bash
# Limpiar y reconstruir
dotnet clean
dotnet restore
dotnet build
```

---

## 🔐 Seguridad Implementada

El sistema implementa múltiples capas de seguridad para proteger datos sensibles y prevenir accesos no autorizados.

### 1. Autenticación

#### 1.1 ASP.NET Core Identity

El sistema utiliza **ASP.NET Core Identity** como framework de autenticación base:

```csharp
// Configuración en Program.cs
builder.Services.AddIdentity<User, Role>(opt =>
{
    opt.SignIn.RequireConfirmedEmail = false;
    opt.Password.RequiredLength = 8;
    opt.Password.RequireLowercase = true;
    opt.Password.RequireUppercase = true;
    opt.Password.RequireNonAlphanumeric = true;
    opt.Password.RequiredUniqueChars = 1;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
})
```

**Características:**
- ✅ Almacenamiento seguro de contraseñas con hash (PBKDF2)
- ✅ Bloqueo de cuenta después de intentos fallidos
- ✅ Tokens de recuperación de contraseña con expiración
- ✅ Validación de fortaleza de contraseñas

#### 1.2 JWT (JSON Web Tokens)

**Implementación:**

```
POST /api/User/login
{
  "userName": "Admin",
  "password": "Gandarias1."
}
```

**Respuesta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJMYXN0TmFtZSI6ImFkbWluIiwiVXNlck5hbWUiOiJBZG1pbiIsIlVzZXJJZCI6IjNmYTg1ZjY0LTU3MTctNDU2Mi1iM2ZjLTJjOTYzZjY2YWZhNiIsIkVtYWlsIjoiIiwiTmFtZSI6ImFkbWluIiwiUm9sZSI6IkFkbWluIiwiUGVybWlzc2lvbnMiOiJXb3Jrc3RhdGlvbixVc2VyLFVzZXJXb3Jrc3RhdGlvbixXb3JrQXJlYSxXb3JrU2NoZWR1bGUsU2hpZnRNYW5hZ2VtZW50LExpY2Vuc2UsLi4uIiwiZXhwIjoxNzM2ODUwMDAwLCJpc3MiOiJodHRwczovL2xvY2FsaG9zdDo4MDgwIiwiYXVkIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6ODA4MCJ9.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
  "expiration": "2025-01-14T10:00:00Z"
}
```

**Estructura del Token:**

```json
// Header
{
  "alg": "HS256",
  "typ": "JWT"
}

// Payload (Claims)
{
  "LastName": "admin",
  "UserName": "Admin",
  "UserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "Email": "",
  "Name": "admin",
  "Role": "Admin,Employee",
  "Permissions": "Workstation,User,WorkArea,...",
  "exp": 1736850000
}
```

**Configuración JWT:**

- **Algoritmo**: HMAC-SHA256
- **Clave mínima**: 32 caracteres
- **Expiración**: 
  - Usuarios normales: 1 hora
  - Coordinadores: 24 horas
- **Clock Skew**: 5 minutos (tolerancia de tiempo)
- **Validaciones**: Lifetime, IssuerSigningKey

**Uso del Token:**

```bash
# Todas las peticiones autenticadas deben incluir el header:
Authorization: Bearer {token}

# Ejemplo:
curl -X GET http://localhost:8080/api/User \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 2. Autorización Basada en Roles (RBAC)

#### 2.1 Roles del Sistema

El sistema define 3 roles principales:

| Rol | Descripción | Permisos |
|-----|-------------|----------|
| **Admin** | Administrador del sistema | Acceso total a todas las funcionalidades |
| **Coordinator** | Coordinador/Encargado | Gestión de fichajes, visualización de horarios |
| **Employee** | Empleado | Solo visualización de sus propios horarios |

**Enum de Roles:**

```csharp
public enum RoleType
{
    Admin,        // Gestión completa del sistema
    Employee,     // Acceso limitado (solo lectura)
    Coordinator   // Gestión de fichajes y reportes
}
```

#### 2.2 Permisos Granulares

El sistema utiliza un modelo de permisos granulares que permite control fino sobre las acciones:

```csharp
public enum Permissions
{
    Workstation,              // Gestión de puestos de trabajo
    User,                     // Gestión de usuarios
    UserWorkstation,          // Asignación usuario-puesto
    WorkArea,                 // Gestión de áreas de trabajo
    WorkSchedule,             // Gestión de horarios
    ShiftManagement,          // Gestión de turnos
    License,                  // Gestión de licencias
    HybridWorkstation,        // Puestos híbridos
    UserRestrictionShift,     // Restricciones de empleados
    AbsenteeismType,          // Tipos de ausencias
    UserAbsenteeism,          // Registro de ausencias
    EmployeeScheduleException,// Excepciones de horario
    LawRestriction,           // Restricciones legales
    WorkstationDemandTemplate // Plantillas de demanda
}
```

**Matriz de Roles y Permisos:**

| Permiso | Admin | Coordinator | Employee |
|---------|-------|-------------|----------|
| Workstation | ✅ | ❌ | ❌ |
| User | ✅ | ❌ | ❌ |
| WorkArea | ✅ | ❌ | ❌ |
| WorkSchedule | ✅ | ✅ (Lectura) | ✅ (Lectura propia) |
| UserAbsenteeism | ✅ | ❌ | ❌ |
| ShiftManagement | ✅ | ✅ | ❌ |
| LawRestriction | ✅ | ❌ | ❌ |

#### 2.3 Implementación en Controladores

```csharp
// Solo accesible por usuarios autenticados
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ScheduleController : ControllerBase
{
    // Solo Administradores pueden eliminar horarios de una semana completa
    [HttpDelete("{fechaIni}")]
    public async Task<IActionResult> DeleteAsync(DateOnly fechaIni)
    {
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden eliminar horarios.");
        }
        // ...
    }
}
```

**Verificación de roles en runtime:**

```csharp
// Obtener roles del usuario actual desde el token JWT
var roles = User.Claims
    .Where(c => c.Type == "Role")
    .Select(c => c.Value)
    .ToList();

// Verificar si tiene un rol específico
bool isAdmin = roles.Contains(RoleType.Admin.ToString());
```

### 3. Políticas de Contraseñas

#### 3.1 Requisitos de Contraseña

**Configuración obligatoria:**

- ✅ **Longitud mínima**: 8 caracteres
- ✅ **Al menos 1 letra minúscula** (a-z)
- ✅ **Al menos 1 letra mayúscula** (A-Z)
- ✅ **Al menos 1 dígito** (0-9)
- ✅ **Al menos 1 carácter especial** (!@#$%^&*...)
- ✅ **Caracteres únicos**: Mínimo 1

**Ejemplos:**
- ✅ Válida: `Gandarias1!`
- ✅ Válida: `MyP@ssw0rd`
- ❌ Inválida: `password` (falta mayúscula, dígito, especial)
- ❌ Inválida: `Pass123` (falta carácter especial)

#### 3.2 Generación Automática de Contraseñas

El sistema genera contraseñas seguras automáticamente al crear usuarios:

```csharp
// Ejemplo de contraseña generada: "Xk9$mP2wQ7@nL5r"
password = Extensions.GenerateRandomPassword();
```

#### 3.3 Recuperación de Contraseñas

**Flujo de recuperación:**

1. Usuario solicita reset: `POST /api/User/forgot-password`
2. Sistema genera token único con SHA256
3. Token se envía por email con URL de recuperación
4. Token expira en **1 hora**
5. Usuario establece nueva contraseña con token válido

```bash
# Solicitar recuperación
POST /api/User/forgot-password
{
  "email": "usuario@example.com"
}

# Reset con token
POST /api/User/reset-password
{
  "username": "12345678A",
  "token": "BASE64_ENCODED_TOKEN",
  "newPassword": "NuevaPassword123!"
}
```

**Seguridad del token:**
- Hash SHA256 del token aleatorio
- Codificación Base64URL
- Almacenado con hash en BD
- Expiración automática después de 1 hora
- Invalidado después de uso exitoso

### 4. Protección contra Ataques

#### 4.1 Bloqueo de Cuenta (Account Lockout)

**Configuración:**

```csharp
opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
opt.Lockout.MaxFailedAccessAttempts = 5; // Default ASP.NET Identity
```

**Funcionamiento:**
- Después de **5 intentos fallidos** de login
- Cuenta bloqueada por **30 minutos**
- Contador de intentos se resetea después del bloqueo

#### 4.2 Protección CSRF (Cross-Site Request Forgery)

JWT por header `Authorization` previene CSRF automáticamente (no usa cookies).

#### 4.3 Protección XSS (Cross-Site Scripting)

**Headers de seguridad configurados:**

```csharp
// En Program.cs (Producción)
context.Response.Headers.Add("Content-Security-Policy",
    "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:");
context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
context.Response.Headers.Add("X-Frame-Options", "DENY");
context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
context.Response.Headers.Add("Referrer-Policy", "no-referrer");
```

**Validaciones en entrada:**
- Validación de modelos con Data Annotations
- Sanitización automática por EF Core
- Uso de consultas parametrizadas (previene SQL Injection)

### 5. Seguridad en Transporte

#### 5.1 HTTPS/TLS

**Configuración:**

- En **desarrollo**: HTTP permitido en localhost
- En **producción**: HTTPS forzado (AWS App Runner termina TLS)

```csharp
// HTTPS redirection en desarrollo
if (!disableHttps && string.IsNullOrEmpty(portEnv) && app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

#### 5.2 Seguridad de Base de Datos

**Conexión PostgreSQL:**

```
// Desarrollo (sin SSL)
Host=localhost;Port=5432;Username=postgres;Password=***;Database=gandarias;SSL Mode=Disable

// Producción (con SSL)
Host=database-gandarias.ct6gmyi80fdr.eu-central-1.rds.amazonaws.com;Port=5432;Username=postgres;Password=***;Database=postgres;SSL Mode=Require;Trust Server Certificate=true
```

**Mejores prácticas implementadas:**
- ✅ Uso de variables de entorno para credenciales
- ✅ Conexión SSL/TLS en producción
- ✅ Usuario de BD con permisos limitados
- ✅ Parámetros de consulta (no concatenación SQL)
- ✅ Timeouts configurados (300 segundos)
- ✅ Retry logic con backoff exponencial

### 6. Logging y Auditoría

#### 6.1 Registro de Actividad

**Middleware de auditoría:**

```csharp
// ActivityLoggingMiddleware registra todas las acciones de usuarios
app.UseMiddleware<ActivityLoggingMiddleware>();
```

**Tabla: UserActivityLogs**

| Campo | Descripción |
|-------|-------------|
| UserId | Usuario que realizó la acción |
| Action | Endpoint/Acción ejecutada |
| IpAddress | Dirección IP del cliente |
| DateCreated | Timestamp de la acción |

**Ejemplo de log:**

```
[08:30:15 INF] User 3fa85f64 executed POST /api/Schedule from 192.168.1.100
```

#### 6.2 Logs de Sistema (Serilog)

**Configuración:**

```csharp
Logger logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 7)
    .CreateLogger();
```

**Niveles de log:**
- **Information**: Operaciones normales
- **Warning**: Situaciones sospechosas
- **Error**: Errores capturados
- **Fatal**: Errores críticos

**Logs almacenados en:**
- Consola (desarrollo)
- Archivos diarios en `logs/log-YYYY-MM-DD.txt`
- Rotación automática (últimos 7 días)

### 7. Buenas Prácticas de Seguridad

#### ✅ Implementadas

- [x] Autenticación JWT con tokens de corta duración
- [x] Contraseñas hasheadas con PBKDF2
- [x] Validación de entrada con Data Annotations
- [x] Protección contra SQL Injection (EF Core parametrizado)
- [x] Headers de seguridad HTTP
- [x] Logging de auditoría
- [x] Soft delete en lugar de eliminación física
- [x] Separación de entornos (Development/Production)
- [x] Gestión de secretos por variables de entorno
- [x] CORS configurado
- [x] Health checks habilitados

---

## 🗄️ Esquema de Base de Datos

### Diagrama Entidad-Relación (ERD)

```
┌─────────────────────┐       ┌──────────────────────┐
│   AspNetUsers       │       │   AspNetRoles        │
│   (Identity)        │       │                      │
├─────────────────────┤       ├──────────────────────┤
│ Id (PK)             │       │ Id (PK)              │
│ UserName            │       │ Name                 │
│ Email               │       │ NormalizedName       │
│ FirstName           │       │ IsActive             │
│ LastName            │       └──────────────────────┘
│ NickName            │              │
│ HireDate            │              │ AspNetUserRoles
│ HireTypeId (FK)     │──────────────┘
│ IsActive            │
│ IsDelete            │       ┌──────────────────────┐
│ HiredHours          │       │   HireTypes          │
│ ComplementHours     │       ├──────────────────────┤
│ LawApply            │       │ Id (PK)              │
│ ExtraHours          │       │ Name                 │
│ PasswordResetToken  │       │ IsActive             │
│ PasswordResetTokenExpiration |    │ DateCreated          │
└─────────────────────┘       └──────────────────────┘
         │                    └──────────────────────┘
         │                    ┌──────────────────────┐
         │                    │  UserRoles           │
         │                    ├──────────────────────┤
         │                    │ UserId (FK)          │
         │                    │ RoleId (FK)          │
         │                    └──────────────────────┘
         │
         ├─────────────────┐
         │                 │
         ▼                 ▼
┌─────────────────────┐  ┌──────────────────────┐
│  Schedules          │  │  UserAbsenteeisms    │
├─────────────────────┤  ├──────────────────────┤
│ Id (PK)             │  │ Id (PK)              │
│ Date                │  │ UserId (FK)          │
│ UserId (FK)         │  │ AbsenteeismTypeId    │
│ WorkstationId (FK)  │  │ StartDate (date)     │
│ StartTime (interval)│  │ EndDate (date)       │
│ EndTime (interval)  │  │ Observation          │
│ Observation         │  │ DateCreated          │
│ IsDeleted           │  └──────────────────────┘
│ DateCreated         │           │
└─────────────────────┘           │
         │                        ▼
         │              ┌──────────────────────┐
         │              │  AbsenteeismTypes    │
         │              ├──────────────────────┤
         │              │ Id (PK)              │
         │              │ Name                 │
         │              │ IsDeleted            │
         │              │ DateCreated          │
         │              └──────────────────────┘
         ▼                    ┌──────────────────────┐
┌─────────────────────┐       │  Signings            │
│   Workstations      │       ├──────────────────────┤
├─────────────────────┤       │ Id (PK)              │
│ Id (PK)             │       │ UserId (FK)          │
│ Name                │       │ Date (date)          │
│ WorkAreaId (FK)     │───────│ StartTime (time)     │
│ IsActive            │       │ EndTime (time)       │
│ IsDeleted           │       │ TipoFichaje (int)    │
│ MaxCapacity         │       │ Observaciones        │
│ IsHybrid            │       │ LastUpdateUserId     │
│ DateCreated         │       │ UpdatedAt            │
└─────────────────────┘       │ DateCreated          │
         │                    └──────────────────────┘
         │                        │
         │                    ┌──────────────────────┐
         ▼                    │ UserWorkstations     │
┌─────────────────────┐       ├──────────────────────┤
│    WorkAreas        │       │ Id (PK)              │
├─────────────────────┤       │ UserId (FK)          │
│ Id (PK)             │       │ WorkstationId (FK)   │
│ Name                │       │ Preference           │
│ IsActive            │       │ DateCreated          │
│ IsDeleted           │       └──────────────────────┘
│ Color               │       ┌──────────────────────┐
│ DateCreated         │       │ EmployeeSchedule     │
└─────────────────────┘       │ Restrictions         │
                              ├──────────────────────┤
┌─────────────────────┐       │ Id (PK)              │
│    ShiftTypes       │       │ UserId (FK)          │
├─────────────────────┤       │ DayOfWeek            │
│ Id (PK)             │       │ ShiftTypeId (FK)     │
│ Name                │       │ Preference           │
│ StartTime           │       │ IsDeleted            │
│ EndTime             │       │ DateCreated          │
│ Type                │       └──────────────────────┘
│ WeekType            │       ┌──────────────────────┐
│ IsDeleted           │       │ WorkstationDemands   │
│ DateCreated         │       ├──────────────────────┤
└─────────────────────┘       │ Id (PK)              │
                              │ Date (date)          │
                              │ WorkstationId (FK)   │
                              │ ShiftTypeId (FK)     │
                              │ RequiredCount        │
                              │ DateCreated          │
                              └──────────────────────┘
```

### Descripción de Tablas Principales

#### **AspNetUsers** (Schema: Management)
Tabla de usuarios extendida de ASP.NET Identity.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | uuid | Identificador único (PK) |
| UserName | varchar(256) | Nombre de usuario (DNI) |
| Email | varchar(256) | Correo electrónico |
| FirstName | varchar(20) | Nombre |
| LastName | varchar(20) | Apellido |
| NickName | text | Apodo/Nombre corto |
| HireDate | timestamp | Fecha de contratación |
| HireTypeId | uuid | Tipo de contrato (FK → HireTypes) |
| IsActive | boolean | Usuario activo |
| IsDelete | boolean | Usuario eliminado (soft delete) |
| HiredHours | integer | Horas contratadas |
| ComplementHours | boolean | Permite complementar horas |
| LawApply | boolean | Aplican restricciones legales |
| ExtraHours | boolean | Permite horas extra |
| PasswordResetToken | text | Token de recuperación |
| PasswordResetTokenExpiration | timestamp | Expiración del token |

#### **Schedules** (Horarios)
Almacena los horarios asignados a empleados.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | uuid | Identificador único (PK) |
| Date | date | Fecha del horario |
| UserId | uuid | Empleado (FK → AspNetUsers) |
| WorkstationId | uuid | Puesto de trabajo (FK → Workstations) |
| StartTime | interval | Hora de inicio |
| EndTime | interval | Hora de fin |
| Observation | text | Observaciones |
| IsDeleted | boolean | Eliminado lógicamente |
| IdUpdate | text | Usuario que actualizó |
| DateUpdate | timestamp | Fecha de actualización |
| DateCreated | timestamp | Fecha de creación |

#### **UserAbsenteeisms** (Ausencias)
Registra las ausencias de los empleados (vacaciones, bajas, etc.).

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | uuid | Identificador único (PK) |
| UserId | uuid | Empleado (FK → AspNetUsers) |
| AbsenteeismTypeId | uuid | Tipo de ausencia (FK) |
| StartDate | date | Fecha de inicio |
| EndDate | date | Fecha de fin (nullable) |
| Observation | text | Observaciones |
| DateCreated | timestamp | Fecha de creación |

#### **Signings** (Fichajes)
Registro de entradas y salidas de empleados.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | uuid | Identificador único (PK) |
| UserId | uuid | Empleado (FK → AspNetUsers) |
| Date | date | Fecha del fichaje |
| StartTime | time | Hora de entrada |
| EndTime | time | Hora de salida (nullable) |
| TipoFichaje | integer | Tipo: Manual/Automático |
| Observaciones | text | Observaciones |
| LastUpdateUserId | uuid | Último usuario que modificó |
| UpdatedAt | timestamp | Fecha de actualización |
| DateCreated | timestamp | Fecha de creación |

#### **Workstations** (Puestos de Trabajo)
Define los puestos de trabajo disponibles.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | uuid | Identificador único (PK) |
| Name | text | Nombre del puesto |
| WorkAreaId | uuid | Área de trabajo (FK → WorkAreas) |
| IsActive | boolean | Puesto activo |
| IsDeleted | boolean | Eliminado lógicamente |
| MaxCapacity | integer | Capacidad máxima |
| IsHybrid | boolean | Permite trabajo híbrido |
| DateCreated | timestamp | Fecha de creación |

#### **WorkAreas** (Áreas de Trabajo)
Agrupa puestos de trabajo por área.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | uuid | Identificador único (PK) |
| Name | text | Nombre del área |
| IsActive | boolean | Área activa |
| IsDeleted | boolean | Eliminado lógicamente |
| Color | text | Color para UI (#FFFFFF) |
| DateCreated | timestamp | Fecha de creación |

#### **WorkstationDemands** (Demanda de Puestos)
Define la demanda de personal por puesto y turno.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| Id | uuid | Identificador único (PK) |
| Date | date | Fecha |
| WorkstationId | uuid | Puesto (FK → Workstations) |
| ShiftTypeId | uuid | Turno (FK → ShiftTypes) |
| RequiredCount | integer | Cantidad requerida |
| DateCreated | timestamp | Fecha de creación |

### Scripts de Base de Datos

#### Script de Creación de Schema

```sql
-- Crear schema Management si no existe
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'Management') THEN
        CREATE SCHEMA "Management";
    END IF;
END $EF$;

-- Habilitar extensión para UUID
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Función para generar UUID v4
CREATE OR REPLACE FUNCTION gen_random_uuid()
RETURNS uuid
AS 'SELECT uuid_generate_v4()'
LANGUAGE SQL;
```

#### Script para Ver Todas las Tablas

```sql
-- Ver todas las tablas en el schema Management
SELECT 
    table_name,
    (SELECT COUNT(*) FROM information_schema.columns 
     WHERE table_schema = 'Management' 
     AND table_name = t.table_name) as column_count
FROM information_schema.tables t
WHERE table_schema = 'Management'
ORDER BY table_name;

-- Ver estructura de una tabla específica
SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'Management'
  AND table_name = 'AspNetUsers'
ORDER BY ordinal_position;
```

#### Script de Datos de Ejemplo

```sql
-- Insertar tipo de contrato
INSERT INTO "Management"."HireTypes" ("Id", "Name", "IsActive", "DateCreated")
VALUES 
    (gen_random_uuid(), 'Tiempo Completo', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Medio Tiempo', true, CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Por Horas', true, CURRENT_TIMESTAMP);

-- Insertar área de trabajo
INSERT INTO "Management"."WorkAreas" ("Id", "Name", "IsActive", "IsDeleted", "Color", "DateCreated")
VALUES 
    (gen_random_uuid(), 'Cocina', true, false, '#FF5733', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Salón', true, false, '#33FF57', CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Barra', true, false, '#3357FF', CURRENT_TIMESTAMP);

-- Insertar tipos de ausencia
INSERT INTO "Management"."AbsenteeismTypes" ("Id", "Name", "IsDeleted", "DateCreated")
VALUES 
    (gen_random_uuid(), 'Vacaciones', false, CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Baja Médica', false, CURRENT_TIMESTAMP),
    (gen_random_uuid(), 'Permiso Personal', false, CURRENT_TIMESTAMP);
```

---

## 📚 Documentación de APIs

### Base URL
- **Desarrollo**: `http://localhost:8080/api`
- **Producción**: `https://tu-dominio.com/api`

### Autenticación

Todas las APIs (excepto `/auth/login` y `/health`) requieren autenticación JWT.

**Header requerido:**
```
Authorization: Bearer {jwt_token}
```

### Endpoints Principales

---

#### **🔐 Authentication**

##### POST `/api/User/login`
Inicia sesión y obtiene un token JWT.

**Request:**
```json
{
  "userName": "12345678A",
  "password": "Password123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2025-01-14T10:00:00Z",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "usuario@example.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "nickName": "Juanito",
    "rolName": "Employee"
  }
}
```

**Errores:**
- `400 Bad Request`: Credenciales inválidas
- `401 Unauthorized`: Usuario bloqueado o inactivo

---

##### POST `/api/User/register`
Registra un nuevo usuario (Solo Admin).

**Request:**
```json
{
  "dni": "12345678A",
  "email": "nuevo@example.com",
  "password": "Password123!",
  "firstName": "María",
  "lastName": "García",
  "nickName": "Mari",
  "phoneNumber": "666123456",
  "hireDate": "2025-01-01",
  "hireTypeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "hiredHours": 40,
  "rolName": "Employee"
}
```

**Response (200 OK):**
```json
{
  "id": "new-user-uuid",
  "message": "Usuario creado exitosamente"
}
```

---

##### POST `/api/User/forgot-password`
Solicita recuperación de contraseña.

**Request:**
```json
{
  "email": "usuario@example.com"
}
```

**Response (200 OK):**
```json
{
  "message": "Email enviado con instrucciones para restablecer la contraseña"
}
```

---

#### **👥 Users (Usuarios)**

##### GET `/api/User`
Obtiene todos los usuarios activos.

**Query Parameters:**
- `includeInactive` (bool): Incluir usuarios inactivos

**Response (200 OK):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "dni": "12345678A",
    "email": "usuario@example.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "nickName": "Juanito",
    "phoneNumber": "666123456",
    "isActive": true,
    "hireDate": "2020-01-15",
    "hireTypeId": "type-uuid",
    "hireTypeName": "Tiempo Completo",
    "hiredHours": 40,
    "rolName": "Employee"
  }
]
```

---

##### GET `/api/User/{id}`
Obtiene un usuario por ID.

**Response (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "dni": "12345678A",
  "email": "usuario@example.com",
  "firstName": "Juan",
  "lastName": "Pérez",
  "nickName": "Juanito",
  "phoneNumber": "666123456",
  "isActive": true,
  "hireDate": "2020-01-15",
  "hireTypeId": "type-uuid",
  "hiredHours": 40,
  "complementHours": true,
  "lawApply": false,
  "extraHours": true,
  "rolName": "Employee"
}
```

**Errores:**
- `404 Not Found`: Usuario no existe

---

##### PUT `/api/User/{id}`
Actualiza un usuario existente.

**Request:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "nuevo-email@example.com",
  "firstName": "Juan Carlos",
  "lastName": "Pérez",
  "phoneNumber": "666999888",
  "hiredHours": 38,
  "isActive": true
}
```

**Response (200 OK):**
```json
{
  "message": "Usuario actualizado exitosamente"
}
```

---

##### DELETE `/api/User`
Elimina un usuario (soft delete).

**Request:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response (200 OK):**
```json
{
  "message": "Usuario eliminado exitosamente"
}
```

---

#### **📅 Schedules (Horarios)**

##### GET `/api/Schedule`
Obtiene todos los horarios.

**Query Parameters:**
- `startDate` (date): Fecha inicio (formato: YYYY-MM-DD)
- `endDate` (date): Fecha fin
- `userId` (uuid): Filtrar por usuario

**Response (200 OK):**
```json
[
  {
    "id": "schedule-uuid",
    "date": "2025-01-15",
    "userId": "user-uuid",
    "userFullName": "Juan Pérez",
    "workstationId": "workstation-uuid",
    "workstationName": "Barra Principal",
    "workAreaName": "Barra",
    "startTime": "09:00:00",
    "endTime": "17:00:00",
    "observation": "Turno de mañana",
    "isDeleted": false
  }
]
```

---

##### POST `/api/Schedule`
Crea un nuevo horario.

**Request:**
```json
{
  "date": "2025-01-20",
  "userId": "user-uuid",
  "workstationId": "workstation-uuid",
  "startTime": "09:00:00",
  "endTime": "17:00:00",
  "observation": "Turno matinal"
}
```

**Response (200 OK):**
```json
{
  "id": "new-schedule-uuid",
  "message": "Horario creado exitosamente"
}
```

**Errores:**
- `400 Bad Request`: "La hora de inicio debe ser menor que la hora de fin"
- `400 Bad Request`: "Ya existe un horario para este usuario en ese rango de tiempo"

---

##### PUT `/api/Schedule/{id}`
Actualiza un horario existente.

**Request:**
```json
{
  "id": "schedule-uuid",
  "date": "2025-01-20",
  "userId": "user-uuid",
  "workstationId": "workstation-uuid",
  "startTime": "10:00:00",
  "endTime": "18:00:00",
  "observation": "Horario modificado"
}
```

**Response (200 OK):**
```json
{
  "message": "Horario actualizado exitosamente"
}
```

---

##### DELETE `/api/Schedule`
Elimina un horario (soft delete).

**Request:**
```json
{
  "id": "schedule-uuid"
}
```

**Response (200 OK):**
```json
{
  "message": "Horario eliminado exitosamente"
}
```

---

##### DELETE `/api/Schedule/{fechaIni}`
Elimina todos los horarios de una semana (Solo Admin).

**Response (200 OK):**
```json
{
  "message": "Horario eliminado correctamente."
}
```

**Errores:**
- `401 Unauthorized`: "Solo los administradores pueden eliminar horarios"
- `400 Bad Request`: "No hay horarios para eliminar en el rango de fechas especificado"

---

#### **🏢 Work Areas (Áreas de Trabajo)**

##### GET `/api/WorkArea`
Obtiene todas las áreas de trabajo.

**Response (200 OK):**
```json
[
  {
    "id": "area-uuid",
    "name": "Cocina",
    "isActive": true,
    "isDeleted": false,
    "color": "#FF5733"
  }
]
```

---

##### POST `/api/WorkArea`
Crea una nueva área de trabajo.

**Request:**
```json
{
  "name": "Terraza",
  "isActive": true,
  "color": "#33FF99"
}
```

**Response (200 OK):**
```json
{
  "id": "new-area-uuid",
  "message": "Área creada exitosamente"
}
```

---

##### PUT `/api/WorkArea/{id}`
Actualiza un área de trabajo.

**Request:**
```json
{
  "id": "area-uuid",
  "name": "Cocina Principal",
  "isActive": true,
  "color": "#FF6633"
}
```

---

##### DELETE `/api/WorkArea`
Elimina un área y todos sus puestos asociados.

**Request:**
```json
{
  "id": "area-uuid"
}
```

---

#### **🪑 Workstations (Puestos de Trabajo)**

##### GET `/api/Workstation`
Obtiene todos los puestos de trabajo.

**Response (200 OK):**
```json
[
  {
    "id": "workstation-uuid",
    "name": "Barra Principal",
    "workAreaId": "area-uuid",
    "workAreaName": "Barra",
    "isActive": true,
    "isDeleted": false,
    "maxCapacity": 2,
    "isHybrid": false
  }
]
```

---

##### POST `/api/Workstation`
Crea un nuevo puesto de trabajo.

**Request:**
```json
{
  "name": "Mesa VIP",
  "workAreaId": "area-uuid",
  "isActive": true,
  "maxCapacity": 1,
  "isHybrid": false
}
```

---

##### PUT `/api/Workstation/{id}`
Actualiza un puesto de trabajo.

---

##### DELETE `/api/Workstation`
Elimina un puesto de trabajo (soft delete).

---

#### **🏥 User Absenteeisms (Ausencias)**

##### GET `/api/UserAbsenteeism`
Obtiene todas las ausencias activas (últimos 365 días).

**Response (200 OK):**
```json
[
  {
    "id": "absenteeism-uuid",
    "userId": "user-uuid",
    "userFullName": "Juan Pérez",
    "absenteeismTypeId": "type-uuid",
    "absenteeismTypeName": "Vacaciones",
    "startDate": "2025-01-20",
    "endDate": "2025-01-27",
    "observation": "Vacaciones de invierno"
  }
]
```

---

##### GET `/api/UserAbsenteeism/{id}`
Obtiene una ausencia específica.

---

##### GET `/api/UserAbsenteeism/GetByUserId/{id}`
Obtiene todas las ausencias de un usuario.

---

##### POST `/api/UserAbsenteeism`
Registra una nueva ausencia.

**Request:**
```json
{
  "userId": "user-uuid",
  "absenteeismTypeId": "type-uuid",
  "startDate": "2025-02-01",
  "endDate": "2025-02-05",
  "observation": "Vacaciones"
}
```

**Response (200 OK):**
```json
{
  "id": "new-absenteeism-uuid",
  "message": "Ausencia registrada exitosamente"
}
```

**Errores:**
- `400 Bad Request`: "Ya existe una novedad para el periodo seleccionado"

---

##### PUT `/api/UserAbsenteeism/{id}`
Actualiza una ausencia.

---

##### DELETE `/api/UserAbsenteeism`
Elimina una ausencia.

---

#### **⏰ Signings (Fichajes)**

##### GET `/api/Signing`
Obtiene todos los fichajes.

**Query Parameters:**
- `startDate` (date): Fecha inicio
- `endDate` (date): Fecha fin
- `userId` (uuid): Filtrar por usuario

**Response (200 OK):**
```json
[
  {
    "id": "signing-uuid",
    "userId": "user-uuid",
    "userFullName": "Juan Pérez",
    "date": "2025-01-15",
    "startTime": "09:05:00",
    "endTime": "17:10:00",
    "tipoFichaje": 1,
    "observaciones": "Fichaje automático",
    "lastUpdateUserId": "admin-uuid",
    "updatedAt": "2025-01-15T17:15:00Z"
  }
]
```

**Tipos de Fichaje:**
- `0`: Manual
- `1`: Automático

---

##### POST `/api/Signing`
Registra un nuevo fichaje.

**Request:**
```json
{
  "userId": "user-uuid",
  "date": "2025-01-15",
  "startTime": "09:00:00",
  "endTime": "17:00:00",
  "tipoFichaje": 1,
  "observaciones": ""
}
```

---

##### PUT `/api/Signing/{id}`
Actualiza un fichaje (Solo Admin).

---

##### DELETE `/api/Signing`
Elimina un fichaje.

---

#### **📊 Reports (Reportes)**

##### GET `/api/Reports/schedule-summary`
Obtiene resumen de horarios por período.

**Query Parameters:**
- `startDate` (date): Fecha inicio
- `endDate` (date): Fecha fin

**Response (200 OK):**
```json
{
  "totalSchedules": 150,
  "totalUsers": 20,
  "totalHours": 1200,
  "schedulesByWorkArea": [
    {
      "workAreaName": "Cocina",
      "count": 50,
      "totalHours": 400
    }
  ]
}
```

---

### Códigos de Estado HTTP

| Código | Descripción |
|--------|-------------|
| 200 | OK - Solicitud exitosa |
| 201 | Created - Recurso creado |
| 400 | Bad Request - Datos inválidos |
| 401 | Unauthorized - No autenticado |
| 403 | Forbidden - Sin permisos |
| 404 | Not Found - Recurso no encontrado |
| 409 | Conflict - Conflicto de datos |
| 500 | Internal Server Error - Error del servidor |

### Swagger/OpenAPI

La documentación interactiva está disponible en:
- **Desarrollo**: `http://localhost:8080/swagger`
- **Producción**: `https://tu-dominio.com/swagger`

---

## 🚢 Despliegue

### Docker

```bash
# Build
docker build -t gandarias-api:latest .

# Run
docker run -d \
  -p 8080:8080 \
  -e DATABASE_URL="Host=..." \
  -e JWT_SECRET_KEY="tu_clave_secreta" \
  --name gandarias-api \
  gandarias-api:latest
```

### AWS App Runner

1. **Push imagen a ECR:**
```bash
aws ecr get-login-password --region eu-central-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.eu-central-1.amazonaws.com
docker tag gandarias-api:latest <account-id>.dkr.ecr.eu-central-1.amazonaws.com/gandarias-api:latest
docker push <account-id>.dkr.ecr.eu-central-1.amazonaws.com/gandarias-api:latest
```

2. **Configurar App Runner:**
   - Source: ECR
   - Port: 8080
   - Environment variables: Ver sección siguiente
   - Health check: `/health`

---

## 🔐 Variables de Entorno

### Requeridas

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `DATABASE_URL` | Connection string PostgreSQL | `Host=...;Port=5432;Username=...` |
| `JWT_SECRET_KEY` | Clave secreta JWT (min 32 chars) | `tu_clave_muy_segura_de_32_caracteres` |

### Opcionales

| Variable | Descripción | Default |
|----------|-------------|---------|
| `PORT` | Puerto HTTP | `8080` |
| `ASPNETCORE_ENVIRONMENT` | Entorno | `Production` |
| `DISABLE_HTTPS_REDIRECT` | Desactivar redirección HTTPS | `false` |
| `SMTP_SERVER` | Servidor SMTP | - |
| `SMTP_PORT` | Puerto SMTP | `587` |
| `SMTP_USER` | Usuario SMTP | - |
| `SMTP_PASSWORD` | Password SMTP | - |
| `SMTP_ENABLE_SSL` | Habilitar SSL en SMTP | `true` |

---

## 📝 Migraciones de Base de Datos

### Crear nueva migración

```bash
dotnet ef migrations add NombreDeLaMigracion \
  --project CC.Infraestructure \
  --startup-project Api-Gandarias
```

### Aplicar migraciones

```bash
dotnet ef database update \
  --project CC.Infraestructure \
  --startup-project Api-Gandarias
```

### Revertir migración

```bash
dotnet ef database update NombreMigracionAnterior \
  --project CC.Infraestructure \
  --startup-project Api-Gandarias
```

### Eliminar última migración

```bash
dotnet ef migrations remove \
  --project CC.Infraestructure \
  --startup-project Api-Gandarias
```
Este proyecto es propiedad de Restaurante Gandarias.

---

## 👥 Contacto

- **GitHub**: [diazbetancur/Gandarias-back](https://github.com/diazbetancur/Gandarias-back)
---