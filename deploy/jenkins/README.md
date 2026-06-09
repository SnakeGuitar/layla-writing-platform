# Layla - Jenkins CI/CD Pipeline

Este directorio documenta el pipeline de `Jenkinsfile` en la raiz del repo. El flujo cubre:

1. Restore de .NET y Node.
2. Pruebas unitarias de `server-core`, `client-web.Tests` y `server-worldbuilding`.
3. Build de proyectos y frontend.
4. Build de imagenes Docker.
5. Despliegue parametrizable:
   - `vagrant-docker`: levanta servidores Ubuntu locales con Vagrant, los configura con Puppet y despliega con Docker Compose.
   - `kubernetes`: aplica los manifests de `deploy/k8s` contra el cluster local configurado en `kubectl`.
6. Smoke test contra el API Gateway.

## Servidor Linux local

El destino recomendado es `DEPLOY_TARGET=vagrant-docker`. Jenkins ejecuta:

```bash
cd deploy/vagrant
vagrant up data apps edge --provision
```

Eso crea o reprovisiona tres servidores Linux locales:

| VM | IP | Rol |
| --- | --- | --- |
| `layla-data` | `192.168.56.10` | SQL Server, MongoDB, Neo4j, RabbitMQ |
| `layla-apps` | `192.168.56.11` | `server-core`, `server-worldbuilding`, `layla-web` |
| `layla-edge` | `192.168.56.12` | API Gateway YARP, expuesto como `http://localhost:5000` |

Con esto se cumple el despliegue en servidores Linux locales. Puppet instala Docker y ejecuta el Compose correspondiente en cada VM.

## Requisitos del agente Jenkins

Instalar en el nodo donde corre el job:

- Jenkins 2.x con Pipeline.
- Git.
- .NET SDK 10 y .NET SDK 9.
- Node.js 22+ con Corepack.
- Docker Engine/CLI con acceso al daemon.
- Vagrant 2.4+ y VirtualBox 7+ para `vagrant-docker`.
- `kubectl` con contexto local valido para `kubernetes`.

En Windows, el servicio de Jenkins debe ejecutarse con un usuario que tenga acceso a Docker Desktop, VirtualBox y la carpeta del workspace. En Linux, el usuario `jenkins` debe pertenecer al grupo `docker`.

## Crear el job

1. Crear un job tipo **Pipeline** o **Multibranch Pipeline**.
2. Apuntar el SCM a este repositorio.
3. Usar `Jenkinsfile` como script path.
4. Ejecutar con parametros:
   - `DEPLOY_TARGET`: `vagrant-docker` por defecto.
   - `RUN_DEPLOY`: `true` para desplegar; `false` para solo CI.
   - `GATEWAY_URL`: vacio usa `http://localhost:5000` en Vagrant o `http://localhost:30500` en Kubernetes.
   - `ENV_FILE_CREDENTIAL_ID`: opcional, ver seccion de secretos.

## Secretos y entorno

Para Vagrant, Puppet espera:

```text
deploy/vagrant/files/env/.env.shared
```

El pipeline admite dos modos:

- Si `ENV_FILE_CREDENTIAL_ID` esta vacio y `.env.shared` no existe, Jenkins genera un archivo local de demo con passwords fuertes pero no productivos.
- Si se define `ENV_FILE_CREDENTIAL_ID`, debe ser una credencial Jenkins tipo **Secret file** cuyo contenido sea un `.env.shared` real.

No subir `.env.shared` al repo. Esta ignorado por `.gitignore`.

Para Kubernetes, el pipeline genera `deploy/k8s/.generated-secret.yaml` en tiempo de ejecucion, lo aplica al namespace `layla` y lo elimina al terminar. Para ambientes compartidos, reemplazar esa parte por una credencial administrada por Jenkins o por un secret manager.

## Pipeline por defecto: Vagrant + Puppet + Docker

Ejecutar el job con:

```text
DEPLOY_TARGET=vagrant-docker
RUN_DEPLOY=true
```

Resultado esperado:

- `layla-data`, `layla-apps` y `layla-edge` en VirtualBox.
- Contenedores activos en `/srv/layla` dentro de cada VM.
- Gateway disponible en `http://localhost:5000/health`.

Comandos utiles desde el host:

```bash
cd deploy/vagrant
vagrant status
vagrant ssh apps -c "sudo docker compose -f /srv/layla/compose.yml ps"
vagrant ssh edge -c "curl -fsS http://localhost:5000/health"
```

## Pipeline alternativo: Kubernetes local

Preparar Docker Desktop Kubernetes, minikube o kind y asegurar que `kubectl config current-context` apunte al cluster local.

Ejecutar el job con:

```text
DEPLOY_TARGET=kubernetes
RUN_DEPLOY=true
GATEWAY_URL=http://localhost:30500
```

El pipeline construye imagenes locales con estos tags:

- `layla-server-core:latest`
- `layla-worldbuilding:latest`
- `layla-api-gateway:latest`
- `layla-client-web:latest`

Los manifests usan `imagePullPolicy: Never`, por eso el cluster debe compartir el daemon de Docker o tener esas imagenes cargadas localmente.

## Troubleshooting

- **Jenkins no ve Docker**: revisar permisos del usuario del servicio y reiniciar Jenkins.
- **`vagrant up` falla al esperar SSH**: ejecutar `vagrant reload` o validar VirtualBox Guest Additions.
- **Puppet no encuentra compose/env**: confirmar que el repo esta montado en `/vagrant`; el `Vagrantfile` ya define `config.vm.synced_folder "../..", "/vagrant"`.
- **SQL Server no arranca**: la password debe cumplir complejidad; el valor generado por Jenkins ya la cumple.
- **Kubernetes no encuentra imagenes**: usar Docker Desktop Kubernetes o cargar las imagenes en minikube/kind antes de aplicar manifests.
