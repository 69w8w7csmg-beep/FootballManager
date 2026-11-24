# מטרה: מפעיל ספק בינה מקומי אופציונלי ואת WCF.
# מי משתמש: מי שמפעיל את אפשרות הבינה המקומית.
# פעולות: יוצר מפתח זמני ומפעיל שני שירותים מקומיים.
# תלויות: מפעיל מודל בעזרת llama.cpp ויוצר מפתח זמני לספק המקומי.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$LlamaServer,
    [Parameter(Mandatory)][string]$Model
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$exe = (Get-Item -LiteralPath $LlamaServer -ErrorAction Stop).FullName
$modelPath = (Get-Item -LiteralPath $Model -ErrorAction Stop).FullName
$providerPort = 58175
foreach ($port in @($providerPort, 5152)) {
    if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) {
        throw "Port $port is in use. Stop the existing demo service first."
    }
}

$keyPath = [IO.Path]::GetTempFileName()
$originalKey = $env:FOOTBALLMANAGER_LOCAL_AI_KEY
$originalEndpoint = $env:LocalAi__Endpoint
$originalModel = $env:LocalAi__Model
$provider = $null
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $bytes = New-Object byte[] 32
    $rng.GetBytes($bytes)
    $env:FOOTBALLMANAGER_LOCAL_AI_KEY = [Convert]::ToBase64String($bytes)
    [IO.File]::WriteAllText($keyPath, $env:FOOTBALLMANAGER_LOCAL_AI_KEY)
    $env:LocalAi__Model = 'football-scout'
    $env:LocalAi__Endpoint = "http://127.0.0.1:$providerPort/v1/chat/completions"
    $modelArgs = @('-m', ('"{0}"' -f $modelPath), '--alias', $env:LocalAi__Model,
        '--host', '127.0.0.1', '--port', $providerPort, '--api-key-file', ('"{0}"' -f $keyPath),
        '--reasoning', 'off', '--parallel', '1', '-c', '4096', '--no-webui',
        '-t', [Math]::Min(12, [Environment]::ProcessorCount))
    $provider = Start-Process -FilePath $exe -ArgumentList $modelArgs -WindowStyle Hidden -PassThru
    Write-Host 'Loading the local model...'
    $ready = $false
    $limit = (Get-Date).AddMinutes(3)
    while ((Get-Date) -lt $limit) {
        if ($provider.HasExited) { throw 'The model service exited. Check the executable and model paths.' }
        try {
            $health = Invoke-RestMethod -Uri "http://127.0.0.1:$providerPort/health" -TimeoutSec 2
            if ($health.status -eq 'ok') { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'The model service did not become ready within three minutes.' }
    Write-Host 'Model ready. Starting FootballManager WCF on localhost:5152.'
    dotnet run --project (Join-Path $PSScriptRoot 'FootballManager.WCF/FootballManager.WCF.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'FootballManager WCF failed to run.' }
} finally {
    if ($provider -and -not $provider.HasExited) { $provider.Kill(); $provider.WaitForExit() }
    $rng.Dispose()
    Remove-Item -LiteralPath $keyPath -Force
    $env:FOOTBALLMANAGER_LOCAL_AI_KEY = $originalKey
    $env:LocalAi__Endpoint = $originalEndpoint
    $env:LocalAi__Model = $originalModel
}
