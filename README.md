**Open PowerShell (it cannot be in elevated mode because of OneDrive design)**

[Download here](https://github.com/rodneyviana/ODSyncService/releases)

**Before running the first time, use this to unblock the DLL that you downloaded:**
```
PS C:\ODTool> Unblock-File -Path C:\ODTool\OneDriveLib.dll # change path if necessary
```

**Run this:**
```
Import-Module OneDriveLib.dll
Get-ODStatus
```

**This is an example of the output:**
```
PS C:\ODTool> Import-Module OneDriveLib.dll
PS C:\ODTool> Get-ODStatus

StatusString : UpToDate
LocalPath    : E:\MicrosoftOnedrive\OneDrive - My Company
UserSID      : S-1-5-21-124000000-708000000-1543000000-802052
UserName     : CONTOSO\rodneyviana
ServiceType  : Business1

StatusString : UpToDate
LocalPath    : D:\Onedrive
UserSID      : S-1-5-21-124000000-708000000-1543000000-802052
UserName     : CONTOSO\rodneyviana
ServiceType  : Personal
```

**Syntax**

```
Get-ODStatus [-ByPath <file-or-folder-path>] [-Type <type-of-service>]
             [-CLSID <icon-overlay-guid>] [-IncludeLog $true|$false]

(All parameters are optional and normally not used on most cases)

Where:
    -ByPath <file-or-folder-path> - Test a particular file of folder to seee if it is synchronized
    -Type <type-of-service> - Filter response by type of service (e.g. Business1 or Personal)
    -CLSID <icon-overlay-guid> - Test a different type of icon overlay (you may use this to test other services like DropBox
                                 if know their Guid)
    -IncludeLog $true|$false - If $true will create a detailed log file on temp path (use $env:Temp in PowerPoint to find
                               the temp folder). The file name starts with OneDriveLib.

Examples:

List status of all OneDrive instances:

Get-ODStatus

Check if a particular file of folder is synchronized

Get-ODStatus -ByPath "$($env:OneDrive)\DalyReports\"

Save and list the log file:

Get-ODStatus -IncludeLog $true

Get-Item -Path "$($env:Temp)\OneDriveLib*"


```
