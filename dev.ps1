param([string]$Client = "")

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src  = "$root\src"

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$src\server-core\Layla.Api'; dotnet watch"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$src\server-worldbuilding'; npm run dev"

if ($Client -eq "web")     { Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$src\client-web'; dotnet watch" }
if ($Client -eq "desktop") { Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$src\client-desktop'; dotnet run" }
