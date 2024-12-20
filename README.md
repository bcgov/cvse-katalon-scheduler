<<<<<<< HEAD
# cvse-katalon-scheduler
Scheduler Application for Katalon Tests
=======
# Deployment Instructions
Note: .net 9 runtime required
Note: you'll need to copy db files from source to location simply take the KatalonScheduler and KatalonScheduler.Hangfire from repo and place in Katalon Sechduler folder
Note: You'll need to download the latest katalon runtime engine

1. First, publish the application:(this is done already or you can republish on your own using the below)
Note: the app folder that will need copied to the machine is in the main source folder
dotnet publish -c Release -o C:\KatalonScheduler

2. Set up environment variables:


Open PowerShell as Administrator
Navigate to this directory
Edit the script first to add your actual API keys
Run: .\setup-env.ps1



3. Install the Windows Service:


Open PowerShell as Administrator
Navigate to this directory
Run: .\install-service.ps1


4. Verify Installation:


Open Services (services.msc)
Look for "Katalon Scheduler Service"
Service should be running
Access Hangfire dashboard at http://localhost:5000/hangfire
>>>>>>> Chris-Dev
