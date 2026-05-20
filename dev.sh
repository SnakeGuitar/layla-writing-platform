#!/usr/bin/env bash
# =============================================================================
#  dev.sh — Layla Development Launcher
#  Levanta server-core + server-worldbuilding y, opcionalmente, el cliente.
#
#  Uso:
#    ./dev.sh [--client <web|desktop>]
#    ./dev.sh -c <web|desktop>
#    ./dev.sh          ← solo APIs, sin cliente
#
#  Ejemplos:
#    ./dev.sh                   # APIs únicamente
#    ./dev.sh --client web      # APIs + cliente web (Blazor)
#    ./dev.sh -c desktop        # APIs + cliente de escritorio (WPF/MAUI)
# =============================================================================

set -euo pipefail

# ─── Colores ─────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
RESET='\033[0m'

# ─── Rutas relativas a la ubicación del script ───────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$SCRIPT_DIR/src"

SERVER_CORE_DIR="$SRC_DIR/server-core/Layla.Api"
SERVER_WORLDBUILDING_DIR="$SRC_DIR/server-worldbuilding"
CLIENT_WEB_DIR="$SRC_DIR/client-web"
CLIENT_DESKTOP_DIR="$SRC_DIR/client-desktop"

# ─── PIDs de procesos lanzados ────────────────────────────────────────────────
PIDS=()

# ─── Limpieza al salir (Ctrl+C o error) ──────────────────────────────────────
cleanup() {
  echo -e "\n${YELLOW}⏹  Deteniendo procesos...${RESET}"
  for pid in "${PIDS[@]}"; do
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid" 2>/dev/null || true
    fi
  done
  echo -e "${GREEN}✓  Todos los procesos detenidos.${RESET}"
  exit 0
}
trap cleanup SIGINT SIGTERM EXIT

# ─── Helpers ─────────────────────────────────────────────────────────────────
log_header() {
  echo -e "\n${BOLD}${CYAN}══════════════════════════════════════════${RESET}"
  echo -e "${BOLD}${CYAN}  $1${RESET}"
  echo -e "${BOLD}${CYAN}══════════════════════════════════════════${RESET}\n"
}

log_info()    { echo -e "${GREEN}▶${RESET}  $1"; }
log_warning() { echo -e "${YELLOW}⚠${RESET}  $1"; }
log_error()   { echo -e "${RED}✗${RESET}  $1" >&2; }

require_cmd() {
  if ! command -v "$1" &>/dev/null; then
    log_error "Comando requerido no encontrado: '${BOLD}$1${RESET}'"
    exit 1
  fi
}

# ─── Parseo de argumentos ─────────────────────────────────────────────────────
CLIENT=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    -c|--client)
      if [[ -z "${2:-}" ]]; then
        log_error "El flag '$1' requiere un valor: 'web' o 'desktop'."
        exit 1
      fi
      CLIENT="$2"
      shift 2
      ;;
    -h|--help)
      echo -e "
${BOLD}Uso:${RESET}
  ./dev.sh [--client <web|desktop>]

${BOLD}Opciones:${RESET}
  -c, --client <web|desktop>   Cliente a iniciar junto con las APIs
  -h, --help                   Muestra esta ayuda

${BOLD}Ejemplos:${RESET}
  ./dev.sh                   Solo las APIs (server-core + worldbuilding)
  ./dev.sh --client web      APIs + cliente web (Blazor)
  ./dev.sh -c desktop        APIs + cliente de escritorio (WPF/MAUI)
"
      exit 0
      ;;
    *)
      log_error "Argumento desconocido: '$1'. Usa --help para ver las opciones."
      exit 1
      ;;
  esac
done

# Validar valor del cliente si se proporcionó
if [[ -n "$CLIENT" && "$CLIENT" != "web" && "$CLIENT" != "desktop" ]]; then
  log_error "Valor inválido para --client: '${CLIENT}'. Usa 'web' o 'desktop'."
  exit 1
fi

# ─── Verificar herramientas necesarias ───────────────────────────────────────
log_header "Layla Dev Launcher"
log_info "Verificando dependencias..."

require_cmd dotnet
require_cmd pnpm

if [[ "$CLIENT" == "desktop" ]]; then
  require_cmd dotnet   # ya verificado, pero es el mismo runner para MAUI/WPF
fi

# ─── 1. Iniciar server-core (ASP.NET Core) ───────────────────────────────────
log_info "Iniciando ${BOLD}server-core${RESET} (ASP.NET — http://localhost:5287 | https://localhost:5288)..."

if [[ ! -d "$SERVER_CORE_DIR" ]]; then
  log_error "No se encontró el directorio de server-core: $SERVER_CORE_DIR"
  exit 1
fi

(
  cd "$SERVER_CORE_DIR"
  dotnet run --environment Development
) &
PIDS+=($!)
log_info "server-core PID: ${BOLD}${PIDS[-1]}${RESET}"

# ─── 2. Iniciar server-worldbuilding (Node/pnpm) ─────────────────────────────
log_info "Iniciando ${BOLD}server-worldbuilding${RESET} (Node.js — http://localhost:3000)..."

if [[ ! -d "$SERVER_WORLDBUILDING_DIR" ]]; then
  log_error "No se encontró el directorio de worldbuilding: $SERVER_WORLDBUILDING_DIR"
  exit 1
fi

(
  cd "$SERVER_WORLDBUILDING_DIR"
  NODE_ENV=development pnpm run dev
) &
PIDS+=($!)
log_info "server-worldbuilding PID: ${BOLD}${PIDS[-1]}${RESET}"

# ─── 3. Iniciar cliente (opcional) ───────────────────────────────────────────
case "$CLIENT" in
  web)
    log_info "Iniciando ${BOLD}client-web${RESET} (Blazor — https://localhost:7126 | http://localhost:5233)..."

    if [[ ! -d "$CLIENT_WEB_DIR" ]]; then
      log_error "No se encontró el directorio del cliente web: $CLIENT_WEB_DIR"
      exit 1
    fi

    (
      cd "$CLIENT_WEB_DIR"
      dotnet run --environment Development
    ) &
    PIDS+=($!)
    log_info "client-web PID: ${BOLD}${PIDS[-1]}${RESET}"
    ;;

  desktop)
    log_info "Iniciando ${BOLD}client-desktop${RESET} (WPF/MAUI)..."

    if [[ ! -d "$CLIENT_DESKTOP_DIR" ]]; then
      log_error "No se encontró el directorio del cliente de escritorio: $CLIENT_DESKTOP_DIR"
      exit 1
    fi

    (
      cd "$CLIENT_DESKTOP_DIR"
      dotnet run --environment Development
    ) &
    PIDS+=($!)
    log_info "client-desktop PID: ${BOLD}${PIDS[-1]}${RESET}"
    ;;

  "")
    log_warning "No se especificó cliente. Solo se levantarán las APIs."
    ;;
esac

# ─── Resumen ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${BOLD}${GREEN}✓ Servicios iniciados:${RESET}"
echo -e "  ${CYAN}•${RESET} server-core        → http://localhost:5287  |  https://localhost:5288"
echo -e "  ${CYAN}•${RESET} server-worldbuilding → http://localhost:3000"
[[ "$CLIENT" == "web" ]]     && echo -e "  ${CYAN}•${RESET} client-web          → http://localhost:5233  |  https://localhost:7126"
[[ "$CLIENT" == "desktop" ]] && echo -e "  ${CYAN}•${RESET} client-desktop      → (aplicación de escritorio)"
echo ""
echo -e "${YELLOW}  Presiona Ctrl+C para detener todos los servicios.${RESET}\n"

# ─── Esperar a que todos los procesos terminen ────────────────────────────────
wait "${PIDS[@]}"
