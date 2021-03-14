using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using ScreenRecorderLib;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ScreenRecorderBySashitha
{
    public static class Constants
    {
        //windows message id for hotkey
        public const int WM_HOTKEY_MSG_ID = 0x0312;
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;
    }
    public class KeyHandler
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public static bool Register(Keys key, Form form, int i)
        {
            return RegisterHotKey(form.Handle, i, Constants.MOD_CONTROL, (int)key);
        }

        public static bool Unregiser(Form form, int i)
        {
            return UnregisterHotKey(form.Handle, i);
        }
    }
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Bitmap bmp = new Bitmap(Properties.Resources.power);
            IntPtr Hicon = bmp.GetHicon();
            notifyIcon1.Icon = Icon.FromHandle(Hicon);
            notifyIcon1.Text = "Screen Recorder By Sashitha";

            notifyIcon1.Click += OnNotifyIconClick;
            notifyIcon1.DoubleClick += OnNotifyIconDoubleClick;

            notifyIcon1.Visible = true;

            KeyHandler.Register(Keys.F9, this, 0); //capture photo
            KeyHandler.Register(Keys.F10, this, 1); //start recording
            KeyHandler.Register(Keys.F11, this, 2); //end recoding
            KeyHandler.Register(Keys.F12, this, 3); //show / hide

            try {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                rk.SetValue("ScreenRecorderBySashitha", Application.ExecutablePath);
            } catch { }
        }

        private void HandleHotkey(int id)
        {
            if(id == 3)
            {
                if (notifyIcon1.Visible)
                    notifyIcon1.Visible = false;
                else
                    notifyIcon1.Visible = true;
            }else if (id == 0)
            {
                if (state == 0)
                {
                    state = 2;
                    TakeSnapshot();
                }
            }
            else if (id == 1)
            {
                if (state == 0)
                {
                    state = 1;
                    CreateRecording();
                    Bitmap bmp = new Bitmap(Properties.Resources.stop);
                    IntPtr Hicon = bmp.GetHicon();
                    notifyIcon1.Icon = Icon.FromHandle(Hicon);
                }
            }
            else if (id == 2)
            {
                if (state == 1)
                {
                    EndRecording();
                    Bitmap bmp = new Bitmap(Properties.Resources.power);
                    IntPtr Hicon = bmp.GetHicon();
                    notifyIcon1.Icon = Icon.FromHandle(Hicon);
                }
            }

        }

        protected override void WndProc(ref Message m)
        {
            
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
            {
                int id = m.WParam.ToInt32();
                HandleHotkey(id);
            }
                
            base.WndProc(ref m);
        }


        Recorder _rec;
        //Stream _outStream;

        private bool allowshowdisplay = false;

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowshowdisplay ? value : allowshowdisplay);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            //this.Hide();
        }

        bool clicked;

        private async void OnNotifyIconClick(object sender, EventArgs e)
        {
            MouseEventArgs f = (MouseEventArgs)e;
            if(f.Button == MouseButtons.Right)
            {
                notifyIcon1.Visible = false;
                Application.Exit();
                return;
            }
            if (clicked) return;
            clicked = true;
            await Task.Delay(SystemInformation.DoubleClickTime);
            if (!clicked) return;
            clicked = false;
            Debug.WriteLine("Click");
            if (state == 1)
            {
                EndRecording();
                Bitmap bmp = new Bitmap(Properties.Resources.power);
                IntPtr Hicon = bmp.GetHicon();
                notifyIcon1.Icon = Icon.FromHandle(Hicon);
            }
            else if (state == 0)
            {
                state = 2;
                TakeSnapshot();
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            clicked = false;
            Debug.WriteLine("Double Click");
            if(state == 0)
            {
                state = 1;
                CreateRecording();
                Bitmap bmp = new Bitmap(Properties.Resources.stop);
                IntPtr Hicon = bmp.GetHicon();
                notifyIcon1.Icon = Icon.FromHandle(Hicon);
            }else if(state == 1)
            {
                EndRecording();
                Bitmap bmp = new Bitmap(Properties.Resources.power);
                IntPtr Hicon = bmp.GetHicon();
                notifyIcon1.Icon = Icon.FromHandle(Hicon);
            }
        }

        int state = 0;

        void TakeSnapshot()
        {
            if (!Directory.Exists(Path.Combine(Application.StartupPath, "Screenshots")))
            {
                Directory.CreateDirectory(Path.Combine(Application.StartupPath, "Screenshots"));
            }
            string videoPath = Path.Combine(Application.StartupPath, "Screenshots", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".png");

            RecorderOptions options = new RecorderOptions
            {
                RecorderMode = RecorderMode.Snapshot,
            };
            _rec = Recorder.CreateRecorder(options);
            //_rec = Recorder.CreateRecorder();
            _rec.OnRecordingComplete += Rec_OnRecordingComplete;
            _rec.OnRecordingFailed += Rec_OnRecordingFailed;
            _rec.OnStatusChanged += Rec_OnStatusChanged;
            //Record to a file
            //string videoPath = Path.Combine(Path.GetTempPath(), "test.mp4");
            _rec.Record(videoPath);
            //..Or to a stream
            //_outStream = new MemoryStream();
            //_rec.Record(_outStream);
            Debug.WriteLine("Started capturing");
        }


        void CreateRecording()
        {
            if(!Directory.Exists(Path.Combine(Application.StartupPath, "Recordings"))){
                Directory.CreateDirectory(Path.Combine(Application.StartupPath, "Recordings"));
            }
            string videoPath = Path.Combine(Application.StartupPath, "Recordings", DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss") + ".mp4");

            RecorderOptions options = new RecorderOptions
            {
                RecorderMode = RecorderMode.Video,
                //If throttling is disabled, out of memory exceptions may eventually crash the program,
                //depending on encoder settings and system specifications.
                //IsThrottlingDisabled = false,
                //Hardware encoding is enabled by default.
                //IsHardwareEncodingEnabled = true,
                //Low latency mode provides faster encoding, but can reduce quality.
                //IsLowLatencyEnabled = false,
                //Fast start writes the mp4 header at the beginning of the file, to facilitate streaming.
                //IsMp4FastStartEnabled = false,
                AudioOptions = new AudioOptions
                {
                    Bitrate = AudioBitrate.bitrate_128kbps,
                    Channels = AudioChannels.Stereo,
                    IsAudioEnabled = true
                },
                /*VideoOptions = new VideoOptions
                {
                    BitrateMode = BitrateControlMode.UnconstrainedVBR,
                    Bitrate = 8000 * 1000,
                    Framerate = 60,
                    IsFixedFramerate = true,
                    EncoderProfile = H264Profile.Main
                },
                MouseOptions = new MouseOptions
                {
                    //Displays a colored dot under the mouse cursor when the left mouse button is pressed.	
                    IsMouseClicksDetected = true,
                    MouseClickDetectionColor = "#FFFF00",
                    MouseRightClickDetectionColor = "#FFFF00",
                    MouseClickDetectionRadius = 30,
                    MouseClickDetectionDuration = 100,

                    IsMousePointerEnabled = true,
                    MouseClickDetectionMode = MouseDetectionMode.Hook
                } */
            };
            _rec = Recorder.CreateRecorder(options);
            //_rec = Recorder.CreateRecorder();
            _rec.OnRecordingComplete += Rec_OnRecordingComplete;
            _rec.OnRecordingFailed += Rec_OnRecordingFailed;
            _rec.OnStatusChanged += Rec_OnStatusChanged;
            //Record to a file
            //string videoPath = Path.Combine(Path.GetTempPath(), "test.mp4");
            _rec.Record(videoPath);
            //..Or to a stream
            //_outStream = new MemoryStream();
            //_rec.Record(_outStream);
            Debug.WriteLine("Started recording");
        }
        void EndRecording()
        {
            _rec.Stop();
            Debug.WriteLine("Ended");
        }
        private void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            //Get the file path if recorded to a file
            string path = e.FilePath;
            OpenFolderAndSelectFile(path);
            //or do something with your stream
            //... something ...
            //_outStream?.Dispose();
            state = 0;
            Debug.WriteLine("Completed");
        }
        private void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            string error = e.Error;
            Debug.WriteLine(error);
            //_outStream?.Dispose();
        }
        private void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
        {
            RecorderStatus status = e.Status;
            Debug.WriteLine(status.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CreateRecording();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            EndRecording();
        }

        public void OpenFolderAndSelectFile(string filePath)
        {
            if(notifyIcon1.Visible == true)
            {
                if (filePath == null)
                    throw new ArgumentNullException("filePath");

                IntPtr pidl = ILCreateFromPathW(filePath);
                SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
                ILFree(pidl);
            }
            
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll")]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

        [DllImport("shell32.dll")]
        private static extern void ILFree(IntPtr pidl);
    }
}
