using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace QuanLyHoSoCB
{
    /// <summary>
    /// Form của Staff / Employee (Primary Data Provider):
    ///  - Xem thông báo từ hệ thống
    ///  - Tự khai báo / cập nhật thông tin cá nhân của CHÍNH MÌNH
    ///  - Đính kèm file hồ sơ PDF
    ///  - Sau khi lưu, hồ sơ chuyển sang "Chờ duyệt" để Trưởng đơn vị xem xét
    /// </summary>
    public class StaffForm : Form
    {
        // ----- Controls nhập liệu -----
        private readonly TextBox txtMa = new() { ReadOnly = true, BackColor = SystemColors.Control };
        private readonly TextBox txtHoTen = new() { MaxLength = 100 };
        private readonly TextBox txtCCCD = new() { MaxLength = 12 };
        private readonly DateTimePicker dtpNgaySinh = new()
        { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
        private readonly ComboBox cboGioiTinh = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox txtQueQuan = new() { MaxLength = 200 };
        private readonly TextBox txtDiaChi = new() { MaxLength = 250 };
        private readonly TextBox txtDanToc = new() { MaxLength = 50 };
        private readonly TextBox txtNgheNghiep = new() { MaxLength = 100 };
        private readonly TextBox txtCoQuan = new() { MaxLength = 150 };
        private readonly TextBox txtChucVu = new() { MaxLength = 100 };
        private readonly ComboBox cboNgoaiNgu = new() { DropDownStyle = ComboBoxStyle.DropDown };
        private readonly TextBox txtPdf = new() { ReadOnly = true, BackColor = SystemColors.Control };

        private readonly Button btnChonPdf = new() { Text = "Chọn..." };
        private readonly Button btnMoPdf = new() { Text = "Xem" };
        private readonly Button btnLuu = new() { Text = "Lưu & Gửi duyệt", Width = 150, Height = 34 };
        private readonly Button btnTaiLai = new() { Text = "Tải lại", Width = 90, Height = 34 };
        private readonly Button btnDangXuat = new() { Text = "Đăng xuất", Width = 100, Height = 34 };

        private readonly Label lblTrangThai = new() { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        private readonly ListBox lstThongBao = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };

        private string? pdfNguon;   // file PDF vừa chọn (chưa copy vào thư mục hệ thống)

        public StaffForm()
        {
            Text = "Hồ sơ cá nhân - Cán bộ";
            ClientSize = new Size(1020, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            BuildUI();

            cboGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
            cboNgoaiNgu.Items.AddRange(new object[]
            {
                "Chưa có", "A2", "B1", "B2", "C1", "C2",
                "IELTS", "TOEIC", "TOEFL", "Khác"
            });

            btnChonPdf.Click += BtnChonPdf_Click;
            btnMoPdf.Click += BtnMoPdf_Click;
            btnLuu.Click += BtnLuu_Click;
            btnTaiLai.Click += (s, e) => { LoadProfile(); LoadNotifications(); };
            btnDangXuat.Click += (s, e) =>
            {
                if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DialogResult = DialogResult.Retry;   // Program.cs sẽ mở lại form đăng nhập
                    Close();
                }
            };

            Load += (s, e) => { LoadProfile(); LoadNotifications(); };
        }

        // =====================================================
        //  DỰNG GIAO DIỆN
        // =====================================================
        private void BuildUI()
        {
            // --- Bảng nhập liệu (2 cột: nhãn | ô nhập) ---
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoScroll = true
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            void Row(string label, Control c)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
                tbl.Controls.Add(new Label
                {
                    Text = label,
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                });
                c.Dock = DockStyle.Fill;
                c.Margin = new Padding(3, 4, 3, 4);
                tbl.Controls.Add(c);
            }

            // Dòng PDF: [ textbox ........ ][Chọn...][Xem]
            var pnlPdf = new Panel();
            btnMoPdf.Dock = DockStyle.Right;
            btnMoPdf.Width = 60;
            btnChonPdf.Dock = DockStyle.Right;
            btnChonPdf.Width = 80;
            txtPdf.Dock = DockStyle.Fill;
            pnlPdf.Controls.Add(txtPdf);        // Fill thêm TRƯỚC
            pnlPdf.Controls.Add(btnChonPdf);
            pnlPdf.Controls.Add(btnMoPdf);

            Row("Mã cán bộ", txtMa);
            Row("Họ và tên (*)", txtHoTen);
            Row("Số CCCD (*)", txtCCCD);
            Row("Ngày sinh", dtpNgaySinh);
            Row("Giới tính (*)", cboGioiTinh);
            Row("Quê quán", txtQueQuan);
            Row("Địa chỉ thường trú", txtDiaChi);
            Row("Dân tộc", txtDanToc);
            Row("Nghề nghiệp", txtNgheNghiep);
            Row("Cơ quan", txtCoQuan);
            Row("Chức vụ", txtChucVu);
            Row("Trình độ ngoại ngữ", cboNgoaiNgu);
            Row("File hồ sơ (PDF)", pnlPdf);

            var grpHoSo = new GroupBox { Text = "Thông tin hồ sơ", Dock = DockStyle.Fill, Padding = new Padding(10) };
            grpHoSo.Controls.Add(tbl);

            var grpTb = new GroupBox { Text = "Thông báo từ hệ thống", Dock = DockStyle.Fill, Padding = new Padding(10) };
            grpTb.Controls.Add(lstThongBao);

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            main.Controls.Add(grpHoSo, 0, 0);
            main.Controls.Add(grpTb, 1, 0);

            // --- Thanh trên: trạng thái hồ sơ ---
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(12, 10, 0, 0) };
            pnlTop.Controls.Add(lblTrangThai);

            // --- Thanh dưới: nút chức năng ---
            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 8, 10, 0)
            };
            pnlBottom.Controls.Add(btnDangXuat);
            pnlBottom.Controls.Add(btnTaiLai);
            pnlBottom.Controls.Add(btnLuu);

            // Control Fill phải được thêm TRƯỚC các control Dock Top/Bottom
            Controls.Add(main);
            Controls.Add(pnlTop);
            Controls.Add(pnlBottom);
        }

        // =====================================================
        //  TẢI HỒ SƠ CỦA CHÍNH NGƯỜI ĐANG ĐĂNG NHẬP
        // =====================================================
        private static string S(SqlDataReader rd, string col) =>
            rd[col] == DBNull.Value ? "" : rd[col].ToString()!;

        private void LoadProfile()
        {
            try
            {
                using var conn = Db.GetConnection();
                conn.Open();
                using var cmd = new SqlCommand("SELECT * FROM HoSo WHERE MaCanBo = @ma", conn);
                cmd.Parameters.AddWithValue("@ma", Session.MaCanBo);
                using var rd = cmd.ExecuteReader();

                if (!rd.Read())
                {
                    MessageBox.Show("Tài khoản chưa được liên kết với hồ sơ cán bộ nào. Vui lòng liên hệ Phòng HC-NS.",
                                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnLuu.Enabled = false;
                    return;
                }

                txtMa.Text = S(rd, "MaCanBo");
                txtHoTen.Text = S(rd, "HoTen");
                txtCCCD.Text = S(rd, "SoCCCD").Trim();
                dtpNgaySinh.Value = rd["NgaySinh"] is DateTime d ? d : DateTime.Today.AddYears(-30);
                cboGioiTinh.SelectedItem = S(rd, "GioiTinh") == "" ? null : S(rd, "GioiTinh");
                txtQueQuan.Text = S(rd, "QueQuan");
                txtDiaChi.Text = S(rd, "DiaChiThuongTru");
                txtDanToc.Text = S(rd, "DanToc");
                txtNgheNghiep.Text = S(rd, "NgheNghiep");
                txtCoQuan.Text = S(rd, "CoQuan");
                txtChucVu.Text = S(rd, "ChucVu");
                cboNgoaiNgu.Text = S(rd, "TrinhDoNgoaiNgu");
                txtPdf.Text = S(rd, "FilePDF");
                pdfNguon = null;

                lblTrangThai.Text = "Trạng thái hồ sơ: " + TenTrangThai(S(rd, "TrangThai"));
                Text = $"Hồ sơ cá nhân - {S(rd, "HoTen")} ({S(rd, "MaCanBo")})";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải hồ sơ:\n" + ex.Message, "Lỗi",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string TenTrangThai(string code) => code switch
        {
            "ChuaCapNhat" => "Chưa cập nhật",
            "ChoDuyet" => "Đã gửi - chờ Trưởng đơn vị duyệt",
            "DaDuyetCap1" => "Trưởng đơn vị đã duyệt - chờ Ban lãnh đạo phê duyệt",
            "DaPheDuyet" => "Đã được phê duyệt",
            "TuChoi" => "Bị trả lại - cần chỉnh sửa",
            _ => code
        };

        // =====================================================
        //  THÔNG BÁO
        // =====================================================
        private void LoadNotifications()
        {
            try
            {
                lstThongBao.Items.Clear();
                using var conn = Db.GetConnection();
                conn.Open();
                const string sql = @"SELECT NgayGui, NoiDung FROM ThongBao
                                     WHERE MaCanBo = @ma OR MaCanBo IS NULL
                                     ORDER BY NgayGui DESC";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ma", Session.MaCanBo);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    lstThongBao.Items.Add($"[{rd.GetDateTime(0):dd/MM/yyyy}] {rd.GetString(1)}");

                if (lstThongBao.Items.Count == 0)
                    lstThongBao.Items.Add("(Chưa có thông báo nào)");
            }
            catch (Exception ex)
            {
                lstThongBao.Items.Add("Lỗi tải thông báo: " + ex.Message);
            }
        }

        // =====================================================
        //  FILE PDF
        // =====================================================
        private void BtnChonPdf_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn file hồ sơ PDF",
                Filter = "PDF (*.pdf)|*.pdf"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                pdfNguon = ofd.FileName;
                txtPdf.Text = Path.GetFileName(ofd.FileName) + "  (chưa lưu)";
            }
        }

        private void BtnMoPdf_Click(object? sender, EventArgs e)
        {
            // Ưu tiên file vừa chọn; nếu không có thì mở file đã lưu trong hệ thống
            string path = pdfNguon ?? Path.Combine(AppContext.BaseDirectory, txtPdf.Text);

            if (string.IsNullOrWhiteSpace(txtPdf.Text) || !File.Exists(path))
            {
                MessageBox.Show("Chưa có file PDF hoặc không tìm thấy file.", "Thông báo",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        // =====================================================
        //  LƯU HỒ SƠ
        // =====================================================
        private static object Nz(string s) =>
            string.IsNullOrWhiteSpace(s) ? DBNull.Value : s.Trim();

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void BtnLuu_Click(object? sender, EventArgs e)
        {
            // ----- Kiểm tra dữ liệu -----
            if (string.IsNullOrWhiteSpace(txtHoTen.Text))
            { Warn("Vui lòng nhập họ và tên."); txtHoTen.Focus(); return; }

            if (!Regex.IsMatch(txtCCCD.Text.Trim(), @"^\d{12}$"))
            { Warn("Số CCCD phải gồm đúng 12 chữ số."); txtCCCD.Focus(); return; }

            if (dtpNgaySinh.Value.Date >= DateTime.Today)
            { Warn("Ngày sinh không hợp lệ."); dtpNgaySinh.Focus(); return; }

            if (cboGioiTinh.SelectedIndex < 0)
            { Warn("Vui lòng chọn giới tính."); cboGioiTinh.Focus(); return; }

            try
            {
                // ----- Copy file PDF mới vào thư mục HoSoPDF cạnh file .exe -----
                string duongDanPdf = txtPdf.Text.Replace("  (chưa lưu)", "");
                if (pdfNguon != null)
                {
                    string dir = Path.Combine(AppContext.BaseDirectory, "HoSoPDF");
                    Directory.CreateDirectory(dir);
                    string tenFile = $"{Session.MaCanBo}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                    File.Copy(pdfNguon, Path.Combine(dir, tenFile), true);
                    duongDanPdf = Path.Combine("HoSoPDF", tenFile);   // lưu đường dẫn tương đối
                }

                // ----- Cập nhật CSDL: Staff chỉ được sửa hồ sơ của chính mình -----
                using var conn = Db.GetConnection();
                conn.Open();
                const string sql = @"
                    UPDATE HoSo SET
                        HoTen           = @ten,
                        SoCCCD          = @cccd,
                        NgaySinh        = @ns,
                        GioiTinh        = @gt,
                        QueQuan         = @qq,
                        DiaChiThuongTru = @dc,
                        DanToc          = @dt,
                        NgheNghiep      = @nn,
                        CoQuan          = @cq,
                        ChucVu          = @cv,
                        TrinhDoNgoaiNgu = @nnu,
                        FilePDF         = @pdf,
                        TrangThai       = 'ChoDuyet',
                        NgayCapNhat     = GETDATE()
                    WHERE MaCanBo = @ma";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ten", txtHoTen.Text.Trim());
                cmd.Parameters.AddWithValue("@cccd", txtCCCD.Text.Trim());
                cmd.Parameters.AddWithValue("@ns", dtpNgaySinh.Value.Date);
                cmd.Parameters.AddWithValue("@gt", cboGioiTinh.SelectedItem!.ToString()!);
                cmd.Parameters.AddWithValue("@qq", Nz(txtQueQuan.Text));
                cmd.Parameters.AddWithValue("@dc", Nz(txtDiaChi.Text));
                cmd.Parameters.AddWithValue("@dt", Nz(txtDanToc.Text));
                cmd.Parameters.AddWithValue("@nn", Nz(txtNgheNghiep.Text));
                cmd.Parameters.AddWithValue("@cq", Nz(txtCoQuan.Text));
                cmd.Parameters.AddWithValue("@cv", Nz(txtChucVu.Text));
                cmd.Parameters.AddWithValue("@nnu", Nz(cboNgoaiNgu.Text));
                cmd.Parameters.AddWithValue("@pdf", Nz(duongDanPdf));
                cmd.Parameters.AddWithValue("@ma", Session.MaCanBo);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Đã lưu hồ sơ và gửi cho Trưởng đơn vị xem xét.", "Thành công",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadProfile();
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                Warn("Số CCCD này đã tồn tại trong hệ thống (thuộc hồ sơ khác).");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu hồ sơ:\n" + ex.Message, "Lỗi",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
