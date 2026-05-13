# Layla — Despliegue automatizado (Vagrant + Puppet)

Este directorio contiene la infraestructura como código para desplegar Layla en **3 máquinas virtuales** usando Vagrant (orquestación) y Puppet (provisión).

## Topología

```
┌──────────────────────┐    ┌──────────────────────┐    ┌──────────────────────┐
│  layla-data          │    │  layla-apps          │    │  layla-edge          │
│  192.168.56.10       │◄───┤  192.168.56.11       │◄───┤  192.168.56.12       │
│                      │    │                      │    │                      │
│  · SQL Server 2022   │    │  · server-core       │    │  · api-gateway       │
│  · MongoDB 7         │    │  · server-worldbldg  │    │    (YARP, :5000)     │
│  · Neo4j 5           │    │  · layla-web         │    │                      │
│  · RabbitMQ 3        │    │                      │    │  [único expuesto al  │
│                      │    │                      │    │   host vía :5000]    │
└──────────────────────┘    └──────────────────────┘    └──────────────────────┘
```

Acceso desde Windows host: `http://localhost:5000` → forwarded a `layla-edge:5000`.

## Requisitos

- VirtualBox 7.0+
- Vagrant 2.4+
- Box `bento/ubuntu-22.04` (se baja con `vagrant box add bento/ubuntu-22.04 --provider virtualbox`)

## Uso

**Primera vez** — copiar la plantilla de variables y rellenar valores reales:

```powershell
cd deploy/files/env
cp .env.shared.example .env.shared
# Editar .env.shared con un editor y reemplazar todos los CHANGE_ME
```

`.env.shared` esta gitignorado: las credenciales nunca llegan al repositorio.

**Despliegue completo**:

```powershell
cd deploy
vagrant up              # crea las 3 VMs y provisiona todo
vagrant status          # ver estado de las 3
vagrant ssh data        # entrar a una VM
vagrant halt            # apagar las 3
vagrant destroy -f      # destruir todo (limpio)
```

Para levantar **una sola VM**:

```powershell
vagrant up data
vagrant up apps
vagrant up edge
```

> **Orden recomendado**: `data` → `apps` → `edge` (las apps esperan a las DBs; el gateway espera a las apps).

## Estructura

```
deploy/
├── Vagrantfile                      Define las 3 VMs (CPU/RAM/red)
├── README.md                        Este archivo
├── puppet/
│   ├── bootstrap.sh                 Instala Puppet agent en cada VM
│   └── manifests/
│       ├── common.pp                Clase 'docker_install' + define 'layla_stack'
│       ├── data.pp                  Compose de bases de datos
│       ├── apps.pp                  Compose de servicios de aplicación
│       └── edge.pp                  Compose del API gateway
└── files/
    ├── compose/
    │   ├── compose.data.yml         Solo imágenes oficiales (no build)
    │   ├── compose.apps.yml         Build desde /vagrant/src
    │   └── compose.edge.yml         Build del gateway
    └── env/
        └── .env.shared              Variables comunes a las 3 VMs
```

## Cómo funciona

1. `vagrant up` lee el `Vagrantfile` → crea cada VM desde la box `bento/ubuntu-22.04`.
2. Vagrant monta el repo entero (carpeta padre del `Vagrantfile`) en `/vagrant` dentro de cada VM.
3. Ejecuta `bootstrap.sh` → instala Puppet agent.
4. Ejecuta `puppet apply <manifest>.pp` → instala Docker + copia compose + `docker compose up -d`.
5. Las imágenes `apps` y `edge` se construyen desde `/vagrant/src/...` (código fuente vivo del repo).

## Despliegue alternativo — manera manual (modo 1)

Sin Vagrant ni Docker, instalando todo a mano en VMs Ubuntu 22.04 preexistentes:

### VM1 — layla-data
```bash
# SQL Server
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | sudo tee /etc/apt/sources.list.d/mssql.list
sudo apt update && sudo apt install -y mssql-server
sudo /opt/mssql/bin/mssql-conf setup

# MongoDB 7
wget -qO- https://www.mongodb.org/static/pgp/server-7.0.asc | sudo gpg --dearmor -o /usr/share/keyrings/mongodb.gpg
echo "deb [signed-by=/usr/share/keyrings/mongodb.gpg] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/7.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb.list
sudo apt update && sudo apt install -y mongodb-org
sudo systemctl enable --now mongod

# Neo4j 5
wget -qO- https://debian.neo4j.com/neotechnology.gpg.key | sudo gpg --dearmor -o /usr/share/keyrings/neo4j.gpg
echo "deb [signed-by=/usr/share/keyrings/neo4j.gpg] https://debian.neo4j.com stable 5" | sudo tee /etc/apt/sources.list.d/neo4j.list
sudo apt update && sudo apt install -y neo4j
sudo systemctl enable --now neo4j

# RabbitMQ
sudo apt install -y rabbitmq-server
sudo rabbitmq-plugins enable rabbitmq_management
sudo systemctl enable --now rabbitmq-server
```

Configurar cada servicio para escuchar en `0.0.0.0` (no solo localhost) y crear usuarios/passwords coherentes con `.env.shared`.

### VM2 — layla-apps
```bash
# .NET 10 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-sdk-10.0

# Node 22 + pnpm
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo bash -
sudo apt install -y nodejs
sudo npm install -g pnpm

# Publicar y crear systemd units para server-core, server-worldbuilding, layla-web
dotnet publish src/server-core/Layla.Api -c Release -o /opt/layla/core
pnpm --dir src/server-worldbuilding install && pnpm --dir src/server-worldbuilding build
dotnet publish src/client-web -c Release -o /opt/layla/web
# Crear /etc/systemd/system/layla-core.service, layla-wbldg.service, layla-web.service
# con EnvironmentFile=/etc/layla/core.env apuntando a 192.168.56.10
```

### VM3 — layla-edge
```bash
sudo apt install -y dotnet-sdk-10.0
dotnet publish src/infraestructure-api_gateway -c Release -o /opt/layla/gateway
# systemd unit layla-gateway.service con appsettings sobreescrito por env vars
```

## Despliegue alternativo — Docker en VMs (modo 2)

Mismo resultado que Vagrant+Puppet pero asumiendo VMs preexistentes:

```bash
# En cada VM, una vez:
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER

# Copiar compose.<vm>.yml y .env.shared a la VM (por SCP/Bitvise)
# Después:
docker compose -f compose.data.yml --env-file .env.shared up -d   # en layla-data
docker compose -f compose.apps.yml --env-file .env.shared up -d   # en layla-apps
docker compose -f compose.edge.yml --env-file .env.shared up -d   # en layla-edge
```

## Comparativa de los 3 modos

| Aspecto | Manual | Docker en VMs | Vagrant + Puppet |
|---------|--------|---------------|-------------------|
| Crear VMs | Manual (clic-clic en VirtualBox) | Manual | **Automático** |
| Instalar runtimes | apt-get por servicio | `apt install docker` | **Automático** |
| Levantar servicios | systemd units a mano | `docker compose up` | **Automático** |
| Reproducible | ❌ No | ⚠ Parcial | ✅ Sí |
| Tiempo total | ~6 h | ~30 min | ~10 min |
| Comandos | ~60+ | ~5 | **1 (`vagrant up`)** |

## Troubleshooting

- **`vagrant up` se queda colgado en "Waiting for SSH"**: la box tiene problemas con guest additions. `vagrant reload` suele resolverlo.
- **Imágenes Docker no se rebuilds**: forzar con `vagrant ssh apps -c "cd /srv/layla && sudo docker compose build --no-cache && sudo docker compose up -d"`.
- **server-core no conecta a SQL Server**: verificar que `layla-data` levantó completo con `vagrant ssh data -c "sudo docker compose ps"`. SQL Server tarda ~30 seg en estar listo.
- **Apagar todo rápido**: `vagrant halt`. **Borrar todo**: `vagrant destroy -f`.
