using System;
using System.Text;

namespace StickerApp.Interop
{
    public static class DesktopInterop
    {
        public static IntPtr FindWallpaperWorkerW()
        {
            IntPtr progman = Win32.FindWindow("Progman", null);
            if (progman == IntPtr.Zero) return IntPtr.Zero;

            // Send WM_SPAWN_WORKER message to Progman to spawn a WorkerW
            Win32.SendMessageTimeout(
                progman,
                Win32.WM_SPAWN_WORKER,
                IntPtr.Zero,
                IntPtr.Zero,
                0x0000, // SMTO_NORMAL
                1000,
                out _);

            IntPtr wallpaperWorkerW = IntPtr.Zero;

            // Enumerate windows to find SHELLDLL_DefView and identify its sibling WorkerW
            Win32.EnumWindows((hWnd, lParam) =>
            {
                IntPtr shellDll = Win32.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDll != IntPtr.Zero)
                {
                    // The wallpaper WorkerW is created as a sibling directly after the SHELLDLL_DefView window
                    wallpaperWorkerW = Win32.FindWindowEx(IntPtr.Zero, hWnd, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            // Fallback: search for any WorkerW that does not contain SHELLDLL_DefView
            if (wallpaperWorkerW == IntPtr.Zero)
            {
                Win32.EnumWindows((hWnd, lParam) =>
                {
                    var sb = new StringBuilder(256);
                    Win32.GetClassName(hWnd, sb, sb.Capacity);
                    if (sb.ToString() == "WorkerW")
                    {
                        IntPtr shellDll = Win32.FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (shellDll == IntPtr.Zero)
                        {
                            wallpaperWorkerW = hWnd;
                            return false; // Stop enum
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }

            return wallpaperWorkerW;
        }
    }
}
