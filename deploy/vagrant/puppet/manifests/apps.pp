# Manifiesto para VM2 — layla-apps
# server-core (.NET) + server-worldbuilding (Node) + layla-web (Blazor)
include laylacommon::docker_install

laylacommon::stack { 'apps':
  compose_source => '/vagrant/deploy/files/compose/compose.apps.yml',
  env_source     => '/vagrant/deploy/files/env/.env.shared',
}
