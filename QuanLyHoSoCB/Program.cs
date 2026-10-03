using Microsoft.Data.SqlClient;
using QuanLyHoSoCB;
using static System.Collections.Specialized.BitVector32;

namespace QuanLyHoSoCB
{
    // =====================================================
    //  PROGRAM.CS  (thay toàn bộ nội dung file Program.cs)
    // =====================================================
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            while (true)
            {
                // 1. Đăng nhập
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) break;   // đóng form đăng nhập -> thoát
                }

                // 2. Mở form theo vai trò
                Form? main = Session.Role switch
                {
                    "Staff" => new StaffForm(),
                    "Head" => new HeadForm(),
                    "HR" => new HrForm(),
                    "Board" => new BoardForm(),
                    _ => null
                };

                if (main == null)
                {
                    MessageBox.Show($"Chức năng của vai trò '{Session.Role}' chưa được xây dựng.",
                                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Session.Clear();
                    continue;
                }

                // 3. Form chính trả về Retry khi bấm "Đăng xuất" -> quay lại đăng nhập
                var result = main.ShowDialog();
                main.Dispose();
                Session.Clear();
                if (result != DialogResult.Retry) break;                // bấm X -> thoát chương trình
            }
        }
    }
}
