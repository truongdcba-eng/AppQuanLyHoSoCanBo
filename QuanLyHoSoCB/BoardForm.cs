namespace QuanLyHoSoCB;

/// <summary>
/// Form của Board of Management / Principal (Final Approver):
///  Tab 1: Ký duyệt (hoặc từ chối) kế hoạch cập nhật hồ sơ hằng năm do HR lập
///  Tab 2: Ký duyệt (hoặc trả lại) hồ sơ cán bộ đã được Trưởng đơn vị duyệt
/// Chữ ký số được mô phỏng: yêu cầu nhập lại mật khẩu + lưu mã băm SHA-256 của nội dung, người ký, thời điểm ký.
/// </summary>
public class BoardForm : Form
{
    // Tab 1
    private readonly DataGridView gridKH = Ui.CreateGrid();
    private readonly TextBox txtHuongDan = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = SystemColors.Control };

    // Tab 2
    private readonly DataGridView gridHS = Ui.CreateGrid();
    private readonly TextBox txtNhanXet = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = SystemColors.Control };

    public BoardForm()
    {
        Text = "Ban lãnh đạo - Phê duyệt";
        ClientSize = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildTabKeHoach());
        tabs.TabPages.Add(BuildTabHoSo());

        Controls.Add(tabs);
        Controls.Add(Ui.Header(this, "Ban lãnh đạo: " + Ui.TenNguoiDung()));

        gridKH.SelectionChanged += (s, e) => txtHuongDan.Text = Ui.Selected(gridKH, "HuongDan") ?? "";
        gridHS.SelectionChanged += (s, e) => txtNhanXet.Text = Ui.Selected(gridHS, "DanhGiaTruongDonVi") ?? "";

        Load += (s, e) => { TaiKeHoach(); TaiHoSo(); };
    }

    // =====================================================
    //  TAB 1 - KẾ HOẠCH CẬP NHẬT HỒ SƠ
    // =====================================================
    private TabPage BuildTabKeHoach()
    {
        var page = new TabPage("1. Kế hoạch chờ ký duyệt");

        var grp = new GroupBox { Text = "Nội dung hướng dẫn của kế hoạch đang chọn", Dock = DockStyle.Bottom, Height = 190, Padding = new Padding(8) };
        grp.Controls.Add(txtHuongDan);

        var pnl = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8, 8, 0, 0) };
        var btnKy = new Button { Text = "Ký phê duyệt kế hoạch", Width = 210, Height = 34 };
        var btnTuChoi = new Button { Text = "Từ chối", Width = 110, Height = 34 };
        var btnLamMoi = new Button { Text = "Tải lại", Width = 90, Height = 34 };
        pnl.Controls.AddRange(new Control[] { btnKy, btnTuChoi, btnLamMoi });

        // Fill trước, Bottom sau (control thêm sau nằm ngoài cùng)
        page.Controls.Add(gridKH);
        page.Controls.Add(grp);
        page.Controls.Add(pnl);

        btnKy.Click += (s, e) => KyKeHoach();
        btnTuChoi.Click += (s, e) => TuChoiKeHoach();
        btnLamMoi.Click += (s, e) => TaiKeHoach();
        return page;
    }

    private void TaiKeHoach()
    {
        try
        {
            var dt = Ui.Query(@"SELECT Id AS [Mã KH], TieuDe AS [Tiêu đề], Nam AS [Năm], HanChot AS [Hạn chót],
                                           NguoiTao AS [Người lập], NgayTao AS [Ngày lập], HuongDan
                                    FROM KeHoach WHERE TrangThai = 'ChoPheDuyet' ORDER BY NgayTao");
            gridKH.DataSource = dt;
            Ui.AnCot(gridKH, "HuongDan");
            Ui.FormatCot(gridKH, "Hạn chót", "dd/MM/yyyy");
            Ui.FormatCot(gridKH, "Ngày lập", "dd/MM/yyyy HH:mm");
            txtHuongDan.Text = Ui.Selected(gridKH, "HuongDan") ?? "";
        }
        catch (Exception ex) { Ui.Error("Lỗi tải kế hoạch:\n" + ex.Message); }
    }

    private void KyKeHoach()
    {
        string? id = Ui.Selected(gridKH, "Mã KH");
        if (id == null) { Ui.Info("Vui lòng chọn một kế hoạch."); return; }
        if (!Ui.Confirm($"Ký phê duyệt kế hoạch số {id}?")) return;
        if (!Ui.XacThucMatKhau()) return;

        try
        {
            var dt = Ui.Query("SELECT * FROM KeHoach WHERE Id = @id AND TrangThai = 'ChoPheDuyet'",
                              ("@id", int.Parse(id)));
            if (dt.Rows.Count == 0) { Ui.Warn("Kế hoạch không còn ở trạng thái chờ ký."); TaiKeHoach(); return; }

            string noiDung = string.Join("|", dt.Rows[0].ItemArray.Select(x => Convert.ToString(x)));
            DateTime now = DateTime.Now;
            string chuKy = Ui.TaoChuKy(noiDung, now);

            int n = Ui.Exec(@"UPDATE KeHoach SET TrangThai = 'DaPheDuyet', NguoiKy = @u, NgayKy = @t, ChuKySo = @s
                                  WHERE Id = @id AND TrangThai = 'ChoPheDuyet'",
                ("@u", Session.Username), ("@t", now), ("@s", chuKy), ("@id", int.Parse(id)));

            if (n == 0) Ui.Warn("Kế hoạch không còn ở trạng thái chờ ký.");
            else Ui.Info("Đã ký phê duyệt kế hoạch.\nChữ ký số: " + chuKy.Substring(0, 16) + "...");
            TaiKeHoach();
        }
        catch (Exception ex) { Ui.Error("Lỗi khi ký kế hoạch:\n" + ex.Message); }
    }

    private void TuChoiKeHoach()
    {
        string? id = Ui.Selected(gridKH, "Mã KH");
        if (id == null) { Ui.Info("Vui lòng chọn một kế hoạch."); return; }

        string? lyDo = Ui.Prompt("Từ chối kế hoạch", "Nhập lý do từ chối:", multiline: true);
        if (lyDo == null) return;
        if (string.IsNullOrWhiteSpace(lyDo)) { Ui.Warn("Vui lòng nhập lý do."); return; }

        try
        {
            Ui.Exec(@"UPDATE KeHoach SET TrangThai = 'TuChoi', LyDoTuChoi = @ld
                          WHERE Id = @id AND TrangThai = 'ChoPheDuyet'",
                ("@ld", lyDo.Trim()), ("@id", int.Parse(id)));
            Ui.Info("Đã từ chối kế hoạch. HR sẽ thấy lý do trong danh sách kế hoạch.");
            TaiKeHoach();
        }
        catch (Exception ex) { Ui.Error("Lỗi:\n" + ex.Message); }
    }

    // =====================================================
    //  TAB 2 - HỒ SƠ CÁN BỘ
    // =====================================================
    private TabPage BuildTabHoSo()
    {
        var page = new TabPage("2. Hồ sơ chờ ký duyệt");

        var grp = new GroupBox { Text = "Nhận xét của Trưởng đơn vị", Dock = DockStyle.Bottom, Height = 150, Padding = new Padding(8) };
        grp.Controls.Add(txtNhanXet);

        var pnl = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8, 8, 0, 0) };
        var btnXem = new Button { Text = "Xem chi tiết & PDF", Width = 180, Height = 34 };
        var btnKy = new Button { Text = "Ký phê duyệt hồ sơ", Width = 190, Height = 34 };
        var btnTraLai = new Button { Text = "Trả lại", Width = 110, Height = 34 };
        var btnLamMoi = new Button { Text = "Tải lại", Width = 90, Height = 34 };
        pnl.Controls.AddRange(new Control[] { btnXem, btnKy, btnTraLai, btnLamMoi });

        page.Controls.Add(gridHS);
        page.Controls.Add(grp);
        page.Controls.Add(pnl);

        btnXem.Click += (s, e) => Xem();
        btnKy.Click += (s, e) => KyHoSo();
        btnTraLai.Click += (s, e) => TraLaiHoSo();
        btnLamMoi.Click += (s, e) => TaiHoSo();
        gridHS.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Xem(); };
        return page;
    }

    private void TaiHoSo()
    {
        try
        {
            var dt = Ui.Query(@"SELECT MaCanBo AS [Mã cán bộ], HoTen AS [Họ tên], CoQuan AS [Cơ quan],
                                           ChucVu AS [Chức vụ], XepLoai AS [Xếp loại],
                                           NguoiDuyetCap1 AS [Người duyệt cấp 1], NgayDuyetCap1 AS [Ngày duyệt],
                                           DanhGiaTruongDonVi
                                    FROM HoSo WHERE TrangThai = 'DaDuyetCap1' ORDER BY NgayDuyetCap1");
            gridHS.DataSource = dt;
            Ui.AnCot(gridHS, "DanhGiaTruongDonVi");
            Ui.FormatCot(gridHS, "Ngày duyệt", "dd/MM/yyyy HH:mm");
            txtNhanXet.Text = Ui.Selected(gridHS, "DanhGiaTruongDonVi") ?? "";
        }
        catch (Exception ex) { Ui.Error("Lỗi tải danh sách hồ sơ:\n" + ex.Message); }
    }

    private string? ChonMa()
    {
        string? ma = Ui.Selected(gridHS, "Mã cán bộ");
        if (ma == null) Ui.Info("Vui lòng chọn một hồ sơ.");
        return ma;
    }

    private void Xem()
    {
        string? ma = ChonMa();
        if (ma == null) return;
        using var f = new ProfileViewForm(ma);
        f.ShowDialog(this);
    }

    private void KyHoSo()
    {
        string? ma = ChonMa();
        if (ma == null) return;
        if (!Ui.Confirm($"Ký phê duyệt hồ sơ chính thức của cán bộ {ma}?")) return;
        if (!Ui.XacThucMatKhau()) return;

        try
        {
            var dt = Ui.Query("SELECT * FROM HoSo WHERE MaCanBo = @m AND TrangThai = 'DaDuyetCap1'", ("@m", ma));
            if (dt.Rows.Count == 0) { Ui.Warn("Hồ sơ không còn ở trạng thái chờ ký."); TaiHoSo(); return; }

            // Nội dung được ký = toàn bộ dữ liệu hồ sơ tại thời điểm ký
            string noiDung = string.Join("|", dt.Rows[0].ItemArray.Select(x => Convert.ToString(x)));
            DateTime now = DateTime.Now;
            string chuKy = Ui.TaoChuKy(noiDung, now);

            int n = Ui.Exec(@"UPDATE HoSo SET TrangThai = 'DaPheDuyet', NguoiPheDuyet = @u,
                                         NgayPheDuyet = @t, ChuKySo = @s, LuuTru = 0
                                  WHERE MaCanBo = @m AND TrangThai = 'DaDuyetCap1'",
                ("@u", Session.Username), ("@t", now), ("@s", chuKy), ("@m", ma));

            if (n == 0)
            {
                Ui.Warn("Hồ sơ không còn ở trạng thái chờ ký.");
            }
            else
            {
                Ui.Exec("INSERT INTO ThongBao (MaCanBo, NoiDung) VALUES (@m, @nd)",
                    ("@m", ma),
                    ("@nd", "Hồ sơ của bạn đã được Ban lãnh đạo phê duyệt."));
                Ui.Info("Đã ký phê duyệt hồ sơ.\nChữ ký số: " + chuKy.Substring(0, 16) + "...");
            }
            TaiHoSo();
        }
        catch (Exception ex) { Ui.Error("Lỗi khi ký hồ sơ:\n" + ex.Message); }
    }

    private void TraLaiHoSo()
    {
        string? ma = ChonMa();
        if (ma == null) return;

        string? lyDo = Ui.Prompt("Trả lại hồ sơ", "Nhập lý do trả lại:", multiline: true);
        if (lyDo == null) return;
        if (string.IsNullOrWhiteSpace(lyDo)) { Ui.Warn("Vui lòng nhập lý do."); return; }

        try
        {
            int n = Ui.Exec(@"UPDATE HoSo SET TrangThai = 'TuChoi', LyDoTraLai = @ld
                                  WHERE MaCanBo = @m AND TrangThai = 'DaDuyetCap1'",
                ("@ld", lyDo.Trim()), ("@m", ma));

            if (n == 0)
            {
                Ui.Warn("Hồ sơ không còn ở trạng thái chờ ký.");
            }
            else
            {
                Ui.Exec("INSERT INTO ThongBao (MaCanBo, NoiDung) VALUES (@m, @nd)",
                    ("@m", ma),
                    ("@nd", "Hồ sơ của bạn bị Ban lãnh đạo trả lại. Lý do: " + lyDo.Trim()));
                Ui.Info("Đã trả lại hồ sơ.");
            }
            TaiHoSo();
        }
        catch (Exception ex) { Ui.Error("Lỗi:\n" + ex.Message); }
    }
}
