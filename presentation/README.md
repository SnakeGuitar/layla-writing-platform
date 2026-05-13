# Despliegue de Layla — Materia "Despliegue de Software"

Demostración de tres formas de desplegar el mismo sistema (Layla) usando **3 máquinas virtuales**:

| Modo | Documento | Tiempo total | Comandos manuales |
|------|-----------|--------------|-------------------|
| 1. Manual en VMs | [`01-manual.md`](01-manual.md) | ~6 horas | ~60+ |
| 2. Contenedores en VMs | [`02-docker.md`](02-docker.md) | ~30 minutos | ~5 |
| 3. Vagrant + Puppet en VMs | [`03-vagrant-puppet.md`](03-vagrant-puppet.md) | ~10 minutos | **1** |

Los tres modos despliegan **la misma arquitectura** (8 servicios distribuidos en 3 VMs):

```
┌──────────────────────┐    ┌──────────────────────┐    ┌──────────────────────┐
│  layla-data          │    │  layla-apps          │    │  layla-edge          │
│  192.168.56.10       │◄───┤  192.168.56.11       │◄───┤  192.168.56.12       │
│                      │    │                      │    │                      │
│  · SQL Server 2022   │    │  · server-core       │    │  · api-gateway       │
│  · MongoDB 4.4       │    │  · server-worldbldg  │    │    (YARP, :5000)     │
│  · Neo4j 5           │    │  · layla-web         │    │                      │
│  · RabbitMQ 3        │    │                      │    │  [único expuesto al  │
│                      │    │                      │    │   host vía :5000]    │
└──────────────────────┘    └──────────────────────┘    └──────────────────────┘
       4 contenedores               3 contenedores              1 contenedor
```

## Especificaciones de las VMs

| VM | RAM | CPU | Disco | Red |
|----|-----|-----|-------|-----|
| `layla-data` | 3 GB | 2 | 20 GB | NAT + 192.168.56.10 |
| `layla-apps` | 3 GB | 2 | 20 GB | NAT + 192.168.56.11 |
| `layla-edge` | 1 GB | 1 | 20 GB | NAT + 192.168.56.12 |

**Sistema operativo en todas**: Ubuntu Server 22.04 LTS.

## Estructura del repositorio (relevante para la demo)

```
/
├── src/                          Código fuente de Layla
│   ├── server-core/              Backend .NET 10
│   ├── server-worldbuilding/     Backend Node.js 22
│   ├── infraestructure-api_gateway/  Gateway YARP
│   ├── client-web/               Cliente Blazor Server
│   └── docker-compose.yml        Compose monolítico (para desarrollo)
├── deploy/                       Infraestructura para los 3 modos
│   ├── Vagrantfile               Define las 3 VMs (modo 3)
│   ├── puppet/                   Manifiestos Puppet (modo 3)
│   └── files/
│       ├── compose/              compose.data.yml, compose.apps.yml, compose.edge.yml (modos 2 y 3)
│       └── env/.env.shared       Variables compartidas
└── presentation/                 Este directorio
    ├── images/                   Capturas para diapositivas
    ├── 01-manual.md
    ├── 02-docker.md
    └── 03-vagrant-puppet.md
```

## Pre-requisitos comunes a los 3 modos

- **VirtualBox 7.0+** (probado con 7.0.20)
- **Bitvise SSH Client** (o cualquier cliente SSH)
- **ISO Ubuntu Server 22.04.5 LTS** (~2 GB) — solo para modos 1 y 2

Adicionales por modo:
- Modo 3: **Vagrant 2.4+** y la box `bento/ubuntu-22.04`.

## Flujo recomendado de la presentación

1. **Introducción** (2 min): qué es Layla, qué se va a desplegar.
2. **Modo 1 — Manual** (5 min): mostrar la lista de comandos del `01-manual.md`, screenshots de instalación. Sin demo en vivo (toma horas).
3. **Modo 2 — Docker** (5 min): mostrar los 3 archivos `compose.*.yml` y un `docker compose up -d` en vivo (puede correr en una de las VMs ya provisionadas).
4. **Modo 3 — Vagrant + Puppet** (10 min): **demo en vivo**. `vagrant destroy -f && vagrant up`. Mientras corre, explicar `Vagrantfile` y manifiestos Puppet.
5. **Comparativa** (3 min): tabla comparativa, lecciones aprendidas.
6. **Q&A** (5 min).
