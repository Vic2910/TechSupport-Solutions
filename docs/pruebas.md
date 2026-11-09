# Pruebas verificadas

Resultado: **42 peticiones HTTP correctas** en un escenario de integración xUnit con WebApplicationFactory y SQLite temporal.

## Nueve casos mínimos

| # | Prueba | Esperado / obtenido | Captura |
|---|---|---|---|
| 1 | Login correcto | 200 / 200 | [Captura](evidencias/http-01.png) |
| 2 | Contraseña incorrecta | 401 / 401 | [Captura](evidencias/http-02.png) |
| 3 | Consultar sin JWT | 401 / 401 | [Captura](evidencias/http-03.png) |
| 4 | Técnico intenta eliminar | 403 / 403 | [Captura](evidencias/http-04.png) |
| 5 | Crear sin título | 400 / 400 | [Captura](evidencias/http-05.png) |
| 6 | Crear correctamente | 201 / 201 | [Captura](evidencias/http-06.png) |
| 7 | Usuario consulta ajeno | 404 / 404 | [Captura](evidencias/http-07.png) |
| 8 | Cerrar sin resolver | 400 / 400 | [Captura](evidencias/http-08.png) |
| 9 | Excepción controlada | 500 / 500 | [Captura](evidencias/http-09.png) |

Las capturas son representaciones de las transcripciones HTTP obtenidas durante la ejecución, no respuestas inventadas. El archivo fuente es [http.json](evidencias/http.json). Se omiten las credenciales y los JWT.

## Cobertura adicional

| Caso | HTTP | Resultado |
|---|---|---|
| Login admin | 200 | Correcto |
| Login tecnico | 200 | Correcto |
| Login usuario | 200 | Correcto |
| Login tecnico2 | 200 | Correcto |
| Login usuario2 | 200 | Correcto |
| 02 - Contraseña incorrecta | 401 | Correcto |
| Usuario inexistente: mismo mensaje | 401 | Correcto |
| 03 - Sin JWT | 401 | Correcto |
| JWT alterado | 401 | Correcto |
| 05 - Sin título | 400 | Correcto |
| Título solo espacios | 400 | Correcto |
| Enum inválido | 400 | Correcto |
| 06 - Crear correctamente; ignora UsuarioId del cuerpo | 201 | Correcto |
| 04 - Técnico no elimina | 403 | Correcto |
| 07 - Usuario consulta incidente ajeno | 404 | Correcto |
| Usuario consulta propio | 200 | Correcto |
| 08 - Cerrar sin resolver | 400 | Correcto |
| Usuario no cambia estado | 403 | Correcto |
| Técnico no asigna | 403 | Correcto |
| No asignar a usuario | 400 | Correcto |
| No asignar técnico inexistente | 400 | Correcto |
| Asignación válida | 200 | Correcto |
| No reasignar un incidente en atención | 400 | Correcto |
| Caso 1 - Técnico modifica ajeno | 404 | Correcto |
| Lista del técnico asignado | 200 | Correcto |
| Lista del otro técnico | 200 | Correcto |
| Lista del otro usuario | 200 | Correcto |
| No saltar de Asignado a Resuelto | 400 | Correcto |
| Avanzar a EnProceso | 200 | Correcto |
| No retroceder | 400 | Correcto |
| Resolver y registrar fecha | 200 | Correcto |
| Cerrar después de resolver | 200 | Correcto |
| Cerrado no se reabre | 400 | Correcto |
| Límite de paginación | 400 | Correcto |
| Página negativa | 400 | Correcto |
| Filtro por estado y página | 200 | Correcto |
| Estado de filtro inválido | 400 | Correcto |
| 09 - Excepción controlada (ruta solo de pruebas) | 500 | Correcto |
| Administrador elimina cerrado | 204 | Correcto |
| Eliminado devuelve 404 | 404 | Correcto |
| Técnico puede crear | 201 | Correcto |
| Técnico creador sin asignación no ve detalle | 404 | Correcto |

El test también valida BCrypt, proveedor SQLite, lectura desde un contexto independiente y preservación de FechaResolucion UTC. La ruta de excepción se registra únicamente desde Tests.

## Frontend

El script scripts/test-mvc.cjs verifica en Microsoft Edge: login, crear, asignar, avanzar, cerrar, denegar recurso ajeno, eliminar y rechazar POST sin antiforgery. Capturas y resultados en evidencias/mvc-*.png y evidencias/mvc-resultados.json. Requiere Node.js, Playwright y Microsoft Edge; ejecutar con ambos servidores iniciados.

## Reproducibilidad

dotnet test TechSupport.Tests/TechSupport.Tests.csproj ejecuta el escenario y regenera http.json. scripts/generate_delivery.py regenera los PDF y las capturas a partir de esa evidencia; requiere Python, reportlab, pymupdf y Pillow.

### Resultado del recorrido MVC

Verificado en Microsoft Edge, escritorio 1440 x 960 y móvil 390 x 844. Sin desbordamiento horizontal.

- Login MVC: usuario
- Crear desde formulario MVC: incidente #2
- Login MVC: admin
- Admin asigna tÃ©cnico desde MVC
- Login MVC: tecnico
- TÃ©cnico avanza a EnProceso
- TÃ©cnico avanza a Resuelto
- TÃ©cnico avanza a Cerrado
- Login MVC: tecnico2
- Otro tÃ©cnico recibe 404 desde MVC
- Login MVC: admin
- Administrador elimina incidente cerrado desde MVC
- Ruta temporal de excepciÃ³n ausente en API normal: 404
- POST MVC sin antiforgery rechazado: 400
