# Layla — Deployment

Three independent deployment methods, one per subdirectory:

| Method | Directory | Entry point |
|--------|-----------|-------------|
| Jenkins CI/CD | [`jenkins/`](jenkins/) | Pipeline job using the root [`../Jenkinsfile`](../Jenkinsfile) |
| Local dev (Docker Compose) | [`docker/`](docker/) | `cd deploy/docker && docker compose up -d` |
| Kubernetes | [`k8s/`](k8s/) | `kubectl apply -f deploy/k8s/` |
| 3-VM automated (Vagrant + Puppet) | [`vagrant/`](vagrant/) | `cd deploy/vagrant && vagrant up` |

Each subdirectory contains its own README with full instructions.
