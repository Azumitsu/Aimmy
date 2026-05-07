using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace MouseMovementLibraries.CustomDriverSupport
{
    internal class CustomDriverMouse
    {
        private const string RegistryKeyPath = @"SOFTWARE\AimmyCustomDriver";
        private const string ValueName = "RegistryCleaner";

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MouseMoveRequest
        {
            public int X;
            public int Y;
            public byte Buttons;
            public sbyte Wheel;
        }

        // Button event flags matching the kernel driver
        private const byte PERF_EVENT_1_DOWN = 0x01;
        private const byte PERF_EVENT_1_UP = 0x02;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegOpenKeyEx(nint hKey, string subKey, uint options, uint samDesired, out nint phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegSetValueEx(nint hKey, string lpValueName, uint Reserved, uint dwType, byte[] lpData, uint cbData);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(nint hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegCreateKeyEx(nint hKey, string subKey, uint Reserved, string? lpClass, uint dwOptions, uint samDesired, nint lpSecurityAttributes, out nint phkResult, out uint lpdwDisposition);

        private static readonly nint HKEY_CURRENT_USER = new(-2147483647); // 0x80000001
        private const uint KEY_SET_VALUE = 0x0002;
        private const uint KEY_ALL_ACCESS = 0xF003F;
        private const uint REG_BINARY = 3;

        private static nint _hKey = nint.Zero;

        public static bool EnsureRegistryKeyOpen()
        {
            if (_hKey != nint.Zero)
                return true;

            int result = RegOpenKeyEx(HKEY_CURRENT_USER, RegistryKeyPath, 0, KEY_SET_VALUE, out _hKey);
            if (result != 0)
            {
                // Try to create the key
                result = RegCreateKeyEx(HKEY_CURRENT_USER, RegistryKeyPath, 0, null, 0, KEY_ALL_ACCESS, nint.Zero, out _hKey, out _);
                if (result != 0)
                {
                    _hKey = nint.Zero;
                    return false;
                }
            }
            return true;
        }

        private static void SendRequest(MouseMoveRequest request)
        {
            if (!EnsureRegistryKeyOpen())
                return;

            int size = Marshal.SizeOf<MouseMoveRequest>();
            byte[] data = new byte[size];

            nint ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(request, ptr, false);
                Marshal.Copy(ptr, data, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            // The driver intercepts this write and returns STATUS_ACCESS_DENIED,
            // so the return code will be non-zero — this is expected behavior.
            RegSetValueEx(_hKey, ValueName, 0, REG_BINARY, data, (uint)data.Length);
        }

        public static void Move(int x, int y)
        {
            SendRequest(new MouseMoveRequest
            {
                X = x,
                Y = y,
                Buttons = 0,
                Wheel = 0
            });
        }

        public static void MouseDown()
        {
            SendRequest(new MouseMoveRequest
            {
                X = 0,
                Y = 0,
                Buttons = PERF_EVENT_1_DOWN,
                Wheel = 0
            });
        }

        public static void MouseUp()
        {
            SendRequest(new MouseMoveRequest
            {
                X = 0,
                Y = 0,
                Buttons = PERF_EVENT_1_UP,
                Wheel = 0
            });
        }

        public static void Close()
        {
            if (_hKey != nint.Zero)
            {
                RegCloseKey(_hKey);
                _hKey = nint.Zero;
            }
        }
    }
}
