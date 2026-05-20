# Modo 3 — Despliegue automatizado con Vagrant + Puppet

> **Objetivo didáctico**: alcanzar **reproducibilidad total** del entorno con un único comando. Vagrant crea las VMs desde una imagen base; Puppet instala Docker y levanta los servicios dentro.

**Tiempo total estimado**: 10 minutos (primera vez; ~2 min con imágenes cacheadas).
**Comandos a ejecutar**: **1** (`vagrant up`).
**Reproducibilidad**: total — cualquier persona con Vagrant + VirtualBox obtiene exactamente el mismo entorno.

---

## Pre-requisitos

- **VirtualBox 7.0+**
- **Vagrant 2.4+** ([descargar](https://developer.hashicorp.com/vagrant/install))
- Box base: `bento/ubuntu-22.04`

```powershell
vagrant box add bento/ubuntu-22.04 --provider virtualbox
```

> No hace falta tener VMs preexistentes. Vagrant las crea de cero.

---

## Demo (el comando único)

```powershell
cd deploy
vagrant up
```

Eso es **todo**. Por dentro Vagrant:
1. Crea 3 VMs (`data`, `apps`, `edge`) clonando la box base.
2. Configura red privada `192.168.56.0/24` con IPs fijas (`.10`, `.11`, `.12`).
3. Configura port forwarding `localhost:5000` → `edge:5000` (acceso desde el host).
4. Monta el repo entero en `/vagrant` dentro de cada VM (synced folder).
5. Ejecuta `bootstrap.sh` para instalar Puppet agent.
6. Ejecuta `puppet apply <vm>.pp` que:
   - Instala Docker Engine + Compose plugin.
   - Copia `compose.<vm>.yml` y `.env.shared` a `/srv/layla/`.
   - Corre `docker compose up -d`.

## Verificación

```powershell
vagrant status                              # Muestra las 3 VMs running
vagrant ssh data -c "sudo docker ps"        # 4 contenedores en data
vagrant ssh apps -c "sudo docker ps"        # 3 contenedores en apps
vagrant ssh edge -c "sudo docker ps"        # 1 contenedor en edge

# Desde el host Windows:
curl http://localhost:5000/health
curl http://localhost:5000/api/projects/public
```

---

## Arquitectura de la solución

```
deploy/
├── Vagrantfile                          # Define las 3 VMs (CPU/RAM/red)
├── puppet/
│   ├── bootstrap.sh                     # Instala Puppet agent en cada VM
│   ├── manifests/
│   │   ├── data.pp                      # 4 líneas — solo invoca el módulo
│   │   ├── apps.pp                      # idem
│   │   └── edge.pp                      # idem
│   └── modules/
│       └── laylacommon/manifests/
│           ├── init.pp                  # Clase paraguas
│           ├── docker_install.pp        # Instala Docker Engine + Compose
│           └── stack.pp                 # Define que copia compose + corre docker compose
└── files/
    ├── compose/
    │   ├── compose.data.yml             # Imágenes oficiales (sin build)
    │   ├── compose.apps.yml             # Build desde /vagrant/src/...
    │   └── compose.edge.yml             # Build del gateway
    └── env/.env.shared                  # Variables compartidas
```

### `Vagrantfile` — la parte clave

```ruby
Vagrant.configure("2") do |config|
  config.vm.box = "bento/ubuntu-22.04"
  config.vm.synced_folder "..", "/vagrant", type: "virtualbox"

  config.vm.define "data" do |m|
    m.vm.hostname = "layla-data"
    m.vm.network "private_network", ip: "192.168.56.10"
    m.vm.provider("virtualbox") { |vb| vb.memory = 3072; vb.cpus = 2 }
    m.vm.provision "shell", path: "puppet/bootstrap.sh"
    m.vm.provision "puppet" do |p|
      p.manifests_path = "puppet/manifests"
      p.manifest_file  = "data.pp"
      p.module_path    = "puppet/modules"
    end
  end
  # ... mismo patrón para apps y edge
end
```

### Manifiesto Puppet por VM — **4 líneas útiles**

`data.pp`:
```puppet
include laylacommon::docker_install

laylacommon::stack { 'data':
  compose_source => '/vagrant/deploy/files/compose/compose.data.yml',
  env_source     => '/vagrant/deploy/files/env/.env.shared',
}
```

Toda la lógica reusable vive en el módulo `laylacommon`.

### `laylacommon::docker_install` (resumen)
Declarativo en vez de imperativo:
- `package { 'docker-ce': ensure => installed }` en vez de `apt install -y docker-ce`.
- `service { 'docker': ensure => running, enable => true }` en vez de `systemctl enable --now docker`.
- Puppet entiende **dependencias entre recursos** con `require` y `notify` — no hace falta ordenar comandos manualmente.

### `laylacommon::stack` (resumen)
- `file { '/srv/layla/compose.yml': source => ... }` — copia desde el repo.
- `exec { 'layla-compose-up-<vm>': command => 'docker compose up -d', ... }` — corre el deploy.
- `notify` se asegura que si cambia el compose o el `.env`, se re-ejecuta automáticamente el `up`.

---

## Comandos útiles

```powershell
vagrant up                        # crear/encender las 3 VMs
vagrant up <data|apps|edge>       # una sola
vagrant status                    # estado de las 3
vagrant ssh <vm>                  # entrar por SSH
vagrant halt                      # apagar (mantiene el disco)
vagrant suspend                   # suspender RAM
vagrant resume                    # despertar
vagrant reload <vm>               # reiniciar la VM
vagrant provision <vm>            # re-aplicar Puppet sin recrear la VM
vagrant destroy -f                # borrar todo
```

**Flujo de demo recomendado**:
1. Antes de la presentación: `vagrant up` una vez para tener las imágenes Docker cacheadas en las VMs.
2. `vagrant halt`.
3. En la demo: `vagrant up` — tarda 1-2 min porque ya tiene las imágenes.
4. Mostrar `vagrant status`, `vagrant ssh data -c "sudo docker ps"`, `curl http://localhost:5000/api/projects/public`.

---

## Bugs reales encontrados durante el desarrollo

Cosas que el modo 1 (manual) no nos había mostrado porque cada paso se hace "a ojo". Al automatizar todo, los bugs salieron a la luz:

| Bug | Archivo | Síntoma | Fix |
|-----|---------|---------|-----|
| `UseUrls("https://localhost:...")` | `src/server-core/Layla.Api/Config/Builder.cs` | Service no responde desde fuera del contenedor | Bind a `+` en Producción |
| Idem | `src/client-web/Config/Builder.cs` | Idem | Mismo patrón |
| `UseHttpsRedirection()` en producción | `src/server-core/Layla.Api/Program.cs` | Gateway recibe 307 y falla | Removido en producción (el gateway termina TLS) |
| Gateway llama `UseAuthentication()` sin `AddAuthentication()` | `src/infraestructure-api_gateway/Program.cs` | Crash al startup | Removido (auth no implementada todavía) |
| Worldbuilding lee `process.env["PORT"]` | `src/server-worldbuilding/src/config/env.ts` | Server arranca en puerto aleatorio | Agregar `PORT=...` al compose |
| MongoDB 7 exit 132 (SIGILL) | imagen `mongo:7` | Contenedor crashea cada 30s | Bajar a `mongo:4.4` (sin AVX) |

> **Lección**: la automatización **fuerza** consistencia. Un bug que en modo manual quedaría enmascarado por la intervención humana, en modo automatizado **te detiene en seco** hasta arreglarlo en el código.

---

## Comparativa final

| Aspecto | Modo 1 | Modo 2 | Modo 3 |
|---------|--------|--------|--------|
| Crear VMs | Manual | Manual | **Automático** |
| Instalar OS | Manual (ISO) | Manual (ISO) | **Box base reusable** |
| Configurar red | Manual (netplan, cloud-init) | Manual | **Declarado en Vagrantfile** |
| Instalar runtimes | apt-get por servicio | `docker install` por VM | **Puppet** |
| Levantar servicios | systemd units a mano | `docker compose up` | **`docker compose up` orquestado por Puppet** |
| Reproducibilidad | ❌ Nula | ⚠ Parcial (faltan las VMs) | ✅ **Total** |
| Tiempo total | ~6 h | ~30 min | ~10 min |
| Comandos manuales | ~60+ | ~5 | **1** |
| Onboarding de un compañero | "Acá tenés la guía de 60 pasos" | "Acá tenés los 3 composes y el .env" | "Corré `vagrant up`" |

---

## Conclusión

El modo 3 transforma la pregunta "¿cómo despliego esto?" en **un solo comando**, y como bonus colateral, **fuerza al equipo a hacer su código compatible con producción** desde el día 1.

> Si la presentación es sobre **Despliegue de Software**, el mensaje es: el despliegue es **código**. Y como código, se versiona, se revisa, se prueba y se reproduce.
