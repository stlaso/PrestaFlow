# Roadmap de PrestaFlow

Hoja de ruta del proyecto, organizada en fases. Cada fase se cierra con un Pull Request.

**Estado:** ✅ completa · 🚧 en curso · ⏳ pendiente

| Fase | Tema | Estado |
| --- | --- | --- |
| 0 | Preparación del entorno | ✅ |
| 1 | API y dominio | 🚧 |
| 2 | Tests de backend | ⏳ |
| 3 | Frontend React | ⏳ |
| 4 | Publicación en Azure | ⏳ |
| 5 | CI/CD | ⏳ |
| 6 | Secretos y observabilidad | ⏳ |
| 7 | Documentos adjuntos y notificaciones asíncronas | ⏳ |
| 8 | Autenticación, roles y API Gateway | ⏳ |
| 9 | Documentación y demo | ⏳ |

---

## Fase 0 · Preparación del entorno

- [x] Repositorio con README y `.gitignore` para .NET y Node
- [x] Suscripción de Azure con alerta de presupuesto
- [x] Organización y proyecto en Azure DevOps
- [x] Entorno local: .NET 10 SDK, Node LTS, Azure CLI y Docker

## Fase 1 · API y dominio

**Objetivo:** API funcionando en local con base de datos y documentación OpenAPI.

- [x] Solución en capas: Domain, Application, Infrastructure y Api, con sus referencias
- [ ] Entidades `Cliente` y `Solicitud`, con el enum de estados
- [ ] Regla de cálculo de riesgo en el dominio, sin dependencias externas
- [ ] `DbContext`, configuración de entidades y migraciones con EF Core (SQL Server en Docker)
- [ ] Endpoints de clientes: alta y listado
- [ ] Endpoints de solicitudes: alta, listado con filtros, aprobación y rechazo
- [ ] Validaciones y manejo global de errores con Problem Details

**Entregable:** la API se prueba completa desde Swagger.

## Fase 2 · Tests de backend

**Objetivo:** lógica de negocio cubierta por tests.

- [ ] Proyecto de tests unitarios con xUnit y Moq
- [ ] Tests de la regla de riesgo con casos parametrizados (`[Theory]`)
- [ ] Tests de los servicios de aplicación con repositorios simulados
- [ ] Tests de integración de los endpoints con `WebApplicationFactory`

**Entregable:** `dotnet test` en verde.

## Fase 3 · Frontend React

**Objetivo:** interfaz completa consumiendo la API.

- [ ] Proyecto con Vite, React, TypeScript y Tailwind CSS
- [ ] Rutas: listado y detalle de solicitudes, alta, clientes y panel
- [ ] Integración con la API usando TanStack Query (listar, crear, aprobar, rechazar)
- [ ] Estado global con Zustand: filtros activos y usuario actual
- [ ] Formularios con React Hook Form y validación con Zod
- [ ] Estados de carga, error y lista vacía en cada pantalla
- [ ] Tests de componentes con Vitest y React Testing Library
- [ ] CORS habilitado en la API para el entorno local

**Entregable:** flujo completo usable desde el navegador.

## Fase 4 · Publicación en Azure

**Objetivo:** sistema accesible desde internet.

- [ ] Resource Group del proyecto
- [ ] Azure SQL Database con las migraciones aplicadas
- [ ] API publicada en Azure App Service
- [ ] Frontend publicado en Azure Static Web Apps
- [ ] CORS y variables de entorno configuradas en ambos

**Entregable:** demo pública funcionando.

## Fase 5 · CI/CD

**Objetivo:** cada cambio se compila, se prueba y se publica automáticamente.

- [ ] Azure Pipelines conectado al repositorio de GitHub
- [ ] `azure-pipelines.yml` con etapas de build, test y deploy
- [ ] Backend: build, tests y deploy a App Service
- [ ] Frontend: instalación, tests, build y deploy a Static Web Apps
- [ ] Validación de Pull Requests: el deploy se bloquea si fallan los tests

**Entregable:** un push a `main` publica automáticamente.

## Fase 6 · Secretos y observabilidad

**Objetivo:** ningún secreto en el código y visibilidad de errores en producción.

- [ ] Azure Key Vault con la connection string
- [ ] Managed Identity en el App Service con acceso de lectura al Key Vault
- [ ] API leyendo secretos desde Key Vault
- [ ] Application Insights en la API y el frontend
- [ ] Alerta ante errores en producción

**Entregable:** ningún secreto en el repositorio ni en la configuración del App Service.

## Fase 7 · Documentos adjuntos y notificaciones asíncronas

**Objetivo:** adjuntar documentos a las solicitudes y notificar aprobaciones sin bloquear la API.

- [ ] Storage Account con contenedor Blob para documentos
- [ ] Endpoints de subida y descarga con links temporales (SAS)
- [ ] Azure Service Bus con la cola `solicitudes-aprobadas`
- [ ] Publicación de un mensaje al aprobar una solicitud
- [ ] Azure Function con trigger de Service Bus que envía el mail
- [ ] Function incluida en el pipeline

**Entregable:** al aprobar una solicitud se envía un mail de forma asíncrona.

## Fase 8 · Autenticación, roles y API Gateway

**Objetivo:** login real con permisos por rol y acceso controlado a la API.

- [ ] Registro de la API y el frontend en Microsoft Entra ID
- [ ] Roles de aplicación: Operador y Analista
- [ ] API protegida con JWT y autorización por rol (solo Analista aprueba o rechaza)
- [ ] Login en React con MSAL, con acciones visibles según el rol
- [ ] Azure API Management (plan Consumption) con rate limiting

**Entregable:** cada usuario ve y puede hacer solo lo que le permite su rol.

## Fase 9 · Documentación y demo

- [ ] README con descripción, diagrama de arquitectura, capturas y link a la demo
- [ ] Instrucciones para correr el proyecto en local
- [ ] Decisiones técnicas documentadas
- [ ] Usuarios de prueba para cada rol

## Extra · Panel de estadísticas

- [ ] Endpoint de estadísticas en la API
- [ ] Gráficos con Recharts: solicitudes por estado, montos por mes y distribución de riesgo
