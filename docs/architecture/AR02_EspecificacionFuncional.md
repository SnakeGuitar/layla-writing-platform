# Especificación Funcional — Layla

**Equipo Layla** · AR02 · Ingeniería de Requisitos
_(Sustituir esta línea por la portada oficial del equipo antes de exportar a PDF como `AR02_EquipoLayla.pdf`.)_

---

## 1. Introducción

Layla es una plataforma de **escritura creativa colaborativa y worldbuilding** dirigida a autores de ficción (novelistas independientes, grupos de roleo narrativo, equipos de guionistas). Permite que varias personas co-escriban un manuscrito en tiempo real, mantengan una wiki del universo de ficción, visualicen las relaciones entre entidades en un grafo narrativo y se comuniquen mediante una sesión de voz _push-to-talk_ mientras trabajan.

Este documento es el artefacto **Especificación Funcional (AR02)** del proyecto. Recoge el resultado de las fases de elicitación, análisis y especificación de requisitos, y sirve como contrato mínimo entre el equipo de desarrollo y el resto de _stakeholders_ para las fases posteriores de diseño e implementación.

El documento se organiza en siete secciones: propósito, alcance, definiciones, referencias, requisitos (contexto, clases de usuario, casos de uso, requisitos funcionales y no funcionales) y restricciones. Un anexo final mapea dónde se responde cada pregunta de la pauta del artefacto.

## 2. Propósito del documento

El propósito de este documento es **comunicar a todos los interesados** (equipo de desarrollo, profesores/evaluadores, y futuros usuarios de la API) qué debe hacer el sistema Layla, para qué tipos de usuarios, bajo qué restricciones y con qué criterios de calidad.

Objetivos específicos:

- Establecer el contexto y el valor de negocio del producto.
- Enumerar las **clases de usuario** y los **casos de uso** que el sistema debe soportar.
- Detallar los **requisitos funcionales y no funcionales** trazables hasta los componentes ya implementados (endpoints REST, hubs de tiempo real, eventos asíncronos).
- Documentar las **restricciones** técnicas, académicas y regulatorias aceptadas por el equipo.

Este documento **no cubre** diseño detallado, plan de pruebas, ni despliegue en producción; esos artefactos se entregarán en etapas posteriores.

## 3. Alcance

La especificación cubre el sistema Layla **end-to-end** en su estado actual al cierre del ciclo de elicitación:

**Incluye**

- Dos servicios de _backend_:
  - `server-core` (ASP.NET Core 10) — autenticación, usuarios, proyectos, roles, mensajería de tiempo real.
  - `server-worldbuilding` (Node.js + Express 5) — manuscritos, wiki, grafo narrativo.
- Tres clientes: escritorio (WPF .NET 9), web (Blazor .NET 9) y móvil (Android — Kotlin + Jetpack Compose). Escritorio y web comparten el workspace principal de escritura; Android queda limitado a administración de proyectos y estadísticas del sistema.
- Tres almacenes de datos: SQL Server (relacional), MongoDB (documentos) y Neo4j (grafo).
- Un _broker_ de mensajería: RabbitMQ para eventos de dominio entre servicios.

**Excluye**

- Diseño de UI de alta fidelidad, guías de estilo visual y kit de componentes (se documentan aparte).
- Planes de despliegue productivo, monitorización, DR/BCP y operaciones.
- Modelo comercial, acuerdos legales con creadores y gestión de pagos.
- Pruebas de aceptación, plan de QA, estrategia de _release_.

## 4. Definiciones, acrónimos y abreviaciones

| Término                     | Significado                                                                                                                            |
| --------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| **API**                     | _Application Programming Interface_ — interfaz de programación expuesta por un servidor.                                               |
| **JWT**                     | _JSON Web Token_ — token firmado usado como credencial de sesión.                                                                      |
| **RBAC**                    | _Role-Based Access Control_ — control de acceso basado en roles.                                                                       |
| **SignalR**                 | Librería de ASP.NET para comunicación en tiempo real (WebSocket, SSE, _long polling_).                                                 |
| **PTT**                     | _Push-to-Talk_ — modo de transmisión de voz en el que el usuario mantiene pulsado un botón para hablar.                                |
| **RTF**                     | _Rich Text Format_ — formato de texto enriquecido usado para almacenar capítulos.                                                      |
| **Outbox Pattern**          | Patrón que garantiza que los eventos de dominio se publiquen tras confirmar la transacción de base de datos, evitando inconsistencias. |
| **RabbitMQ**                | Broker AMQP usado como bus de mensajes asíncronos entre servicios.                                                                     |
| **CU**                      | Caso de uso.                                                                                                                           |
| **RF / RNF**                | Requisito funcional / Requisito no funcional.                                                                                          |
| **Owner / Editor / Reader** | Roles sobre un proyecto: propietario, colaborador con permiso de edición, y lector.                                                    |
| **Manuscrito**              | Agrupación ordenada de capítulos dentro de un proyecto.                                                                                |
| **Capítulo**                | Unidad de texto editable en formato RTF dentro de un manuscrito.                                                                       |
| **Wiki**                    | Conjunto de entradas descriptivas sobre entidades del universo de ficción (personajes, lugares, objetos).                              |
| **Grafo narrativo**         | Representación en Neo4j de entidades de la wiki y sus relaciones (nodos y aristas).                                                    |
| **DAU**                     | _Daily Active Users_ — usuarios activos diarios.                                                                                       |

## 5. Referencias

**Bibliografía**

- Hunter, K. (2016). _Irresistible APIs: Designing web APIs that developers will love_. Manning.
- IEEE Std 830-1998. _Recommended Practice for Software Requirements Specifications_.

**Documentación interna del proyecto**

- [`README.md`](../README.md) — arquitectura y referencia de API actual.
- [`desktop-web-compatibility.md`](desktop-web-compatibility.md) — análisis de compatibilidad actual entre cliente de escritorio y web, y alcance reducido de Android.
- `docs/Layla (Documento de Proyecto).pdf` — versión inicial del documento de proyecto (parcialmente vigente).

**Documentación técnica externa**

- Microsoft: _ASP.NET Core 10_ y _SignalR_.
- Node.js 22 y _Express 5_.
- MongoDB _Document Model_.
- Neo4j _Cypher Query Language_.
- RabbitMQ _Topic Exchanges_ y _AMQP 0-9-1_.
- Jetpack Compose (Android) y WPF con MaterialDesignInXAML / FluentWPF.

## 6. Requisitos

### 6.1 Contexto

#### Problema que resuelve Layla

La escritura colaborativa de ficción larga **está fragmentada** entre herramientas genéricas que no se comunican entre sí: los equipos usan Word o Google Docs para la prosa, MediaWiki o Notion para el _lore_, Discord o TeamSpeak para coordinarse por voz, y hojas de cálculo o Miro para relacionar personajes. Esta fragmentación obliga al autor a mantener sincronizadas varias fuentes de verdad, rompe la continuidad narrativa y eleva los costos (3–4 suscripciones mensuales por persona en promedio).

Layla unifica los cuatro pilares — **prosa colaborativa, wiki de universo, grafo de relaciones y voz en vivo** — en un solo producto integrado, con sincronización en tiempo real entre los autores.

#### Valor de negocio

- **Para el autor**: reduce el costo total de herramientas, elimina fricción de sincronización, conserva el contexto narrativo dentro de un mismo producto.
- **Para el negocio**: retención alta de una vertical desatendida (escritores amateur e _indie_), potencial de monetización vía suscripción unificada y marketplace de lectores que consumen las obras publicadas públicamente.
- **Para el equipo académico**: proyecto íntegramente distribuido con tres clientes, dos backends, tres bases de datos y mensajería asíncrona — un ejemplo práctico y defendible de arquitectura distribuida moderna.

#### Diagrama de contexto

```
                    ┌──────────────────────┐
                    │   Escritor / Owner   │
                    └──────────┬───────────┘
                               │ HTTPS + WebSocket
                               ▼
  ┌────────────┐   ┌────────────────────────────────┐   ┌──────────────┐
  │  Lector    │──▶│                                │◀──│ Administrador│
  └────────────┘   │           SISTEMA              │   └──────────────┘
                   │            LAYLA               │
                   │                                │
                   │  ┌──────────────┐  ┌────────┐  │
                   │  │ server-core  │  │  wb    │  │
                   │  │  .NET 10     │──│ Node22 │  │
                   │  └──────┬───────┘  └───┬────┘  │
                   └─────────┼──────────────┼───────┘
                   ┌─────────┴──┐ ┌────┐ ┌──┴───────┐
                   │ SQL Server │ │Mongo│ │  Neo4j  │
                   └────────────┘ └────┘ └─────────┘
                                  ▲
                                  │
                          ┌───────┴────────┐
                          │   RabbitMQ     │
                          │ worldbuilding. │
                          │    events      │
                          └────────────────┘
```

Actores externos: **Escritor**, **Lector**, **Administrador**, tres sistemas de almacenamiento (SQL Server, MongoDB, Neo4j) y un _broker_ de mensajería (RabbitMQ). El sistema se compone de dos servicios internos que se comunican por eventos asíncronos.

### 6.2 Clases de usuario

| Clase                 | Descripción                                                                           | Pericia técnica                                                                    | Frecuencia de uso             | Necesidades principales                                                   |
| --------------------- | ------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- | ----------------------------- | ------------------------------------------------------------------------- |
| **Lector invitado**   | Visitante sin cuenta que explora proyectos públicos.                                  | Baja.                                                                              | Ocasional.                    | Descubrir y previsualizar historias.                                      |
| **Lector registrado** | Usuario autenticado que se une como _Reader_ y consume proyectos en escritorio o web. | Baja–Media.                                                                        | Semanal.                      | Leer capítulos completos y recibir actualizaciones.                       |
| **Escritor**          | Autor con cuenta que crea proyectos (_Owner_) o colabora en los de otros (_Editor_).  | Media–Alta — familiarizado con procesadores de texto y herramientas colaborativas. | Diaria durante fases activas. | Editar capítulos, gestionar wiki, mapear relaciones, coordinarse por voz. |
| **Administrador**     | Operador de la plataforma con permisos elevados.                                      | Alta.                                                                              | Según incidencia.             | Gestionar usuarios, banear cuentas, generar reportes.                     |

### 6.3 Casos de uso (análisis)

La siguiente tabla lista los 15 casos de uso identificados, agrupados por módulo, con su actor, _backend_ responsable y estado actual de implementación.

| ID                         | Nombre                               | Actor              | Backend       | Estado |
| -------------------------- | ------------------------------------ | ------------------ | ------------- | ------ |
| **Descubrimiento público** |                                      |                    |               |        |
| CU-01                      | Explorar catálogo público            | Cualquiera         | server-core   | ✅     |
| CU-02                      | Previsualizar sinopsis               | Cualquiera         | server-core   | ✅     |
| CU-13                      | Leer historia completa               | Lector             | worldbuilding | ✅     |
| **Cuenta de usuario**      |                                      |                    |               |        |
| CU-03                      | Login / Registro                     | Usuario            | server-core   | ✅     |
| CU-04                      | Gestionar perfil                     | Usuario            | server-core   | ✅     |
| CU-15                      | Administrar usuarios (_ban_ / roles) | Administrador      | server-core   | ✅     |
| **Gestión de proyectos**   |                                      |                    |               |        |
| CU-05                      | Crear proyecto                       | Escritor           | server-core   | ✅     |
| CU-06                      | Gestionar colaboradores              | Escritor (_Owner_) | server-core   | ✅     |
| CU-07                      | Configurar privacidad                | Escritor (_Owner_) | server-core   | ✅     |
| **Creación colaborativa**  |                                      |                    |               |        |
| CU-08                      | Editar manuscrito                    | Editor / Escritor  | worldbuilding | ✅     |
| CU-09                      | Gestionar wiki (nodos)               | Editor / Escritor  | worldbuilding | ✅     |
| CU-10                      | Visualizar grafo narrativo           | Lector / Editor    | worldbuilding | ✅     |
| CU-11                      | Sesión de voz (hablar)               | Escritor           | server-core   | ✅     |
| CU-12                      | Unirse como oyente                   | Lector             | server-core   | ✅     |
| CU-14                      | Reportes del sistema                 | Administrador      | server-core   | ✅     |

_Leyenda_: ✅ implementado · 🔧 parcial · ❌ pendiente.

#### Detalle de los casos de uso clave

A modo de muestra se detallan los cinco CU más representativos. El resto sigue el mismo formato y queda expandido en el anexo técnico del proyecto.

**CU-03 · Login / Registro**

- **Actor**: Lector registrado o Escritor.
- **Precondiciones**: el usuario dispone de email y contraseña (o crea una cuenta nueva).
- **Flujo básico**: (1) el cliente envía credenciales a `POST /api/tokens` (o registra con `POST /api/users`). (2) El servidor valida, emite un JWT de 24 h con `TokenVersion` y lo devuelve. (3) El cliente almacena el token y lo adjunta en cabecera `Authorization` para llamadas posteriores.
- **Flujo alternativo**: credenciales inválidas → 401 `InvalidCredentials`; email duplicado en registro → 409 `DuplicateEmail`.
- **Postcondiciones**: sesión activa con JWT válido.

**CU-05 · Crear proyecto**

- **Actor**: Escritor.
- **Precondiciones**: sesión válida.
- **Flujo básico**: (1) `POST /api/projects` con `{title, synopsis, literaryGenre, coverImageUrl, isPublic}`. (2) Se persiste en SQL Server, el creador queda como `Owner`. (3) Tras el _commit_ se publica `project.created` en RabbitMQ (Outbox Pattern). (4) `server-worldbuilding` consume el evento y crea las colecciones MongoDB y los nodos Neo4j iniciales.
- **Postcondiciones**: proyecto disponible con manuscritos y wiki vacíos listos para editar.

**CU-08 · Editar manuscrito**

- **Actor**: Escritor / Editor.
- **Precondiciones**: el usuario tiene rol `Owner` o `Editor` en el proyecto.
- **Flujo básico**: (1) el cliente obtiene el capítulo con `GET /api/manuscripts/{p}/{m}/chapters/{c}`. (2) El usuario edita el contenido RTF. (3) El cliente envía `PUT` con el contenido actualizado. (4) El servidor aplica estrategia _Last-Write-Wins_.
- **Flujo alternativo**: rol insuficiente → 403 `Forbidden`.
- **Postcondiciones**: capítulo actualizado; el sistema conserva historial de versiones y permite revisar/restaurar snapshots según el cliente.

**CU-11 / CU-12 · Sesión de voz**

- **Actores**: Escritor (habla) / Lector (escucha).
- **Precondiciones**: proyecto existente; usuario con rol válido.
- **Flujo básico**: (1) cliente se conecta al _hub_ SignalR `/hubs/voice`. (2) Invoca `JoinRoom(projectId)`. (3) Al pulsar PTT envía paquetes de audio; el _hub_ retransmite al resto de la sala.
- **Postcondiciones**: todos los miembros conectados reciben el audio en tiempo real mientras PTT esté activo.

**CU-15 · Administrar usuarios**

- **Actor**: Administrador.
- **Flujo básico**: listar (`GET /api/users`), banear (`POST /api/users/{id}/ban`) → invalida la sesión vía incremento de `TokenVersion`. El middleware `TokenVersionValidator` rechaza peticiones con token obsoleto.

### 6.4 Prototipo de UI (baja fidelidad del cliente de escritorio)

Los prototipos de baja fidelidad del cliente de escritorio se entregan como **anexo independiente** junto con este documento. Cubren las 12 vistas principales: `Login`, `SignUp`, `ProjectList`, `PublicProjects`, `Workspace`, `ManuscriptEditor`, `WikiEntityEditor`, `NarrativeGraph`, `VoicePanel`, `ReaderWorkspace` y `Settings`.

### 6.5 Requisitos funcionales

Los requisitos funcionales se listan **por módulo**, con detalle fino en los módulos de prioridad **Alta** y agrupados para los de prioridad **Media/Baja**, conforme a la decisión del equipo.

#### Módulo A — Autenticación y cuenta (Prioridad: Alta)

| ID     | Descripción                                                                                                                        | CU           | Prioridad |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------- | ------------ | --------- |
| RF-A01 | El sistema permitirá a un usuario registrarse proporcionando email, contraseña y nombre para mostrar (`DisplayName`).              | CU-03        | Alta      |
| RF-A02 | El sistema permitirá autenticar usuarios con email y contraseña, devolviendo un JWT firmado válido por 24 h con su `TokenVersion`. | CU-03        | Alta      |
| RF-A03 | El sistema permitirá al usuario consultar su perfil (`GET /api/users/{id}`) y actualizar `DisplayName` y `Bio` (`PUT`).            | CU-04        | Alta      |
| RF-A04 | El sistema permitirá al usuario eliminar su propia cuenta (`DELETE /api/users/{id}` con permiso _Self_).                           | CU-04        | Alta      |
| RF-A05 | El sistema invalidará de inmediato las sesiones activas cuando se incremente `TokenVersion` (cambio de contraseña o baneo).        | CU-03, CU-15 | Alta      |

#### Módulo B — Gestión de proyectos (Prioridad: Alta)

| ID     | Descripción                                                                                                                                                               | CU           | Prioridad |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------ | --------- |
| RF-B01 | El sistema permitirá crear un proyecto con título, sinopsis, género literario, imagen de portada y visibilidad pública/privada. El creador queda como `Owner`.            | CU-05, CU-07 | Alta      |
| RF-B02 | El sistema permitirá al `Owner` actualizar los metadatos del proyecto (`PUT /api/projects/{id}`).                                                                         | CU-05, CU-07 | Alta      |
| RF-B03 | El sistema permitirá al `Owner` eliminar el proyecto (`DELETE /api/projects/{id}`), propagando el borrado a MongoDB y Neo4j.                                              | CU-05        | Alta      |
| RF-B04 | El sistema publicará el evento `project.created` en RabbitMQ tras el _commit_ transaccional de SQL Server (Outbox Pattern).                                               | CU-05        | Alta      |
| RF-B05 | El sistema listará los proyectos del usuario, los proyectos públicos (`GET /api/projects/public`) y, para administradores, todos los proyectos (`GET /api/projects/all`). | CU-01, CU-02 | Alta      |
| RF-B06 | El sistema permitirá a un usuario unirse a un proyecto público como `Reader` (`POST /api/projects/{id}/join`).                                                            | CU-02        | Alta      |

#### Módulo C — Colaboradores (Prioridad: Alta)

| ID     | Descripción                                                                                                              | CU           | Prioridad |
| ------ | ------------------------------------------------------------------------------------------------------------------------ | ------------ | --------- |
| RF-C01 | El `Owner` podrá invitar colaboradores por email asignando un rol (`Editor` o `Reader`).                                 | CU-06        | Alta      |
| RF-C02 | Cualquier miembro podrá listar los colaboradores del proyecto.                                                           | CU-06        | Alta      |
| RF-C03 | El `Owner` podrá remover colaboradores (`DELETE /api/projects/{id}/collaborators/{userId}`).                             | CU-06        | Alta      |
| RF-C04 | Las llamadas a endpoints protegidos validarán el rol del solicitante mediante `ProjectRoles.IsValid` y la política RBAC. | CU-06, CU-07 | Alta      |

#### Módulo D — Manuscritos y capítulos (Prioridad: Alta)

| ID     | Descripción                                                                                                                                                | CU    | Prioridad |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | --------- |
| RF-D01 | El sistema permitirá crear, listar, renombrar/reordenar y eliminar manuscritos dentro de un proyecto.                                                      | CU-08 | Alta      |
| RF-D02 | El sistema permitirá crear, consultar, editar y eliminar capítulos con contenido RTF dentro de un manuscrito.                                              | CU-08 | Alta      |
| RF-D03 | Al consultar un manuscrito sin especificar capítulo, el sistema devolverá un **índice** (sin contenido) para reducir transferencia.                        | CU-08 | Alta      |
| RF-D04 | La edición de capítulos aplicará la estrategia _Last-Write-Wins_ y registrará versiones/autoguardados consultables desde los clientes de escritorio y web. | CU-08 | Alta      |

#### Módulo E — Wiki del universo (Prioridad: Alta)

| ID     | Descripción                                                                                                                                                       | CU    | Prioridad |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | --------- |
| RF-E01 | El sistema permitirá CRUD completo de entradas de wiki (`/api/wiki/{projectId}`), con campos de tipo (personaje, lugar, objeto, evento), descripción y atributos. | CU-09 | Alta      |
| RF-E02 | Los clientes de escritorio y web permitirán crear, editar, filtrar y consultar entradas de wiki, incluyendo etiquetas y apariciones en capítulos.                 | CU-09 | Alta      |

#### Módulo F — Grafo narrativo (Prioridad: Media)

| ID     | Descripción                                                                                                                                                                                         | CU    | Prioridad |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | --------- |
| RF-F01 | El sistema permitirá consultar el grafo del proyecto (nodos + aristas) y crear/eliminar relaciones tipadas mediante `/api/graph/{projectId}`. La visualización está disponible en escritorio y web. | CU-10 | Media     |

#### Módulo G — Voz y presencia en tiempo real (Prioridad: Media)

| ID     | Descripción                                                                                                                                                                     | CU           | Prioridad |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------ | --------- |
| RF-G01 | El sistema proporcionará un _hub_ SignalR `/hubs/voice` que permita unirse a una sala por proyecto, transmitir audio _push-to-talk_ y recibir audio del resto de participantes. | CU-11, CU-12 | Media     |
| RF-G02 | El sistema proporcionará un _hub_ SignalR `/hubs/presence` que notifique a la sala cuando un usuario se conecta o desconecta.                                                   | CU-11, CU-12 | Media     |

#### Módulo H — Administración y reportes (Prioridad: Media/Baja)

| ID     | Descripción                                                                                                                                           | CU    | Prioridad |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------- | ----- | --------- |
| RF-H01 | El sistema permitirá a un administrador listar usuarios y banear cuentas (bloquea el acceso e invalida sesiones).                                     | CU-15 | Media     |
| RF-H02 | El sistema proporcionará reportes agregados de usuarios y proyectos (`/api/admin/reports/system`) accesibles por administradores desde web y Android. | CU-14 | Baja      |

### 6.6 Requisitos no funcionales

Clasificados según **ISO/IEC 25010**.

| Categoría             | ID     | Requisito                                                                                                                                               |
| --------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Rendimiento**       | RNF-01 | La latencia p95 de endpoints CRUD deberá ser < 300 ms bajo carga nominal.                                                                               |
|                       | RNF-02 | La edición de capítulos no mantendrá _locks_ prolongados — se adopta _Last-Write-Wins_ como _trade-off_ explícito.                                      |
| **Disponibilidad**    | RNF-03 | Los servicios expondrán _healthchecks_ (`/health`, `/api/health`) consumibles por Docker y orquestadores.                                               |
|                       | RNF-04 | Objetivo de disponibilidad del 99 % mensual en entorno productivo.                                                                                      |
| **Seguridad**         | RNF-05 | Autenticación por JWT firmado, con `TokenVersion` que permita invalidación inmediata.                                                                   |
|                       | RNF-06 | Validación _fail-fast_ en arranque: `JWT_SECRET` y `JWT_SECRET_REFRESH` deben tener al menos 32 caracteres.                                             |
|                       | RNF-07 | _Rate limiting_ por IP en endpoints de autenticación.                                                                                                   |
|                       | RNF-08 | CORS restringido a orígenes autorizados (`WORLDBUILDING_ALLOWED_ORIGINS`).                                                                              |
|                       | RNF-09 | RBAC en todos los endpoints de proyecto (`Owner`, `Editor`, `Reader`).                                                                                  |
| **Mantenibilidad**    | RNF-10 | `server-core` sigue arquitectura limpia: `Layla.Api` → `Layla.Core` → `Layla.Infrastructure`.                                                           |
|                       | RNF-11 | `server-worldbuilding` usa alias de ruta `@/` para imports estables.                                                                                    |
|                       | RNF-12 | Todos los errores se modelarán con la enum tipada `ErrorCode` (27 códigos), mapeada a HTTP status por `ApiControllerBase.RespondWithError(ErrorCode?)`. |
| **Interoperabilidad** | RNF-13 | Ambos _backends_ expondrán Swagger/OpenAPI (`/swagger` y `/api-docs`).                                                                                  |
|                       | RNF-14 | Todos los intercambios serán JSON UTF-8.                                                                                                                |
| **Escalabilidad**     | RNF-15 | Los servicios estarán desacoplados por RabbitMQ; se adopta el patrón _Outbox_ para garantizar consistencia eventual.                                    |
|                       | RNF-16 | `server-worldbuilding` implementará _graceful shutdown_ sobre `SIGTERM`/`SIGINT`, cerrando ordenadamente HTTP, RabbitMQ y Neo4j.                        |
| **Usabilidad**        | RNF-17 | El cliente de escritorio usará Material Design / FluentWPF para una experiencia visual coherente con Windows 11.                                        |
|                       | RNF-18 | El cliente de escritorio funcionará con caché local y sincronización diferida cuando la red no esté disponible.                                         |
| **Portabilidad**      | RNF-19 | Todos los servicios se distribuirán como imágenes Docker basadas en Debian Slim, orquestadas por `docker-compose`.                                      |

#### Métricas para medir éxito

Las métricas principales combinan los reportes ya implementados en CU-14 con métricas de producto que se instrumentarán conforme evolucione worldbuilding:

- **DAU de escritores** y **WAU de lectores**.
- **Capítulos editados por semana** y **palabras escritas por sesión**.
- **Sesiones de voz activas por semana** y **duración media**.
- **Tasa de colaboraciones aceptadas** tras invitación.
- **Latencia p50/p95** de endpoints críticos.
- **Tasa de error RabbitMQ** y **retraso de consumo** (lag entre `project.created` publicado y manuscrito/wiki creados).

#### Recursos necesarios

- **Infraestructura**: contenedores Docker para SQL Server, MongoDB, Neo4j, RabbitMQ y ambos _backends_.
- **Cuentas de servicio**: credenciales administrativas de cada base de datos y usuario AMQP con permisos sobre el _exchange_ `worldbuilding.events`.
- **Secretos**: `JWT_SECRET`, `JWT_SECRET_REFRESH`, credenciales de SMTP para invitaciones por email (opcional en MVP).
- **Entornos de desarrollo**: .NET 10 SDK, Node.js 22 + pnpm 10, Android Studio con JDK 17.
- **Equipo humano**: 4 desarrolladores (escritorio, web, móvil, backend) y coordinación del _Owner_ del proyecto académico.

### 6.7 Restricciones

#### Restricciones académicas (impuestas por el curso)

- Se requieren **3 clientes** diferenciados: escritorio, web y móvil. La diferenciación actual es de superficie: escritorio y web cubren autoría completa; Android cubre administración y estadísticas.
- Se requieren **3 bases de datos** con modelos diferentes: relacional, documental y grafo.
- Se requieren **≥ 2 backends**, uno en .NET y otro en un _stack_ distinto, comunicados por un _broker_.
- La documentación de entrega se produce sobre la plantilla oficial del equipo.

#### Restricciones técnicas

- _Stack_ fijado: .NET 10, Node.js 22 + Express 5, SQL Server, MongoDB 7, Neo4j 5, RabbitMQ 3.
- El JWT caduca a las **24 h** y requiere _re-login_ (sin _refresh token_ automático en MVP).
- Puertos reservados: `5288` (HTTPS core), `5287` (HTTP core), `3000` (worldbuilding).
- Los identificadores de entidad son GUID en `server-core` y `ObjectId`/`uuid` en `server-worldbuilding`.

#### Restricciones de negocio

- Plazos académicos estrictos (calendario Eminus) — sin margen para _re-scoping_.
- Equipo pequeño y sin presupuesto para servicios _cloud_ de pago → despliegue local y _self-hosted_.
- El producto debe poder demostrarse _offline_ (evaluaciones presenciales).

#### Restricciones regulatorias y de propiedad intelectual

- Los manuscritos son **propiedad intelectual del autor**; el sistema nunca los redistribuye fuera del proyecto sin consentimiento.
- Se ofrecerá **derecho al olvido** (GDPR-_like_): al eliminar una cuenta se borran o anonimizan los datos personales.
- Los proyectos privados quedan estrictamente cerrados al `Owner` y sus colaboradores.

#### Riesgos (¿qué podría salir mal?)

| Riesgo                                                                                 | Mitigación                                                                                               |
| -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Pérdida de eventos entre `server-core` y `server-worldbuilding` por caída de RabbitMQ. | Outbox Pattern (publicación tras _commit_); reintentos idempotentes en el consumidor.                    |
| Conflicto de edición simultánea en un capítulo.                                        | Estrategia _Last-Write-Wins_ aceptada; en fases posteriores se evaluará CRDT u _operational transforms_. |
| Grafos grandes en Neo4j degradan la visualización.                                     | Paginación de aristas; _layout_ perezoso en el cliente.                                                  |
| Latencia de voz en redes móviles.                                                      | Paquetes de audio pequeños + _backoff_; degradar a chat si PTT no es viable.                             |
| Filtrado de contenido inapropiado en salas de voz.                                     | Moderación reactiva (_mute_ / baneo por `Owner` o Administrador); política de uso.                       |
| Token JWT filtrado.                                                                    | `TokenVersion` permite invalidación inmediata; caducidad de 24 h acota la ventana.                       |
| Mal comportamiento de un `Owner` (expulsar a colaboradores masivamente).               | Registro de auditoría; CU-14 (reportes) como canal de revisión.                                          |

---

## Anexo — Mapa de preguntas de la pauta

| Pregunta de la guía                                    | Sección(es) que la responden                                    |
| ------------------------------------------------------ | --------------------------------------------------------------- |
| ¿Cuál es el problema que el proyecto está resolviendo? | §6.1 — _Problema que resuelve Layla_.                           |
| ¿Cuál es el valor del negocio?                         | §6.1 — _Valor de negocio_.                                      |
| ¿Cuáles son las métricas y los casos de uso?           | §6.3 — Casos de uso y §6.6 — _Métricas para medir éxito_.       |
| ¿Qué recursos se necesitan disponibles?                | §6.6 — _Recursos necesarios_ y §6.7 — _Restricciones técnicas_. |
| ¿Qué podría salir mal?                                 | §6.7 — _Riesgos_.                                               |

---

_Fin del documento — Especificación Funcional AR02, Equipo Layla._
