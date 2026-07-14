param(
    [string]$ProjectName = "StaffEngineerHandbook",
    [string]$Framework = "net8.0"
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Copy-Template {
    param(
        [string]$Source,
        [string]$Destination
    )

    $parent = Split-Path $Destination -Parent

    if ($parent) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    Copy-Item $Source $Destination -Force
}

Write-Host ""
Write-Host "Staff Engineer Handbook - Bootstrap" -ForegroundColor Green

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK is not installed or is not available in PATH."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git is not installed or is not available in PATH."
}

if (-not (Test-Path "templates")) {
    throw "The templates directory was not found."
}

Write-Step "Creating directory structure"

$directories = @(
    "docs/level-1-backend-foundations",
    "docs/level-2-web-apis",
    "docs/level-3-data-access",
    "docs/level-4-cloud",
    "docs/level-5-architecture",
    "docs/level-6-staff-engineering",
    "src/Level1.BackendFoundations/Examples",
    "src/Level2.WebApis",
    "src/Level3.DataAccess",
    "src/Level4.Cloud",
    "src/Level5.Architecture",
    "src/Level6.StaffEngineering",
    "scripts"
)

foreach ($directory in $directories) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$plannedDirectories = @(
    "docs/level-2-web-apis",
    "docs/level-3-data-access",
    "docs/level-4-cloud",
    "docs/level-5-architecture",
    "docs/level-6-staff-engineering",
    "src/Level2.WebApis",
    "src/Level3.DataAccess",
    "src/Level4.Cloud",
    "src/Level5.Architecture",
    "src/Level6.StaffEngineering",
    "scripts"
)

foreach ($directory in $plannedDirectories) {
    New-Item -ItemType File -Path "$directory/.gitkeep" -Force | Out-Null
}

Write-Step "Creating .NET solution and Level 1 project"

$solutionPath = "$ProjectName.sln"
$projectPath = "src/Level1.BackendFoundations/Level1.BackendFoundations.csproj"

if (-not (Test-Path $solutionPath)) {
    dotnet new sln --name $ProjectName
}

if (-not (Test-Path $projectPath)) {
    dotnet new console `
        --name "Level1.BackendFoundations" `
        --output "src/Level1.BackendFoundations" `
        --framework $Framework `
        --use-program-main
}

$projectAlreadyAdded =
    dotnet sln $solutionPath list |
    Select-String -SimpleMatch $projectPath

if (-not $projectAlreadyAdded) {
    dotnet sln $solutionPath add $projectPath
}

Write-Step "Copying templates"

Copy-Template `
    "templates/Program.cs.txt" `
    "src/Level1.BackendFoundations/Program.cs"

Copy-Template `
    "templates/Q001ValueTypesVsReferenceTypes.cs.txt" `
    "src/Level1.BackendFoundations/Examples/Q001ValueTypesVsReferenceTypes.cs"

Copy-Template `
    "templates/001-value-types-vs-reference-types.md.txt" `
    "docs/level-1-backend-foundations/001-value-types-vs-reference-types.md"

Copy-Template "templates/README.md.txt" "README.md"
Copy-Template "templates/editorconfig.txt" ".editorconfig"

$workspaceContent = Get-Content `
    "templates/Workspace.code-workspace.txt" `
    -Raw

$workspaceContent = $workspaceContent.Replace(
    "__PROJECT_NAME__",
    $ProjectName
)

Set-Content `
    -Path "$ProjectName.code-workspace" `
    -Value $workspaceContent `
    -Encoding utf8

Write-Step "Creating repository support files"

dotnet new gitignore --force

if (-not (Test-Path ".git")) {
    git init
}

Write-Step "Restoring and building"

dotnet restore $solutionPath
dotnet build $solutionPath --no-restore

Write-Host ""
Write-Host "Bootstrap completed successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Open the workspace:"
Write-Host "  code $ProjectName.code-workspace" -ForegroundColor Yellow
Write-Host ""
Write-Host "Run question 001:"
Write-Host "  dotnet run --project src/Level1.BackendFoundations -- 001" `
    -ForegroundColor Yellow
