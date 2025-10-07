# Engitrack - Sistema de Gestión de Proyectos de Construcción

Backend completo desarrollado con .NET 9, DDD + CQRS, EF Core y Azure SQL.

## 🚀 Tecnologías

- **.NET 9** con Minimal APIs
- **Entity Framework Core 9** (Code-First)
- **Azure SQL Database**
- **MediatR** para CQRS
- **FluentValidation** para validaciones
- **JWT Authentication** con BCrypt para hashing
- **Swagger/OpenAPI** con Bearer token
- **Arquitectura DDD** con Bounded Contexts

## 📁 Estructura del Proyecto

```
Engitrack.sln
└── src/
    ├── BuildingBlocks/          # Clases base compartidas
    ├── Projects/                # Contexto de Proyectos
    │   ├── Domain/             # Entidades User, Project, ProjectTask
    │   ├── Application/        # Commands/Queries + Validaciones
    │   └── Infrastructure/     # EF DbContext + Repositorios
    ├── Inventory/              # Contexto de Inventario
    ├── Workers/                # Contexto de Trabajadores
    ├── Incidents/              # Contexto de Incidentes
    ├── Machinery/              # Contexto de Maquinaria
    └── Api/                    # Host único con Minimal APIs
```

## 🎯 Bounded Contexts

### Projects (Implementado completo)
- **User**: email, nombre, teléfono, rol (SUPERVISOR/CONTRACTOR/USER)
- **Project**: nombre, fechas, presupuesto, estado, propietario
- **ProjectTask**: título, estado (PENDING/IN_PROGRESS/DONE), fecha límite
- **Regla**: No se puede completar un proyecto con tareas abiertas

### Inventory (Dominio implementado)
- **Material**: stock, nivel mínimo, unidades
- **Transaction**: ENTRY/USAGE/ADJUSTMENT con SP atómico
- **Supplier**: información de proveedores
- **Regla**: Stock nunca negativo

### Workers (Dominio implementado)
- **Worker**: datos personales, tarifa por hora
- **Assignment**: asignación worker-proyecto
- **Attendance**: asistencia diaria con check-in/out
- **Regla**: Unicidad (WorkerId, ProjectId, Day)

### Incidents (Dominio implementado)
- **Incident**: severidad (LOW/MEDIUM/HIGH/CRITICAL), estado
- **Attachment**: archivos adjuntos
- **Regla**: HIGH/CRITICAL disparan IntegrationEvents

### Machinery (Dominio implementado)
- **Machine**: número serie único, estado operacional
- **MachineAssignment**: asignación máquina-proyecto
- **UsageLog**: registro de horas de uso
- **Regla**: No registrar uso si está UNDER_MAINTENANCE

## 🛠️ Configuración e Instalación

### 1. Requisitos Previos
- .NET 9 SDK
- Azure SQL Database
- Visual Studio Code o Visual Studio 2022

### 2. Configurar Base de Datos

Edita `src/Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=tcp:TU-SERVIDOR.database.windows.net,1433;Database=Engitrack;User ID=sqladmin;Password=TU-CLAVE;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Connection Timeout=30;"
  }
}
```

### 3. Ejecutar Migraciones

```bash
# Navegar al directorio del proyecto
cd "d:\APP MOVIL\BACKEND"

# Aplicar migraciones a la base de datos
dotnet ef database update --project src\Projects\Engitrack.Projects.Infrastructure --startup-project src\Api --context ProjectsDbContext
```

### 4. Compilar y Ejecutar

```bash
# Compilar toda la solución
dotnet build

# Ejecutar la API
dotnet run --project src\Api
```

### 5. Acceder a la Aplicación

- **API**: https://localhost:7XXX (puerto asignado automáticamente)
- **Swagger UI**: https://localhost:7XXX/ (raíz del sitio)
- **Health Check**: https://localhost:7XXX/api/health

### 6. Autenticación

1. **Registrar usuario**:
   ```bash
   POST /auth/register
   {
     "email": "admin@engitrack.com",
     "fullName": "Administrator",
     "password": "SecurePass123!",
     "role": "SUPERVISOR"
   }
   ```

2. **Obtener token**:
   ```bash
   POST /auth/login
   {
     "email": "admin@engitrack.com",
     "password": "SecurePass123!"
   }
   ```

3. **Usar token en requests**:
   - Agregar header: `Authorization: Bearer <token>`
   - En Swagger: Usar el botón "Authorize" y pegar el token

## 📚 API Endpoints

### Sistema
- `GET /api/health` - Estado del sistema

### Autenticación
- `POST /auth/register` - Registro de nuevo usuario
- `POST /auth/login` - Login y obtención de JWT token

### Proyectos (Requieren autenticación JWT)
- `GET /api/projects` - Listar proyectos del usuario (con filtros y paginación)
- `GET /api/projects/{id}` - Obtener proyecto específico
- `POST /api/projects` - Crear nuevo proyecto
- `POST /api/projects/{id}/tasks` - Crear nueva tarea en proyecto
- `PATCH /api/projects/{id}/tasks/{taskId}/status` - Actualizar estado de tarea
- `DELETE /api/projects/{id}/tasks/{taskId}` - Eliminar tarea
- `PATCH /api/projects/{id}/complete` - Completar proyecto
- `PATCH /api/projects/{id}` - Actualizar información del proyecto

### Ejemplo de Payloads

#### Registro de Usuario
```json
{
  "email": "admin@engitrack.com",
  "fullName": "Administrator",
  "password": "SecurePass123!",
  "role": "SUPERVISOR"
}
```

#### Login
```json
{
  "email": "admin@engitrack.com",
  "password": "SecurePass123!"
}
```

#### Crear Proyecto
```json
{
  "name": "Construcción Edificio A",
  "startDate": "2025-01-15",
  "endDate": "2025-12-15",
  "budget": 150000.00,
  "tasks": [
    {
      "title": "Preparación del terreno",
      "dueDate": "2025-02-01"
    },
    {
      "title": "Cimentación",
      "dueDate": "2025-03-15"
    }
  ]
}
```

## 🏗️ Arquitectura

### Patrones Implementados
- **Domain-Driven Design (DDD)**
- **Command Query Responsibility Segregation (CQRS)**
- **Repository Pattern**
- **Unit of Work** (via EF DbContext)
- **Domain Events** (base implementada)

### Principios SOLID
- **Single Responsibility**: Cada clase tiene una responsabilidad específica
- **Open/Closed**: Extensible via interfaces y abstracciones
- **Liskov Substitution**: Herencia coherente con clases base
- **Interface Segregation**: Interfaces específicas por contexto
- **Dependency Inversion**: Dependencias hacia abstracciones

### Características Técnicas
- **Schemas por contexto**: `projects`, `inventory`, `workers`, `incidents`, `machinery`
- **Índices optimizados** para consultas frecuentes
- **Validaciones de dominio** en entidades
- **Conversión de enums** a string en BD
- **Nullable reference types** habilitado
- **Migraciones automáticas** con EF Core

## 🔧 Próximos Pasos

Para completar el sistema, implementar:

1. **Application Layer completo**: Commands/Queries con MediatR
2. **Validaciones FluentValidation** para todos los contextos
3. **Stored Procedure** `inventory.usp_RegisterTransaction`
4. **DbContexts adicionales** para todos los bounded contexts
5. **Repositorios** para todos los contextos
6. **Endpoints Minimal API** completos
7. **Seeds de datos** para testing
8. **Logging con Serilog**
9. **Manejo de errores** con ProblemDetails
10. **Autenticación/Autorización**

## 🔍 Reglas de Negocio Implementadas

### Projects
- ✅ Email único por usuario
- ✅ Validaciones de longitud en strings
- ✅ No completar proyecto con tareas abiertas
- ✅ Estados válidos de proyecto y tarea

### Inventory
- ✅ Stock nunca negativo
- ✅ Transacciones solo con cantidad > 0
- ✅ Material vinculado a proyecto específico

### Workers
- ✅ Unicidad de asistencia por día
- ✅ CheckOut debe ser después de CheckIn
- ✅ Validaciones de fechas en asignaciones

### Incidents
- ✅ Incidentes HIGH/CRITICAL generan domain events
- ✅ Solo se pueden cerrar incidentes resueltos
- ✅ Validación de estados de transición

### Machinery
- ✅ Número de serie único
- ✅ No usar máquina en mantenimiento
- ✅ Validación de horas >= 0

## 📝 Comandos Útiles

```bash
# Compilar
dotnet build

# Ejecutar
dotnet run --project src\Api

# Crear migración
dotnet ef migrations add MigrationName --project src\Projects\Engitrack.Projects.Infrastructure --startup-project src\Api

# Aplicar migraciones
dotnet ef database update --project src\Projects\Engitrack.Projects.Infrastructure --startup-project src\Api

# Ver migraciones pendientes
dotnet ef migrations list --project src\Projects\Engitrack.Projects.Infrastructure --startup-project src\Api
```

## 🤝 Contribución

El proyecto está estructurado para facilitar el desarrollo colaborativo:
- Cada bounded context es independiente
- Interfaces bien definidas
- Separación clara de responsabilidades
- Extensible para nuevos contextos

---
