param([switch]$Demo)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$project = Join-Path $PSScriptRoot 'Web/FinanceTracker.Web'
if ($Demo) {
    # Separate sample database so the normal personal database remains empty.
    $demoDb = Join-Path $project 'App_Data/demo.db'
    dotnet run --project $project -- "--ConnectionStrings:Finance=Data Source=$demoDb" --Database:SeedDemo=true
} else {
    dotnet run --project $project
}
exit $LASTEXITCODE

