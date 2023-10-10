using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OneDrive.Logging;

namespace OneDrive.Native {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OneDriveState {
        public int CurrentState;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = API.MAX_STATE_LABEL)]
        public string Label;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = API.MAX_ICON_URI)]
        public string IconUri;
        [MarshalAs(UnmanagedType.Bool)]
        public bool isQuotaAvailable;
        public ulong TotalQuota;
        public ulong UsedQuota;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = API.MAX_QUOTA_LABEL)]
        public string QuotaLabel;
        public byte IconColorA;
        public byte IconColorR;
        public byte IconColorG;
        public byte IconColorB;
    };

    public class API {
        public const int MAX_STATE_LABEL = 255;
        public const int MAX_ICON_URI = 1024;
        public const int MAX_QUOTA_LABEL = 255;

        [DllImport("ODNative.dll", EntryPoint = "?GetStatusByTypeApi@@YAJPEA_WPEAUOneDriveState@@@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetStatusByTypeApi(
            [In, MarshalAs(UnmanagedType.LPWStr)] string SyncRootId,
            ref OneDriveState State
        );

        [DllImport("ODNative.dll", EntryPoint = "?GetStatusByTypeApi@@YAJPA_WPAUOneDriveState@@@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetStatusByTypeApi32(
            [In, MarshalAs(UnmanagedType.LPWStr)] string SyncRootId,
            ref OneDriveState State
        );

        [DllImport("ODNative.dll", EntryPoint = "?GetShellInterfaceFromGuid@@YAJPEAHPEA_W1@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetShellInterfaceFromGuid(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool IsTrue,
            [In, MarshalAs(UnmanagedType.LPWStr)] string GuidString,
            [In, MarshalAs(UnmanagedType.LPWStr)] string Path);

        [DllImport("ODNative.dll", EntryPoint = "?GetShellInterfaceFromGuid@@YAJPAHPA_W1@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetShellInterfaceFromGuid32(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool IsTrue,
            [In, MarshalAs(UnmanagedType.LPWStr)] string GuidString,
            [In, MarshalAs(UnmanagedType.LPWStr)] string Path);

        [DllImport("ODNative.dll", EntryPoint = "?GetStatusByType@@YAJPEA_W0HPEAH@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetStatusByType(
            [In, MarshalAs(UnmanagedType.LPWStr)] string OneDriveType,
            [In, Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder Status,
            [In, MarshalAs(UnmanagedType.I4)] int Size,
            [In, Out, MarshalAs(UnmanagedType.I4)] ref int ActualSize);

        [DllImport("ODNative.dll", EntryPoint = "?GetStatusByType@@YAJPA_W0HPAH@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint GetStatusByType32(
            [In, MarshalAs(UnmanagedType.LPWStr)] string OneDriveType,
            [In, Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder Status,
            [In, MarshalAs(UnmanagedType.I4)] int Size,
            [In, Out, MarshalAs(UnmanagedType.I4)] ref int ActualSize);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);
        public static bool UnloadModule() {
            foreach (ProcessModule mod in Process.GetCurrentProcess().Modules) {
                if (mod.ModuleName.ToLower() == "ODNative.dll".ToLower()) {
                    FreeLibrary(mod.BaseAddress);
                    return true;
                }
            }
            return false;
        }
        public static bool LoadModuleIfNot() {
            bool loaded = false;
            foreach (ProcessModule mod in Process.GetCurrentProcess().Modules) {
                loaded = mod.ModuleName.ToLower() == "ODNative.dll".ToLower();
            }
            if (!loaded) {
                return IntPtr.Zero != LoadLibrary("ODNative.dll");
            }
            return true;
        }
        static public bool IsTrue<T>(string Path) {

            Guid CLSID = typeof(T).GUID;

            var isType = IsCertainType(Path, CLSID);
            WriteLog.WriteToFile = true;
            WriteLog.WriteInformationEvent(String.Format("Testing Type: {0} [{1}], Path: {2}: {3}", typeof(T).ToString(), CLSID, Path, isType));

            return IsCertainType(Path, CLSID);
        }

        static public uint GetStateBySyncRootId(string SyncRootId, out OneDriveState State) {
            State = new OneDriveState();
            uint hr;
            if (Marshal.SizeOf(IntPtr.Zero) == 8)
                hr = GetStatusByTypeApi(SyncRootId, ref State);
            else
                hr = GetStatusByTypeApi32(SyncRootId, ref State);
            return hr;
        }

        static public string GetStatusByDisplayName(string DisplayName) {
            int size = 2000;
            StringBuilder status = new StringBuilder(2000);
            if (Marshal.SizeOf(IntPtr.Zero) == 8)
                _ = GetStatusByType(DisplayName, status, size, ref size);
            else
                _ = GetStatusByType32(DisplayName, status, size, ref size);
            string retStatus = status.ToString();
            var start = Math.Max(0, retStatus.IndexOf("\n"));
            return retStatus.Replace("\n", "").Substring(start);
        }
        static public bool IsCertainType(string Path, Guid CLSID) {
            bool isTrue = false;
            uint hr = 1;
            if (Marshal.SizeOf(IntPtr.Zero) == 8)
                hr = GetShellInterfaceFromGuid(out isTrue, CLSID.ToString("B"), Path + "\\");
            else
                hr = GetShellInterfaceFromGuid32(out isTrue, CLSID.ToString("B"), Path + "\\");
#if DEBUG
            WriteLog.WriteToFile = true;
#endif
            WriteLog.WriteInformationEvent(String.Format("Testing CLSID: {0}, Path: {1}, HR=0x{2:X}", CLSID.ToString("B"), Path, hr));
            if (hr == 0)
                return isTrue;
            else
                return false;
        }
    }


    [ComVisible(false)]
    [ComImport]
    [Guid("0C6C4200-C589-11D0-999A-00C04FD655E1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

    public interface IShellIconOverlayIdentifier {

        [PreserveSig]
        int IsMemberOf([MarshalAs(UnmanagedType.LPWStr)] string path,
            uint attributes);

        [PreserveSig]
        int GetOverlayInfo(
        IntPtr iconFileBuffer,
        int iconFileBufferSize,
        out int iconIndex,
        out uint flags);

        [PreserveSig]
        int GetPriority(out int priority);

    }

    [ComVisible(false)]
    [ComImport]
    [Guid("F241C880-6982-4CE5-8CF7-7085BA96DA5A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconUpToDate : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("BBACC218-34EA-4666-9D7A-C78F2274A524")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconError : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("5AB7172C-9C11-405C-8DD5-AF20F3606282")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconShared : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("A78ED123-AB77-406B-9962-2A5D9D2F7F30")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconSharedSync : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("A0396A93-DC06-4AEF-BEE9-95FFCCAEF20E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconSync : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("9AA2F32D-362A-42D9-9328-24A483E2CCC3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconReadOnly : IShellIconOverlayIdentifier { }


    [ComVisible(false)]
    [ComImport]
    [Guid("8BA85C75-763B-4103-94EB-9470F12FE0F7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconProErrorConflict : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("CD55129A-B1A1-438E-A425-CEBC7DC684EE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconProSyncInProgress : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("E768CD3B-BDDC-436D-9C13-E1B39CA257B1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconProInSync : IShellIconOverlayIdentifier { }


    [ComVisible(false)]
    [ComImport]
    [Guid("8BA85C75-763B-4103-94EB-9470F12FE0F7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconGrooveError : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("CD55129A-B1A1-438E-A425-CEBC7DC684EE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconGrooveSync : IShellIconOverlayIdentifier { }

    [ComVisible(false)]
    [ComImport]
    [Guid("E768CD3B-BDDC-436D-9C13-E1B39CA257B1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconGrooveUpToDate : IShellIconOverlayIdentifier { }


    [ComVisible(false)]
    [ComImport]
    [Guid("0C6C4200-C589-11D0-999A-00C04FD655E1")]
    public class ShellIconOverlayIdentifier {  }
    /*
   --RegistryStringValue(L"", L"{BBACC218-34EA-4666-9D7A-C78F2274A524}"), // " OneDrive1", Error Icon Overlay
   --RegistryStringValue(L"", L"{5AB7172C-9C11-405C-8DD5-AF20F3606282}"), // " OneDrive2", Shared Icon Overlay
   --RegistryStringValue(L"", L"{A78ED123-AB77-406B-9962-2A5D9D2F7F30}"), // " OneDrive3", Shared Syncing Icon Overlay
   --RegistryStringValue(L"", L"{F241C880-6982-4CE5-8CF7-7085BA96DA5A}"), // " OneDrive4", Up-to-Date Icon Overlay
   --RegistryStringValue(L"", L"{A0396A93-DC06-4AEF-BEE9-95FFCCAEF20E}"), // " OneDrive5", Syncing Icon Overlay
   --RegistryStringValue(L"", L"{9AA2F32D-362A-42D9-9328-24A483E2CCC3}"), // " OneDrive6", Read Only Up to Date Overlay

    */
}
