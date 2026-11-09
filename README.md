# TechSupport Solutions
Solución .NET 10 con API ASP.NET Core, frontend ASP.NET Core MVC, contratos compartidos y pruebas de integración. El MVC consume la API mediante HttpClient; solo la API accede a SQLite.

## Ejecutar
Requisito: SDK de .NET 10. En esta carpeta:
```powershell
dotnet restore "TechSupport Solutions.slnx"
dotnet build "TechSupport Solutions.slnx"
```
En dos terminales:
```powershell
dotnet run --project TechSupport.API/TechSupport.API.csproj --launch-profile http
dotnet run --project "TechSupport Solutions.csproj" --launch-profile http
```
Abrir http://localhost:5014. API: http://localhost:5080.
En Visual Studio, abrir la solución y configurar **varios proyectos de inicio**: TechSupport.API y TechSupport Solutions, ambos con perfil http. Contracts y Tests no se inician.
La dirección de la API se configura en appsettings.json del MVC: Api:BaseUrl.
SQLite se crea automáticamente mediante EnsureCreated en TechSupport.API/techsupport.db cuando se usa el comando anterior. Los incidentes persisten entre reinicios. EnsureCreated es apropiado para este ejercicio; al evolucionar el esquema se deben usar migraciones EF Core.

## Cuentas de demostración
Se crean solo en Development/Testing cuando la tabla Usuarios está vacía. Contraseña para todas: **Demo123!**.
| Usuario | Rol |
|---|---|
| admin | Administrador |
| tecnico, tecnico2 | Tecnico |
| usuario, usuario2 | Usuario |

Las contraseñas se guardan con BCrypt, no en texto plano. Las cuentas son exclusivamente de prueba; no desplegar una base sembrada con estas credenciales.
1. Entrar como usuario y crear un incidente.
2. Salir, entrar como admin, abrir el detalle y asignar a tecnico.
3. Entrar como tecnico y avanzar: EnProceso, Resuelto, Cerrado.
4. Entrar como tecnico2/usuario2: el incidente no aparece ni puede consultarse por su id.
El técnico puede crear, pero solo ve incidentes asignados a él: un reporte suyo recién creado se mostrará al administrador, no al técnico hasta la asignación.

## Seguridad y decisiones
- JWT: firma, emisor, audiencia y expiración validados. Claims NameIdentifier, Name y Role. Vigencia: 60 minutos.
- Clave de clase en TechSupport.API/appsettings.Development.json. En un proyecto real NO se versiona: usar dotnet user-secrets en desarrollo y Jwt__Key en variables de entorno o un gestor de secretos en producción. El arranque rechaza claves ausentes o menores a 32 bytes.
- El MVC conserva el token dentro del ticket de cookie cifrado y autenticado por ASP.NET Data Protection, HttpOnly. Los formularios incluyen antiforgery. No usa localStorage.
- El perfil HTTP es local para clase. En despliegue real se requiere HTTPS y configuración de proxy/host, cookies Secure y almacenamiento protegido de claves Data Protection.
- Incidente ajeno y no existente: 404 uniforme; rol sin permiso para una operación: 403. Así no se revela la existencia de recursos ajenos.
- Administrador puede eliminar incluso Cerrados según matriz; se pierde historial. El MVC pide confirmación. Un producto real debería conservar auditoría o aplicar borrado lógico.
- Asignar solo Pendientes a Tecnico; sin reasignación porque el enunciado no define ese flujo.
- Las fechas son UTC (indicadas en pantalla). FechaResolucion se fija al resolver y se conserva al cerrar.
- Middleware al principio: registra excepción completa con requestId y devuelve JSON genérico. Al final no capturaría excepciones de componentes anteriores que no lo invocan. No cubre fallos de arranque.
- Serilog escribe Logs/api-AAAAmmdd.log con rotación diaria y retención de 14 archivos; Logs no se sube a Git. Nunca se registran cuerpos de login ni tokens.
- Un Guid de concurrencia impide sobrescribir cambios simultáneos; responde 409 para recargar el incidente.
- DTOs no exponen PasswordHash ni aceptan el id del creador: se toma del JWT.

## Endpoints
Todos salvo login requieren Authorization: Bearer TOKEN.
| Método | Ruta | Permiso |
|---|---|---|
| POST | /api/auth/login | Anónimo |
| POST | /api/incidentes | Todos |
| GET | /api/incidentes?estado=Pendiente&page=1&pageSize=10 | Filtrado por rol |
| GET | /api/incidentes/{id} | Propietario/asignado/admin |
| PUT | /api/incidentes/{id}/asignar | Administrador |
| PUT | /api/incidentes/{id}/estado | Administrador o técnico asignado |
| DELETE | /api/incidentes/{id} | Administrador |
| GET | /api/usuarios/tecnicos | Administrador, para el selector MVC |

Enums JSON: Pendiente, Asignado, EnProceso, Resuelto, Cerrado; Baja, Media, Alta, Critica.
La colección devuelve items, total, page y pageSize. pageSize entre 1 y 100; entradas fuera de rango retornan 400.
Usar **TechSupport.API/TechSupport.API.http** en Visual Studio/VS Code o importar las peticiones en Postman. No se necesita Swagger para el criterio del documento.

## Pruebas y entregables
```powershell
dotnet test TechSupport.Tests/TechSupport.Tests.csproj --logger "trx;LogFileName=integration.trx" --results-directory TestResults
```
Detener los servidores antes de recompilar en Windows para evitar bloqueos de DLL.
El escenario xUnit usa WebApplicationFactory y SQLite temporal, comprueba la matriz de roles y estados y genera docs/evidencias/http.json con peticiones/respuestas reales. Tokens y contraseñas se ocultan en la entrega. La ruta /_test/throw está solo en el ensamblado Tests y no existe en la API entregada.
- output/pdf/Entrega_TechSupport.pdf: análisis, diagramas de estados, relaciones y arquitectura.
- output/pdf/Evidencias_HTTP.pdf: nueve casos mínimos con capturas de transcripciones HTTP reales.
- docs/analisis.md y docs/diagramas.md: fuentes editables.
- docs/pruebas.md: tabla de pruebas, 42 peticiones HTTP verificadas y recorrido MVC completo.
- docs/evidencias/: transcripciones, capturas HTTP y capturas del MVC.
- docs/retos-extra.md: JWT revocado, idempotencia y alcance de los extras.
Los commits documentan avances reales de esta implementación; no se simulan dos sesiones de clase ni fechas anteriores.

## Guía para explicar el código
1. Contracts define enums y DTOs con validación.
2. AppDbContext separa las relaciones creador/técnico y configura SQLite.
3. AuthController verifica BCrypt y firma JWT.
4. IncidenteService aplica propiedad, transición, asignación y fecha de resolución.
5. IncidentesController traduce HTTP y aplica autorización por rol.
6. ExceptionHandlingMiddleware registra fallos y oculta detalles internos.
7. MVC CuentaController establece cookie; ApiClient adjunta JWT al HTTP saliente.
8. Las vistas Razor muestran formularios, y la API vuelve a validar cada operación.

Referencia técnica consultada: [JWT Bearer en ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).
