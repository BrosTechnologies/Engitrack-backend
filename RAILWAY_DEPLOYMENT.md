# Railway Deployment Instructions

## Archivos creados para el deployment:

1. **Dockerfile** - Containerización de la aplicación .NET 9.0
2. **railway.toml** - Configuración específica de Railway
3. **.dockerignore** - Optimización del build
4. **start.sh** - Script de inicio alternativo

## Variables de entorno requeridas en Railway:

Configura estas variables en el dashboard de Railway:

### Base de datos:
```
ConnectionStrings__SqlServer=Server=tcp:tu-server.database.windows.net,1433;Database=Engitrack;User ID=tu-usuario;Password=tu-password;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True;Connection Timeout=30;
```

### JWT Configuration:
```
Jwt__Key=TU_SUPER_SECRET_KEY_DE_AL_MENOS_32_CARACTERES_AQUI
Jwt__Issuer=Engitrack
Jwt__Audience=Engitrack.Client
```

### Otras variables:
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

## Pasos para deployar:

1. **Configura las variables de entorno** en Railway dashboard
2. **Conecta tu repositorio** a Railway
3. **Selecciona la rama** que quieres deployar
4. **Railway detectará automáticamente** el Dockerfile y empezará el build

## Endpoints importantes:

- **Health Check**: `/api/health` - Usado por Railway para verificar el estado de la app
- **API Base**: `/api/` - Todos tus endpoints de la API
- **Swagger** (solo en Development): Root path `/`

## Troubleshooting:

### Si el build falla:
- Verifica que todas las dependencias estén en el .csproj
- Revisa los logs de build en Railway
- Asegúrate de que el puerto esté correctamente configurado

### Si la app no arranca:
- Verifica las variables de entorno
- Revisa que la connection string sea válida
- Verifica los logs de la aplicación en Railway

### Si la base de datos no conecta:
- Asegúrate de que el servidor SQL Server permita conexiones desde Railway
- Verifica la connection string
- Configura el firewall de Azure SQL (si usas Azure) para permitir servicios de Azure

## Configuración de CORS:

La aplicación ya está configurada para permitir cualquier origen en desarrollo. Para producción, considera configurar orígenes específicos por seguridad.

## Notas importantes:

- La aplicación usa .NET 9.0
- El puerto se configura automáticamente desde la variable `$PORT` de Railway
- El health check está en `/api/health`
- Swagger solo está disponible en Development environment