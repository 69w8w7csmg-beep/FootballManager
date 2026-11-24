# מטרה: מפעיל את שרת FootballManager.
# מי משתמש: מי שמפעיל ומדגים את שרת FootballManager.
# פעולות: טוען הגדרת בינה ומפעיל dotnet run.
# תלויות: טוען הגדרות בינה למופע השרת ומריץ את שני נתיבי הלקוחות.
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:FOOTBALLMANAGER_API_FOOTBALL_KEY)) {
    $env:FOOTBALLMANAGER_API_FOOTBALL_KEY = [Environment]::GetEnvironmentVariable('FOOTBALLMANAGER_API_FOOTBALL_KEY', 'User')
}
if ([string]::IsNullOrWhiteSpace($env:FOOTBALLMANAGER_GEMINI_API_KEY) -and
    [string]::IsNullOrWhiteSpace($env:LocalAi__Endpoint)) {
    $env:FOOTBALLMANAGER_GEMINI_API_KEY = [Environment]::GetEnvironmentVariable('FOOTBALLMANAGER_GEMINI_API_KEY', 'User')
    if ([string]::IsNullOrWhiteSpace($env:FOOTBALLMANAGER_GEMINI_API_KEY)) {
        Write-Warning 'Gemini is not configured. The website and WPF start; AI displays a service unavailable message.'
    }
}
dotnet run --project (Join-Path $PSScriptRoot 'FootballManager.WCF/FootballManager.WCF.csproj')
