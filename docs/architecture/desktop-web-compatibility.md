# Analisis de compatibilidad: cliente de escritorio vs cliente web

Estado verificado contra codigo fuente en `src/client-desktop`, `src/client-web` y `src/client-shared`.

## Resumen ejecutivo

El cliente de escritorio WPF y el cliente web Blazor Server ya no estan segmentados por funcionalidad principal: ambos cubren el workspace de autoria completo para proyectos Layla. La diferencia real esta en la plataforma y en algunos detalles de implementacion, no en el alcance funcional base.

Android queda reducido a una consola movil de administracion y estadisticas. No debe considerarse cliente de escritura, lector de manuscritos, wiki, grafo ni voz.

## Compatibilidad funcional

| Area                     | Escritorio WPF                                                                                | Web Blazor Server                                                                                                      | Compatibilidad                                                                                                                        |
| ------------------------ | --------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Autenticacion            | Login, registro, verificacion, sesion local y logout mediante `AuthService`/`SessionManager`. | Login, registro, auth state Blazor, `ProtectedSessionStorage`, redirect con `returnUrl`.                               | Equivalente en flujo y endpoints. La web debe hidratar sesion tras render por prerendering.                                           |
| Perfil/configuracion     | `SettingsView` actualiza perfil y preferencias locales.                                       | `Settings.razor` actualiza perfil y preferencias visuales via JS/storage.                                              | Parcialmente equivalente; ambos tienen perfil, las preferencias son plataforma-especificas.                                           |
| Mis proyectos            | Crear, listar, editar y eliminar proyectos.                                                   | Crear, listar, editar y eliminar proyectos.                                                                            | Equivalente.                                                                                                                          |
| Proyectos publicos       | Listar publicos, unirse y abrir proyecto.                                                     | Explorar publicos, filtrar, unirse y abrir proyecto.                                                                   | Equivalente; UI distinta.                                                                                                             |
| Workspace de escritura   | Tabs de manuscrito, wiki, grafo y voz en `WorkspaceView`.                                     | Tabs de manuscrito, wiki, grafo y drawer de voz en `ProjectWorkspace.razor`.                                           | Equivalente en alcance.                                                                                                               |
| Roles por proyecto       | `OWNER`, `EDITOR`, `READER`; `IsReadOnly` bloquea edicion para lectores.                      | `OWNER`, `EDITOR`, `READER`; `IsReadOnly` bloquea edicion para lectores.                                               | Equivalente.                                                                                                                          |
| Colaboradores            | Owner invita, lista, cambia `READER`/`EDITOR` y elimina colaboradores.                        | Owner invita, lista, cambia rol y elimina colaboradores.                                                               | Equivalente.                                                                                                                          |
| Manuscritos              | CRUD de manuscritos y capitulos, editor RTF, autoguardado/hitos, historial.                   | CRUD de manuscritos y capitulos, editor enriquecido HTML/RTF-compatible, autoguardado/hitos, historial y restauracion. | Muy cercano. Web expone restauracion y lectura completa para `READER`; escritorio expone comparacion/restauracion en ventana de diff. |
| Colaboracion de capitulo | `ManuscriptHubClient` compartido y sincronizacion de cursor/texto.                            | `ManuscriptHubClient` compartido, cursores y broadcast desde `RichTextEditor`.                                         | Equivalente en infraestructura; la UI del editor difiere.                                                                             |
| Deteccion de menciones   | Usa `WikiTokenizer` compartido para entidades detectables.                                    | Usa `WikiTokenizer` compartido y refresca entidades con eventos de wiki.                                               | Equivalente, con mejor rehidratacion visible en web.                                                                                  |
| Wiki                     | CRUD, tipos, tags, apariciones por entidad.                                                   | CRUD, filtros, tags, apariciones por entidad.                                                                          | Equivalente.                                                                                                                          |
| Grafo narrativo          | Consulta grafo, crea y elimina relaciones.                                                    | Consulta grafo, canvas SVG interactivo, crea y elimina relaciones.                                                     | Equivalente en API; web tiene interaccion visual mas rica en canvas.                                                                  |
| Voz                      | `VoicePanelView` con sala SignalR y PTT.                                                      | `VoiceDrawer`/`VoiceRoom` con sala SignalR, PTT y audio JS.                                                            | Equivalente en objetivo; audio depende de stack nativo WPF vs JS/browser.                                                             |
| Presencia                | Heartbeat de autor y presencia por proyecto.                                                  | Servicios SignalR para presencia/estado de autor.                                                                      | Equivalente en concepto; implementacion no identica.                                                                                  |
| Administracion           | No hay dashboard admin dedicado en escritorio.                                                | Dashboard admin, gestion de usuarios y reporte de sistema.                                                             | Diferencia deliberada: web es el cliente admin completo.                                                                              |

## Contratos y rutas compartidas

Ambos clientes consumen los mismos backends por los mismos dominios funcionales:

- `server-core`: `/api/tokens`, `/api/users`, `/api/projects`, `/hubs/presence`, `/hubs/voice`, `/hubs/manuscript`.
- `server-worldbuilding`: `/api/manuscripts`, `/api/wiki`, `/api/graph`.
- `client-shared`: `WikiTokenizer`, modelos de versiones y `ManuscriptHubClient`.

Esto mantiene compatibilidad de comportamiento aunque WPF use servicios propios (`IManuscriptApiService`, `IWikiApiService`, `IGraphApiService`) y Blazor use servicios equivalentes (`IManuscriptService`, `IWikiService`, `IGraphService`).

## Diferencias tecnicas relevantes

| Tema                   | Escritorio                                              | Web                                                            | Riesgo/nota                                                           |
| ---------------------- | ------------------------------------------------------- | -------------------------------------------------------------- | --------------------------------------------------------------------- |
| Estado de sesion       | Proceso de un usuario; `SessionManager` local.          | Circuitos Blazor multiusuario; servicios `Scoped`.             | No compartir singletons con identidad en web.                         |
| Render/editor          | WPF/XAML, RichTextBox/RTF y ventanas auxiliares.        | Blazor + JS interop (`richTextEditor.js`) y DOM editable.      | Validar conversion/conservacion de contenido rico entre plataformas.  |
| Almacenamiento del JWT | Local al cliente.                                       | `ProtectedSessionStorage`, no disponible durante prerender.    | Toda pagina protegida debe hidratar en `OnAfterRenderAsync`.          |
| Errores HTTP           | Servicios devuelven `null`/`false` y muestran UI local. | `ApiClient` lanza `APIException`; servicios degradan/capturan. | Mantener manejo de error por servicio, no burbujear excepciones a UI. |
| Admin                  | Ausente.                                                | Presente en web.                                               | Es una diferencia funcional aceptada.                                 |

## Brechas detectadas

- La documentacion antigua describia Android como companion con voz, lectura/wiki o referencia movil. Eso ya no corresponde al codigo ni al alcance solicitado.
- Algunos textos antiguos seguian implicando segmentacion de funcionalidades por cliente. El estado actual es: escritorio y web comparten las capacidades de autoria; Android solo administra proyectos y muestra estadisticas.
- Web tiene un dashboard administrativo que escritorio no implementa.
- En manuscritos, web expone lectura completa para `READER` y endpoints de restauracion de version de forma explicita; escritorio tiene flujo de diff/restauracion en ventana dedicada. Conviene probar round-trip de contenido rico entre ambos clientes antes de considerar esa parte 100% intercambiable.

## Estado objetivo por cliente

| Cliente                | Funcion actual                                                                                                                                      |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Desktop WPF            | Workspace principal de autoria: proyectos, manuscritos, wiki, grafo, voz, colaboradores, perfil.                                                    |
| Web Blazor Server      | Workspace principal de autoria y administracion web: mismas funciones de escritura que escritorio, mas dashboard/admin.                             |
| Android Kotlin Compose | Consola movil reducida: autenticacion, administracion de proyectos propios y estadisticas de sistema. Sin manuscritos, wiki, grafo, lectura ni voz. |
