[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "O comando 'dotnet $($Arguments -join ' ')' falhou com o código $LASTEXITCODE."
    }
}

$coverageDirectory = Join-Path $PSScriptRoot 'coverage'
$reportDirectory = Join-Path $PSScriptRoot 'TestResults/Report'
$coverageSettings = Join-Path $PSScriptRoot 'coverage.settings.xml'
$testProjects = @(
    @{
        Name = 'unit'
        Project = Join-Path $PSScriptRoot 'BotDeScans.UnitTests/BotDeScans.UnitTests.csproj'
    },
    @{
        Name = 'integration'
        Project = Join-Path $PSScriptRoot 'BotDeScans.IntegrationTests/BotDeScans.IntegrationTests.csproj'
    }
)

if (-not $NoBuild) {
    Write-Host 'Compilando a solution em Release...'
    Invoke-DotNet -Arguments @('build', 'BotDeScans.sln', '--configuration', 'Release')
}

Write-Host 'Restaurando as ferramentas locais...'
Invoke-DotNet -Arguments @('tool', 'restore', '--tool-manifest', '.config/dotnet-tools.json')

if (Test-Path -LiteralPath $reportDirectory) {
    Remove-Item -LiteralPath $reportDirectory -Recurse -Force
}

$coverageFiles = @()
foreach ($testProject in $testProjects) {
    $coverageOutputDirectory = Join-Path $coverageDirectory $testProject.Name
    New-Item -ItemType Directory -Path $coverageOutputDirectory -Force | Out-Null

    $coverageOutput = Join-Path $coverageOutputDirectory 'coverage.cobertura.xml'
    if (Test-Path -LiteralPath $coverageOutput) {
        Remove-Item -LiteralPath $coverageOutput -Force
    }

    $coverageFiles += $coverageOutput

    Write-Host "Executando testes $($testProject.Name) com cobertura..."
    Invoke-DotNet -Arguments @(
        'test',
        '--project', $testProject.Project,
        '--configuration', 'Release',
        '--no-build',
        '--coverage',
        '--coverage-output-format', 'cobertura',
        '--coverage-output', $coverageOutput,
        '--coverage-settings', $coverageSettings
    )
}

Write-Host 'Gerando relatório de cobertura...'
$reports = $coverageFiles -join ';'
Invoke-DotNet -Arguments @(
    'tool', 'run', 'reportgenerator', '--',
    "-reports:$reports",
    "-targetdir:$reportDirectory",
    '-reporttypes:Html;TextSummary'
)

$summaryPath = Join-Path $reportDirectory 'Summary.txt'
if (Test-Path -LiteralPath $summaryPath) {
    Get-Content -LiteralPath $summaryPath
}

Write-Host "Relatório HTML: $(Join-Path $reportDirectory 'index.html')"
