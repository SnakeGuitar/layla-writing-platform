# Diagramas — arquitectura, despliegue, casos de uso y flujos

Diagramas de Layla en Mermaid. Cada uno tiene su fuente `.mmd` y su render `.png`.

## Diagramas de secuencia (flujos)

| # | Flujo | CU | Fuente | Imagen |
|---|-------|----|--------|--------|
| 01 | Inicio de sesión (JWT + TokenVersion) | CU-03 | [01_autenticacion_login.mmd](01_autenticacion_login.mmd) | [PNG](01_autenticacion_login.png) |
| 02 | Crear proyecto + bootstrap (outbox-after-commit, RabbitMQ → Mongo/Neo4j) | CU-05 | [02_crear_proyecto_outbox.mmd](02_crear_proyecto_outbox.mmd) | [PNG](02_crear_proyecto_outbox.png) |
| 03 | Edición colaborativa de manuscrito en tiempo real (ManuscriptHub) | CU-08 | [03_edicion_colaborativa_tiempo_real.mmd](03_edicion_colaborativa_tiempo_real.mmd) | [PNG](03_edicion_colaborativa_tiempo_real.png) |
| 04 | Autorización en worldbuilding (ProjectGuard: Neo4j → fallback server-core) | — | [04_projectguard_autorizacion.mmd](04_projectguard_autorizacion.mmd) | [PNG](04_projectguard_autorizacion.png) |
| 05 | Sala de voz (VoiceHub / PresenceHub) | CU-11 / CU-12 | [05_sala_de_voz.mmd](05_sala_de_voz.mmd) | [PNG](05_sala_de_voz.png) |

## Arquitectura, despliegue y casos de uso

| # | Diagrama | Fuente | Imagen |
|---|----------|--------|--------|
| 00 | Diagrama de contexto (actores, canales cliente, backends, datos y bus) | [00_diagrama_contexto.mmd](00_diagrama_contexto.mmd) | [PNG](00_diagrama_contexto.png) |
| 06 | Arquitectura lógica (clientes · gateway · 2 backends · 3 BD · bus) | [06_arquitectura_logica.mmd](06_arquitectura_logica.mmd) | [PNG](06_arquitectura_logica.png) |
| 07 | Despliegue Vagrant (3 VMs: data · apps · edge) | [07_despliegue_vagrant.mmd](07_despliegue_vagrant.mmd) | [PNG](07_despliegue_vagrant.png) |
| 08 | Casos de uso (15 CU · Lector / Autor / Administrador) | [08_casos_de_uso.mmd](08_casos_de_uso.mmd) | [PNG](08_casos_de_uso.png) |

## Máquinas de estado

| # | Máquina de estado | Fuente | Imagen |
|---|-------------------|--------|--------|
| 09 | Conexión SignalR del cliente (Disconnected → Connecting → Connected → Reconnecting / Evicted) | [09_estado_conexion_signalr.mmd](09_estado_conexion_signalr.mmd) | [PNG](09_estado_conexion_signalr.png) |
| 10 | Sesión / JWT (Anónimo → Autenticado → Expirada / Invalidada por TokenVersion) | [10_estado_sesion_jwt.mmd](10_estado_sesion_jwt.mmd) | [PNG](10_estado_sesion_jwt.png) |
| 11 | Capítulo en el editor (Cargando → Sincronizado ↔ Editando → Guardando · debounces · RTF remoto) | [11_estado_editor_capitulo.mmd](11_estado_editor_capitulo.mmd) | [PNG](11_estado_editor_capitulo.png) |
| 12 | Participante en sala de voz (FueraDeSala → Uniéndose → EnSala: Escuchando/Hablando) | [12_estado_participante_voz.mmd](12_estado_participante_voz.mmd) | [PNG](12_estado_participante_voz.png) |
| 13 | Ciclo de vida del proyecto (Creando → Persistido → Bootstrapping → Listo: Privado/Público) | [13_estado_proyecto.mmd](13_estado_proyecto.mmd) | [PNG](13_estado_proyecto.png) |

## Flujos de vistas

| # | Diagrama | Fuente | Imagen |
|---|----------|--------|--------|
| 14 | Flujo de vistas por cliente (Desktop WPF, Web Blazor Server y Android Compose) | [14_flujo_vistas_clientes.mmd](14_flujo_vistas_clientes.mmd) | [PNG](14_flujo_vistas_clientes.png) |

## Regenerar las imágenes

```bash
cd docs/architecture/sequence-diagrams
for f in *.mmd; do
  npx -y -p @mermaid-js/mermaid-cli mmdc -i "$f" -o "${f%.mmd}.png" -t neutral -b white -s 2
done
```

> Nota de sintaxis Mermaid: en el texto de notas/mensajes evita `;` (separador de sentencias) y `{ }` (rompen el parser de `sequenceDiagram`). Usa `,` y `( )`.
