using System;
using System.Runtime.InteropServices;
using System.Threading;
using StickerApp.Interop;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace StickerApp.Services
{
    public class TrayService : IDisposable
    {
        private readonly LoggerService _logger;
        private readonly JsonStorageService _storage;
        private IntPtr _hwnd;
        private bool _isInitialized;

        private const int WM_TRAYICON = 0x8000 + 100; // WM_USER + 100
        private const int TRAY_ID = 1;

        // Tray message flags
        private const int NIM_ADD = 0;
        private const int NIM_MODIFY = 1;
        private const int NIM_DELETE = 2;

        private const int NIF_MESSAGE = 1;
        private const int NIF_ICON = 2;
        private const int NIF_TIP = 4;

        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        // Context Menu Flags
        private const int MF_STRING = 0;
        private const int MF_SEPARATOR = 0x800;
        private const int TPM_LEFTALIGN = 0;
        private const int TPM_RETURNCMD = 0x100;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uVersionOrTimeout;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool Shell_NotifyIcon(int dwMessage, [In] ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        private static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Win32 Subclassing APIs for WndProc hooking
        private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, IntPtr uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        private SUBCLASSPROC? _subclassProc;

        public event EventHandler? NewStickerRequested;
        public event EventHandler? ShowAllRequested;
        public event EventHandler? RestoreDeletedRequested;
        public event EventHandler? SettingsRequested;
        public event EventHandler? BackupRequested;
        public event EventHandler? ExitRequested;

        public TrayService(LoggerService logger, JsonStorageService storage)
        {
            _logger = logger;
            _storage = storage;
        }

        public void Initialize()
        {
            if (_isInitialized || !_storage.Settings.TrayEnabled) return;

            // Wait for App.MainWindow to exist
            if (App.MainWindow == null)
            {
                _logger.LogWarning("TrayService failed to initialize: App.MainWindow is null");
                return;
            }

            _hwnd = WindowNative.GetWindowHandle(App.MainWindow);

            // Install subclass callback to listen to WM_TRAYICON messages on MainWindow
            _subclassProc = new SUBCLASSPROC(WndProc);
            SetWindowSubclass(_hwnd, _subclassProc, new IntPtr(1), IntPtr.Zero);

            AddTrayIcon();
            _isInitialized = true;
            _logger.LogInfo("TrayService successfully initialized with custom Win32 tray icon.");
        }

        private void AddTrayIcon()
        {
            // Use IDI_APPLICATION standard system icon as a generic fallback
            IntPtr hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = TRAY_ID,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = hIcon,
                szTip = "Sticker App"
            };

            Shell_NotifyIcon(NIM_ADD, ref nid);
        }

        private void RemoveTrayIcon()
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = TRAY_ID
            };
            Shell_NotifyIcon(NIM_DELETE, ref nid);
        }

        private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData)
        {
            if (uMsg == WM_TRAYICON)
            {
                int eventId = (int)lParam;
                if (eventId == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                    return IntPtr.Zero;
                }
                else if (eventId == WM_LBUTTONDBLCLK)
                {
                    ShowAllRequested?.Invoke(this, EventArgs.Empty);
                    return IntPtr.Zero;
                }
            }

            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private void ShowContextMenu()
        {
            IntPtr hMenu = CreatePopupMenu();
            if (hMenu == IntPtr.Zero) return;

            AppendMenu(hMenu, MF_STRING, new IntPtr(100), "New Sticker");
            AppendMenu(hMenu, MF_STRING, new IntPtr(101), "Show All Stickers");
            AppendMenu(hMenu, MF_STRING, new IntPtr(102), "Restore Deleted");
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, "");
            AppendMenu(hMenu, MF_STRING, new IntPtr(103), "Settings");
            AppendMenu(hMenu, MF_STRING, new IntPtr(104), "Backup Now");
            AppendMenu(hMenu, MF_SEPARATOR, IntPtr.Zero, "");
            AppendMenu(hMenu, MF_STRING, new IntPtr(105), "Exit");

            GetCursorPos(out POINT pt);
            SetForegroundWindow(_hwnd);

            // TrackPopupMenu returns the selected Command ID
            int cmd = (int)TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_RETURNCMD, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
            DestroyMenu(hMenu);

            switch (cmd)
            {
                case 100: NewStickerRequested?.Invoke(this, EventArgs.Empty); break;
                case 101: ShowAllRequested?.Invoke(this, EventArgs.Empty); break;
                case 102: RestoreDeletedRequested?.Invoke(this, EventArgs.Empty); break;
                case 103: SettingsRequested?.Invoke(this, EventArgs.Empty); break;
                case 104: BackupRequested?.Invoke(this, EventArgs.Empty); break;
                case 105: ExitRequested?.Invoke(this, EventArgs.Empty); break;
            }
        }

        public void Dispose()
        {
            if (_isInitialized)
            {
                RemoveTrayIcon();
                _isInitialized = false;
            }
            GC.SuppressFinalize(this);
        }
    }
}
