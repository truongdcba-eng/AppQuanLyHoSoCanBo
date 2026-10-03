using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyHoSoCB
{
    /// <summary>Kết nối CSDL + hàm băm mật khẩu.</summary>
    public static class Db
    {
        // Đổi Server cho phù hợp máy bạn:
        //   SQL Server Express : .\SQLEXPRESS
        //   LocalDB            : (localdb)\MSSQLLocalDB
        public static readonly string ConnectionString =
            @"workstation id=QuanLyHoSoCBVC.mssql.somee.com;packet size=4096;user id=truongdz20cm_SQLLogin_1;pwd=vsc6vr2q6u;data source=QuanLyHoSoCBVC.mssql.somee.com;persist security info=False;initial catalog=QuanLyHoSoCBVC;TrustServerCertificate=True";

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        /// <summary>SHA-256 dạng HEX viết hoa (khớp với HASHBYTES trong script SQL).</summary>
        public static string Hash(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "");
            }
        }
    }

    /// <summary>Thông tin người dùng đang đăng nhập.</summary>
    public static class Session
    {
        public static string Username { get; set; } = "";
        public static string Role { get; set; } = "";      // Staff | Head | HR | Board
        public static string MaCanBo { get; set; } = "";

        public static void Clear()
        {
            Username = Role = MaCanBo = "";
        }
    }
}
