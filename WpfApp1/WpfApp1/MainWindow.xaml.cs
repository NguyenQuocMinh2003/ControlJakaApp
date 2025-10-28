using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using jakaApi;
using jkType;
//testttt git
namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        int handle = 0;
        bool connected = false;
        CancellationTokenSource cts; // để hủy luồng cập nhật khi đóng app
        private double[] lastJoints = new double[6]; // Lưu góc hiện tại của 6 khớp (rad)

        public MainWindow()
        {
            InitializeComponent();
        }

        // Thiết lập môi trường neeeeeeeeeeeeeeeeeeeee

        void SetEnvironment()
        {
            string cur_path = Environment.CurrentDirectory;
            string[] paths = cur_path.Split("WpfApp");
            var path = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH",
                System.IO.Path.Join(paths[0], "out\\shared\\Release\\") + ";" + path);
        }

        // ==============================
        // 🔌 Sự kiện nút bấm
        // ==============================
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            SetEnvironment();
            string ip = txtIp.Text.Trim();

            int ret = jakaAPI.create_handler(ip, ref handle);
            if (ret == 0)
            {
                lblStatus.Text = $"✅ Đã kết nối: {ip}";
                lblStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                connected = true;

                StartRealtimeUpdate(); // ✅ bắt đầu cập nhật trạng thái
            }
            else
            {
                lblStatus.Text = $"❌ Kết nối thất bại ({ret})";
                lblStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void btnPowerOn_Click(object sender, RoutedEventArgs e)
        {
            if (!connected) return;
            jakaAPI.power_on(ref handle);
        }

        private void btnPowerOff_Click(object sender, RoutedEventArgs e)
        {
            if (!connected) return;
            jakaAPI.power_off(ref handle);
        }

        private void btnEnable_Click(object sender, RoutedEventArgs e)
        {
            if (!connected) return;
            jakaAPI.enable_robot(ref handle);
        }

        private void btnDisable_Click(object sender, RoutedEventArgs e)
        {
            if (!connected) return;
            jakaAPI.disable_robot(ref handle);
        }
        //---------
        private void btnMove_Click(object sender, RoutedEventArgs e)
        {
            JKTYPE.JointValue current = new JKTYPE.JointValue();
            jakaAPI.get_joint_position(ref handle, ref current);
            for (int i = 0; i < 6; i++)
                lastJoints[i] = current.jVal[i];

            if (!connected) return;

            JKTYPE.JointValue newJoint = new JKTYPE.JointValue();
            newJoint.jVal = new double[6];

            // Đọc input
            try
            {
                double[] inputDeg = new double[6];
                inputDeg[0] = double.Parse(txtJ1.Text);
                inputDeg[1] = double.Parse(txtJ2.Text);
                inputDeg[2] = double.Parse(txtJ3.Text);
                inputDeg[3] = double.Parse(txtJ4.Text);
                inputDeg[4] = double.Parse(txtJ5.Text);
                inputDeg[5] = double.Parse(txtJ6.Text);

                // Chuyển sang radian và chỉ cập nhật khi có thay đổi
                for (int i = 0; i < 6; i++)
                {
                    double newVal = inputDeg[i] * Math.PI / 180;
                    if (Math.Abs(newVal - lastJoints[i]) > 0.001) // thay đổi > ~0.06°
                        newJoint.jVal[i] = newVal;
                    else
                        newJoint.jVal[i] = lastJoints[i];
                }
            }
            catch
            {
                MessageBox.Show("⚠️ Vui lòng nhập số hợp lệ cho các joint (độ)!");
                return;
            }

            // Gửi lệnh move
            int ret = jakaAPI.joint_move(ref handle, ref newJoint, JKTYPE.MoveMode.ABS, true, 20);
            if (ret != 0)
                MessageBox.Show($"❌ Lỗi di chuyển (mã {ret})");
            else
            {
                // Cập nhật trạng thái mới
                for (int i = 0; i < 6; i++)
                    lastJoints[i] = newJoint.jVal[i];
            }
        }

        //-------
        // ==============================
        // 📡 Cập nhật góc thực tế liên tục
        // ==============================
        void StartRealtimeUpdate()
        {
            cts = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (!connected)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    JKTYPE.RobotStatus status = new JKTYPE.RobotStatus();
                    int ret = jakaAPI.get_robot_status(ref handle, ref status);
                    if (ret != 0)
                    {
                        Thread.Sleep(200);
                        continue;
                    }

                    double[] deg = new double[6];
                    for (int i = 0; i < 6; i++)
                        deg[i] = status.joint_position[i] * 180 / Math.PI;

                    Dispatcher.Invoke(() =>
                    {
                        txtCurJ1.Text = $"{deg[0]:F2}";
                        txtCurJ2.Text = $"{deg[1]:F2}";
                        txtCurJ3.Text = $"{deg[2]:F2}";
                        txtCurJ4.Text = $"{deg[3]:F2}";
                        txtCurJ5.Text = $"{deg[4]:F2}";
                        txtCurJ6.Text = $"{deg[5]:F2}";
                    });

                    Thread.Sleep(200);
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            cts?.Cancel();
            if (connected)
            {
                jakaAPI.destory_handler(ref handle);
                connected = false;
            }
            base.OnClosed(e);
        }
    }
}
