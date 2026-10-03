using Microsoft.Data.SqlClient;

namespace QuanLyHoSoCB
{
    /// <summary>
    /// Form của HR &amp; Admin Department (System Administrator / Process Coordinator):
    ///  Tab 1: Lập kế hoạch cập nhật hồ sơ hằng năm -> gửi Ban lãnh đạo ký -> phát hành hướng dẫn
    ///  Tab 2: Quản lý hồ sơ &amp; tài khoản (thêm cán bộ, cấp tài khoản, đặt lại mật khẩu, khóa/mở khóa)
    ///  Tab 3: Lưu trữ số an toàn (sao chép PDF vào kho, băm SHA-256, kiểm tra toàn vẹn)
    /// </summary>
    public class HrForm : Form
    {
        private const string MatKhauMacDinh = "123456";

        // ----- Tab 1: Kế hoạch -----
        private readonly TextBox txtTieuDe = new() { MaxLength = 200 };
        private readonly NumericUpDown numNam = new() { Minimum = 2020, Maximum = 2100, Value = DateTime.Now.Year };
        private readonly DateTimePicker dtpHan = new()
        { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = DateTime.Today.AddDays(30) };
        private readonly TextBox txtHuongDan = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
        private readonly Button btnTaoKH = new() { Text = "Lập kế hoạch & gửi Ban lãnh đạo", Height = 36 };
        private readonly DataGridView gridKH = Ui.CreateGrid();
        private readonly Button btnPhatHanh = new() { Text = "Phát hành hướng dẫn", Width = 190, Height = 34 };
        private readonly Button btnLamMoiKH = new() { Text = "Tải lại", Width = 90, Height = 34 };

        // ----- Tab 2: Hồ sơ & tài khoản -----
        private readonly TextBox txtTim = new();
        private readonly DataGridView gridHS = Ui.CreateGrid();

        // ----- Tab 3: Lưu trữ -----
        private readonly ComboBox cboLuuTru = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly DataGridView gridLT = Ui.CreateGrid();

        public HrForm()
        {
            Text = "HR & Admin - Quản lý hồ sơ cán bộ";
            ClientSize = new Size(1150, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildTabKeHoach());
            tabs.TabPages.Add(BuildTabHoSo());
            tabs.TabPages.Add(BuildTabLuuTru());

            Controls.Add(tabs);
            Controls.Add(Ui.Header(this, "HR & Admin: " + Ui.TenNguoiDung()));

            Load += (s, e) => { TaiKeHoach(); TaiHoSo(); TaiLuuTru(); };
        }

        // =====================================================
        //  TAB 1 - KẾ HOẠCH CẬP NHẬT HỒ SƠ
        // =====================================================
        private TabPage BuildTabKeHoach()
        {
            var page = new TabPage("1. Kế hoạch cập nhật hồ sơ");

            var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            // Trái: form lập kế hoạch
            var grp = new GroupBox { Text = "Lập kế hoạch cập nhật hồ sơ mới", Dock = DockStyle.Fill, Padding = new Padding(8) };
            var f = Ui.FormTable();
            Ui.AddRow(f, "Tiêu đề (*)", txtTieuDe);
            Ui.AddRow(f, "Năm", numNam);
            Ui.AddRow(f, "Hạn hoàn thành", dtpHan);
            Ui.AddRow(f, "Hướng dẫn (*)", txtHuongDan, 220);
            Ui.AddRow(f, "", btnTaoKH, 46);
            grp.Controls.Add(f);
            main.Controls.Add(grp, 0, 0);

            // Phải: danh sách kế hoạch + nút
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            right.Controls.Add(gridKH, 0, 0);
            var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
            fl.Controls.Add(btnPhatHanh);
            fl.Controls.Add(btnLamMoiKH);
            right.Controls.Add(fl, 0, 1);
            main.Controls.Add(right, 1, 0);

            page.Controls.Add(main);

            btnTaoKH.Click += (s, e) => TaoKeHoach();
            btnPhatHanh.Click += (s, e) => PhatHanh();
            btnLamMoiKH.Click += (s, e) => TaiKeHoach();
            return page;
        }

        private void TaiKeHoach()
        {
            try
            {
                var dt = Ui.Query(@"SELECT Id AS [Mã KH], TieuDe AS [Tiêu đề], Nam AS [Năm], HanChot AS [Hạn chót],
                                           CASE TrangThai WHEN 'ChoPheDuyet' THEN N'Chờ Ban lãnh đạo ký'
                                                          WHEN 'DaPheDuyet'  THEN N'Đã ký duyệt'
                                                          ELSE N'Bị từ chối' END AS [Trạng thái],
                                           CASE WHEN DaPhatHanh = 1 THEN N'Đã phát hành' ELSE N'Chưa' END AS [Phát hành],
                                           LyDoTuChoi AS [Lý do từ chối]
                                    FROM KeHoach ORDER BY Id DESC");
                gridKH.DataSource = dt;
                Ui.FormatCot(gridKH, "Hạn chót", "dd/MM/yyyy");
            }
            catch (Exception ex) { Ui.Error("Lỗi tải kế hoạch:\n" + ex.Message); }
        }

        private void TaoKeHoach()
        {
            if (string.IsNullOrWhiteSpace(txtTieuDe.Text)) { Ui.Warn("Vui lòng nhập tiêu đề."); return; }
            if (string.IsNullOrWhiteSpace(txtHuongDan.Text)) { Ui.Warn("Vui lòng nhập nội dung hướng dẫn."); return; }
            if (dtpHan.Value.Date < DateTime.Today) { Ui.Warn("Hạn hoàn thành không được nhỏ hơn ngày hôm nay."); return; }

            try
            {
                Ui.Exec(@"INSERT INTO KeHoach (TieuDe, Nam, HanChot, HuongDan, NguoiTao)
                          VALUES (@t, @n, @h, @hd, @u)",
                    ("@t", txtTieuDe.Text.Trim()),
                    ("@n", (int)numNam.Value),
                    ("@h", dtpHan.Value.Date),
                    ("@hd", txtHuongDan.Text.Trim()),
                    ("@u", Session.Username));

                Ui.Info("Đã lập kế hoạch và gửi Ban lãnh đạo xem xét, ký duyệt.");
                txtTieuDe.Clear();
                txtHuongDan.Clear();
                TaiKeHoach();
            }
            catch (Exception ex) { Ui.Error("Lỗi khi lập kế hoạch:\n" + ex.Message); }
        }

        private void PhatHanh()
        {
            string? id = Ui.Selected(gridKH, "Mã KH");
            if (id == null) { Ui.Info("Vui lòng chọn một kế hoạch."); return; }

            try
            {
                var dt = Ui.Query(@"SELECT TieuDe, Nam, HanChot, HuongDan, TrangThai, DaPhatHanh
                                    FROM KeHoach WHERE Id = @id", ("@id", int.Parse(id)));
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];

                if (Convert.ToString(r["TrangThai"]) != "DaPheDuyet")
                { Ui.Warn("Chỉ được phát hành kế hoạch đã được Ban lãnh đạo ký duyệt."); return; }
                if (Convert.ToBoolean(r["DaPhatHanh"]))
                { Ui.Info("Kế hoạch này đã được phát hành."); return; }

                string tieuDe = Convert.ToString(r["TieuDe"]) ?? "";
                DateTime han = Convert.ToDateTime(r["HanChot"]);
                string huongDan = Convert.ToString(r["HuongDan"]) ?? "";

                if (!Ui.Confirm($"Phát hành kế hoạch \"{tieuDe}\" và gửi thông báo tới TOÀN BỘ cán bộ?")) return;

                string nd = $"[KẾ HOẠCH CẬP NHẬT HỒ SƠ {r["Nam"]}] {tieuDe}. Hạn hoàn thành: {han:dd/MM/yyyy}. Hướng dẫn: {huongDan}";
                if (nd.Length > 500) nd = nd.Substring(0, 497) + "...";   // cột NoiDung tối đa 500 ký tự

                Ui.Exec("UPDATE KeHoach SET DaPhatHanh = 1, NgayPhatHanh = GETDATE() WHERE Id = @id",
                        ("@id", int.Parse(id)));
                Ui.Exec("INSERT INTO ThongBao (MaCanBo, NoiDung) VALUES (NULL, @nd)", ("@nd", nd));

                if (Ui.Confirm("Bạn có muốn đặt lại trạng thái TẤT CẢ hồ sơ về \"Chưa cập nhật\" " +
                               "để cán bộ khai báo lại theo kế hoạch mới không?"))
                {
                    Ui.Exec("UPDATE HoSo SET TrangThai = 'ChuaCapNhat'");
                }

                Ui.Info("Đã phát hành kế hoạch và gửi thông báo tới toàn bộ cán bộ.");
                TaiKeHoach();
                TaiHoSo();
            }
            catch (Exception ex) { Ui.Error("Lỗi khi phát hành:\n" + ex.Message); }
        }

        // =====================================================
        //  TAB 2 - HỒ SƠ & TÀI KHOẢN
        // =====================================================
        private TabPage BuildTabHoSo()
        {
            var page = new TabPage("2. Hồ sơ & tài khoản");

            var pnl = new Panel { Dock = DockStyle.Top, Height = 50 };
            txtTim.SetBounds(12, 10, 220, 28);
            var btnTim = new Button { Text = "Tìm", Location = new Point(240, 8), Size = new Size(70, 32) };
            var btnThem = new Button { Text = "Thêm cán bộ + tài khoản", Location = new Point(325, 8), Size = new Size(210, 32) };
            var btnReset = new Button { Text = "Đặt lại mật khẩu", Location = new Point(545, 8), Size = new Size(150, 32) };
            var btnKhoa = new Button { Text = "Khóa / Mở khóa", Location = new Point(705, 8), Size = new Size(140, 32) };
            var btnXem = new Button { Text = "Xem hồ sơ", Location = new Point(855, 8), Size = new Size(110, 32) };
            pnl.Controls.AddRange(new Control[] { txtTim, btnTim, btnThem, btnReset, btnKhoa, btnXem });

            page.Controls.Add(gridHS);   // Fill trước
            page.Controls.Add(pnl);      // Top sau

            btnTim.Click += (s, e) => TaiHoSo();
            txtTim.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { TaiHoSo(); e.SuppressKeyPress = true; } };
            btnThem.Click += (s, e) => ThemCanBo();
            btnReset.Click += (s, e) => DatLaiMatKhau();
            btnKhoa.Click += (s, e) => KhoaMoKhoa();
            btnXem.Click += (s, e) => XemHoSo(gridHS);
            gridHS.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) XemHoSo(gridHS); };
            return page;
        }

        private void TaiHoSo()
        {
            try
            {
                var dt = Ui.Query(@"SELECT h.MaCanBo AS [Mã cán bộ], h.HoTen AS [Họ tên], h.CoQuan AS [Cơ quan],
                                           h.ChucVu AS [Chức vụ], t.TenDangNhap AS [Tài khoản], t.VaiTro AS [Vai trò],
                                           t.TrangThai AS [Hoạt động], h.TrangThai AS [Trạng thái hồ sơ]
                                    FROM HoSo h LEFT JOIN TaiKhoan t ON t.MaCanBo = h.MaCanBo
                                    WHERE h.MaCanBo LIKE @k OR h.HoTen LIKE @k OR h.CoQuan LIKE @k OR t.TenDangNhap LIKE @k
                                    ORDER BY h.MaCanBo",
                    ("@k", "%" + txtTim.Text.Trim() + "%"));
                Ui.MapStatus(dt, "Trạng thái hồ sơ");
                gridHS.DataSource = dt;
            }
            catch (Exception ex) { Ui.Error("Lỗi tải danh sách hồ sơ:\n" + ex.Message); }
        }

        private void XemHoSo(DataGridView g)
        {
            string? ma = Ui.Selected(g, "Mã cán bộ");
            if (ma == null) { Ui.Info("Vui lòng chọn một hồ sơ."); return; }
            using var f = new ProfileViewForm(ma);
            f.ShowDialog(this);
        }

        private void ThemCanBo()
        {
            using var dlg = new AddStaffDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using var conn = Db.GetConnection();
                conn.Open();
                using var tx = conn.BeginTransaction();
                try
                {
                    using (var c1 = new SqlCommand(
                        "INSERT INTO HoSo (MaCanBo, HoTen, CoQuan, ChucVu) VALUES (@m, @t, @c, @v)", conn, tx))
                    {
                        c1.Parameters.AddWithValue("@m", dlg.TxtMa.Text.Trim());
                        c1.Parameters.AddWithValue("@t", dlg.TxtTen.Text.Trim());
                        c1.Parameters.AddWithValue("@c", string.IsNullOrWhiteSpace(dlg.TxtCoQuan.Text) ? DBNull.Value : dlg.TxtCoQuan.Text.Trim());
                        c1.Parameters.AddWithValue("@v", string.IsNullOrWhiteSpace(dlg.TxtChucVu.Text) ? DBNull.Value : dlg.TxtChucVu.Text.Trim());
                        c1.ExecuteNonQuery();
                    }
                    using (var c2 = new SqlCommand(
                        "INSERT INTO TaiKhoan (TenDangNhap, MatKhau, VaiTro, MaCanBo) VALUES (@u, @p, @r, @m)", conn, tx))
                    {
                        c2.Parameters.AddWithValue("@u", dlg.TxtUser.Text.Trim());
                        c2.Parameters.AddWithValue("@p", Db.Hash(MatKhauMacDinh));
                        c2.Parameters.AddWithValue("@r", dlg.CboRole.SelectedItem!.ToString()!);
                        c2.Parameters.AddWithValue("@m", dlg.TxtMa.Text.Trim());
                        c2.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }

                Ui.Info($"Đã tạo hồ sơ và tài khoản.\nMật khẩu mặc định: {MatKhauMacDinh}");
                TaiHoSo();
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                Ui.Warn("Mã cán bộ hoặc tên đăng nhập đã tồn tại.");
            }
            catch (Exception ex) { Ui.Error("Lỗi khi thêm cán bộ:\n" + ex.Message); }
        }

        private string? ChonTaiKhoan()
        {
            string? user = Ui.Selected(gridHS, "Tài khoản");
            if (user == null) Ui.Info("Dòng đang chọn chưa có tài khoản.");
            return user;
        }

        private void DatLaiMatKhau()
        {
            string? user = ChonTaiKhoan();
            if (user == null) return;
            if (!Ui.Confirm($"Đặt lại mật khẩu của \"{user}\" về \"{MatKhauMacDinh}\"?")) return;
            try
            {
                Ui.Exec("UPDATE TaiKhoan SET MatKhau = @p WHERE TenDangNhap = @u",
                        ("@p", Db.Hash(MatKhauMacDinh)), ("@u", user));
                Ui.Info("Đã đặt lại mật khẩu.");
            }
            catch (Exception ex) { Ui.Error("Lỗi:\n" + ex.Message); }
        }

        private void KhoaMoKhoa()
        {
            string? user = ChonTaiKhoan();
            if (user == null) return;
            if (user == Session.Username) { Ui.Warn("Không thể tự khóa tài khoản đang đăng nhập."); return; }
            try
            {
                Ui.Exec("UPDATE TaiKhoan SET TrangThai = CASE WHEN TrangThai = 1 THEN 0 ELSE 1 END WHERE TenDangNhap = @u",
                        ("@u", user));
                TaiHoSo();
            }
            catch (Exception ex) { Ui.Error("Lỗi:\n" + ex.Message); }
        }

        // =====================================================
        //  TAB 3 - LƯU TRỮ SỐ
        // =====================================================
        private TabPage BuildTabLuuTru()
        {
            var page = new TabPage("3. Lưu trữ số");

            cboLuuTru.Items.AddRange(new object[] { "Chờ lưu trữ", "Đã lưu trữ" });
            cboLuuTru.SelectedIndex = 0;

            var pnl = new Panel { Dock = DockStyle.Top, Height = 50 };
            var lbl = new Label { Text = "Hiển thị:", AutoSize = true, Location = new Point(12, 15) };
            cboLuuTru.Location = new Point(85, 10);
            var btnLuu = new Button { Text = "Lưu trữ hồ sơ đã chọn", Location = new Point(305, 8), Size = new Size(200, 32) };
            var btnKiemTra = new Button { Text = "Kiểm tra toàn vẹn", Location = new Point(515, 8), Size = new Size(170, 32) };
            var btnXem = new Button { Text = "Xem hồ sơ", Location = new Point(695, 8), Size = new Size(110, 32) };
            pnl.Controls.AddRange(new Control[] { lbl, cboLuuTru, btnLuu, btnKiemTra, btnXem });

            page.Controls.Add(gridLT);
            page.Controls.Add(pnl);

            cboLuuTru.SelectedIndexChanged += (s, e) => TaiLuuTru();
            btnLuu.Click += (s, e) => LuuTruHoSo();
            btnKiemTra.Click += (s, e) => KiemTraToanVen();
            btnXem.Click += (s, e) => XemHoSo(gridLT);
            return page;
        }

        private void TaiLuuTru()
        {
            try
            {
                var dt = Ui.Query(@"SELECT MaCanBo AS [Mã cán bộ], HoTen AS [Họ tên], CoQuan AS [Cơ quan],
                                           NguoiPheDuyet AS [Người phê duyệt], NgayPheDuyet AS [Ngày phê duyệt],
                                           NgayLuuTru AS [Ngày lưu trữ], DuongDanLuuTru AS [File lưu trữ]
                                    FROM HoSo
                                    WHERE TrangThai = 'DaPheDuyet' AND LuuTru = @lt
                                    ORDER BY NgayPheDuyet",
                    ("@lt", cboLuuTru.SelectedIndex == 0 ? 0 : 1));
                gridLT.DataSource = dt;
                Ui.FormatCot(gridLT, "Ngày phê duyệt", "dd/MM/yyyy HH:mm");
                Ui.FormatCot(gridLT, "Ngày lưu trữ", "dd/MM/yyyy HH:mm");
            }
            catch (Exception ex) { Ui.Error("Lỗi tải danh sách lưu trữ:\n" + ex.Message); }
        }

        private void LuuTruHoSo()
        {
            if (cboLuuTru.SelectedIndex != 0) { Ui.Info("Hãy chuyển sang mục \"Chờ lưu trữ\"."); return; }
            string? ma = Ui.Selected(gridLT, "Mã cán bộ");
            if (ma == null) { Ui.Info("Vui lòng chọn một hồ sơ."); return; }

            try
            {
                string? rel = Convert.ToString(Ui.Scalar(
                    "SELECT FilePDF FROM HoSo WHERE MaCanBo = @m AND TrangThai = 'DaPheDuyet' AND LuuTru = 0",
                    ("@m", ma)));

                string? duongDanLuu = null;
                string? maBam = null;

                if (!string.IsNullOrWhiteSpace(rel))
                {
                    string nguon = Ui.DuongDanDayDu(rel);
                    if (!File.Exists(nguon)) { Ui.Warn("Không tìm thấy file PDF gốc:\n" + nguon); return; }

                    string thuMuc = Path.Combine("KhoLuuTru", DateTime.Now.Year.ToString());
                    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, thuMuc));

                    duongDanLuu = Path.Combine(thuMuc, $"{ma}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                    string dich = Path.Combine(AppContext.BaseDirectory, duongDanLuu);
                    File.Copy(nguon, dich, false);
                    maBam = Ui.HashFile(dich);
                    File.SetAttributes(dich, FileAttributes.ReadOnly);   // chống sửa/xóa nhầm
                }
                else if (!Ui.Confirm("Hồ sơ này chưa có file PDF. Vẫn lưu trữ phần dữ liệu?"))
                {
                    return;
                }

                int n = Ui.Exec(@"UPDATE HoSo SET LuuTru = 1, NgayLuuTru = GETDATE(),
                                         DuongDanLuuTru = @d, MaBamLuuTru = @h
                                  WHERE MaCanBo = @m AND TrangThai = 'DaPheDuyet' AND LuuTru = 0",
                                ("@d", duongDanLuu), ("@h", maBam), ("@m", ma));

                if (n == 0) Ui.Warn("Hồ sơ không còn ở trạng thái chờ lưu trữ.");
                else Ui.Info("Đã lưu trữ hồ sơ " + ma + ".");
                TaiLuuTru();
            }
            catch (Exception ex) { Ui.Error("Lỗi khi lưu trữ:\n" + ex.Message); }
        }

        private void KiemTraToanVen()
        {
            string? ma = Ui.Selected(gridLT, "Mã cán bộ");
            if (ma == null) { Ui.Info("Vui lòng chọn một hồ sơ."); return; }

            try
            {
                var dt = Ui.Query("SELECT DuongDanLuuTru, MaBamLuuTru FROM HoSo WHERE MaCanBo = @m AND LuuTru = 1",
                                  ("@m", ma));
                if (dt.Rows.Count == 0) { Ui.Info("Hồ sơ này chưa được lưu trữ."); return; }

                string rel = dt.Rows[0]["DuongDanLuuTru"] == DBNull.Value ? "" : Convert.ToString(dt.Rows[0]["DuongDanLuuTru"]) ?? "";
                string hashGoc = dt.Rows[0]["MaBamLuuTru"] == DBNull.Value ? "" : Convert.ToString(dt.Rows[0]["MaBamLuuTru"]) ?? "";
                if (rel == "") { Ui.Info("Hồ sơ này được lưu trữ không kèm file PDF."); return; }

                string full = Ui.DuongDanDayDu(rel);
                if (!File.Exists(full)) { Ui.Warn("File lưu trữ đã bị xóa hoặc di chuyển:\n" + full); return; }

                if (Ui.HashFile(full) == hashGoc)
                    Ui.Info("File lưu trữ còn nguyên vẹn (mã băm SHA-256 khớp).");
                else
                    Ui.Warn("CẢNH BÁO: file lưu trữ đã bị thay đổi so với lúc lưu trữ!");
            }
            catch (Exception ex) { Ui.Error("Lỗi kiểm tra:\n" + ex.Message); }
        }
    }

    // =====================================================
    //  HỘP THOẠI THÊM CÁN BỘ + TÀI KHOẢN
    // =====================================================
    public class AddStaffDialog : Form
    {
        public readonly TextBox TxtMa = new() { MaxLength = 20 };
        public readonly TextBox TxtTen = new() { MaxLength = 100 };
        public readonly TextBox TxtCoQuan = new() { MaxLength = 150 };
        public readonly TextBox TxtChucVu = new() { MaxLength = 100 };
        public readonly TextBox TxtUser = new() { MaxLength = 50 };
        public readonly ComboBox CboRole = new() { DropDownStyle = ComboBoxStyle.DropDownList };

        public AddStaffDialog()
        {
            Text = "Thêm cán bộ và cấp tài khoản";
            ClientSize = new Size(480, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 10);

            CboRole.Items.AddRange(new object[] { "Staff", "Head", "HR", "Board" });
            CboRole.SelectedIndex = 0;

            var tbl = Ui.FormTable();
            Ui.AddRow(tbl, "Mã cán bộ (*)", TxtMa);
            Ui.AddRow(tbl, "Họ và tên (*)", TxtTen);
            Ui.AddRow(tbl, "Cơ quan / đơn vị", TxtCoQuan);
            Ui.AddRow(tbl, "Chức vụ", TxtChucVu);
            Ui.AddRow(tbl, "Tên đăng nhập (*)", TxtUser);
            Ui.AddRow(tbl, "Vai trò", CboRole);

            var pnl = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10, 8, 10, 0)
            };
            var btnHuy = new Button { Text = "Hủy", Width = 90, Height = 34, DialogResult = DialogResult.Cancel };
            var btnOk = new Button { Text = "Tạo", Width = 90, Height = 34 };
            pnl.Controls.Add(btnHuy);
            pnl.Controls.Add(btnOk);

            Controls.Add(tbl);
            Controls.Add(pnl);
            CancelButton = btnHuy;

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(TxtMa.Text) ||
                    string.IsNullOrWhiteSpace(TxtTen.Text) ||
                    string.IsNullOrWhiteSpace(TxtUser.Text))
                {
                    Ui.Warn("Vui lòng nhập đủ các mục có dấu (*).");
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
