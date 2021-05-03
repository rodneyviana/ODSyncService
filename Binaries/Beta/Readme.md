## Beta version of OnDemand monitoring

## Disclaimer
Be aware that this software is provided "AS IS". This is a beta and may never see the light of the day.

## 1. Getting things ready (choose Option 1 or 2 not both)

## 1.1 Option 1. Using installer (easier): ##

 - Download Setup.zip
 - Unblock the zip
 - Run the installer
 
## 1.2 Option 2. Not using Installer: ##

- You may need to install C++ Runtime before running the application if you have a previous version
- The standalone BETA version is not digitally signed. 
- Download ODOnDemand.exe from this Beta folder. Unblock the file ODOnDemand.exe (beta is not signed). Unblock the file as it was downloaded from the Internet it will most likely be marked as blocked.
- Make sure the application is located on a folder that all users of the machine have access (e.g. c:\tools).
- In order to monitor OneDrive NEEDS to be on the notification area of the taskbar
- If auto monitoring is not enabled the log will onlys starts after the first change in status
- The log file location is *%LOCALAPPDATA%\OneDriveMonitor\Logs*
- The application for now spawns a console window


## 2. Registering the application to monitor OneDrive (auto monitoring)

- Run cmd as administrator
- run this command: *ODOnDemand.exe -onLaunch*
- If things were correct you will see [Debugger="*path-to-app*\ODOnDemmand.exe -launch] under one of the following keys in regedit.
- For 64-bit OS: HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\OneDrive.exe
- For 32-bit OS: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\OneDrive.exe
- The monitoring will ONLY take effect next time OneDrive.exe is loaded

## 3. Unregistering the monitor

- Run cmd as administrator
- run this command: *ODOnDemand.exe -clear*
- If the command does not work you need only to delete key Debugger on one of the keys in previous instructions

## 4. Running for the first time or running when auto monitoring is not set (-onLaunch)

- The automatic monitoring will only happens when OneDrive.exe starts (reboot or new sign in)
- You may choose to run only when you need it, but remember that any log entry will happen after a change in status
- If you want to attach to a running OneDrive instance get the PID in Task Manager or using this PowerShell command: *Get-Process -Name OneDrive | Select Id*
- There may be more than one instance of OneDrive (personal and business) and you need to attach to both
- This command will attach to the process: *ODOnDemand.exe -attach PID* where PID is the process id.

## 5. Example of PowerShell script to capture the latest status

```

$content = Get-Content -Path "$($env:LOCALAPPDATA)\OneDriveMonitor\Logs\OneDriveMonitor_$((Get-Date).ToString('yyyy-MM-dd')).log";
$lastStatus = '';
for($count=$content.Count-1;$count -gt 0; $count--)
{
    if($content[$count].Contains('IconChange'))
    {
        $lastStatus = $content[$count].Split("`t")[3];
        $lastStatus = $lastStatus.Split("'")[1];
        break;
    }
    
}

Write-Host $lastStatus
```

## 6. Example of PowerShell script to capture latest status when there is more than one OneDrive client installed ##

```
Add-Type -AssemblyName System.DirectoryServices.AccountManagement
$sid = [System.DirectoryServices.AccountManagement.UserPrincipal]::Current.Sid.Value


$items = Get-ChildItem -Recurse HKLM:SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager | Where-Object { $_.Name -imatch $sid }

$hashName = @{}
$items | ? { $_.GetValue("DisplayNameResource") } | % { $hashName.Add($_.GetValue("DisplayNameResource"), '') } 
$path = "$($env:LOCALAPPDATA)\OneDriveMonitor\Logs\OneDriveMonitor_$((Get-Date).ToString('yyyy-MM-dd')).log";
$content = @();
if([System.IO.File]::Exists($path))
{
    $content = Get-Content -Path $path;
} else
{
    Write-Error "Log file not found at '$path'" -Category OpenError
    $hashName.Clear();
    $hashName.Add("_error", "Log file not found at '$path'");
}
$lastStatus = '';
for($count=$content.Count-1;$count -gt 0; $count--)
{
    if($content[$count].Contains('IconChange'))
    {
        $lastStatus = $content[$count].Split("`t")[3];
        $lastName = $lastStatus.Split("'")[1];
        if($hashName[$lastName] -ne '')
        {
            $hashName[$lastName] = $lastStatus;
            $count = ($hashName.Values | ? { [string]::IsNullOrEmpty($_) }).Count;
            if($count -eq 0)
            {
                break;
            }

        }
    }
    
}


$hashName | ConvertTo-Json

```
