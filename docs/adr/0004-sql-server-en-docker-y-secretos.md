# ADR-0004: SQL Server en Docker y secretos fuera del repositorio

- **Estado:** Aceptada
- **Fecha:** 2026-10

## Contexto

El desarrollo local necesita una base SQL Server reproducible, y el repositorio es público: ninguna credencial puede quedar en el código ni en el historial de git.

## Decisión

- SQL Server 2022 en un contenedor definido en `docker-compose.yml`, con volumen persistente y *healthcheck*.
- La contraseña del contenedor se lee de un archivo `.env` ignorado por git. El repositorio solo incluye `.env.example` con un valor de muestra.
- La connection string de la Api vive en **User Secrets**, no en `appsettings.json`.
- Si falta la connection string o la configuración del negocio, la aplicación no arranca (*fail fast*).
- Esquema *code-first* con migraciones de EF Core. Antes de aplicar una migración se revisa el SQL con `dotnet ef migrations script`.
- Configuración de mapeo con `IEntityTypeConfiguration<T>`, una clase por entidad:
  - `HasPrecision(18, 2)` en importes.
  - `HasMaxLength` con las constantes de las entidades.
  - `varchar` para DNI y teléfono.
  - FK con `DeleteBehavior.Restrict`.

## Alternativas consideradas

- **SQL Server instalado localmente o LocalDB:** depende de la máquina de cada desarrollador; Docker da la misma versión en cualquier entorno.
- **Credenciales en `appsettings.Development.json`:** quedarían en el historial de git aunque se borren después.
- **Database-first:** el modelo de la base dejaría de seguir al Domain; con code-first, el esquema se versiona junto con el código.

## Consecuencias

- Levantar el entorno es `docker compose up -d` más `dotnet ef database update`.
- Cada desarrollador configura su `.env` y sus User Secrets al clonar el repositorio.
- El `UserSecretsId` del `.csproj` es solo un identificador del almacén local y no contiene secretos.
