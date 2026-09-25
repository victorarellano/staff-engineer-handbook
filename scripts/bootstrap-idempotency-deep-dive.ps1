param(
    [string]$Solution = "StaffEngineerHandbook.sln"
)

$ErrorActionPreference = "Stop"

$Root = Get-Location
$DeepDiveRoot = Join-Path $Root "src/DeepDives/Idempotency"

Write-Host ""
Write-Host "=== Idempotency Deep Dive Bootstrap ==="
Write-Host "Root: $Root"
Write-Host ""

# ------------------------------------------------------------
# Helpers
# ------------------------------------------------------------

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$ErrorMessage
    )

    & dotnet @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw $ErrorMessage
    }
}

function New-Lab {
    param(
        [Parameter(Mandatory)]
        [string]$Directory,

        [Parameter(Mandatory)]
        [string]$ProjectPrefix
    )

    $LabRoot = Join-Path $DeepDiveRoot $Directory
    $ApiProject = Join-Path $LabRoot "$ProjectPrefix.Api"
    $TestsProject = Join-Path $LabRoot "$ProjectPrefix.IntegrationTests"

    $ApiCsproj = Join-Path $ApiProject "$ProjectPrefix.Api.csproj"
    $TestsCsproj = Join-Path $TestsProject "$ProjectPrefix.IntegrationTests.csproj"

    Write-Host ""
    Write-Host "--- $Directory ---"

    New-Item -ItemType Directory -Force -Path $LabRoot | Out-Null

    if (-not (Test-Path $ApiCsproj)) {
        Write-Host "Creating $ProjectPrefix.Api..."

        Invoke-DotNet `
            -Arguments @(
                "new", "webapi",
                "-n", "$ProjectPrefix.Api",
                "-o", $ApiProject,
                "--no-https"
            ) `
            -ErrorMessage "Failed to create $ProjectPrefix.Api"
    }
    else {
        Write-Host "$ProjectPrefix.Api already exists. Skipping."
    }

    if (-not (Test-Path $TestsCsproj)) {
        Write-Host "Creating $ProjectPrefix.IntegrationTests..."

        Invoke-DotNet `
            -Arguments @(
                "new", "xunit",
                "-n", "$ProjectPrefix.IntegrationTests",
                "-o", $TestsProject
            ) `
            -ErrorMessage "Failed to create $ProjectPrefix.IntegrationTests"
    }
    else {
        Write-Host "$ProjectPrefix.IntegrationTests already exists. Skipping."
    }

    Write-Host "Adding API reference to integration tests..."

    $References = & dotnet list $TestsCsproj reference

    if ($References -notmatch [regex]::Escape("$ProjectPrefix.Api.csproj")) {
        Invoke-DotNet `
            -Arguments @(
                "add", $TestsCsproj,
                "reference", $ApiCsproj
            ) `
            -ErrorMessage "Failed to add API reference for $ProjectPrefix"
    }
    else {
        Write-Host "Project reference already exists. Skipping."
    }

    Write-Host "Adding projects to solution..."

    Invoke-DotNet `
        -Arguments @(
            "sln", $SolutionPath,
            "add", $ApiCsproj
        ) `
        -ErrorMessage "Failed to add $ProjectPrefix.Api to solution"

    Invoke-DotNet `
        -Arguments @(
            "sln", $SolutionPath,
            "add", $TestsCsproj
        ) `
        -ErrorMessage "Failed to add $ProjectPrefix.IntegrationTests to solution"
}

# ------------------------------------------------------------
# 1. Validate repository
# ------------------------------------------------------------

$SolutionPath = Join-Path $Root $Solution

if (-not (Test-Path $SolutionPath)) {
    throw @"
Solution not found:

    $SolutionPath

Run this script from the StaffEngineerHandbook repository root.
"@
}

# ------------------------------------------------------------
# 2. Create Deep Dive root
# ------------------------------------------------------------

Write-Host "[1/5] Creating Deep Dive structure..."

New-Item `
    -ItemType Directory `
    -Force `
    -Path $DeepDiveRoot | Out-Null

# ------------------------------------------------------------
# 3. Create labs
# ------------------------------------------------------------

Write-Host "[2/5] Creating labs..."

New-Lab `
    -Directory "01-Naive" `
    -ProjectPrefix "Idempotency.Naive"

New-Lab `
    -Directory "02-InMemory" `
    -ProjectPrefix "Idempotency.InMemory"

New-Lab `
    -Directory "03-Database" `
    -ProjectPrefix "Idempotency.Database"

# ------------------------------------------------------------
# 4. Create lab README
# ------------------------------------------------------------

Write-Host ""
Write-Host "[3/5] Creating README..."

$ReadmePath = Join-Path $DeepDiveRoot "README.md"

if (-not (Test-Path $ReadmePath)) {

@'
# Idempotency Labs

These labs progressively explore the guarantees required to make
backend operations idempotent.

They accompany:

`docs/deep-dives/integration/idempotency.md`

## 01 - Naive

Demonstrates the initial problem.

Two HTTP requests containing the same payload are treated as two
independent operations and therefore create two different expenses.

The server has no information that allows it to distinguish a retry
from a new business operation.

## 02 - In-Memory Idempotency

Introduces a client-generated `Idempotency-Key`.

The server associates the logical operation with the resource created
by that operation.

This demonstrates the idempotency mental model, but the guarantee is
limited to application state held by a single running instance.

## 03 - Database-Enforced Idempotency

Moves the idempotency invariant to PostgreSQL.

The database becomes responsible for atomically enforcing uniqueness
of the operation identifier.

This lab will explore concurrent requests and multiple application
instances sharing the same persistence layer.

## Learning Progression

The labs intentionally preserve intermediate implementations:

    identical payload
          |
          v
    01 - Naive
          |
          | introduce operation identity
          v
    02 - In-Memory
          |
          | move invariant to persistence
          v
    03 - Database

The goal is not only to show the final implementation, but also to
make the problem and the evolution of its guarantees executable.
'@ | Set-Content -Path $ReadmePath -Encoding UTF8

}
else {
    Write-Host "README.md already exists. Skipping."
}

# ------------------------------------------------------------
# 5. Build
# ------------------------------------------------------------

Write-Host "[4/5] Restoring and building solution..."

Invoke-DotNet `
    -Arguments @(
        "build", $SolutionPath
    ) `
    -ErrorMessage "Solution build failed"

# ------------------------------------------------------------
# Summary
# ------------------------------------------------------------

Write-Host ""
Write-Host "[5/5] Bootstrap completed successfully."
Write-Host ""
Write-Host "Structure:"
Write-Host ""
Write-Host "src/DeepDives/Idempotency/"
Write-Host "|-- 01-Naive/"
Write-Host "|   |-- Idempotency.Naive.Api/"
Write-Host "|   `-- Idempotency.Naive.IntegrationTests/"
Write-Host "|"
Write-Host "|-- 02-InMemory/"
Write-Host "|   |-- Idempotency.InMemory.Api/"
Write-Host "|   `-- Idempotency.InMemory.IntegrationTests/"
Write-Host "|"
Write-Host "|-- 03-Database/"
Write-Host "|   |-- Idempotency.Database.Api/"
Write-Host "|   `-- Idempotency.Database.IntegrationTests/"
Write-Host "|"
Write-Host "`-- README.md"
Write-Host ""
Write-Host "Next step:"
Write-Host "  Preserve the current implementation as 01-Naive."