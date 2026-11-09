# Retos extra
## Usuario desactivado con JWT vigente
El JWT sigue siendo válido porque la validación criptográfica comprueba firma y expiración, no consulta el estado actual del usuario.
| Solución propuesta | Ventaja | Costo |
|---|---|---|
| Consultar Activo/TokenVersion en BD o caché durante OnTokenValidated | Revocación inmediata, permite revocar todas las sesiones | Una consulta por petición; invalidar la caché |
| Access token de corta duración y refresh token revocable en servidor | Reduce consultas al recurso y permite renovar solo usuarios activos | El token de acceso sigue funcionando hasta expirar; hay que implementar rotación y revocación de refresh tokens |

El modelo mínimo no incluye desactivación ni refresh tokens. Estas son propuestas, no funcionalidades afirmadas como implementadas.

## Incidentes duplicados e idempotencia
Tres POST válidos pueden crear tres incidentes. Deshabilitar Guardar en MVC reduce doble clic, pero no cubre reintentos de red o clientes externos. La API NO garantiza idempotencia.
Propuesta: el cliente genera Idempotency-Key por intención de creación; servidor almacena (UsuarioId, clave, hash del cuerpo, respuesta) con índice único y transacción. Mismo cuerpo/clave devuelve la respuesta guardada; mismo clave con otro cuerpo devuelve 409. La clave debe sobrevivir a reintentos, expirar de forma definida y no compartirse entre usuarios.
No deduplicar solo por título: dos solicitudes legítimas pueden tener el mismo título.

## Extras implementados
- Serilog con rotación diaria y Logs ignorado.
- Filtro por estado, páginas y pageSize máximo 100 (400 si excede).
- xUnit con WebApplicationFactory y SQLite para propiedad de recursos, integridad y errores.
- Segundo proyecto MVC funcional que consume la API, sin referencia a EF Core.
