using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AmiyaBotMaaAdapter.Interop
{
    public enum InstanceOptionType
    {
        touch_type = 2,
        deployment_with_pause = 3
    }

    public static class AsstInterop
    {
        public const string AsstPortDll = "E:\\Tools\\MAA\\MaaCore.dll";

        [DllImport(AsstPortDll)]
        public static extern byte AsstSetUserDir(string path);

        [DllImport(AsstPortDll)]
        public static extern byte AsstLoadResource(string path);

        [DllImport(AsstPortDll)]
        public static extern byte AsstSetStaticOption(int key, string value);

        [DllImport(AsstPortDll)]
        public static extern IntPtr AsstCreate();

        [DllImport(AsstPortDll)]
        public static extern IntPtr AsstCreateEx(AsstApiCallback callback, IntPtr custom_arg);

        [DllImport(AsstPortDll)]
        public static extern void AsstDestroy(IntPtr handle);

        [DllImport(AsstPortDll)]
        public static extern byte AsstSetInstanceOption(IntPtr handle, int key, string value);

        [DllImport(AsstPortDll)]
        public static extern bool AsstConnect(IntPtr handle, string adb_path, string address, string config);

        [DllImport(AsstPortDll)]
        public static extern int AsstAppendTask(IntPtr handle, string type, string parameters);

        [DllImport(AsstPortDll)]
        public static extern byte AsstSetTaskParams(IntPtr handle, int id, string parameters);

        [DllImport(AsstPortDll)]
        public static extern bool AsstStart(IntPtr handle);

        [DllImport(AsstPortDll)]
        public static extern byte AsstStop(IntPtr handle);

        [DllImport(AsstPortDll)]
        public static extern bool AsstRunning(IntPtr handle);

        [DllImport(AsstPortDll)]
        public static extern byte AsstConnected(IntPtr handle);

        [DllImport(AsstPortDll)]
        public static extern int AsstAsyncConnect(IntPtr handle, string adb_path, string address, string config, byte block);

        [DllImport(AsstPortDll)]
        public static extern int AsstAsyncClick(IntPtr handle, int x, int y, byte block);

        [DllImport(AsstPortDll)]
        public static extern int AsstAsyncScreencap(IntPtr handle, byte block);

        [DllImport(AsstPortDll)]
        public static extern ulong AsstGetImage(IntPtr handle, IntPtr buff, ulong buff_size);

        [DllImport(AsstPortDll)]
        public static extern ulong AsstGetUUID(IntPtr handle, IntPtr buff, ulong buff_size);

        [DllImport(AsstPortDll)]
        public static extern ulong AsstGetTasksList(IntPtr handle, IntPtr buff, ulong buff_size);

        [DllImport(AsstPortDll)]
        public static extern ulong AsstGetNullSize();

        [DllImport(AsstPortDll)]
        public static extern IntPtr AsstGetVersion();

        [DllImport(AsstPortDll)]
        public static extern void AsstLog(string level, string message);

        public delegate void AsstApiCallback(int msg, string details_json, IntPtr custom_arg);
    }
}
