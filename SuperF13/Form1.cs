using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperF13
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        private int WINDOW_WIDTH = 1000;
        private int WINDOW_HEIGHT = 500;
        private int INPUTBOX_PADDING = 10;
        private int INPUTBOX_HEIGHT = 30;  // includes padding

        private Color COLOR_BORDER = Color.FromArgb(0x75, 0x75, 0x75);
        private Color COLOR_BG = Color.FromArgb(0x3a, 0x3a, 0x3a);

        private IntPtr _hookID = IntPtr.Zero;

        private BufferedTextBox inputBox;

        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;

            this.Width = WINDOW_WIDTH;
            this.Height = WINDOW_HEIGHT;

            this.StartPosition = FormStartPosition.CenterScreen;

            // no taskbar icon
            this.ShowInTaskbar = false;

            // Start minimized (hidden)
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Deactivate += Form1_Deactivate;

            _hookID = SetHook(KeyDownHookCallback);

            // Optional: Hide from Alt+Tab
            SetWindowStyle();

            inputBox = new BufferedTextBox
            {
                Location = new Point(INPUTBOX_PADDING, INPUTBOX_PADDING),
                Size = new Size(WINDOW_WIDTH - INPUTBOX_PADDING * 2, INPUTBOX_HEIGHT - INPUTBOX_PADDING * 2),
                Font = new Font("JetBrainsMono NF", 16),
                BorderStyle = BorderStyle.None,
                BackColor = COLOR_BG,
                ForeColor = Color.White
            };
            inputBox.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    // todo handle doing stuff

                    HideStuffs();
                } else
                {
                    // todo handle searching
                }
            };

            this.Load += (s, e) => {
                this.Hide(); // This will work reliably
                //Debug.WriteLine($"After Load Hide - Visible: {this.Visible}");
                
                this.Controls.Add(inputBox);
            };
        }

        // idk if this helps double buffering
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // WS_EX_COMPOSITED
                return cp;
            }
        }

        public void ShowStuffs()
        {
            overlayForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                BackColor = Color.Black,
                Opacity = 0.2f,  // 20% opacity
                ShowInTaskbar = false,
                TopMost = false
            };

            overlayForm.Show();
            overlayActive = true;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        public void HideStuffs()
        {
            inputBox.Clear();
            this.WindowState = FormWindowState.Minimized;
            this.BringToFront();
            this.Hide();
            if (overlayActive)
            {
                overlayActive = false;
                overlayForm.Close();
                overlayForm = null;
            }
        }

        private void SetWindowStyle()
        {
            this.BackColor = COLOR_BG;

            const int GWL_EXSTYLE = -20;
            const int WS_EX_TOOLWINDOW = 0x00000080;

            if (this.Handle != IntPtr.Zero)
            {
                int extendedStyle = GetWindowLong(this.Handle, GWL_EXSTYLE);
                SetWindowLong(this.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
            }
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_KEYDOWN = 0x0100;

        private Form overlayForm = null;
        private bool overlayActive = false;

        private void ToggleWindowVisibility()
        {
            if (this.Visible)
            {
                HideStuffs();
            }
            else
            {
                ShowStuffs();
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr KeyDownHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                if (key == Keys.R && (GetAsyncKeyState((int)Keys.F13) & 0x8000) > 0)
                {
                    this.BeginInvoke(new Action(() => {
                        this.Activate();
                        ToggleWindowVisibility();
                    }));
                    return (IntPtr)1;
                }

                if (key == Keys.Escape && overlayActive)
                {
                    HideStuffs();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
            base.OnFormClosing(e);
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            HideStuffs();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int radius = 0; // Adjust corner radius as needed
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            GraphicsPath path = CreateRoundRect(rect, radius);
            Rectangle rectShrinked = new Rectangle(0, 0, this.Width, this.Height);
            GraphicsPath innerPath = CreateRoundRectBorder(rectShrinked, radius);

            this.Region = new Region(path);

            // Optional: Draw border
            using (Pen pen = new Pen(COLOR_BORDER, 1))
            {
                e.Graphics.DrawPath(pen, innerPath);
            }
        }

        private GraphicsPath CreateRoundRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(rect);
                path.CloseFigure();
                return path;
            }

            //path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            //path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            //path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            //path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            //path.CloseFigure();

            return path;
        }

        private GraphicsPath CreateRoundRectBorder(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(new Rectangle(rect.Left, rect.Top, rect.Width-1, rect.Height-1));
                path.CloseFigure();
                return path;
            }

            //path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            //path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2 - 1, radius * 2 - 1, 270, 90);
            //path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            //path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            //path.CloseFigure();

            return path;
        }
    }
}
