# Hoja de análisis
## A1. Problema, requerimiento y prueba
| Problema | Requerimiento | Prueba |
|---|---|---|
| Solicitudes perdidas | Registrar en SQLite con id, título, descripción y fecha; listar | POST 201, GET incluye registro y otro DbContext lo recupera |
| Asignaciones poco claras | Solo admin asigna a un técnico; estado cambia a Asignado | Técnico obtiene 403 al asignar; asignar a Usuario obtiene 400; asignación válida 200 |
| Información ajena | JWT, roles y propiedad obtenida del token | Usuario ajeno y técnico ajeno obtienen 404; listas filtradas |
| Errores sin registro o con detalles internos | Middleware e ILogger/Serilog con requestId | Excepción: 500 genérico; log contiene detalle y el mismo requestId |

## A2. Estados
Pendiente -> Asignado -> EnProceso -> Resuelto -> Cerrado.
La primera transición exige asignación por Administrador a rol Tecnico.
Las demás requieren Administrador o el técnico asignado.
Prohibido explícitamente: cerrar sin resolver; también saltos, retrocesos y reabrir Cerrado.
Ver diagramas.md y el PDF entregado.

## A3. Decisiones y justificación
1. Responder 404 ante incidente ajeno, igual que inexistente, porque evita enumerar recursos; con 403 se confirmaría su existencia.
2. Permitir eliminar Cerrado al administrador porque la matriz no restringe el estado; sin esa decisión se añadiría una limitación no especificada. Al eliminar se pierde historial, por eso hay confirmación explícita.

## Justificaciones adicionales
- Servicio único: evita reglas divergentes entre endpoints; duplicarlas permitiría inconsistencias.
- DTOs: evitan filtrar PasswordHash y sobreasignación; devolver entidades expondría información interna.
- Dos relaciones explícitas: distinguen reportante y técnico; sin configuración EF podría asociar mal las claves.
- BCrypt: incorpora sal y costo de cómputo; texto plano expondría todas las contraseñas.
- Mismo 401 para usuario inexistente y contraseña incorrecta: evita enumeración de cuentas.
- Middleware inicial: envuelve autenticación, autorización y endpoints; al final no captura fallos de componentes anteriores.
- MVC por HTTP: utiliza el mismo contrato y seguridad; acceso directo a SQLite eludiría las reglas de la API.
- Límite de paginación: evita cargar cantidades arbitrarias; sin límite una petición agotaría memoria.
