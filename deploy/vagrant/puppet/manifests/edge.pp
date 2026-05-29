# Manifiesto para VM3 — layla-edge
# API Gateway (YARP)
include laylacommon::docker_install

laylacommon::stack { 'edge':
  compose_source => '/vagrant/deploy/files/compose/compose.edge.yml',
  env_source     => '/vagrant/deploy/files/env/.env.shared',
}
