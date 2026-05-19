# Modo 2 — Despliegue con contenedores en VMs

> **Objetivo didáctico**: mostrar cómo Docker resuelve los problemas de runtime, configuración y reproducibilidad del modo manual, **manteniendo la misma topología** de 3 VMs.

**Tiempo total estimado**: 30 minutos.
**Comandos por VM**: 3 (instalar Docker, copiar compose, `docker compose up -d`).
**Reproducibilidad**: alta — la misma imagen produce el mismo contenedor en cualquier máquina.

---

## Pre-requisitos

Tener **las 3 VMs ya creadas** con Ubuntu 22.04 + SSH + IP fija (igual que en el [modo 1, paso 1-2](01-manual.md#paso-1--crear-3-máquinas-virtuales-en-virtualbox)).

> En la demo real, una manera de hacer el modo 2 sobre la marcha es **restaurar el snapshot `base-clean`** de las VMs que ya tenés provisionadas.

---

## Paso 1 — Instalar Docker en cada VM (`data`, `apps`, `edge`)

Por SSH en cada una:
```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker
docker --version
```

## Paso 2 — Copiar los archivos compose y el `.env`

Desde el host (Windows con Bitvise SFTP), subir:

| VM | Archivo origen | Destino |
|----|----------------|---------|
| `layla-data` | `deploy/files/compose/compose.data.yml` | `~/compose.yml` |
| `layla-apps` | `deploy/files/compose/compose.apps.yml` | `~/compose.yml` |
| `layla-edge` | `deploy/files/compose/compose.edge.yml` | `~/compose.yml` |
| (las 3) | `deploy/files/env/.env.shared` | `~/.env` |

Para que `layla-apps` y `layla-edge` puedan **buildear las imágenes**, también hay que subir `src/` a un path conocido (por ej. `/home/layla/repo/src/`) o ajustar los `build.context` en los compose para que apunten a una ruta que exista en la VM.

> En el modo 3 (Vagrant) esto se resuelve solo con el `synced_folder`.

## Paso 3 — Levantar cada stack

**En `layla-data`** (primero — las apps dependen de las DBs):
```bash
docker compose -f ~/compose.yml --env-file ~/.env up -d
```
Resultado: 4 contenedores corriendo (SQL Server, MongoDB, Neo4j, RabbitMQ).
Verificar: `docker ps` — todos en `(healthy)` tras ~30-60 seg.

**En `layla-apps`**:
```bash
docker compose -f ~/compose.yml --env-file ~/.env up -d
```
La primera vez **buildea 3 imágenes** desde `src/` (.NET multi-stage + Node TypeScript). Tarda 10-15 min. Luego usa cache.
Resultado: `server-core`, `layla-worldbuilding`, `layla-web`.

**En `layla-edge`**:
```bash
docker compose -f ~/compose.yml --env-file ~/.env up -d
```
Resultado: `layla-api-gateway` (YARP) escuchando en `:5000`.

## Paso 4 — Verificación end-to-end

Desde el host Windows:
```powershell
curl http://192.168.56.12:5000/health
curl http://192.168.56.12:5000/api/projects/public
```

Esperado: `HTTP 200`, body `[]`.

---

## Archivos clave

### `compose.data.yml` (extracto)
```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SQL_PASSWORD}
    ports: ["1433:1433"]
    volumes: [sql_data:/var/opt/mssql]
    healthcheck: { ... }
    restart: unless-stopped

  mongodb:
    image: mongo:4.4
    # ...

  neo4j:
    image: neo4j:5
    # ...

  rabbitmq:
    image: rabbitmq:3-management
    # ...
```

**Lo que Docker resuelve aquí**:
- No hay que añadir repos APT de Microsoft, MongoDB Inc., Neo4j ni RabbitMQ.
- No hay configuración interactiva (`mssql-conf setup`, cambio de password de Neo4j, etc.).
- `restart: unless-stopped` da self-healing **gratis** (el contenedor se reinicia solo si crashea).
- Las versiones quedan **fijas en el archivo**: `mongo:4.4`, `neo4j:5`. Reproducible en cualquier máquina con Docker.

### `compose.apps.yml` (extracto)
```yaml
services:
  server-core:
    build:
      context: /home/layla/repo/src/server-core
      dockerfile: Layla.Api/Dockerfile
    environment:
      - DatabaseConfigs__SQL__ConnectionString=Server=192.168.56.10,1433;...
      - RabbitMQ__HostName=192.168.56.10
      # ...
    restart: unless-stopped
```

**Cómo se conecta a las DBs de otra VM**: las env vars apuntan a `192.168.56.10` (IP de `layla-data` en la red privada de VirtualBox). No hay nombres DNS internos a Docker porque cada compose vive en su propia VM/red.

---

## Comparativa rápida con Modo 1

| Aspecto | Modo 1 (manual) | Modo 2 (Docker) |
|---------|-----------------|-----------------|
| Comandos para instalar SQL Server | 6 (apt-key, repo, install, conf setup, systemctl, sqlcmd) | 0 (la imagen ya viene preparada) |
| Tiempo de provisión por VM | ~2 h | ~5 min |
| Self-healing | systemd unit con `Restart=always` (manual) | `restart: unless-stopped` (declarativo) |
| Versiones fijas | depende de qué versión esté en el repo APT al momento | fijas en `image: mongo:4.4` |
| Configuración interactiva | sí (mssql-conf, neo4j password) | no (env vars + ACCEPT_EULA=Y) |
| Si tu compañero quiere replicar | manual de 60 pasos | copiar 3 archivos + `docker compose up` |

---

## Comandos útiles durante la demo

```bash
docker ps                                # contenedores corriendo
docker ps -a                             # incluyendo los muertos
docker logs server-core --tail 30        # logs de un servicio
docker logs server-core -f               # logs en streaming
docker exec -it server-core bash         # shell dentro del contenedor
docker compose ps                        # estado del stack
docker compose restart server-core       # reiniciar un servicio
docker compose down                      # apagar y borrar contenedores
docker compose down -v                   # ... y borrar volúmenes (DBs!)
docker stats --no-stream                 # uso de CPU/RAM por contenedor
```

---

## Lo que **NO** resuelve Docker

- **Crear las VMs**: seguís teniendo que clonar e instalar Ubuntu a mano.
- **Configurar red y SSH**: idem, manual.
- **Onboarding de nuevos miembros del equipo**: tienen que crear sus VMs antes de poder usar Docker.

> **Conclusión narrativa**: Docker pasa de "60 comandos por VM" a "3 comandos por VM". Pero seguimos atados a crear las 3 VMs manualmente. La siguiente etapa es **automatizar también la creación de las VMs**. Eso lo logra **Vagrant** — y para aplicar la configuración Docker dentro de cada VM, usamos **Puppet**.
