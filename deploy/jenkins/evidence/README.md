# Evidencia de ejecucion del pipeline Jenkins

Esta carpeta contiene logs y capturas que demuestran la ejecucion correcta de las etapas tecnicas del pipeline definido para Layla.

## Nota sobre Jenkins local

Al generar esta evidencia, Jenkins no estaba respondiendo en `http://localhost:8080`. Por eso se incluye `00-jenkins-local-check.log` y su captura correspondiente. Los demas archivos ejecutan localmente los mismos pasos de verificacion esperados en Jenkins: preflight de herramientas, pruebas unitarias, builds y validacion de infraestructura.

## Logs

| Archivo | Evidencia |
| --- | --- |
| `00-jenkins-local-check.log` | Jenkins local no estaba levantado en `localhost:8080`. |
| `01-preflight-tools.log` | Herramientas disponibles: .NET, Node, Corepack, Docker, Docker Compose, Vagrant y kubectl. |
| `02-server-core-tests.log` | Pruebas unitarias de `server-core`: 187 correctas, 0 fallidas. |
| `03-client-web-tests.log` | Pruebas unitarias de `client-web`: 5 correctas, 0 fallidas. |
| `04-worldbuilding-tests.log` | Pruebas de `server-worldbuilding`: 131 correctas, 0 fallidas. |
| `05-dotnet-and-ts-builds.log` | Compilacion de `api-gateway`, `client-web` y `server-worldbuilding` con salida correcta. |
| `06-infra-validation.log` | Validacion de `Vagrantfile`, configuraciones Docker Compose y cliente kubectl. |

## Capturas

Las capturas PNG estan en `screenshots/`:

| Archivo | Uso recomendado |
| --- | --- |
| `pipeline-evidence-summary.png` | Resumen general para informe o presentacion. |
| `01-preflight-tools.png` | Evidencia de herramientas instaladas. |
| `02-server-core-tests.png` | Evidencia de pruebas unitarias .NET del backend core. |
| `03-client-web-tests.png` | Evidencia de pruebas unitarias del cliente web. |
| `04-worldbuilding-tests.png` | Evidencia de pruebas Node/TypeScript. |
| `05-dotnet-and-ts-builds.png` | Evidencia de compilacion de servicios. |
| `06-infra-validation.png` | Evidencia de validacion de infraestructura. |

## Pendiente para evidencia 100% Jenkins

Para obtener una captura real del job en Jenkins, levanta Jenkins, crea un Pipeline apuntando al `Jenkinsfile`, ejecuta `Build Now` y captura la consola del build. Esa captura complementaria deberia mostrar las mismas etapas que estos logs locales.
