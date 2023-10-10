using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace OneDrive {
    public static class UacHelper {
        private const string uacRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Policies\System";
        private const string uacRegistryValue = "EnableLUA";
        private const uint STANDARD_RIGHTS_READ = 0x00020000;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public static readonly bool IsUacEnabled = (Registry.LocalMachine.OpenSubKey(uacRegistryKey, false)?.GetValue(uacRegistryValue) ?? 1).Equals(1);
        public enum TOKEN_INFORMATION_CLASS {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }
        public enum TOKEN_ELEVATION_TYPE {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

        public static bool IsProcessElevated {
            get {
                if (IsUacEnabled) {
                    if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_READ, out IntPtr tokenHandle)) {
                        throw new ApplicationException("Could not process token. Win32 Error Code: " + Marshal.GetLastWin32Error());
                    }
                    TOKEN_ELEVATION_TYPE elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
                    int elevationResultSize = Marshal.SizeOf((int)elevationResult);
                    if (GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, Marshal.AllocHGlobal(elevationResultSize), (uint)elevationResultSize, out _)) {
                        return (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(Marshal.AllocHGlobal(elevationResultSize)) == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                    } else {
                        throw new ApplicationException("Unable to determine the current elevation.");
                    }
                } else {
                    return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
        }
    }
}
