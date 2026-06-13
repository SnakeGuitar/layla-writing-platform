# Layla — CI/CD con GitHub Actions

Equivalente del pipeline de Jenkins (`deploy/jenkins/`) pero con GitHub Actions.
El despliegue se hace **en automático sobre un servidor Linux local** usando un
**self-hosted runner** instalado en esa máquina.

| Workflow | Archivo | Runner | Disparadores |
| --- | --- | --- | --- |
| CI | [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) | GitHub-hosted (`ubuntu-latest`) | push y PR a `master`, manual |
| Deploy | [`.github/workflows/deploy.yml`](../../.github/workflows/deploy.yml) | **Self-hosted** (`[self-hosted, linux, layla]`) | push a `master`, manual |

## Por qué un self-hosted runner

Los runners de GitHub viven en la nube y **no pueden alcanzar** una máquina de tu
red local. Para desplegar en un servidor Linux local, se registra en él un runner
self-hosted: el runner hace *polling* saliente a GitHub (no necesita puertos
entrantes), clona el repo y ejecuta Docker Compose **localmente**. Por eso el host
del runner *es* el servidor Linux local del despliegue. Es el mismo rol que cumple
el agente local de Jenkins en el pipeline original.

## Qué hace cada workflow

### CI (`ci.yml`) — equivale a Restore + Unit Tests + Build + Docker Images
Jobs en paralelo sobre `ubuntu-latest`:

- `server-core` — restore, pruebas (`Layla.Core.Tests`) y build (.NET 10).
- `api-gateway` — restore y build (.NET 9).
- `client-web` — restore, build del frontend (Tailwind + TS con pnpm), pruebas
  (`client-web.Tests`) y build (.NET 9).
- `server-worldbuilding` — `pnpm install`, `pnpm test` (vitest) y `pnpm build` (Node 22).
- `docker-images` — construye las 4 imágenes Docker para validar los Dockerfiles
  (depende de que pasen los jobs anteriores).

### Deploy (`deploy.yml`) — equivale al stage Deploy + Smoke Test
Sobre el self-hosted runner Linux:

1. Checkout del repo.
2. Genera `deploy/docker/.env` con
   [`generate-env.sh`](generate-env.sh) (secrets de GitHub o passwords demo fuertes).
3. `docker compose build` (omitible en ejecución manual con `rebuild=false`).
4. `docker compose up -d --remove-orphans` con el Compose de
   [`deploy/docker/docker-compose.yml`](../docker/docker-compose.yml).
5. Smoke test con reintentos contra `http://localhost:5000/health`.
6. `docker compose ps`; si algo falla, vuelca los logs.
7. Borra el `.env` generado para no dejar secretos en disco.

Esto levanta los **8 servicios** (SQL Server, MongoDB, Neo4j, RabbitMQ,
server-core, server-worldbuilding, api-gateway, client-web) en un solo servidor
Linux. Como todo corre en la misma red de Compose, los hosts se resuelven por
nombre de servicio (`SQL_SERVER=sqlserver`, `RABBIT_HostName=rabbitmq`), no por las
IPs de las VMs que usa la ruta de Vagrant.

## Requisitos del servidor Linux

- Linux x64 (Ubuntu 22.04+ recomendado).
- Docker Engine + plugin Compose v2 (`docker compose version`).
- El usuario del runner debe pertenecer al grupo `docker`.
- `git` y `curl`.
- Puertos libres según el Compose (gateway en `5000`, web en `5288/5289`, etc.).

> MongoDB 7 requiere AVX. En una VM con CPU restringida puede fallar con SIGILL;
> usa un host con AVX o ajusta la imagen como en la ruta de Vagrant.

## Registrar el runner en el servidor Linux

En GitHub: **Settings → Actions → Runners → New self-hosted runner**, elige Linux y
sigue los comandos que muestra (incluyen tu token). En el servidor:

```bash
mkdir -p ~/actions-runner && cd ~/actions-runner
curl -o actions-runner-linux-x64.tar.gz -L \
  https://github.com/actions/runner/releases/latest/download/actions-runner-linux-x64-<version>.tar.gz
tar xzf actions-runner-linux-x64.tar.gz

# Usa el token que te da la UI. Las labels DEBEN incluir: linux,layla
./config.sh \
  --url https://github.com/SnakeGuitar/layla-writing-platform \
  --token <RUNNER_TOKEN> \
  --labels linux,layla \
  --name layla-local-linux

# Instálalo como servicio para que arranque solo
sudo ./svc.sh install
sudo ./svc.sh start
```

Las etiquetas `self-hosted` y `linux` se agregan solas; basta con añadir `layla`.
El job de deploy selecciona el runner por `runs-on: [self-hosted, linux, layla]`.

## Secrets de GitHub

Define estos secrets en **Settings → Secrets and variables → Actions** para que el
servidor use credenciales estables. Si alguno falta, el script genera un valor demo
fuerte (no productivo):

| Secret | Uso |
| --- | --- |
| `SQL_PASSWORD` | Password de `sa` en SQL Server |
| `MONGO_INITDB_ROOT_PASSWORD` | Password root de MongoDB |
| `NEO4J_PASSWORD` | Password de Neo4j |
| `RABBIT_PASSWORD` | Password de RabbitMQ |
| `JWT_SECRET` | Secreto de firma JWT (server-core y worldbuilding) |
| `JWT_SECRET_REFRESH` | Secreto de refresh JWT |
| `EMAIL_HOST` … `EMAIL_FROM_EMAIL` | SMTP (opcional; demo por defecto) |

> **Importante para un servidor persistente:** define al menos los secrets de
> passwords. Si dejas que se autogeneren, cada despliegue crea passwords nuevos y
> dejarán de coincidir con los volúmenes de datos ya creados (SQL/Mongo/Neo4j/Rabbit
> fallarían la autenticación). Es el mismo comportamiento que la generación
> aleatoria del Jenkinsfile.

## Ejecución

- **Automática:** cada push a `master` dispara CI y, en paralelo, el deploy en el
  servidor local.
- **Manual:** pestaña **Actions → Deploy (local Linux server) → Run workflow**.
  Parámetros:
  - `gateway_url` — URL base para el smoke test (vacío usa `http://localhost:5000`).
  - `rebuild` — reconstruir imágenes antes de levantar (por defecto `true`).

## Verificación desde el servidor

```bash
docker compose -f deploy/docker/docker-compose.yml ps
curl -fsS http://localhost:5000/health
```

## Evidencia

La carpeta [`evidence/`](evidence/) contiene logs y capturas que demuestran que
las etapas del pipeline se ejecutan correctamente (preflight, pruebas, builds y
validacion del Compose y los workflows). Ver [evidence/README.md](evidence/README.md).

## Troubleshooting

- **El job de deploy queda en cola ("Waiting for a runner"):** el runner no está
  online o le faltan las labels `linux,layla`. Revisa `sudo ./svc.sh status`.
- **`permission denied` al hablar con Docker:** agrega el usuario del runner al
  grupo `docker` (`sudo usermod -aG docker $USER`) y reinicia el servicio del runner.
- **El smoke test agota los reintentos:** revisa los logs que vuelca el paso de
  fallo; lo más común es SQL Server tardando en arrancar o un password que cambió
  respecto a un volumen previo (`docker compose down -v` para empezar limpio).
- **Puerto 5000 ocupado:** detén lo que lo use o cambia el mapeo en el Compose.
