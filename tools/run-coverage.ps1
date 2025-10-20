param(
    [string]$Project = 'tests/Fast.UndoRedo.Core.Tests/Fast.UndoRedo.Core.Tests.csproj',
    [string]$Configuration = 'Release',
    [string]$OutputDir = 'CoverageResults'
)

Write-Host "Running tests with coverage for project: $Project"

if (-Not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

$coverletOutput = Join-Path (Resolve-Path $OutputDir).Path 'coverage.'

$cmd = "dotnet test $Project -c $Configuration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=$coverletOutput"
Write-Host "Executing: $cmd"

$exit = & dotnet test $Project -c $Configuration --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=$coverletOutput
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet test failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# locate coverage file
$f = Get-ChildItem -Path $OutputDir -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $f) {
    Write-Error "Coverage file not found in $OutputDir"
    exit 1
}

Write-Host "Found coverage file: $($f.FullName)"

$xml = [xml](Get-Content $f.FullName)
$lineRate = 0.0
if ($xml.DocumentElement -and $xml.DocumentElement.GetAttribute('line-rate')) { $lineRate = [double]$xml.DocumentElement.GetAttribute('line-rate') }
elseif ($xml.coverage -and $xml.coverage.'@line-rate') { $lineRate = [double]$xml.coverage.'@line-rate' }

$percent = [math]::Round($lineRate * 100.0, 2)
Write-Host "COVERAGE_PERCENTAGE:$percent"
exit 0
