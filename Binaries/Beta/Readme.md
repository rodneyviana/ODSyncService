## Beta version of OnDemand monitoring

## Disclaimer
Be aware that this software is provided "AS IS". This is a beta and may never see the light of the day.

## Getting things ready

- The BETA version is not digitally signed. Check the hash at the bottom (Get-FileHash)
- Download OnOnDemand.exe from this Beta folder. Unblock the file ODOnDemand.exe (beta is not signed). Unblock the file as it was downloaded from the Internet it will most likely be marked as blocked.
- Make sure the application is located on a folder that all users of the machine have access.
- In order to monitor OneDrive NEEDS to be on the notification area of the taskbar
- If auto monitoring is not enabled the log will onlys starts after the first change in status
- The log file location is *%LOCALAPPDATA%\OneDriveMonitor\Logs*
- The application for now spawns a console window


## Registering the application to monitor OneDrive (auto monitoring)

- Run cmd as administrator
- run this command: *ODOnDemand.exe -onLaunch*
- If things were correct you will see [Debugger="*path-to-app*\ODOnDemmand.exe -launch] under one of the following keys in regedit.
- For 64-bit OS: HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\OneDrive.exe
- For 32-bit OS: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\OneDrive.exe
- The monitoring will ONLY take effect next time OneDrive.exe is loaded

## Unregistering the monitor

- Run cmd as administrator
- run this command: *ODOnDemand.exe -clear*
- If the command does not work you need only to delete key Debugger on one of the keys in previous instructions

## Running for the first time or running when auto monitoring is not set (-onLaunch)

- The automatic monitoring will only happens when OneDrive.exe starts (reboot or new sign in)
- You may choose to run only when you need it, but remember that any log entry will happen after a change in status
- If you want to attach to a running OneDrive instance get the PID in Task Manager or using this PowerShell command: *Get-Process -Name OneDrive | Select Id*
- There may be more than one instance of OneDrive (personal and business) and you need to attach to both
- This command will attach to the process: *ODOnDemand.exe -attach PID* where PID is the process id.



## Latest version Hash (SHA256)
B155355587DE8BCAEFBBC18D2D05F288B30E34C0AEDE078CEBB52B9289FE977B
