using System;
using System.Threading;
using jakaApi;
using jkType;

class Program
{
    static void SetEnvironment()
    {
        string cur_path = Environment.CurrentDirectory;
        string[] paths = cur_path.Split("example");
        var path = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", Path.Join(paths[0], "out\\shared\\Release\\") + ";" + path);
    }

    static void CheckResult(int ret, string action)
    {
        if (ret == 0)
            Console.WriteLine($"✅ {action} thành công");
        else
            Console.WriteLine($"❌ {action} thất bại (mã lỗi: {ret})");
    }

    public static void Main(string[] args)
    {
        SetEnvironment();

        int handle = 0;
        int ret;

        // 1️⃣ Kết nối robot hoặc mô phỏng
        string robotIP = "192.168.31.61"; // ⚠️ Thay IP phù hợp
        ret = jakaAPI.create_handler(robotIP, ref handle);
        CheckResult(ret, "Kết nối robot");
        if (ret != 0) return;

        // 2️⃣ Bật nguồn và enable robot
        jakaAPI.power_on(ref handle);
        Thread.Sleep(2000);
        jakaAPI.enable_robot(ref handle);
        Thread.Sleep(2000);

        // 3️⃣ Về vị trí home bằng joint_move (MoveJ)
        Console.WriteLine("\n🏠 Đưa robot về vị trí chuẩn (MoveJ)...");

        JKTYPE.JointValue jointHome = new JKTYPE.JointValue();
        jointHome.jVal = new double[6];
        jointHome.jVal[0] = -3.246 * Math.PI / 180.0;
        jointHome.jVal[1] = 74.315 * Math.PI / 180.0;
        jointHome.jVal[2] = -84.073 * Math.PI / 180.0;
        jointHome.jVal[3] = 9.758 * Math.PI / 180.0;
        jointHome.jVal[4] = -3.246 * Math.PI / 180.0;
        jointHome.jVal[5] = 0 * Math.PI / 180.0;

        ret = jakaAPI.joint_move(ref handle, ref jointHome, JKTYPE.MoveMode.ABS, true, 20);
        CheckResult(ret, "Về vị trí home (joint_move)");
        Thread.Sleep(3000);

        //// 4️⃣ Lấy lại vị trí hiện tại sau khi về home
        JKTYPE.CartesianPose curPose = new JKTYPE.CartesianPose();
        jakaAPI.get_tcp_position(ref handle, ref curPose);
        Console.WriteLine($"\n📍 Vị trí hiện tại sau khi về home:");
        Console.WriteLine($"X={curPose.tran.x:F2}, Y={curPose.tran.y:F2}, Z={curPose.tran.z:F2}");

        // =========================
        // 5️⃣ Test MoveMode.ABS (tọa độ tuyệt đối)
        // =========================
        Console.WriteLine("\n==============================");
        Console.WriteLine("🔹 THỬ NGHIỆM MoveMode.ABS");
        Console.WriteLine("==============================");

        JKTYPE.CartesianPose absPose = curPose;
        absPose.tran.x = 450;  // ⚠️ Di chuyển đến X tuyệt đối = 450 mm
        absPose.tran.y = curPose.tran.y;
        absPose.tran.z = curPose.tran.z;

        ret = jakaAPI.linear_move(ref handle, ref absPose, JKTYPE.MoveMode.ABS, true, 20);
        CheckResult(ret, "Di chuyển ABS (tới X=450)");
        Thread.Sleep(3000);

        JKTYPE.CartesianPose posAfterAbs = new JKTYPE.CartesianPose();
        jakaAPI.get_tcp_position(ref handle, ref posAfterAbs);
        Console.WriteLine($"📍 Sau ABS: X={posAfterAbs.tran.x:F2}, Y={posAfterAbs.tran.y:F2}, Z={posAfterAbs.tran.z:F2}");

        //// =========================
        //// 6️⃣ Test MoveMode.INCR (dịch tương đối)
        //// =========================
        Console.WriteLine("\n==============================");
        Console.WriteLine("🔹 THỬ NGHIỆM MoveMode.INCR");
        Console.WriteLine("==============================");

        JKTYPE.CartesianPose incrPose = new JKTYPE.CartesianPose();
        incrPose.tran.x = 50;  // Dịch thêm 50 mm theo trục X (tương đối)
        incrPose.tran.y = 0;
        incrPose.tran.z = 0;
        incrPose.rpy.rx = 0;
        incrPose.rpy.ry = 0;
        incrPose.rpy.rz = 0;

        ret = jakaAPI.linear_move(ref handle, ref incrPose, JKTYPE.MoveMode.INCR, true, 20);
        CheckResult(ret, "Di chuyển INCR (+50mm theo X)");
        Thread.Sleep(3000);

        JKTYPE.CartesianPose posAfterIncr = new JKTYPE.CartesianPose();
        jakaAPI.get_tcp_position(ref handle, ref posAfterIncr);
        Console.WriteLine($"📍 Sau INCR: X={posAfterIncr.tran.x:F2}, Y={posAfterIncr.tran.y:F2}, Z={posAfterIncr.tran.z:F2}");

        // 7️⃣ Kết thúc
        jakaAPI.destory_handler(ref handle);
        Console.WriteLine("\n✅ Hoàn tất test MoveMode.ABS (tọa độ tuyệt đối) vs INCR (tương đối).");
        //Console.ReadKey();
    }
}
