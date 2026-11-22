$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$env:FOOTBALLMANAGER_GEMINI_API_KEY = [Environment]::GetEnvironmentVariable('FOOTBALLMANAGER_GEMINI_API_KEY', 'User')
if ([string]::IsNullOrWhiteSpace($env:FOOTBALLMANAGER_GEMINI_API_KEY)) {
    throw 'FOOTBALLMANAGER_GEMINI_API_KEY is not configured for this Windows user.'
}
dotnet run --project .\FootballManager.WCF\FootballManager.WCF.csproj
