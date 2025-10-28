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

    static double ReadDouble(string name)
    {
        Console.Write($"{name}: ");
        string input = Console.ReadLine();
        return double.TryParse(input, out double val) ? val : 0.0;
    }

    public static void Main(string[] args)
    {
        SetEnvironment();

        int handle = 0;
        int ret;

        // 1️⃣ Kết nối robot
        string robotIP = "192.168.31.61";
        ret = jakaAPI.create_handler(robotIP, ref handle);
        CheckResult(ret, "Kết nối robot");
        if (ret != 0) return;

        // 2️⃣ Bật nguồn và enable
        jakaAPI.power_on(ref handle);
        Thread.Sleep(2000);
        jakaAPI.enable_robot(ref handle);
        Thread.Sleep(2000);

        // 3️⃣ In vị trí hiện tại
        JKTYPE.CartesianPose curPose = new JKTYPE.CartesianPose();
        jakaAPI.get_tcp_position(ref handle, ref curPose);
        Console.WriteLine($"\n📍 Vị trí hiện tại: X={curPose.tran.x:F2}, Y={curPose.tran.y:F2}, Z={curPose.tran.z:F2}");

        // 4️⃣ Nhập tọa độ đích
        Console.WriteLine("\n🧭 Nhập tọa độ đích (X, Y, Z bằng mm — RX, RY, RZ bằng độ):");
        JKTYPE.CartesianPose targetPose = new JKTYPE.CartesianPose();
        targetPose.tran.x = ReadDouble("X");
        targetPose.tran.y = ReadDouble("Y");
        targetPose.tran.z = ReadDouble("Z");

        double RXdeg = ReadDouble("RX (°)");
        double RYdeg = ReadDouble("RY (°)");
        double RZdeg = ReadDouble("RZ (°)");

        targetPose.rpy.rx = RXdeg * Math.PI / 180.0;
        targetPose.rpy.ry = RYdeg * Math.PI / 180.0;
        targetPose.rpy.rz = RZdeg * Math.PI / 180.0;

        // 5️⃣ Thử di chuyển bằng linear_move
        Console.WriteLine("\n🚀 Thử di chuyển bằng linear_move (ABS)...");
        ret = jakaAPI.linear_move(ref handle, ref targetPose, JKTYPE.MoveMode.ABS, true, 20);
        CheckResult(ret, "linear_move");

        // 6️⃣ Nếu lỗi (-12), fallback sang kine_inverse + joint_move
        if (ret == -12)
        {
            Console.WriteLine("\n⚠️ Linear move thất bại (-12: không có nghiệm hợp lệ).");
            Console.WriteLine("➡️ Đang thử chuyển sang joint_move (dựa trên nghịch kinematics)...");

            // 🔹 Lấy tư thế hiện tại làm tham chiếu
            JKTYPE.JointValue refJoint = new JKTYPE.JointValue();
            refJoint.jVal = new double[6];
            jakaAPI.get_joint_position(ref handle, ref refJoint);

            // 🔹 Tính IK
            JKTYPE.JointValue resultJoint = new JKTYPE.JointValue();
            resultJoint.jVal = new double[6];
            int ikRet = jakaAPI.kine_inverse(ref handle, ref refJoint, ref targetPose, ref resultJoint);

            if (ikRet == 0)
            {
                Console.WriteLine("✅ Tính IK thành công → di chuyển bằng joint_move...");
                int moveRet = jakaAPI.joint_move(ref handle, ref resultJoint, JKTYPE.MoveMode.ABS, true, 20);
                CheckResult(moveRet, "joint_move");
            }
            else
            {
                Console.WriteLine($"❌ Không thể tính được nghiệm IK (mã lỗi: {ikRet}).");
            }
        }

        // 7️⃣ In lại vị trí cuối
        Thread.Sleep(3000);
        JKTYPE.CartesianPose newPose = new JKTYPE.CartesianPose();
        jakaAPI.get_tcp_position(ref handle, ref newPose);
        Console.WriteLine($"\n📍 Vị trí sau khi di chuyển:");
        Console.WriteLine($"X={newPose.tran.x:F2}, Y={newPose.tran.y:F2}, Z={newPose.tran.z:F2}");

        // 8️⃣ Kết thúc
        jakaAPI.destory_handler(ref handle);
        Console.WriteLine("\n✅ Hoàn tất chương trình (auto fallback MoveL → MoveJ).");
        Console.ReadKey();
    }
}
