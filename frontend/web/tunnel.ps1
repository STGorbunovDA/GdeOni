<#
.SYNOPSIS
    Показать локальный web наружу через туннель (ngrok или cloudflared).

.DESCRIPTION
    ВРЕМЕННЫЙ инструмент для демо/теста, НЕ для продакшена (см. vite.tunnel.config.ts).
    Поднимает Vite в режиме туннеля (localhost:5173, прокси /api → localhost:5000)
    и пробрасывает его наружу по https.

    Провайдеры:
      cloudflared — по умолчанию. Quick tunnel *.trycloudflare.com: аккаунт не
                    нужен, но адрес СЛУЧАЙНЫЙ и новый при каждом запуске.
      ngrok       — постоянный адрес (статический домен из дашборда ngrok),
                    ссылка не меняется между перезапусками. Требует authtoken:
                    см. GdeOni-инструкция-туннель-ngrok.md на рабочем столе.

    Backend должен уже слушать 0.0.0.0:5000, Docker — быть поднят.

.PARAMETER Provider
    cloudflared (по умолчанию) или ngrok.

.PARAMETER Domain
    Статический домен ngrok, напр. gdeoni-xxxx.ngrok-free.app.
    Если не задан — берётся из переменной окружения NGROK_DOMAIN.
    Если и её нет — ngrok выдаст случайный временный адрес.

.EXAMPLE
    .\tunnel.ps1
    .\tunnel.ps1 -Provider ngrok
    .\tunnel.ps1 -Provider ngrok -Domain gdeoni-xxxx.ngrok-free.app
#>
[CmdletBinding()]
param(
    [ValidateSet('cloudflared', 'ngrok')]
    [string]$Provider = 'cloudflared',

    [string]$Domain = $env:NGROK_DOMAIN,

    [int]$Port = 5173
)

$ErrorActionPreference = 'Stop'
$webDir = $PSScriptRoot

function Resolve-Exe([string]$name, [string[]]$fallbacks) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($p in $fallbacks) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

# 1. Проверяем, что backend слушает 5000 — без него SPA откроется, но всё упадёт на первом запросе.
if (-not (Test-NetConnection -ComputerName localhost -Port 5000 -InformationLevel Quiet -WarningAction SilentlyContinue)) {
    Write-Warning "Backend не отвечает на localhost:5000. Запусти его: dotnet run --project src/GdeOni.API/GdeOni.API.csproj --urls `"http://0.0.0.0:5000`""
}

# 2. Ищем бинарь туннеля до того, как поднимать Vite.
if ($Provider -eq 'ngrok') {
    $tunnelExe = Resolve-Exe 'ngrok' @(
        "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\Ngrok.Ngrok_Microsoft.Winget.Source_8wekyb3d8bbwe\ngrok.exe"
    )
    if (-not $tunnelExe) {
        throw "ngrok не найден. Установи: winget install --id Ngrok.Ngrok -e  (затем перезапусти терминал)"
    }
}
else {
    $tunnelExe = Resolve-Exe 'cloudflared' @(
        'C:\Program Files (x86)\cloudflared\cloudflared.exe',
        'C:\Program Files\cloudflared\cloudflared.exe'
    )
    if (-not $tunnelExe) {
        throw "cloudflared не найден. Установи: winget install --id Cloudflare.cloudflared -e"
    }
}

# 3. Vite в режиме туннеля — в отдельном окне, чтобы его лог не мешался с логом туннеля.
if (Test-NetConnection -ComputerName localhost -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue) {
    Write-Host "Vite уже слушает :$Port — переиспользую." -ForegroundColor DarkGray
}
else {
    Write-Host "Запускаю Vite (режим туннеля) на :$Port ..." -ForegroundColor Cyan
    Start-Process powershell -ArgumentList @(
        '-NoExit', '-Command',
        "Set-Location '$webDir'; npx vite --mode tunnel --config vite.tunnel.config.ts"
    ) | Out-Null

    $deadline = (Get-Date).AddSeconds(90)
    while (-not (Test-NetConnection -ComputerName localhost -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue)) {
        if ((Get-Date) -gt $deadline) { throw "Vite не поднялся на :$Port за 90 секунд — смотри его окно." }
        Start-Sleep -Seconds 2
    }
    Write-Host "Vite поднялся." -ForegroundColor Green
}

# 4. Туннель — в этом окне: публичный адрес печатается в его лог.
if ($Provider -eq 'ngrok') {
    if ($Domain) {
        Write-Host "ngrok → https://$Domain" -ForegroundColor Green
        & $tunnelExe http $Port --domain=$Domain
    }
    else {
        Write-Warning "Домен не задан (-Domain или `$env:NGROK_DOMAIN) — адрес будет случайным и сменится при перезапуске."
        & $tunnelExe http $Port
    }
}
else {
    Write-Host "cloudflared quick tunnel: адрес *.trycloudflare.com появится ниже (он новый при каждом запуске)." -ForegroundColor Yellow
    & $tunnelExe tunnel --url "http://localhost:$Port"
}
