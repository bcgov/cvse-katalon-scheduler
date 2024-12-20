##If AGORRIE deployment script not working as expected download nssm and use the following - Regards CWeiner

# Download NSSM if not present
if (-not (Test-Path "C:\nssm\nssm.exe")) {
    New-Item -ItemType Directory -Force -Path "C:\nssm"
    Invoke-WebRequest -Uri "https://nssm.cc/release/nssm-2.24.zip" -OutFile "C:\nssm\nssm.zip"
    Expand-Archive "C:\nssm\nssm.zip" -DestinationPath "C:\nssm" -Force
    Move-Item "C:\nssm\nssm-2.24\win64\nssm.exe" "C:\nssm\" -Force
}

# Install the service
$serviceName = "KatalonScheduler"
$exePath = "C:\Program Files\dotnet\dotnet.exe"
$appPath = "C:\KatalonScheduler\KatalonScheduler.dll"

# Remove existing service if it exists
& C:\nssm\nssm.exe stop $serviceName
& C:\nssm\nssm.exe remove $serviceName confirm

# Install new service
& C:\nssm\nssm.exe install $serviceName $exePath $appPath
& C:\nssm\nssm.exe set $serviceName AppDirectory "C:\KatalonScheduler"
& C:\nssm\nssm.exe set $serviceName DisplayName "Katalon Scheduler Service"
& C:\nssm\nssm.exe set $serviceName Description "Katalon Test Automation Scheduler"
& C:\nssm\nssm.exe set $serviceName Start SERVICE_AUTO_START

# Set failure actions - restart on failure
& C:\nssm\nssm.exe set $serviceName AppExit Default Restart
& C:\nssm\nssm.exe set $serviceName AppRestartDelay 30000