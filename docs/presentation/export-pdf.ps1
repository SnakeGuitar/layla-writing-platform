# Exporta slides.html (reveal.js) a slides.pdf usando Decktape.
# Uso:
#   .\export-pdf.ps1                   # genera slides.pdf
#   .\export-pdf.ps1 -Open             # genera y abre el PDF al terminar
#   .\export-pdf.ps1 -Size "1920x1080" # cambia la resolucion (default: 1280x800)
#
# Requiere: Node.js instalado (decktape se instala localmente en node_modules/).

param(
    [string]$InputFile = "slides.html",
    [string]$Output = "slides.pdf",
    [string]$Size = "1280x800",
    [switch]$Open,
    [switch]$Reinstall
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

function Write-Step($msg) {
    Write-Host "==> $msg" -ForegroundColor Cyan
}

function Write-Ok($msg) {
    Write-Host "OK  $msg" -ForegroundColor Green
}

function Write-Fail($msg) {
    Write-Host "ERR $msg" -ForegroundColor Red
}

# 1) Verificar Node.js
Write-Step "Verificando Node.js"
try {
    $nodeVer = node --version
    Write-Ok "Node $nodeVer"
} catch {
    Write-Fail "Node.js no esta instalado. Descargalo desde https://nodejs.org/"
    exit 1
}

# 2) Instalar Decktape si falta (o si se pidio reinstalar)
$decktapeBin = Join-Path $PSScriptRoot "node_modules\.bin\decktape.cmd"
$needInstall = (-not (Test-Path $decktapeBin)) -or $Reinstall

if ($needInstall) {
    Write-Step "Instalando Decktape localmente (descarga Chromium, ~150 MB)"
    Write-Host "    Esto puede tomar 2-5 minutos la primera vez..." -ForegroundColor Gray

    if (-not (Test-Path "package.json")) {
        # Escribir package.json SIN BOM (npm no acepta JSON con BOM)
        $pkg = '{"name":"layla-presentation","version":"1.0.0","private":true}'
        [System.IO.File]::WriteAllText(
            (Join-Path $PSScriptRoot "package.json"),
            $pkg,
            (New-Object System.Text.UTF8Encoding $false)
        )
    }

    npm install decktape --no-audit --no-fund --silent
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "Fallo la instalacion de Decktape"
        exit 1
    }
    Write-Ok "Decktape instalado"
} else {
    Write-Ok "Decktape ya instalado"
}

# 3) Verificar que slides.html existe
if (-not (Test-Path $InputFile)) {
    Write-Fail "No se encontro el archivo $InputFile"
    exit 1
}

# 4) Ejecutar Decktape
$inputPath = Resolve-Path $InputFile
$outputPath = Join-Path $PSScriptRoot $Output

Write-Step "Generando PDF"
Write-Host "    Entrada: $inputPath" -ForegroundColor Gray
Write-Host "    Salida:  $outputPath" -ForegroundColor Gray
Write-Host "    Tamano:  $Size" -ForegroundColor Gray

$inputUri = "file:///$($inputPath.Path -replace '\\', '/')"

& $decktapeBin reveal `
    --size $Size `
    --slides "1-1000" `
    --pause 200 `
    --load-pause 1000 `
    $inputUri `
    $outputPath

if ($LASTEXITCODE -ne 0) {
    Write-Fail "Decktape devolvio codigo $LASTEXITCODE"
    exit $LASTEXITCODE
}

if (Test-Path $outputPath) {
    $sizeKB = [math]::Round((Get-Item $outputPath).Length / 1KB, 1)
    Write-Ok "PDF generado: $outputPath ($sizeKB KB)"

    if ($Open) {
        Write-Step "Abriendo PDF"
        Start-Process $outputPath
    }
} else {
    Write-Fail "El PDF no se genero (revisa el log arriba)"
    exit 1
}
