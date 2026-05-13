# Instala Docker Engine + Compose plugin en Ubuntu 22.04
class laylacommon::docker_install {
  exec { 'apt-update-precond':
    command => '/usr/bin/apt-get update -qq',
    unless  => '/usr/bin/test -f /etc/apt/keyrings/docker.gpg',
  }

  package { ['ca-certificates', 'curl', 'gnupg']:
    ensure  => installed,
    require => Exec['apt-update-precond'],
  }

  exec { 'docker-gpg-key':
    command => '/bin/bash -c "install -m 0755 -d /etc/apt/keyrings && curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg && chmod a+r /etc/apt/keyrings/docker.gpg"',
    creates => '/etc/apt/keyrings/docker.gpg',
    require => Package['curl', 'gnupg', 'ca-certificates'],
  }

  file { '/etc/apt/sources.list.d/docker.list':
    ensure  => file,
    content => "deb [arch=amd64 signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu jammy stable\n",
    require => Exec['docker-gpg-key'],
  }

  exec { 'apt-update-docker':
    command     => '/usr/bin/apt-get update -qq',
    refreshonly => true,
    subscribe   => File['/etc/apt/sources.list.d/docker.list'],
  }

  package { ['docker-ce', 'docker-ce-cli', 'containerd.io', 'docker-compose-plugin']:
    ensure  => installed,
    require => [File['/etc/apt/sources.list.d/docker.list'], Exec['apt-update-docker']],
  }

  service { 'docker':
    ensure  => running,
    enable  => true,
    require => Package['docker-ce'],
  }

  exec { 'add-vagrant-to-docker':
    command => '/usr/sbin/usermod -aG docker vagrant',
    unless  => '/usr/bin/groups vagrant | /bin/grep -q docker',
    require => Package['docker-ce'],
  }
}
