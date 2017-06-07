Open PowerShell (it cannot be in elevated mode because of OneDrive design)
Run this:
Import-Module OneDriveLib.dll
Get-ODStatus


This is an example of the output:
PS C:\ODTool> Import-Module OneDriveLib.dll
PS C:\ODTool> Get-ODStatus

StatusString : UpToDate
LocalPath    : E:\MicrosoftOnedrive\OneDrive - My Company
UserSID      : S-1-5-21-124000000-708000000-1543000000-802052
UserName     : CONTOSO\rodneyviana
ServiceType  : Business1

StatusString : UpToDate
LocalPath    : D:\Onedrive
UserSID      : S-1-5-21-124000000-708000000-1543000000-802052
UserName     : CONTOSO\rodneyviana
ServiceType  : Personal


