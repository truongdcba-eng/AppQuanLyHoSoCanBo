using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;

namespace QuanLyHoSoCB
{
    /// <summary>Các hàm dùng chung cho Head / HR / Board.</summary>
    public static class Ui
    {
        // =====================================================
        //  TRUY VẤN CSDL NHANH
        // =====================================================
        public static DataTable Query(string sql, params (string Name, object? Value)[] ps)
        {
            var dt = new DataTable();
            using var conn = Db.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public static int Exec(string sql, params (string Name, object? Value)[] ps)
        {
            using var conn = Db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            return cmd.ExecuteNonQuery();
        }

        public static object? Scalar(string sql, params (string Name, object? Value)[] ps)
        {
            using var conn = Db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
            object? r = cmd.ExecuteScalar();
            return r == DBNull.Value ? null : r;
        }

        public static string TenNguoiDung()
        {
            try
            {
                return Convert.ToString(Scalar("SELECT HoTen FROM HoSo WHERE MaCanBo = @m",
                                               ("@m", Session.MaCanBo))) ?? "";
            }
            catch { return ""; }
        }

        // =====================================================
        //  HỘP THOẠI
        // =====================================================
        public static void Info(string m) =>
            MessageBox.Show(m, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static void Warn(string m) =>
            MessageBox.Show(m, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        public static void Error(string m) =>
            MessageBox.Show(m, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        public static bool Confirm(string m) =>
            MessageBox.Show(m, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        /// <summary>Hộp nhập văn bản. Trả về null nếu bấm Hủy.</summary>
        public static string? Prompt(string title, string label, bool multiline = false,
                                     bool password = false, string text = "")
        {
            using var f = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(430, multiline ? 230 : 140),
                Font = new Font("Segoe UI", 10)
            };
            var lb = new Label { Text = label, Location = new Point(15, 12), Size = new Size(400, 24) };
            var tb = new TextBox
            {
                Location = new Point(15, 40),
                Width = 400,
                Text = text,
                UseSystemPasswordChar = password
            };
            if (multiline)
            {
                tb.Multiline = true;
                tb.Height = 130;
                tb.ScrollBars = ScrollBars.Vertical;
            }
            int y = f.ClientSize.Height - 46;
            var ok = new Button { Text = "OK", Location = new Point(235, y), Size = new Size(85, 32), DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Hủy", Location = new Point(330, y), Size = new Size(85, 32), DialogResult = DialogResult.Cancel };
            f.Controls.AddRange(new Control[] { lb, tb, ok, cancel });
            if (!multiline) f.AcceptButton = ok;   // ô nhiều dòng: Enter = xuống dòng
            f.CancelButton = cancel;

            return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
        }

        // =====================================================
        //  CHỮ KÝ SỐ (mô phỏng bằng SHA-256)
        // =====================================================
        /// <summary>Yêu cầu nhập lại mật khẩu của người đang đăng nhập trước khi ký.</summary>
        public static bool XacThucMatKhau()
        {
            string? pw = Prompt("Xác thực chữ ký số", "Nhập lại mật khẩu của bạn để ký:", password: true);
            if (pw == null) return false;

            object? r = Scalar(@"SELECT COUNT(1) FROM TaiKhoan
                                 WHERE TenDangNhap = @u AND MatKhau = @p AND TrangThai = 1",
                               ("@u", Session.Username), ("@p", Db.Hash(pw)));
            if (Convert.ToInt32(r) != 1)
            {
                Warn("Mật khẩu không đúng. Không thể ký.");
                return false;
            }
            return true;
        }

        /// <summary>Tạo "chữ ký" = SHA-256(nội dung | người ký | thời điểm ký).</summary>
        public static string TaoChuKy(string noiDung, DateTime thoiDiem) =>
            Db.Hash($"{noiDung}|{Session.Username}|{thoiDiem:O}");

        // =====================================================
        //  FILE PDF
        // =====================================================
        public static string DuongDanDayDu(string rel) =>
            Path.IsPathRooted(rel) ? rel : Path.Combine(AppContext.BaseDirectory, rel);

        public static void MoPdf(string? rel)
        {
            if (string.IsNullOrWhiteSpace(rel)) { Info("Hồ sơ chưa có file PDF."); return; }
            string full = DuongDanDayDu(rel);
            if (!File.Exists(full)) { Warn("Không tìm thấy file:\n" + full); return; }
            Process.Start(new ProcessStartInfo(full) { UseShellExecute = true });
        }

        public static string HashFile(string path)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(fs)).Replace("-", "");
        }

        // =====================================================
        //  TRẠNG THÁI HỒ SƠ
        // =====================================================
        public static string TenTrangThai(string code) => code switch
        {
            "ChuaCapNhat" => "Chưa cập nhật",
            "ChoDuyet" => "Chờ Trưởng đơn vị duyệt",
            "DaDuyetCap1" => "Chờ Ban lãnh đạo phê duyệt",
            "DaPheDuyet" => "Đã phê duyệt",
            "TuChoi" => "Bị trả lại",
            _ => code
        };

        /// <summary>Đổi mã trạng thái trong 1 cột của DataTable sang tiếng Việt.</summary>
        public static void MapStatus(DataTable dt, string col)
        {
            if (!dt.Columns.Contains(col)) return;
            foreach (DataRow r in dt.Rows)
                r[col] = TenTrangThai(Convert.ToString(r[col]) ?? "");
        }

        // =====================================================
        //  GIAO DIỆN DÙNG CHUNG
        // =====================================================
        public static DataGridView CreateGrid() => new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window
        };

        /// <summary>Lấy giá trị ô ở dòng đang chọn (null nếu không có).</summary>
        public static string? Selected(DataGridView g, string col)
        {
            if (g.CurrentRow == null || !g.Columns.Contains(col)) return null;
            object? v = g.CurrentRow.Cells[col].Value;
            return v == null || v == DBNull.Value ? null : Convert.ToString(v);
        }

        public static void FormatCot(DataGridView g, string col, string format)
        {
            if (g.Columns.Contains(col)) g.Columns[col].DefaultCellStyle.Format = format;
        }

        public static void AnCot(DataGridView g, string col)
        {
            if (g.Columns.Contains(col)) g.Columns[col].Visible = false;
        }

        /// <summary>Thanh tiêu đề + nút Đăng xuất (Program.cs sẽ mở lại form đăng nhập).</summary>
        public static Panel Header(Form f, string text)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(232, 240, 250) };
            var lbl = new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(14, 13),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            var btn = new Button
            {
                Text = "Đăng xuất",
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btn.Location = new Point(f.ClientSize.Width - btn.Width - 14, 9);
            btn.Click += (s, e) =>
            {
                if (Confirm("Bạn có chắc muốn đăng xuất?"))
                {
                    f.DialogResult = DialogResult.Retry;
                    f.Close();
                }
            };
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(btn);
            return pnl;
        }

        /// <summary>Bảng 2 cột (nhãn | ô nhập) dùng cho các form nhập liệu.</summary>
        public static TableLayoutPanel FormTable()
        {
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return t;
        }

        public static void AddRow(TableLayoutPanel t, string label, Control c, int height = 36)
        {
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            t.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = height > 40 ? AnchorStyles.Top | AnchorStyles.Left : AnchorStyles.Left,
                Margin = new Padding(3, height > 40 ? 8 : 3, 3, 3)
            });
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(3, 4, 3, 4);
            t.Controls.Add(c);
        }
    }

    // =====================================================
    //  FORM XEM CHI TIẾT HỒ SƠ (chỉ đọc) - dùng cho Head / Board / HR
    // =====================================================
    public class ProfileViewForm : Form
    {
        private readonly string? duongDanPdf;

        public ProfileViewForm(string maCanBo)
        {
            Text = "Chi tiết hồ sơ - " + maCanBo;
            ClientSize = new Size(640, 690);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 10);

            var dt = Ui.Query("SELECT * FROM HoSo WHERE MaCanBo = @m", ("@m", maCanBo));
            if (dt.Rows.Count == 0)
            {
                Ui.Warn("Không tìm thấy hồ sơ.");
                Load += (s, e) => Close();
                return;
            }
            var r = dt.Rows[0];
            string G(string c) => r[c] == DBNull.Value ? "" : Convert.ToString(r[c]) ?? "";
            duongDanPdf = G("FilePDF");

            var tbl = Ui.FormTable();
            void Add(string label, string val, bool multi = false)
            {
                var tb = new TextBox
                {
                    Text = val,
                    ReadOnly = true,
                    Multiline = multi,
                    ScrollBars = multi ? ScrollBars.Vertical : ScrollBars.None,
                    BackColor = SystemColors.Control
                };
                Ui.AddRow(tbl, label, tb, multi ? 80 : 34);
            }

            Add("Mã cán bộ", G("MaCanBo"));
            Add("Họ và tên", G("HoTen"));
            Add("Số CCCD", G("SoCCCD").Trim());
            Add("Ngày sinh", r["NgaySinh"] is DateTime d ? d.ToString("dd/MM/yyyy") : "");
            Add("Giới tính", G("GioiTinh"));
            Add("Quê quán", G("QueQuan"));
            Add("Địa chỉ thường trú", G("DiaChiThuongTru"));
            Add("Dân tộc", G("DanToc"));
            Add("Nghề nghiệp", G("NgheNghiep"));
            Add("Cơ quan", G("CoQuan"));
            Add("Chức vụ", G("ChucVu"));
            Add("Trình độ ngoại ngữ", G("TrinhDoNgoaiNgu"));
            Add("File PDF", G("FilePDF"));
            Add("Trạng thái", Ui.TenTrangThai(G("TrangThai")));
            Add("Xếp loại (TĐV)", G("XepLoai"));
            Add("Nhận xét (TĐV)", G("DanhGiaTruongDonVi"), true);

            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 8, 10, 0)
            };
            var btnDong = new Button { Text = "Đóng", Width = 90, Height = 34 };
            var btnPdf = new Button { Text = "Mở file PDF", Width = 130, Height = 34 };
            btnDong.Click += (s, e) => Close();
            btnPdf.Click += (s, e) => Ui.MoPdf(duongDanPdf);
            pnlBottom.Controls.Add(btnDong);
            pnlBottom.Controls.Add(btnPdf);

            Controls.Add(tbl);
            Controls.Add(pnlBottom);
        }
    }
}
