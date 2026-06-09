# Auditoria de documentacion - Layla v3.1

Fecha de auditoria: 2026-06-07
Documento auditado: `C:\Users\snake\Desktop\Cosas\Escuela\Sexto_Semestre\Desarrollo de Sistemas en Red\Layla (Documento de Proyecto) v3.1.docx`
Repositorio revisado: `C:\Users\snake\Desktop\Layla`

## Resultado ejecutivo

La documentacion esta muy cerca de ser defendible, pero hay tres riesgos altos para la entrega:

1. El documento v3.1 todavia tiene un placeholder explicito: `Contexto (incluir diagrama de contexto)`.
2. Los casos de uso de analisis aparecen antes que los de diseno, pero no tienen la misma estructura; hoy son principalmente listas y nombres.
3. La matriz de funcionalidad de clientes esta desfasada respecto al codigo actual, especialmente Android.

Ya se genero el diagrama de contexto en:

- `docs/architecture/sequence-diagrams/00_diagrama_contexto.mmd`
- `docs/architecture/sequence-diagrams/00_diagrama_contexto.png`

## Hallazgos criticos

### P0 - Insertar el diagrama de contexto en el documento final

En el documento v3.1, la seccion `Requisitos > Contexto` conserva el texto `Contexto (incluir diagrama de contexto)`.

Accion recomendada:

- Insertar `docs/architecture/sequence-diagrams/00_diagrama_contexto.png` en esa seccion.
- Cambiar el titulo a `Contexto` o `Diagrama de contexto`.
- Acompanarlo con un parrafo breve: actores externos, canales cliente, backends, datos y bus de eventos.

### P0 - Rehacer "Casos de uso (analisis)" con la misma estructura que diseno

La seccion `Casos de uso (analisis)` esta antes de `Diseno > Descripciones de casos de uso`, lo cual es correcto. El problema es que la seccion de analisis no usa la misma estructura; actualmente lista modulos y nombres, mientras que los casos de uso de diseno si tienen actor, precondicion, flujo basico, alternativos y postcondicion.

Accion recomendada:

Usar este formato para los 15 casos en analisis, con contenido de alto nivel:

| Campo               | Nivel esperado en analisis                       |
| ------------------- | ------------------------------------------------ |
| ID y nombre         | Igual que en diseno.                             |
| Actor principal     | Rol funcional, no componente tecnico.            |
| Objetivo            | Una frase sobre valor de usuario.                |
| Precondicion        | Estado de negocio necesario.                     |
| Flujo basico        | 3-5 pasos conceptuales, sin endpoints ni clases. |
| Flujos alternativos | Solo escenarios principales.                     |
| Postcondicion       | Resultado observable por el usuario/sistema.     |
| Casos relacionados  | Include/extend si aplica.                        |

Ejemplo de tono para analisis:

`CU-08 Editar manuscrito`: el escritor/editor abre un capitulo, redacta contenido enriquecido, el sistema conserva el progreso y comparte la actividad con colaboradores conectados. La version de diseno puede detallar SignalR, RTF, autosave, MongoDB y Last-Write-Wins.

### P0 - Corregir matriz de funcionalidad por cliente

La matriz del documento menciona clases/pantallas que ya no existen en Android: `ProjectFeedScreen`, `WikiPane`, `AudioCaptureManager`, `ReaderWorkspaceScreen`, `VoiceRoomScreen`, `WorkspaceScreen`, entre otras. Esos archivos aparecen eliminados en el arbol de trabajo y el codigo actual de Android solo conserva autenticacion, `MyProjectsScreen`, CRUD de proyectos propios y estadisticas del sistema via `AdminApiService`.

Matriz recomendada:

| Funcionalidad                      | Desktop WPF     | Web Blazor Server | Android Compose                                   |
| ---------------------------------- | --------------- | ----------------- | ------------------------------------------------- |
| Login / registro                   | Completo        | Completo          | Completo                                          |
| Proyectos propios                  | Completo        | Completo          | Completo                                          |
| Crear / editar / eliminar proyecto | Completo        | Completo          | Completo                                          |
| Configurar privacidad              | Completo        | Completo          | Parcial, desde formulario de proyecto             |
| Catalogo publico                   | Completo        | Completo          | No implementado como pantalla dedicada            |
| Gestion de colaboradores           | Completo        | Completo          | No implementado                                   |
| Manuscritos / capitulos            | Completo        | Completo          | No implementado                                   |
| Edicion colaborativa               | Completo        | Completo          | No implementado                                   |
| Historial / restauracion           | Completo        | Completo          | No implementado                                   |
| Wiki                               | Completo        | Completo          | No implementado                                   |
| Grafo narrativo                    | Completo        | Completo          | No implementado                                   |
| Voz PTT                            | Completo        | Completo          | No implementado                                   |
| Lectura de historia publicada      | Completo        | Completo          | No implementado                                   |
| Panel admin / usuarios             | No implementado | Completo          | No implementado                                   |
| Reportes del sistema               | No implementado | Completo          | Parcial, consulta metricas si el usuario es admin |
| Perfil/configuracion               | Completo        | Completo          | No implementado o minimo no documentado           |

Tambien hay que corregir los textos que dicen que Android es "Companion App" de voz/lectura o que centraliza comunicacion en tiempo real. El estado real defendible es: Android es una consola movil reducida para autenticacion, administracion de proyectos propios y metricas.

## Hallazgos altos

### P1 - Tecnologias y versiones mezcladas

El documento contiene versiones inconsistentes:

- `server-core` real: `net10.0`.
- `api-gateway` real: `net9.0`.
- `client-web` real: `net9.0`.
- `client-desktop` real: `net9.0-windows`.
- `server-worldbuilding` real: Node.js + Express 5 + TypeScript.

Correcciones concretas:

- Tabla de pila tecnologica: cambiar `ASP.NET Core 8` por `ASP.NET Core 10` para `server-core`.
- Cambiar `Cliente Escritorio WPF (.NET 10)` por `WPF .NET 9`.
- Evitar decir "API unificada (.NET 10)" para todos los clientes: produccion entra por YARP, pero el dominio se reparte entre `server-core` y `server-worldbuilding`.

### P1 - Comandos de Docker no coinciden con el arbol actual

Las instrucciones de AGENTS/README dicen `cd src && docker compose up -d`, pero no hay `src/docker-compose.yml` en el arbol actual. El compose monolitico disponible esta en `deploy/docker/docker-compose.yml`, y Vagrant usa fragmentos en `deploy/vagrant/files/compose`.

Accion recomendada:

- O mover/copiar el compose esperado a `src/docker-compose.yml`.
- O actualizar los comandos del documento a `cd deploy/docker && docker compose up -d`.
- Revisar que el `.env` usado tenga variables compatibles con `deploy/docker/docker-compose.yml`; por ejemplo ese compose usa `WORLDBUILDING_PORT_HTTP` y `WORLDBUILDING_PORT_HTTPS`, mientras `src/.env.Development` muestra `WORLDBUILDING_PORT`.

### P1 - Resultados de pruebas inconsistentes

Hay tres cifras distintas:

- `docs/guides/testing.md`: total unitario 315, worldbuilding 128.
- Documento v3.1 tabla de pruebas: total 323, worldbuilding 131.
- Documento v3.1 resultados: `Layla.Core.Tests + client-web.Tests` = 187, pero la tabla anterior dice 187 de core + 5 de client-web = 192.

Accion recomendada:

- Ejecutar de nuevo las suites antes de exportar.
- Actualizar una sola tabla maestra con fecha, comando, suite, pasadas, fallidas y omitidas.
- Si no se ejecuta worldbuilding en la entrega, no sumar sus pruebas en "Resultados"; dejarlo como "registrado en plan, no ejecutado en esta corrida".

### P1 - RF/RNF prometen Android voz, lectura u offline que no coinciden con codigo actual

Textos a revisar:

- Introduccion: Android como "comunicacion por voz en tiempo real".
- Clases de usuario: `Editor (Voz)` en Android.
- RF-07: Android solo lectura de manuscritos.
- RF-10: modulo de voz Android + WebSockets.
- RNF-08: resiliencia de Android durante voz o edicion.
- Matriz de construccion: Android como comunicacion y lectura.

Accion recomendada:

Reescribir esos puntos para que Android sea una superficie reducida de proyectos y metricas. La voz debe quedar en Desktop/Web si se documenta como implementada.

## Hallazgos medios

### P2 - Falta accesibilidad basica en imagenes del DOCX

El DOCX contiene 19 archivos de imagen y 22 dibujos, pero no se detectaron atributos `descr`/`title` de texto alternativo en el OOXML.

Accion recomendada:

- Agregar texto alternativo a diagramas principales.
- Usar captions como `Figura 1. Diagrama de contexto`, `Figura 2. Arquitectura logica`, etc.

### P2 - No hay comentarios ni cambios controlados

El DOCX no tiene partes de comentarios ni cambios controlados (`w:ins`, `w:del`, `moveFrom`, `moveTo` en cero). Esto esta bien para una version final limpia, pero significa que la auditoria no queda trazada dentro del Word.

Accion recomendada:

- Si el profesor espera evidencias de revision, usar un anexo de "Cambios realizados en v3.2".
- Si no, mantener el documento limpio.

### P2 - Diagramas existentes y documento principal deben compartir nombres

El repo ya tiene `06_arquitectura_logica`, `08_casos_de_uso`, maquinas de estado y secuencias. El documento v3.1 habla de varias vistas UML, pero debe mapear claramente que imagen se usa en cada vista.

Accion recomendada:

- Agregar una tabla "Fuente de diagramas" en Apendices:
  - Contexto: `00_diagrama_contexto`
  - Casos de uso: `08_casos_de_uso`
  - Arquitectura logica/componentes: `06_arquitectura_logica`
  - Despliegue: `07_despliegue_vagrant`
  - Secuencias: `01` a `05`
  - Estados: `09` a `13`

## Revision visual

Se intento renderizar el DOCX con el renderer de la skill de documentos, pero no se encontro LibreOffice/`soffice` en este entorno. Por eso esta auditoria cubre estructura, texto, tablas y OOXML, pero no certifica layout pagina por pagina.

Antes de entregar, abrir el DOCX en Word y revisar manualmente:

- Portada, indice y numeracion.
- Que cada figura no se corte.
- Que tablas anchas no salgan de margenes.
- Que las secciones de casos de uso no queden partidas con encabezados solos al final de pagina.
- Que el nuevo PNG de contexto sea legible al tamano insertado.

## Checklist de cierre para v3.2

- [ ] Insertar `00_diagrama_contexto.png` en la seccion Contexto.
- [ ] Reescribir `Casos de uso (analisis)` con la misma estructura que diseno, pero a nivel conceptual.
- [ ] Sustituir la matriz de funcionalidad por cliente.
- [ ] Actualizar textos de Android a alcance reducido.
- [ ] Corregir versiones .NET/Node y la frase de API unificada.
- [ ] Corregir comandos de Docker o agregar el compose en la ruta documentada.
- [ ] Unificar resultados de pruebas.
- [ ] Agregar captions y texto alternativo a diagramas.
- [ ] Exportar una v3.2 a PDF y revisar visualmente en Word.
