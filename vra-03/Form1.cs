using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace vra_03
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        private Process videoProcess;
        private bool isF3KeyPressed = false;
        private IntPtr hookId = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int VK_F3 = 0x72;
        // เพิ่ม using statement ด้านล่างเข้าไป


// และแก้โค้ดของ GetModuleHandle ให้เหมือนกับด้านล่าง
[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    public Form1()
        {
            InitializeComponent();
            KeyPreview = true;
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            Screen hdmi0 = Screen.AllScreens.FirstOrDefault(screen => screen.DeviceName == @"\\.\DISPLAY1");
            if (hdmi0 != null)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = hdmi0.Bounds.Location;
            }
            else
            {
                MessageBox.Show("ไม่พบหน้าจอที่ชื่อ 'HDMI 0'");
            }

            Form2 form2 = new Form2();

            Screen hdmi1 = Screen.AllScreens.FirstOrDefault(screen => screen.DeviceName == @"\\.\DISPLAY2");
            if (hdmi1 != null)
            {
                form2.StartPosition = FormStartPosition.Manual;
                form2.Location = hdmi1.Bounds.Location;
                form2.Show();
            }
            else
            {
                MessageBox.Show("ไม่พบหน้าจอที่ชื่อ 'HDMI 1'");
            }
        }


  

        private void OpenMpvPlayer()
        {
            this.TopMost = true;
            if (videoProcess == null || videoProcess.HasExited)
            {
                string videoPath = @"C:\mpv\Tom.mp4";
                videoProcess = new Process();
                videoProcess.StartInfo.FileName = @"C:\mpv\mpv.exe";
                videoProcess.StartInfo.Arguments = $"--ontop --fs --no-border --no-osd-bar --no-osc --screen=0 {videoPath}"; // เพิ่ม --ontop ที่นี่
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



        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                if (!isF3KeyPressed)
                {
                    OpenMpvPlayer();
                    isF3KeyPressed = true;
                }
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                isF3KeyPressed = false;
                CloseMpvPlayer();
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_F3)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN)
                    {
                        if (!isF3KeyPressed)
                        {
                            OpenMpvPlayer();
                            isF3KeyPressed = true;
                        }
                    }
                    else if (wParam == (IntPtr)WM_KEYUP && isF3KeyPressed)
                    {
                        isF3KeyPressed = false;
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
