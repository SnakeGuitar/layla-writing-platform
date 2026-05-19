# Modo 1 — Despliegue manual en VMs

> **Objetivo didáctico**: demostrar todo lo que un administrador de sistemas tendría que hacer "a mano" sin herramientas de automatización. Es la línea base contra la cual se justifican los modos 2 y 3.

**Tiempo total estimado**: 6 horas (primera vez).
**Comandos a ejecutar**: ~60.
**Reproducibilidad**: nula — cada paso es manual y propenso a errores tipográficos.

---

## Paso 1 — Crear 3 máquinas virtuales en VirtualBox

Para cada VM (`layla-data`, `layla-apps`, `layla-edge`):

1. VirtualBox → **Nueva** → nombre, tipo: Linux, versión: Ubuntu (64-bit), ISO de Ubuntu Server 22.04.
2. **Omitir instalación desatendida** (marcado).
3. RAM y CPU según [`README.md`](README.md#especificaciones-de-las-vms).
4. Disco: 20 GB, VDI, dinámicamente asignado.
5. Configuración → **Red**:
   - Adaptador 1: NAT (acceso a internet).
   - Adaptador 2: **Adaptador solo-anfitrión** → `VirtualBox Host-Only Ethernet Adapter`.
6. Configuración → **Audio**: deshabilitar (ahorra recursos).
7. Iniciar e instalar Ubuntu (proceso interactivo, ~10 min cada una).

**Durante la instalación de Ubuntu**:
- Idioma/teclado: a elección.
- Network: dejar DHCP por ahora.
- Storage: "Use entire disk", **sin LVM**.
- Profile: username `layla`, password a elección, hostname según VM.
- **SSH**: ✅ Install OpenSSH server (crítico).
- Featured Snaps: ninguno.

> **Atajo**: instalar una sola VM (`layla-data`) y clonarla 2 veces con **Generar nuevas MAC**. Cambiar hostname + IP en los clones.

## Paso 2 — Fijar IP estática y deshabilitar cloud-init (en cada VM)

Ubuntu Server usa cloud-init, que regenera el archivo de netplan en cada boot. Para hacer la IP persistente:

```bash
# Login en la consola de la VM
echo 'network: {config: disabled}' | sudo tee /etc/cloud/cloud.cfg.d/99-disable-network-config.cfg

sudo tee /etc/netplan/50-cloud-init.yaml > /dev/null <<'EOF'
network:
  version: 2
  ethernets:
    enp0s3:
      dhcp4: true
    enp0s8:
      dhcp4: false
      addresses: [192.168.56.10/24]   # cambiar 10/11/12 según VM
EOF
sudo chmod 600 /etc/netplan/50-cloud-init.yaml
sudo netplan apply
sudo reboot
```

Verificar desde el host con Bitvise: SSH a `192.168.56.10` (y `.11`, `.12`).

## Paso 3 — Provisionar `layla-data` (192.168.56.10)

### 3.1 Actualizar e instalar dependencias base
```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl wget gnupg lsb-release ca-certificates
```

### 3.2 SQL Server 2022
```bash
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft.gpg
curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | sudo tee /etc/apt/sources.list.d/mssql.list
sudo apt update && sudo apt install -y mssql-server
sudo /opt/mssql/bin/mssql-conf setup     # interactivo: Edition=2 (Developer), password=CHANGE_ME
sudo systemctl enable --now mssql-server

# Herramientas (sqlcmd)
sudo ACCEPT_EULA=Y apt install -y mssql-tools18 unixodbc-dev
echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc
source ~/.bashrc

# Crear DB
sqlcmd -S localhost -U sa -P 'CHANGE_ME' -C -Q "CREATE DATABASE LaylaCore"
```

### 3.3 MongoDB 4.4
> Nota: se usa 4.4 (no 7+) porque MongoDB 5+ requiere instrucciones AVX que VirtualBox no expone por defecto.

```bash
wget -qO- https://www.mongodb.org/static/pgp/server-4.4.asc | sudo gpg --dearmor -o /usr/share/keyrings/mongodb.gpg
echo "deb [signed-by=/usr/share/keyrings/mongodb.gpg] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/4.4 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb.list
sudo apt update && sudo apt install -y mongodb-org
# Permitir bind 0.0.0.0
sudo sed -i 's/bindIp: 127.0.0.1/bindIp: 0.0.0.0/' /etc/mongod.conf
sudo systemctl enable --now mongod

# Crear usuario administrador
mongo <<'EOF'
use admin
db.createUser({user: "layla", pwd: "CHANGE_ME", roles: ["root"]})
EOF
# Habilitar auth
sudo sed -i 's/#security:/security:\n  authorization: enabled/' /etc/mongod.conf
sudo systemctl restart mongod
```

### 3.4 Neo4j 5
```bash
wget -qO- https://debian.neo4j.com/neotechnology.gpg.key | sudo gpg --dearmor -o /usr/share/keyrings/neo4j.gpg
echo "deb [signed-by=/usr/share/keyrings/neo4j.gpg] https://debian.neo4j.com stable 5" | sudo tee /etc/apt/sources.list.d/neo4j.list
sudo apt update && sudo apt install -y neo4j
# Bind 0.0.0.0
sudo sed -i 's/#server.default_listen_address=0.0.0.0/server.default_listen_address=0.0.0.0/' /etc/neo4j/neo4j.conf
sudo systemctl enable --now neo4j
# Cambiar password (default: neo4j/neo4j)
cypher-shell -u neo4j -p neo4j "ALTER CURRENT USER SET PASSWORD FROM 'neo4j' TO 'CHANGE_ME';"
```

### 3.5 RabbitMQ
```bash
sudo apt install -y rabbitmq-server
sudo rabbitmq-plugins enable rabbitmq_management
sudo systemctl enable --now rabbitmq-server

# Crear usuario
sudo rabbitmqctl add_user layla 'CHANGE_ME'
sudo rabbitmqctl set_user_tags layla administrator
sudo rabbitmqctl set_permissions -p / layla ".*" ".*" ".*"
```

### 3.6 Firewall
```bash
sudo ufw allow from 192.168.56.0/24 to any port 1433  # SQL
sudo ufw allow from 192.168.56.0/24 to any port 27017 # Mongo
sudo ufw allow from 192.168.56.0/24 to any port 7687  # Neo4j Bolt
sudo ufw allow from 192.168.56.0/24 to any port 5672  # Rabbit
sudo ufw allow ssh
sudo ufw --force enable
```

## Paso 4 — Provisionar `layla-apps` (192.168.56.11)

### 4.1 Instalar runtimes
```bash
sudo apt update
# .NET 10 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update && sudo apt install -y dotnet-sdk-10.0

# Node 22 + pnpm
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo bash -
sudo apt install -y nodejs
sudo npm install -g pnpm
```

### 4.2 Publicar y desplegar `server-core`
Desde el host (Windows con Visual Studio o `dotnet` instalado):
```bash
cd src/server-core
dotnet publish Layla.Api -c Release -o ./publish
```
Subir `publish/` a la VM con Bitvise SFTP a `/opt/layla/core/`.

### 4.3 Crear servicio systemd `/etc/systemd/system/layla-core.service`
```ini
[Unit]
Description=Layla Core API
After=network.target

[Service]
WorkingDirectory=/opt/layla/core
ExecStart=/usr/bin/dotnet /opt/layla/core/Layla.Api.dll
Restart=always
RestartSec=10
SyslogIdentifier=layla-core
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
EnvironmentFile=/etc/layla/core.env

[Install]
WantedBy=multi-user.target
```

Crear `/etc/layla/core.env` con todas las variables (`Ports__HTTPS=7166`, `JwtSettings__Secret=...`, `DatabaseConfigs__SQL__ConnectionString=Server=192.168.56.10,1433;...`, `RabbitMQ__HostName=192.168.56.10`, `EmailConfigs__Host=...`, etc.).

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now layla-core
sudo journalctl -u layla-core -f
```

### 4.4 Repetir para `server-worldbuilding` y `layla-web`
Mismo patrón: publish/build → SFTP → systemd unit + EnvironmentFile.

Para `server-worldbuilding`:
```bash
cd /opt/layla/worldbuilding
pnpm install --prod
pnpm run build
# systemd ExecStart=/usr/bin/node /opt/layla/worldbuilding/dist/index.js
```

### 4.5 Firewall
```bash
sudo ufw allow from 192.168.56.0/24 to any port 5287
sudo ufw allow from 192.168.56.0/24 to any port 3001
sudo ufw allow ssh
sudo ufw --force enable
```

## Paso 5 — Provisionar `layla-edge` (192.168.56.12)

```bash
sudo apt install -y dotnet-sdk-10.0
# Subir publish/ del api-gateway a /opt/layla/gateway/
# Crear systemd unit layla-gateway.service con env vars que apunten a 192.168.56.11
# Abrir puerto 5000 en UFW
```

## Paso 6 — Verificación

Desde el host Windows:
```bash
curl http://192.168.56.12:5000/health             # gateway responde
curl http://192.168.56.12:5000/api/projects/public # round-trip completo
```

---

## Problemas comunes

| Síntoma | Causa | Solución |
|---------|-------|----------|
| `mssql-server` no arranca | RAM insuficiente | Subir VM a 3+ GB |
| MongoDB exit 132 (SIGILL) | Falta AVX en CPU | Usar MongoDB 4.4 |
| server-core no responde desde fuera | Bind a `localhost` | Patch `Builder.cs` para usar `+` en producción |
| Gateway devuelve 502 al proxy | server-core fuerza HTTPS redirect | Quitar `UseHttpsRedirection` en producción |

## Resumen del esfuerzo

- **Pasos manuales**: ~60 comandos distribuidos en 6 etapas.
- **Decisiones interactivas**: 8+ (passwords, particionado, EULAs, certificados).
- **Archivos a editar a mano**: 10+ (netplan, mongod.conf, neo4j.conf, .env, units systemd, ufw rules).
- **Si fallás un paso**: tenés que diagnosticar manualmente con `journalctl`, `systemctl status`, `ss -tlnp`.

> **Conclusión narrativa**: esto funciona, pero es **frágil**, **lento** y **no reproducible**. Si tu compañero quiere replicar el entorno, tiene que seguir cada paso manualmente y rezar para no equivocarse. Lo cual nos lleva al **Modo 2: contenedores**.
