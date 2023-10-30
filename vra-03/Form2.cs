using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace vra_03
{
    public partial class Form2 : DevExpress.XtraEditors.XtraForm
    {
        private Process videoProcess;
        private bool isF4KeyPressed = false;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private IntPtr hookId = IntPtr.Zero;

        public Form2()
        {
            InitializeComponent();
            TopMost = true; // ตั้งค่าเป็นหน้าต่างบนสุด
            KeyPreview = true;
            KeyDown += Form2_KeyDown;
            KeyUp += Form2_KeyUp;
        }

        private void Form2_Load(object sender, EventArgs e)
        { }

        private void OpenMpvPlayer()
        {
            if (videoProcess == null || videoProcess.HasExited)
            {
                string videoPath = @"C:\mpv\Tom.mp4";
                videoProcess = new Process();
                videoProcess.StartInfo.FileName = @"C:\mpv\mpv.exe";
                videoProcess.StartInfo.Arguments = $"--ontop --no-border --no-osd-bar --no-osc -geometry=1920x1080 --screen=1 {videoPath}";
                videoProcess.StartInfo.UseShellExecute = false;
                videoProcess.StartInfo.CreateNoWindow = true;
                videoProcess.StartInfo.RedirectStandardInput = true;
                videoProcess.Start();
            }
        }

        private void CloseMpvPlayer()
        {
            if (videoProcess != null && !videoProcess.HasExited)
            {
                try
                {
                    // ส่ง Ctrl+C ให้กับ mpv
                    SetForegroundWindow(videoProcess.MainWindowHandle);
                    SendKeys.SendWait("^c");
                    videoProcess.WaitForExit(100); // รอ mpv ตอบสนอง 1 วินาที
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }

                if (!videoProcess.HasExited)
                {
                    // กรณีที่ mpv ยังไม่ปิด จะใช้วิธีฉุกเฉิน
                    videoProcess.Kill();
                }
            }
        }
         [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private void Form2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4)
            {
                if (!isF4KeyPressed)
                {
                    OpenMpvPlayer();
                    isF4KeyPressed = true;
                }
            }
        }

        private void Form2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4)
            {
                isF4KeyPressed = false;
                CloseMpvPlayer();
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == (int)Keys.F4)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN)
                    {
                        if (!isF4KeyPressed)
                        {
                            OpenMpvPlayer();
                            isF4KeyPressed = true;
                        }
                    }
                    else if (wParam == (IntPtr)WM_KEYUP && isF4KeyPressed)
                    {
                        isF4KeyPressed = false;
                        CloseMpvPlayer();
                    }
                }
            }
            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private void SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                hookId = SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void Unhook()
        {
            UnhookWindowsHookEx(hookId);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetHook();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Unhook();
        }
    }
}
