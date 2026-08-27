using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SnipAutoSaver;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(true, "Local\\SnipAutoSaver", out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        ApplicationConfiguration.Initialize();
        using var context = new SnipAutoSaverContext();
        Application.Run(context);
    }
}

internal sealed class SnipAutoSaverContext : ApplicationContext
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmClipboardUpdate = 0x031D;
    private const int VkShift = 0x10;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int VkS = 0x53;
    private static readonly TimeSpan SnipTimeout = TimeSpan.FromMinutes(2);

    private readonly string outputDirectory;
    private readonly NotifyIcon trayIcon;
    private readonly StatusForm statusForm;
    private readonly ClipboardWindow clipboardWindow;
    private readonly System.Windows.Forms.Timer clipboardRetryTimer;
    private readonly LowLevelKeyboardProc keyboardProc;
    private IntPtr keyboardHook;
    private DateTime awaitingSnipUntilUtc;
    private uint sequenceAtShortcut;
    private uint pendingSequence;
    private int clipboardRetryCount;
    private bool disposed;

    public SnipAutoSaverContext()
    {
        outputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "ScreenShot");
        Directory.CreateDirectory(outputDirectory);

        var menu = new ContextMenuStrip();
        menu.Items.Add("관리 창 열기", null, (_, _) => ShowStatusWindow());
        menu.Items.Add("ScreenShot 폴더 열기", null, (_, _) => OpenOutputDirectory());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => ExitThread());

        trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Win + Shift + S PNG 자동 저장",
            ContextMenuStrip = menu,
            Visible = true
        };
        trayIcon.DoubleClick += (_, _) => ShowStatusWindow();

        statusForm = new StatusForm(outputDirectory, OpenOutputDirectory, HideStatusWindow, ExitThread);
        statusForm.Show();

        clipboardRetryTimer = new System.Windows.Forms.Timer { Interval = 120 };
        clipboardRetryTimer.Tick += RetryClipboardSave;

        clipboardWindow = new ClipboardWindow(OnClipboardUpdated);
        keyboardProc = KeyboardHookCallback;
        keyboardHook = SetWindowsHookEx(WhKeyboardLl, keyboardProc, IntPtr.Zero, 0);
        if (keyboardHook == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "키보드 감지기를 시작하지 못했습니다.");

        trayIcon.ShowBalloonTip(
            1800,
            "스크린샷 자동 저장 실행 중",
            "Win + Shift + S 캡처가 바탕 화면의 ScreenShot 폴더에 PNG로 저장됩니다.",
            ToolTipIcon.Info);
    }

    private IntPtr KeyboardHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 &&
            (wParam == (IntPtr)WmKeyDown || wParam == (IntPtr)WmSysKeyDown))
        {
            var keyboardData = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if (keyboardData.VirtualKeyCode == VkS && IsPressed(VkShift) &&
                (IsPressed(VkLWin) || IsPressed(VkRWin)))
            {
                awaitingSnipUntilUtc = DateTime.UtcNow + SnipTimeout;
                sequenceAtShortcut = GetClipboardSequenceNumber();
            }
        }

        return CallNextHookEx(keyboardHook, code, wParam, lParam);
    }

    private void OnClipboardUpdated()
    {
        if (DateTime.UtcNow > awaitingSnipUntilUtc)
            return;

        uint currentSequence = GetClipboardSequenceNumber();
        if (currentSequence == 0 || currentSequence == sequenceAtShortcut)
            return;

        pendingSequence = currentSequence;
        clipboardRetryCount = 0;
        clipboardRetryTimer.Stop();
        clipboardRetryTimer.Start();
    }

    private void RetryClipboardSave(object? sender, EventArgs e)
    {
        clipboardRetryCount++;

        try
        {
            if (!Clipboard.ContainsImage())
            {
                StopRetryAfterLimit();
                return;
            }

            using Image? clipboardImage = Clipboard.GetImage();
            if (clipboardImage is null)
            {
                StopRetryAfterLimit();
                return;
            }

            string filePath = GetUniqueFilePath();
            clipboardImage.Save(filePath, ImageFormat.Png);

            clipboardRetryTimer.Stop();
            awaitingSnipUntilUtc = DateTime.MinValue;
            sequenceAtShortcut = pendingSequence;
            trayIcon.Text = $"저장됨: {Path.GetFileName(filePath)}";
        }
        catch (ExternalException)
        {
            StopRetryAfterLimit();
        }
        catch (Exception exception)
        {
            clipboardRetryTimer.Stop();
            awaitingSnipUntilUtc = DateTime.MinValue;
            trayIcon.ShowBalloonTip(3000, "스크린샷 저장 실패", exception.Message, ToolTipIcon.Error);
        }
    }

    private void StopRetryAfterLimit()
    {
        if (clipboardRetryCount < 12)
            return;

        clipboardRetryTimer.Stop();
        awaitingSnipUntilUtc = DateTime.MinValue;
    }

    private string GetUniqueFilePath()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        string path = Path.Combine(outputDirectory, $"Screenshot_{timestamp}.png");
        int suffix = 1;

        while (File.Exists(path))
        {
            path = Path.Combine(outputDirectory, $"Screenshot_{timestamp}_{suffix}.png");
            suffix++;
        }

        return path;
    }

    private void OpenOutputDirectory()
    {
        Directory.CreateDirectory(outputDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = outputDirectory,
            UseShellExecute = true
        });
    }

    private void ShowStatusWindow()
    {
        if (!statusForm.Visible)
            statusForm.Show();

        if (statusForm.WindowState == FormWindowState.Minimized)
            statusForm.WindowState = FormWindowState.Normal;

        statusForm.Activate();
    }

    private void HideStatusWindow() => statusForm.Hide();

    protected override void ExitThreadCore()
    {
        DisposeResources();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            DisposeResources();

        base.Dispose(disposing);
    }

    private void DisposeResources()
    {
        if (disposed)
            return;

        disposed = true;
        clipboardRetryTimer.Stop();
        clipboardRetryTimer.Dispose();
        clipboardWindow.Dispose();
        statusForm.AllowClose();
        statusForm.Dispose();

        if (keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }

        trayIcon.Visible = false;
        trayIcon.Dispose();
    }

    private static bool IsPressed(int virtualKey) =>
        (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct KbdLlHookStruct
    {
        public readonly uint VirtualKeyCode;
        public readonly uint ScanCode;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly UIntPtr ExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

    private sealed class ClipboardWindow : NativeWindow, IDisposable
    {
        private readonly Action clipboardUpdated;

        public ClipboardWindow(Action clipboardUpdated)
        {
            this.clipboardUpdated = clipboardUpdated;
            CreateHandle(new CreateParams { Caption = "SnipAutoSaverClipboardListener" });

            if (!AddClipboardFormatListener(Handle))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "클립보드 감지기를 시작하지 못했습니다.");
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WmClipboardUpdate)
                clipboardUpdated();

            base.WndProc(ref message);
        }

        public void Dispose()
        {
            if (Handle == IntPtr.Zero)
                return;

            RemoveClipboardFormatListener(Handle);
            DestroyHandle();
        }
    }

    private sealed class StatusForm : Form
    {
        private readonly Action exitApplication;
        private bool allowClose;

        public StatusForm(
            string outputDirectory,
            Action openOutputDirectory,
            Action hideWindow,
            Action exitApplication)
        {
            this.exitApplication = exitApplication;

            Text = "스크린샷 자동 저장";
            ClientSize = new Size(430, 190);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            ShowIcon = true;

            var title = new Label
            {
                AutoSize = true,
                Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 12f, FontStyle.Bold),
                Location = new Point(22, 20),
                Text = "Win + Shift + S 자동 저장 실행 중"
            };

            var description = new Label
            {
                AutoEllipsis = true,
                Location = new Point(24, 58),
                Size = new Size(380, 42),
                Text = $"캡처한 이미지를 PNG로 저장합니다.\r\n저장 위치: {outputDirectory}"
            };

            var openButton = new Button
            {
                Location = new Point(23, 125),
                Size = new Size(130, 36),
                Text = "폴더 열기"
            };
            openButton.Click += (_, _) => openOutputDirectory();

            var hideButton = new Button
            {
                Location = new Point(160, 125),
                Size = new Size(110, 36),
                Text = "창 숨기기"
            };
            hideButton.Click += (_, _) => hideWindow();

            var exitButton = new Button
            {
                Location = new Point(277, 125),
                Size = new Size(128, 36),
                Text = "프로그램 종료"
            };
            exitButton.Click += (_, _) => exitApplication();

            Controls.AddRange([title, description, openButton, hideButton, exitButton]);
        }

        public void AllowClose() => allowClose = true;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                exitApplication();
                return;
            }

            base.OnFormClosing(e);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int hookId,
        LowLevelKeyboardProc callback,
        IntPtr moduleHandle,
        uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hookHandle,
        int code,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr windowHandle);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr windowHandle);
}
