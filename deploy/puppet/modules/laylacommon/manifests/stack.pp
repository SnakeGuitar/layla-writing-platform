# Define que copia un compose y un .env a /srv/layla y los levanta con docker compose
define laylacommon::stack (
  String $compose_source,
  String $env_source,
) {
  file { '/srv/layla':
    ensure => directory,
    owner  => 'root',
    group  => 'root',
    mode   => '0755',
  }

  file { '/srv/layla/compose.yml':
    ensure  => file,
    source  => $compose_source,
    require => File['/srv/layla'],
    notify  => Exec["layla-compose-up-${name}"],
  }

  file { '/srv/layla/.env':
    ensure  => file,
    source  => $env_source,
    mode    => '0600',
    require => File['/srv/layla'],
    notify  => Exec["layla-compose-up-${name}"],
  }

  exec { "layla-compose-up-${name}":
    command     => '/usr/bin/docker compose -f /srv/layla/compose.yml --env-file /srv/layla/.env up -d',
    cwd         => '/srv/layla',
    require     => [
      File['/srv/layla/compose.yml'],
      File['/srv/layla/.env'],
      Class['laylacommon::docker_install'],
    ],
    refreshonly => false,
    unless      => '/usr/bin/docker compose -f /srv/layla/compose.yml ps --status running --quiet | /bin/grep -q .',
    timeout     => 1800,
    logoutput   => true,
  }
}
