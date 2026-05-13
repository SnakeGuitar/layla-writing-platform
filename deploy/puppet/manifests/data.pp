# Manifiesto para VM1 — layla-data
# SQL Server + MongoDB + Neo4j + RabbitMQ
include laylacommon::docker_install

laylacommon::stack { 'data':
  compose_source => '/vagrant/deploy/files/compose/compose.data.yml',
  env_source     => '/vagrant/deploy/files/env/.env.shared',
}
