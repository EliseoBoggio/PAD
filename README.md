# Municipalidad.Api

Solución de referencia para la municipalidad con ASP.NET Core Web API.

## Contenido

- `Municipalidad.Api/`: proyecto principal con la API.
- `Municipalidad.Tests/`: pruebas unitarias básicas.
- `Municipalidad.Api/docs/`: documentación de consumo adicional.
- `Municipalidad.Api/Scripts/`: scripts de semillas SQL.

## Requisitos

- .NET 7 SDK
- SQLite

## Configuración

1. Ajuste los valores de `appsettings.json` con las URLs y credenciales reales de los servicios externos (DNRPA, ARCA, Pago Fácil, MercadoPago) y la clave JWT.
2. Ejecute las migraciones y semillas:

   ```bash
   dotnet tool restore
   dotnet ef database update --project Municipalidad.Api/Municipalidad.Api.csproj
   sqlite3 municipalidad.db < Municipalidad.Api/Scripts/seed.sql
   ```

3. Inicie la API:

   ```bash
   dotnet run --project Municipalidad.Api/Municipalidad.Api.csproj
   ```

El Swagger estará disponible en `https://localhost:7243/swagger`.

## Pruebas

```bash
dotnet test Municipalidad.Tests/Municipalidad.Tests.csproj
```

## Despliegue

- Configure las variables de entorno `ASPNETCORE_ENVIRONMENT`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` y las secciones `Clients:*` antes de publicar.
- Puede publicar con `dotnet publish` y desplegar el artefacto resultante en la plataforma elegida (IIS, contenedores, etc.).
