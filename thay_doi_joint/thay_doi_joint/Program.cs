using jakaApi;
using jkType;
using System;
using System.IO;

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
        // 1. Kết nối tới robot
        ret = jakaAPI.create_handler("192.168.31.61", ref handle);
        CheckResult(ret, "Kết nối robot");
        if (ret != 0) return;

        // 2️. Bật nguồn và enable
        ret = jakaAPI.power_on(ref handle);
        CheckResult(ret, "Bật nguồn");
        System.Threading.Thread.Sleep(2000);

        ret = jakaAPI.enable_robot(ref handle);
        CheckResult(ret, "Kích hoạt robot");
        System.Threading.Thread.Sleep(2000);

        // 3️. Hiển thị joint hiện tại
        JKTYPE.JointValue currentJoint = new JKTYPE.JointValue();
        currentJoint.jVal = new double[6];

        ret = jakaAPI.get_joint_position(ref handle, ref currentJoint);
        CheckResult(ret, "Lấy vị trí khớp hiện tại");

        if (ret == 0)
        {
            Console.WriteLine("\n📟 Giá trị các khớp hiện tại:");
            for (int i = 0; i < 6; i++)
            {
                Console.WriteLine($"  Joint {i + 1}: {currentJoint.jVal[i]:F4} rad  ({currentJoint.jVal[i] * 180 / Math.PI:F2}°)");
            }
        }

        // 4. Nhập giá trị joint mới
        Console.WriteLine("\n🔹 Nhập 6 giá trị joint mới (đơn vị ° — độ):");
        JKTYPE.JointValue joint = new JKTYPE.JointValue();
        joint.jVal = new double[6];
        for (int i = 0; i < 6; i++)
        {
            while (true)
            {
                Console.Write($"  Joint {i + 1}: ");
                string? input = Console.ReadLine();
                if (double.TryParse(input, out double val))
                {
                    // đổi từ độ sang radian
                    joint.jVal[i] = val * Math.PI / 180.0;
                    break;
                }
                else
                {
                    Console.WriteLine("⚠️ Giá trị không hợp lệ. Nhập lại!");
                }
            }
        }

        // 5️⃣. Di chuyển robot tới joint mới
        Console.WriteLine("\n🤖 Đang di chuyển robot tới vị trí mới...");
        ret = jakaAPI.joint_move(ref handle, ref joint, JKTYPE.MoveMode.ABS, true, 20);
        CheckResult(ret, "Di chuyển tới joint mới");

        // 6️⃣. Lấy lại joint sau khi di chuyển
        if (ret == 0)
        {
            System.Threading.Thread.Sleep(1000);
            ret = jakaAPI.get_joint_position(ref handle, ref currentJoint);
            CheckResult(ret, "Lấy lại vị trí khớp sau khi di chuyển");

            if (ret == 0)
            {
                Console.WriteLine("\n📟 Vị trí khớp sau khi di chuyển:");
                for (int i = 0; i < 6; i++)
                {
                    Console.WriteLine($"  Joint {i + 1}: {currentJoint.jVal[i]:F4} rad  ({currentJoint.jVal[i] * 180 / Math.PI:F2}°)");
                }
            }
        }

        // 7️⃣. Ngắt kết nối robot
        jakaAPI.destory_handler(ref handle);
        //Console.WriteLine("\n🔚 Đã ngắt kết nối robot.");
        
    }
}
