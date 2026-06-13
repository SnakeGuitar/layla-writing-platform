# Evidencia de ejecucion del pipeline GitHub Actions

Esta carpeta contiene logs y capturas que demuestran que las etapas tecnicas del
pipeline de GitHub Actions de Layla se ejecutan correctamente: preflight de
herramientas, pruebas unitarias, builds y validacion de la configuracion de
despliegue.

## Nota sobre la ejecucion

GitHub Actions corre en infraestructura remota: el workflow de **CI** (`ci.yml`)
usa runners alojados por GitHub (`ubuntu-latest`), y el de **despliegue**
(`deploy.yml`) corre en un **self-hosted runner** instalado en el servidor Linux
local, seleccionado con `runs-on: [self-hosted, linux, layla]`.

Al generar esta evidencia no se disparo una ejecucion en vivo en la nube ni en el
runner self-hosted. Por eso se incluye `00-actions-runner-check.log` con esa
aclaracion, y los demas archivos **reproducen localmente los mismos comandos** que
definen los workflows, para comprobar que cada etapa termina con `ExitCode 0`.

## Logs

| Archivo | Evidencia |
| --- | --- |
| `00-actions-runner-check.log` | Aclaracion: CI en runners de GitHub, deploy en self-hosted runner; pasos reproducidos localmente. |
| `01-preflight-tools.log` | Herramientas disponibles: .NET 10.0.109, Node v24.13.0, Corepack 0.34.5, Docker 29.2.0, Compose v5.0.2. |
| `02-server-core-tests.log` | Pruebas unitarias de `server-core`: 187 correctas, 0 fallidas. |
| `03-client-web-tests.log` | Pruebas unitarias de `client-web`: 5 correctas, 0 fallidas. |
| `04-worldbuilding-tests.log` | Pruebas de `server-worldbuilding`: 131 correctas, 0 fallidas (7 archivos). |
| `05-builds.log` | Build Release de `api-gateway`, `client-web` (frontend + .NET) y `server-worldbuilding`. |
| `06-deploy-validation.log` | `.env` renderizado (40 variables), `docker compose config` valido (8 servicios) y workflows YAML validos. |

## Capturas

Las capturas PNG estan en `screenshots/`:

| Archivo | Uso recomendado |
| --- | --- |
| `pipeline-evidence-summary.png` | Resumen general para informe o presentacion. |
| `00-actions-runner-check.png` | Aclaracion sobre el modelo de ejecucion de GitHub Actions. |
| `01-preflight-tools.png` | Evidencia de herramientas instaladas. |
| `02-server-core-tests.png` | Pruebas unitarias .NET del backend core. |
| `03-client-web-tests.png` | Pruebas unitarias del cliente web. |
| `04-worldbuilding-tests.png` | Pruebas Node/TypeScript de worldbuilding. |
| `05-builds.png` | Compilacion Release de los servicios. |
| `06-deploy-validation.png` | Validacion del `.env`, del Compose y de los workflows. |

## Pendiente para evidencia 100% en GitHub

Para una captura real del pipeline en GitHub Actions:

1. Hacer push a `master` (dispara `ci.yml` en runners de GitHub).
2. Registrar el self-hosted runner en el servidor Linux con las etiquetas
   `linux,layla` (ver [../README.md](../README.md)).
3. El push tambien dispara `deploy.yml`; el runner local hace `docker compose up`
   y el smoke test contra `http://localhost:5000/health`.
4. Capturar la pestana **Actions** con los jobs en verde y los logs de cada paso.
