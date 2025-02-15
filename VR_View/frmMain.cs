using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VR_View
{
    public partial class frmMain : Form
    {
        private bool isWorking;
        private bool isTakingScreenshots;
        private bool isPreview;
        private bool isMouseCapture;
        private object locker = new object();
        private ReaderWriterLock rwl = new ReaderWriterLock();
        private MemoryStream img;

        public frmMain()
        {
            InitializeComponent();

            // for movable semi-borderless form
            this.FormBorderStyle = FormBorderStyle.None;
            //

            CheckForIllegalCrossThreadCalls = false; // For Visual Studio Debuging Only !

            img = new MemoryStream();
            isPreview = true;
            isMouseCapture = true;

            foreach (var screen in Screen.AllScreens)
            {
                comboScreens.Items.Add(screen.DeviceName.Replace("\\","").Replace(".",""));
            }
            comboScreens.SelectedIndex = 0;
        }

        // begin make semi-borderless form movable and resizable
        const int WM_NCHITTEST = 0x0084;
        const int HTCLIENT = 1;
        const int HTCAPTION = 2;
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    if (m.Result == (IntPtr)HTCLIENT)
                    {
                        m.Result = (IntPtr)HTCAPTION;
                    }
                    break;
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x40000;
                return cp;
            }
        }
        // end make semi-borderless form movable and resizable

        private void btnStartViewer_Click(object sender, EventArgs e)
        {
            if (btnStartViewer.Tag.ToString() != "start")
            {
                btnStartViewer.Tag = "start";
                btnStartViewer.Text = "Start Viewer";
                isWorking = false;
                isTakingScreenshots = false;
                return;
            }

            try
            {
                isTakingScreenshots = true;
                isWorking = true;
                Task.Factory.StartNew(() => CaptureScreenEvery((int)numShotEvery.Value)).Wait();
                btnStartViewer.Tag = "stop";
                btnStartViewer.Text = "Stop Viewer";
            }
            catch
            {

            }
        }

        private async Task CaptureScreenEvery(int msec)
        {
            while (isWorking)
            {
                if (isTakingScreenshots)
                {
                    TakeScreenshot(isMouseCapture);
                    msec = (int)numShotEvery.Value;
                    await Task.Delay(msec);
                }
            }
        }
        private void TakeScreenshot(bool captureMouse)
        {
            if (captureMouse)
            {
                var bmp = ScreenCapturePInvoke.CaptureSelectedScreen(true,comboScreens.SelectedIndex);
                rwl.AcquireWriterLock(Timeout.Infinite);
                bmp.Save(Application.StartupPath + "/" + "/VR_View.jpg", ImageFormat.Jpeg);
                rwl.ReleaseWriterLock();
                
                if (isPreview)
                {
                    img = new MemoryStream();
                    bmp.Save(img, ImageFormat.Jpeg);
                    imgPreview.Image = new Bitmap(img);
                    imgPreview2.Image = new Bitmap(img);
                }
                bmp.Dispose();
                bmp = null;
                return;
            }
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                rwl.AcquireWriterLock(Timeout.Infinite);
                bitmap.Save(Application.StartupPath + "/" + "/VR_View.jpg", ImageFormat.Jpeg);
                rwl.ReleaseWriterLock();

                if (isPreview)
                {
                    img = new MemoryStream();
                    bitmap.Save(img, ImageFormat.Jpeg);
                    imgPreview.Image = new Bitmap(img);
                    imgPreview2.Image = new Bitmap(img);
                }


            }
        }

        private void btnStopViewer_Click(object sender, EventArgs e)
        {
            isWorking = false;
            isTakingScreenshots = false;
            btnStartViewer.Enabled = true;
            btnStopServer.Enabled = false;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //Position-Zero initialization
            trackBar1.Minimum = -100;
            trackBar1.Maximum = 100;
            trackBar1.Value = 0;  

            trackBar2.Minimum = -100;
            trackBar2.Maximum = 100;
            trackBar2.Value = 0;  

            trackBar3.Minimum = 50;
            trackBar3.Maximum = 150;
            trackBar3.Value = 100;

            imgPreview.Width = 500;
            imgPreview.Height = 500;
            imgPreview.Left = (panelPreviewContainer.Width / 4 - imgPreview.Width / 2);

            imgPreview2.Width = 500;
            imgPreview2.Height = 500;
            imgPreview2.Left = (panelPreviewContainer.Width * 3 / 4 - imgPreview2.Width / 2);
        }


        // close button
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        bool isMaximized = false;

        // maximize button
        private void pictureBox5_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            pictureBox5.Visible = false;
            pictureBox7.Visible = true;
            isMaximized = true;
        }

        //minimize button
        private void pictureBox6_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        // unmaximize button
        private void pictureBox7_Click(object sender, EventArgs e)
        {
            isMaximized = false;
            this.WindowState = FormWindowState.Normal;
            pictureBox5.Visible = true;
            pictureBox7.Visible = false;
        }

        // hide settings button (gear)
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            label7.Visible = false;
            comboScreens.Visible = false;
            numShotEvery.Visible = false;
            label6.Visible = false;
            btnStartViewer.Visible = false;
            pictureBox1.Visible = false;
            pictureBox2.Visible = false;
            pictureBox4.Visible = true;
            pictureBox3.Visible = false;
            pictureBox6.Visible = false;
            pictureBox5.Visible = false;
            pictureBox7.Visible = false;
            label9.Visible = false;
            versionLabel.Visible = false;

            trackBar1.Visible = false;
            labelTrackBar1.Visible = false;
            trackBar2.Visible = false;
            labelTrackBar2.Visible = false;
            trackBar3.Visible = false;
            labelTrackBar3.Visible = false;
            btnReset.Visible = false;
        }

        // show settings button (arrow thingy)
        private void pictureBox4_Click(object sender, EventArgs e)
        {
            label7.Visible = true;
            comboScreens.Visible = true;
            numShotEvery.Visible = true;
            label6.Visible = true;
            btnStartViewer.Visible = true;
            pictureBox1.Visible = true;
            pictureBox2.Visible = true;
            pictureBox4.Visible = false;
            pictureBox3.Visible = true;
            pictureBox6.Visible = true;
            label9.Visible = true;
            versionLabel.Visible = true;

            trackBar1.Visible = true;
            labelTrackBar1.Visible = true;
            trackBar2.Visible = true;
            labelTrackBar2.Visible = true;
            trackBar3.Visible = true;
            labelTrackBar3.Visible = true;
            btnReset.Visible = true;

            if (isMaximized == true)
            {
                pictureBox7.Visible = true;
            }
            else
            {
                pictureBox5.Visible = true;
            }
        }

        // settings gear tooltip
        private void pictureBox4_MouseHover(object sender, EventArgs e)
        {
            ToolTip ttshow = new ToolTip();
            ttshow.SetToolTip(this.pictureBox4, "Show Settings");
        }

        // settings arrows tooltip
        private void pictureBox2_MouseHover(object sender, EventArgs e)
        {
            ToolTip tthide = new ToolTip();
            tthide.SetToolTip(this.pictureBox2, "Hide Settings");
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Up-Down Position
            int offset = -trackBar1.Value;
            int newTop = (panelPreviewContainer.Height - imgPreview.Height) / 2 + offset;

            imgPreview.Top = newTop;
            imgPreview2.Top = newTop;

            labelTrackBar1.Text = trackBar1.Value.ToString(); 
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            //Left - Right Position
            int offset = trackBar2.Value;
            int panelWidth = panelPreviewContainer.Width;
            int imageWidth = imgPreview.Width;

            imgPreview.Left = (panelWidth / 4 - imageWidth / 2) - offset;
            imgPreview2.Left = (panelWidth * 3 / 4 - imageWidth / 2) - offset;

            labelTrackBar2.Text = trackBar2.Value.ToString(); 
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            //Scale
            float scale = trackBar3.Value / 100f;
            int panelWidth = panelPreviewContainer.Width;
            int panelMiddle = panelWidth / 2;
            int originalWidth = 500;
            int originalHeight = 500;

            int newWidth = (int)(originalWidth * scale);
            int newHeight = (int)(originalHeight * scale);

            imgPreview.Width = newWidth;
            imgPreview.Height = newHeight;
            imgPreview.Left = (panelWidth / 4 - newWidth / 2);

            imgPreview2.Width = newWidth;
            imgPreview2.Height = newHeight;
            imgPreview2.Left = (panelWidth * 3 / 4 - newWidth / 2);

            labelTrackBar3.Text = trackBar3.Value.ToString() + "%"; 
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            // Reset Sliders
            trackBar1.Value = 0;
            trackBar2.Value = 0;
            trackBar3.Value = 100; 

            labelTrackBar1.Text = "0";
            labelTrackBar2.Text = "0";
            labelTrackBar3.Text = "100%";

            int panelWidth = panelPreviewContainer.Width;
            int panelMiddle = panelWidth / 2;
            int originalWidth = 500;
            int originalHeight = 500;

            imgPreview.Width = originalWidth;
            imgPreview.Height = originalHeight;
            imgPreview2.Width = originalWidth;
            imgPreview2.Height = originalHeight;

            // Reset Positions
            imgPreview.Left = panelWidth / 4 - (originalWidth / 2);
            imgPreview2.Left = panelWidth * 3 / 4 - (originalWidth / 2);

            imgPreview.Top = (panelPreviewContainer.Height - imgPreview.Height) / 2;
            imgPreview2.Top = imgPreview.Top;
        }

    }
}
