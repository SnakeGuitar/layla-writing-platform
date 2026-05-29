#!/usr/bin/env bash
# Instala Puppet agent en la VM (si no está) — Vagrant lo invoca antes del 'puppet' provisioner.
set -euo pipefail

if command -v puppet >/dev/null 2>&1; then
  echo "[bootstrap] puppet ya instalado: $(puppet --version)"
  exit 0
fi

echo "[bootstrap] Instalando Puppet agent..."
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq wget
wget -q https://apt.puppet.com/puppet8-release-jammy.deb -O /tmp/puppet-release.deb
dpkg -i /tmp/puppet-release.deb
apt-get update -qq
apt-get install -y -qq puppet-agent
ln -sf /opt/puppetlabs/bin/puppet /usr/local/bin/puppet
echo "[bootstrap] Puppet $(puppet --version) listo"
