# Layla — Automated Deployment (Vagrant + Puppet)

This directory contains the infrastructure-as-code to deploy Layla across **3 virtual machines** using Vagrant (orchestration) and Puppet (provisioning).

## Topology

```
┌──────────────────────┐    ┌──────────────────────┐    ┌──────────────────────┐
│  layla-data          │    │  layla-apps          │    │  layla-edge          │
│  192.168.56.10       │◄───┤  192.168.56.11       │◄───┤  192.168.56.12       │
│                      │    │                      │    │                      │
│  · SQL Server 2022   │    │  · server-core       │    │  · api-gateway       │
│  · MongoDB 7         │    │  · server-worldbldg  │    │    (YARP, :5000)     │
│  · Neo4j 5           │    │  · layla-web         │    │                      │
│  · RabbitMQ 3        │    │                      │    │  [only one exposed   │
│                      │    │                      │    │   to host via :5000] │
└──────────────────────┘    └──────────────────────┘    └──────────────────────┘
```

Access from Windows host: `http://localhost:5000` → forwarded to `layla-edge:5000`.

## Prerequisites

- VirtualBox 7.0+
- Vagrant 2.4+
- Box `bento/ubuntu-22.04` (download with `vagrant box add bento/ubuntu-22.04 --provider virtualbox`)

## Usage

**First time** — copy the variables template and fill in real values:

```powershell
cd deploy/vagrant/files/env
cp .env.shared.example .env.shared
# Edit .env.shared and replace all CHANGE_ME placeholders
```

`.env.shared` is gitignored: credentials never reach the repository.

**Full deployment**:

```powershell
cd deploy/vagrant
vagrant up              # creates all 3 VMs and provisions everything
vagrant status          # check status of all 3
vagrant ssh data        # SSH into a VM
vagrant halt            # shut down all 3
vagrant destroy -f      # destroy everything (clean slate)
```

To bring up **a single VM**:

```powershell
vagrant up data
vagrant up apps
vagrant up edge
```

> **Recommended order**: `data` → `apps` → `edge` (apps wait for databases; the gateway waits for apps).

## Directory Structure

```
deploy/vagrant/
├── Vagrantfile                      Defines the 3 VMs (CPU/RAM/networking)
├── README.md                        This file
├── puppet/
│   ├── bootstrap.sh                 Installs Puppet agent on each VM
│   └── manifests/
│       ├── common.pp                'docker_install' class + 'layla_stack' define
│       ├── data.pp                  Compose for database services
│       ├── apps.pp                  Compose for application services
│       └── edge.pp                  Compose for the API gateway
└── files/
    ├── compose/
    │   ├── compose.data.yml         Official images only (no build)
    │   ├── compose.apps.yml         Build from /vagrant/src
    │   └── compose.edge.yml         Build the gateway
    └── env/
        └── .env.shared              Common variables for all 3 VMs
```

## How It Works

1. `vagrant up` reads the `Vagrantfile` → creates each VM from the `bento/ubuntu-22.04` box.
2. Vagrant mounts the entire repo (parent directory of the `Vagrantfile`) at `/vagrant` inside each VM.
3. Runs `bootstrap.sh` → installs Puppet agent.
4. Runs `puppet apply <manifest>.pp` → installs Docker + copies compose files + `docker compose up -d`.
5. The `apps` and `edge` images are built from `/vagrant/src/...` (live source code from the repo).

## Alternative Deployment — Manual Setup (Mode 1)

Without Vagrant or Docker, installing everything manually on pre-existing Ubuntu 22.04 VMs:

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

Configure each service to listen on `0.0.0.0` (not just localhost) and create users/passwords consistent with `.env.shared`.

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

# Publish and create systemd units for server-core, server-worldbuilding, layla-web
dotnet publish src/server-core/Layla.Api -c Release -o /opt/layla/core
pnpm --dir src/server-worldbuilding install && pnpm --dir src/server-worldbuilding build
dotnet publish src/client-web -c Release -o /opt/layla/web
# Create /etc/systemd/system/layla-core.service, layla-wbldg.service, layla-web.service
# with EnvironmentFile=/etc/layla/core.env pointing to 192.168.56.10
```

### VM3 — layla-edge
```bash
sudo apt install -y dotnet-sdk-10.0
dotnet publish src/api-gateway -c Release -o /opt/layla/gateway
# systemd unit layla-gateway.service with appsettings overridden by env vars
```

## Alternative Deployment — Docker on VMs (Mode 2)

Same result as Vagrant + Puppet but assuming pre-existing VMs:

```bash
# On each VM, once:
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER

# Copy compose.<vm>.yml and .env.shared to the VM (via SCP/Bitvise)
# Then:
docker compose -f compose.data.yml --env-file .env.shared up -d   # on layla-data
docker compose -f compose.apps.yml --env-file .env.shared up -d   # on layla-apps
docker compose -f compose.edge.yml --env-file .env.shared up -d   # on layla-edge
```

## Comparison of the 3 Modes

| Aspect | Manual | Docker on VMs | Vagrant + Puppet |
|---|---|---|---|
| Create VMs | Manual (click-click in VirtualBox) | Manual | **Automatic** |
| Install runtimes | apt-get per service | `apt install docker` | **Automatic** |
| Start services | systemd units by hand | `docker compose up` | **Automatic** |
| Reproducible | ❌ No | ⚠ Partial | ✅ Yes |
| Total time | ~6 h | ~30 min | ~10 min |
| Commands | ~60+ | ~5 | **1 (`vagrant up`)** |

## Troubleshooting

- **`vagrant up` hangs on "Waiting for SSH"**: the box has guest additions issues. `vagrant reload` usually resolves it.
- **Docker images not rebuilding**: force with `vagrant ssh apps -c "cd /srv/layla && sudo docker compose build --no-cache && sudo docker compose up -d"`.
- **server-core cannot connect to SQL Server**: verify that `layla-data` fully started with `vagrant ssh data -c "sudo docker compose ps"`. SQL Server takes ~30 seconds to become ready.
- **Shut down everything quickly**: `vagrant halt`. **Delete everything**: `vagrant destroy -f`.
