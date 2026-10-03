using Microsoft.Data.SqlClient;

namespace QuanLyHoSoCB
{

    // =====================================================
    //  FORM ĐĂNG NHẬP
    // =====================================================
    public class LoginForm : Form
    {
        private readonly TextBox txtUser = new();
        private readonly TextBox txtPass = new() { UseSystemPasswordChar = true };
        private readonly Button btnLogin = new() { Text = "Đăng nhập" };
        private readonly Label lblMsg = new() { ForeColor = Color.Firebrick, AutoSize = false };
        private int soLanSai = 0;

        public LoginForm()
        {
            Text = "Đăng nhập - Quản lý hồ sơ cán bộ";
            ClientSize = new Size(380, 260);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 10);

            var title = new Label
            {
                Text = "QUẢN LÝ HỒ SƠ CÁN BỘ",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            title.SetBounds(0, 15, 380, 35);

            var lbUser = new Label { Text = "Tài khoản", AutoSize = true };
            lbUser.Location = new Point(30, 75);
            txtUser.SetBounds(130, 72, 210, 28);

            var lbPass = new Label { Text = "Mật khẩu", AutoSize = true };
            lbPass.Location = new Point(30, 120);
            txtPass.SetBounds(130, 117, 210, 28);

            lblMsg.SetBounds(30, 155, 320, 25);

            btnLogin.SetBounds(130, 185, 210, 38);
            btnLogin.Click += BtnLogin_Click;

            Controls.AddRange(new Control[] { title, lbUser, txtUser, lbPass, txtPass, lblMsg, btnLogin });
            AcceptButton = btnLogin;   // nhấn Enter = Đăng nhập
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text;

            if (user == "" || pass == "")
            {
                lblMsg.Text = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return;
            }

            try
            {
                using var conn = Db.GetConnection();
                conn.Open();

                const string sql = @"SELECT VaiTro, MaCanBo
                                     FROM TaiKhoan
                                     WHERE TenDangNhap = @u AND MatKhau = @p AND TrangThai = 1";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u", user);
                cmd.Parameters.AddWithValue("@p", Db.Hash(pass));

                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    Session.Username = user;
                    Session.Role = rd.GetString(0);
                    Session.MaCanBo = rd.IsDBNull(1) ? "" : rd.GetString(1);

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    soLanSai++;
                    lblMsg.Text = $"Sai tài khoản hoặc mật khẩu (lần {soLanSai}).";
                    txtPass.Clear();
                    txtPass.Focus();

                    if (soLanSai >= 5)
                    {
                        MessageBox.Show("Bạn đã nhập sai quá 5 lần. Chương trình sẽ đóng.",
                                        "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không kết nối được cơ sở dữ liệu:\n" + ex.Message,
                                "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {

        }
    }
}
