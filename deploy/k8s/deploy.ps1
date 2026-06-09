# deploy.ps1 — Construye las imágenes Docker y aplica todos los manifests de Kubernetes.
# Ejecutar desde la raíz del repo: .\k8s\deploy.ps1
#
# Prerequisitos:
#   - Docker Desktop con Kubernetes habilitado (Settings > Kubernetes > Enable Kubernetes)
#   - kubectl instalado y apuntando al cluster correcto (kubectl config current-context)

param(
    [switch]$SkipBuild   # Usa -SkipBuild para omitir el paso de construcción de imágenes
)

$ErrorActionPreference = "Stop"
$SRC = Join-Path $PSScriptRoot "..\..\src"
$K8S = $PSScriptRoot

Write-Host "==> Contexto de kubectl actual:" -ForegroundColor Cyan
kubectl config current-context

# ── 1. Construir imágenes ────────────────────────────────────────────────────
if (-not $SkipBuild) {
    Write-Host "`n==> Construyendo imágenes Docker..." -ForegroundColor Cyan

    Write-Host "  [1/4] layla-server-core"
    docker build -t layla-server-core:latest -f "$SRC\server-core\Layla.Api\Dockerfile" "$SRC\server-core"

    Write-Host "  [2/4] layla-worldbuilding"
    docker build -t layla-worldbuilding:latest "$SRC\server-worldbuilding"

    Write-Host "  [3/4] layla-api-gateway"
    docker build -t layla-api-gateway:latest "$SRC\api-gateway"

    Write-Host "  [4/4] layla-client-web  (contexto: src/ completo)"
    docker build -t layla-client-web:latest -f "$SRC\client-web\Dockerfile" "$SRC"

    Write-Host "  Imagenes construidas." -ForegroundColor Green
}

# ── 2. Aplicar manifests en orden ───────────────────────────────────────────
Write-Host "`n==> Aplicando manifests de Kubernetes..." -ForegroundColor Cyan

$manifests = @(
    "00-namespace.yaml",
    "01-secret.yaml",
    "02-sqlserver.yaml",
    "03-mongodb.yaml",
    "04-neo4j.yaml",
    "05-rabbitmq.yaml",
    "06-server-core.yaml",
    "07-server-worldbuilding.yaml",
    "08-api-gateway.yaml",
    "09-client-web.yaml"
)

foreach ($file in $manifests) {
    Write-Host "  kubectl apply -f $file"
    kubectl apply -f "$K8S\$file"
}

# ── 3. Estado del despliegue ─────────────────────────────────────────────────
Write-Host "`n==> Pods en el namespace 'layla' (puede tardar ~2 min en arrancar todo):" -ForegroundColor Cyan
kubectl get pods -n layla

Write-Host "`n==> Services (puertos expuestos):" -ForegroundColor Cyan
kubectl get services -n layla

Write-Host @"

==> URLs de acceso (Docker Desktop / minikube):
    Web UI:      http://localhost:30080
    API Gateway: http://localhost:30500

Para ver el estado en tiempo real:
    kubectl get pods -n layla -w

Para ver logs de un pod:
    kubectl logs -n layla deployment/server-core
    kubectl logs -n layla deployment/layla-web

Para eliminar todo:
    kubectl delete namespace layla
"@ -ForegroundColor Yellow
