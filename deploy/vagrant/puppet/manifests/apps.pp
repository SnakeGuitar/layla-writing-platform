# Manifiesto para VM2 — layla-apps
# server-core (.NET) + server-worldbuilding (Node) + layla-web (Blazor)
include laylacommon::docker_install

laylacommon::stack { 'apps':
  compose_source => '/vagrant/deploy/vagrant/files/compose/compose.apps.yml',
  env_source     => '/vagrant/deploy/vagrant/files/env/.env.shared',
}
