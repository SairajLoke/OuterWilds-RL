# update_mod.ps1
# Auto-deploys ShipLogger to OWML Mods folder

$uniqueName = "psi.ShipLogger"


$TargetDir = "C:\Users\Sairaj Loke\AppData\Roaming\OuterWildsModManager\OWML\Mods\$uniqueName"

$SourceDll = "bin\Release\net48\ShipLogger.dll"
$ManifestPath = "$TargetDir\manifest.json"

Write-Host " Updating ShipLogger mod..." -ForegroundColor Green

# Verify DLL exists
if (-not (Test-Path $SourceDll)) {
    Write-Error " DLL not found: $SourceDll" -ForegroundColor Red
    Write-Host " Run: dotnet build ShipLogger.csproj -c Release" -ForegroundColor Yellow
    exit 1
}

# Create target directory
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

# Copy DLL
Copy-Item -Path $SourceDll -Destination $TargetDir -Force
Copy-Item -Path "manifest.json" -Destination $ManifestPath -Force

Write-Host "Copied ShipLogger.dll" -ForegroundColor Green