# Now including support for On Demand!

**Watch this video to get started**

[![OneDriveLib Introduction](https://img.youtube.com/vi/2AqB-7Uq9lc/0.jpg)](https://www.youtube.com/watch?v=2AqB-7Uq9lc)

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

LocalPath    : E:\MicrosoftOnedrive\OneDrive - My Company
UserSID      : S-1-5-21-124000000-708000000-1543000000-802052
UserName     : CONTOSO\rodneyviana
DisplayName  : OneDrive - Contoso
ServiceType  : Business1
StatusString : Looking for changes

StatusString : UpToDate
LocalPath    : D:\Onedrive
UserSID      : S-1-5-21-124000000-708000000-1543000000-802052
DisplayName  : OneDrive - Personal
UserName     : CONTOSO\rodneyviana
ServiceType  : Personal
StatusString : Up To Date
```

**Syntax:**
```
Get-ODStatus [-Type <type-Name>] [-ByPath <path>] [CLSID <guid>]
             [-IncludeLog] [-Verbose]

Or
Get-ODStatus -OnDemandOnly [-Type <type-Name>] [-IncludeLog] [-Verbose]

```
**Where:**
```
-Type <type>       Only returns if Service Type matches <type>
                   Example: Get-ODStatus -Type Personal

-ByPath <path>     Only checks a particular folder or file status
                   Example: Get-ODStatus -Path "$env:OneDrive\docs"

-CLSD <guid>       Verify only a particular GUID (not used normally)
                   Example: Get-ODStatus -CLSD A0396A93-DC06-4AEF-BEE9-95FFCCAEF20E

-IncludeLog        If present will save a log file on the temp folder

-Verbose           Show verbose information

-OnDemandOnly      Normally On Demand is only tested as a fallback, when
                   -OnDemandOnly is present it goes directly to 
                   On Demand status. This may resolve flicker issues
                  
```

**Important:**

On Demand Status **ONLY** works if OneDrive icon is visible on the taskbar

Examples:

List status of all OneDrive instances:

Get-ODStatus

Check if a particular file of folder is synchronized

Get-ODStatus -ByPath "$($env:OneDrive)\DalyReports\"

Save and list the log file:

Get-ODStatus -IncludeLog

Get-Item -Path "$($env:Temp)\OneDriveLib*"

For On Demand installations:

Get-ODStatus -OnDemandOnly
```
