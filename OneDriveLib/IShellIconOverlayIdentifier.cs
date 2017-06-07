using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Native
{

    public class API
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        public extern static bool DestroyIcon(IntPtr handle);

        [DllImport("ODNative.dll", EntryPoint = "?GetShellInterfaceFromGuid@@YAJPEAHPEA_W1@Z", CallingConvention = CallingConvention.StdCall)]
        public static extern uint GetShellInterfaceFromGuid(
            [Out,MarshalAs(UnmanagedType.Bool)] out bool IsTrue,
            [In, MarshalAs(UnmanagedType.LPWStr)] string GuidString,
            [In,MarshalAs(UnmanagedType.LPWStr)] string Path);

        [DllImport("ODNative.dll", EntryPoint = "?GetShellInterfaceFromGuid@@YAJPAHPA_W1@Z", CallingConvention = CallingConvention.StdCall)]
        public static extern uint GetShellInterfaceFromGuid32(
            [Out, MarshalAs(UnmanagedType.Bool)] out bool IsTrue,
            [In, MarshalAs(UnmanagedType.LPWStr)] string GuidString,
            [In, MarshalAs(UnmanagedType.LPWStr)] string Path);

        [DllImport("ole32.dll", EntryPoint = "CoCreateInstance", CallingConvention = CallingConvention.StdCall)]
        static extern uint CoCreateInstance(
            ref Guid rclsid,
            IntPtr pUnkOuter,
            uint dwClsContext,
            ref Guid riid,
            [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppv
            );

        const uint CLSCTX_INPROC = 3;
        static public bool IsTrue<T>(string Path)
        {

            Guid CLSID = typeof(T).GUID;
            bool isTrue = false;
            uint hr = 1;
            if(Marshal.SizeOf(IntPtr.Zero) == 8)
                hr = GetShellInterfaceFromGuid(out isTrue, CLSID.ToString("B"), Path+"\\");
            else
                hr = GetShellInterfaceFromGuid32(out isTrue, CLSID.ToString("B"), Path + "\\");

#if DEBUG
            Console.Write("{0}:{1}({2}) ", typeof(T).ToString(), typeof(T).GUID.ToString("B"), isTrue);
#endif
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

    public interface IShellIconOverlayIdentifier
    {

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
    public interface IIconUpToDate : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("BBACC218-34EA-4666-9D7A-C78F2274A524")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconError : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("5AB7172C-9C11-405C-8DD5-AF20F3606282")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconShared : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("A78ED123-AB77-406B-9962-2A5D9D2F7F30")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconSharedSync : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("A0396A93-DC06-4AEF-BEE9-95FFCCAEF20E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconSync : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("9AA2F32D-362A-42D9-9328-24A483E2CCC3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconReadOnly : IShellIconOverlayIdentifier
    { }


    [ComVisible(false)]
    [ComImport]
    [Guid("8BA85C75-763B-4103-94EB-9470F12FE0F7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconProErrorConflict : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("CD55129A-B1A1-438E-A425-CEBC7DC684EE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconProSyncInProgress : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("E768CD3B-BDDC-436D-9C13-E1B39CA257B1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconProInSync : IShellIconOverlayIdentifier
    { }


    [ComVisible(false)]
    [ComImport]
    [Guid("8BA85C75-763B-4103-94EB-9470F12FE0F7")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconGrooveError : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("CD55129A-B1A1-438E-A425-CEBC7DC684EE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconGrooveSync : IShellIconOverlayIdentifier
    { }

    [ComVisible(false)]
    [ComImport]
    [Guid("E768CD3B-BDDC-436D-9C13-E1B39CA257B1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IIconGrooveUpToDate : IShellIconOverlayIdentifier
    { }


    [ComVisible(false)]
    [ComImport]
    [Guid("0C6C4200-C589-11D0-999A-00C04FD655E1")]
    public class ShellIconOverlayIdentifier
    {

    }
    /*
   --RegistryStringValue(L"", L"{BBACC218-34EA-4666-9D7A-C78F2274A524}"), // " OneDrive1", Error Icon Overlay
   --RegistryStringValue(L"", L"{5AB7172C-9C11-405C-8DD5-AF20F3606282}"), // " OneDrive2", Shared Icon Overlay
   --RegistryStringValue(L"", L"{A78ED123-AB77-406B-9962-2A5D9D2F7F30}"), // " OneDrive3", Shared Syncing Icon Overlay
   --RegistryStringValue(L"", L"{F241C880-6982-4CE5-8CF7-7085BA96DA5A}"), // " OneDrive4", Up-to-Date Icon Overlay
   --RegistryStringValue(L"", L"{A0396A93-DC06-4AEF-BEE9-95FFCCAEF20E}"), // " OneDrive5", Syncing Icon Overlay
   --RegistryStringValue(L"", L"{9AA2F32D-362A-42D9-9328-24A483E2CCC3}"), // " OneDrive6", Read Only Up to Date Overlay

    */

}
