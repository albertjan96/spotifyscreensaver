using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace SpotifyScreenSaver;

public partial class App : System.Windows.Application
{
    [DllImport("user32.dll")]
    static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    const int GWL_STYLE = -16;
    const int WS_CHILD = 0x40000000;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var arg0 = e.Args.FirstOrDefault()?.Trim() ?? "";
        var arg = arg0.ToLowerInvariant();

        if (arg.StartsWith("/c"))
        {
            new ConfigWindow().Show();
            return;
        }

        if (arg.StartsWith("/p"))
        {
            if (e.Args.Length > 1 && IntPtr.TryParse(e.Args[1], out IntPtr previewHandle))
            {
                ShowPreview(previewHandle);
            }
            else
            {
                Shutdown();
            }
            return;
        }

        new ScreenSaverWindow().Show();
    }

    private void ShowPreview(IntPtr previewHandle)
    {
        var preview = new PreviewWindow();
        preview.Show();

        var helper = new WindowInteropHelper(preview);
        var wpfHandle = helper.Handle;

        SetParent(wpfHandle, previewHandle);

        int style = GetWindowLong(wpfHandle, GWL_STYLE);
        SetWindowLong(wpfHandle, GWL_STYLE, new IntPtr(style | WS_CHILD));

        GetClientRect(previewHandle, out RECT rect);
        MoveWindow(wpfHandle, 0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top, true);
    }
}
