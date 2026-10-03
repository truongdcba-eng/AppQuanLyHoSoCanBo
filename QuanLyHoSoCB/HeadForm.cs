namespace QuanLyHoSoCB
{
    /// <summary>
    /// Form của Head of Department / Unit (First-level Reviewer):
    ///  - Xem hồ sơ cán bộ THUỘC ĐƠN VỊ MÌNH (cùng "Cơ quan")
    ///  - Nhập đánh giá chính thức (xếp loại + nhận xét)
    ///  - Duyệt cấp 1 -> chuyển Ban lãnh đạo, hoặc trả lại nhân viên kèm lý do
    /// </summary>
    public class HeadForm : Form
    {
        private readonly DataGridView grid = Ui.CreateGrid();
        private readonly ComboBox cboLoc = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
        private readonly ComboBox cboXepLoai = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly TextBox txtNhanXet = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
        private string coQuan = "";

        public HeadForm()
        {
            Text = "Trưởng đơn vị - Xét duyệt hồ sơ";
            ClientSize = new Size(1050, 660);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            cboLoc.Items.AddRange(new object[] { "Hồ sơ chờ tôi duyệt", "Tất cả hồ sơ trong đơn vị" });
            cboLoc.SelectedIndex = 0;
            cboXepLoai.Items.AddRange(new object[] { "Xuất sắc", "Tốt", "Hoàn thành", "Không hoàn thành" });

            // ----- Thanh lọc -----
            var pnlLoc = new Panel { Dock = DockStyle.Top, Height = 44 };
            var lblLoc = new Label { Text = "Hiển thị:", AutoSize = true, Location = new Point(12, 13) };
            cboLoc.Location = new Point(85, 8);
            var btnTaiLai = new Button { Text = "Tải lại", Location = new Point(340, 7), Size = new Size(90, 30) };
            pnlLoc.Controls.AddRange(new Control[] { lblLoc, cboLoc, btnTaiLai });

            // ----- Khu nhập đánh giá -----
            var grpEval = new GroupBox
            {
                Text = "Đánh giá chính thức của Trưởng đơn vị",
                Dock = DockStyle.Bottom,
                Height = 235
            };
            var lb1 = new Label { Text = "Xếp loại:", AutoSize = true, Location = new Point(15, 33) };
            cboXepLoai.Location = new Point(100, 29);
            var lb2 = new Label { Text = "Nhận xét:", AutoSize = true, Location = new Point(15, 72) };
            txtNhanXet.SetBounds(100, 68, 900, 95);
            txtNhanXet.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var btnXem = new Button { Text = "Xem chi tiết hồ sơ", Location = new Point(100, 182), Size = new Size(170, 36) };
            var btnDuyet = new Button { Text = "Duyệt & chuyển Ban lãnh đạo", Location = new Point(285, 182), Size = new Size(230, 36) };
            var btnTraLai = new Button { Text = "Trả lại nhân viên", Location = new Point(530, 182), Size = new Size(160, 36) };
            grpEval.Controls.AddRange(new Control[] { lb1, cboXepLoai, lb2, txtNhanXet, btnXem, btnDuyet, btnTraLai });

            // Thứ tự thêm: control Fill trước, các control Dock Top/Bottom sau
            Controls.Add(grid);
            Controls.Add(pnlLoc);
            Controls.Add(grpEval);
            Controls.Add(Ui.Header(this, "Trưởng đơn vị: " + Ui.TenNguoiDung()));

            // ----- Sự kiện -----
            cboLoc.SelectedIndexChanged += (s, e) => TaiDanhSach();
            btnTaiLai.Click += (s, e) => TaiDanhSach();
            btnXem.Click += (s, e) => Xem();
            btnDuyet.Click += (s, e) => Duyet();
            btnTraLai.Click += (s, e) => TraLai();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Xem(); };

            Load += (s, e) => { KhoiTao(); TaiDanhSach(); };
        }

        // Lấy tên đơn vị của chính Trưởng đơn vị
        private void KhoiTao()
        {
            try
            {
                coQuan = Convert.ToString(Ui.Scalar("SELECT CoQuan FROM HoSo WHERE MaCanBo = @m",
                                                    ("@m", Session.MaCanBo))) ?? "";
                if (coQuan == "")
                    Ui.Warn("Hồ sơ của bạn chưa có thông tin 'Cơ quan/đơn vị' nên không xác định được cán bộ thuộc đơn vị mình.");
            }
            catch (Exception ex) { Ui.Error("Lỗi tải thông tin đơn vị:\n" + ex.Message); }
        }

        private void TaiDanhSach()
        {
            try
            {
                string sql = @"SELECT MaCanBo AS [Mã cán bộ], HoTen AS [Họ tên], ChucVu AS [Chức vụ],
                                      TrangThai AS [Trạng thái], NgayCapNhat AS [Ngày cập nhật]
                               FROM HoSo
                               WHERE CoQuan = @cq AND MaCanBo <> @me";
                if (cboLoc.SelectedIndex == 0) sql += " AND TrangThai = 'ChoDuyet'";
                sql += " ORDER BY NgayCapNhat DESC";

                var dt = Ui.Query(sql, ("@cq", coQuan), ("@me", Session.MaCanBo));
                Ui.MapStatus(dt, "Trạng thái");
                grid.DataSource = dt;
                Ui.FormatCot(grid, "Ngày cập nhật", "dd/MM/yyyy HH:mm");
            }
            catch (Exception ex) { Ui.Error("Lỗi tải danh sách:\n" + ex.Message); }
        }

        private string? ChonMa()
        {
            string? ma = Ui.Selected(grid, "Mã cán bộ");
            if (ma == null) Ui.Info("Vui lòng chọn một hồ sơ trong danh sách.");
            return ma;
        }

        private void Xem()
        {
            string? ma = ChonMa();
            if (ma == null) return;
            using var f = new ProfileViewForm(ma);
            f.ShowDialog(this);
        }

        private void Duyet()
        {
            string? ma = ChonMa();
            if (ma == null) return;

            if (cboXepLoai.SelectedIndex < 0) { Ui.Warn("Vui lòng chọn xếp loại."); return; }
            if (string.IsNullOrWhiteSpace(txtNhanXet.Text)) { Ui.Warn("Vui lòng nhập nhận xét."); return; }
            if (!Ui.Confirm($"Duyệt hồ sơ {ma} và chuyển lên Ban lãnh đạo?")) return;

            try
            {
                int n = Ui.Exec(@"UPDATE HoSo SET
                                      TrangThai = 'DaDuyetCap1',
                                      XepLoai = @xl,
                                      DanhGiaTruongDonVi = @dg,
                                      NguoiDuyetCap1 = @u,
                                      NgayDuyetCap1 = GETDATE(),
                                      LyDoTraLai = NULL
                                  WHERE MaCanBo = @ma AND CoQuan = @cq AND TrangThai = 'ChoDuyet'",
                    ("@xl", cboXepLoai.SelectedItem!.ToString()),
                    ("@dg", txtNhanXet.Text.Trim()),
                    ("@u", Session.Username),
                    ("@ma", ma),
                    ("@cq", coQuan));

                if (n == 0)
                {
                    Ui.Warn("Hồ sơ không còn ở trạng thái chờ duyệt (có thể nhân viên vừa sửa lại hoặc đã được xử lý).");
                }
                else
                {
                    Ui.Exec("INSERT INTO ThongBao (MaCanBo, NoiDung) VALUES (@ma, @nd)",
                        ("@ma", ma),
                        ("@nd", "Hồ sơ của bạn đã được Trưởng đơn vị duyệt, đang chờ Ban lãnh đạo phê duyệt."));
                    Ui.Info("Đã duyệt và chuyển hồ sơ lên Ban lãnh đạo.");
                    cboXepLoai.SelectedIndex = -1;
                    txtNhanXet.Clear();
                }
                TaiDanhSach();
            }
            catch (Exception ex) { Ui.Error("Lỗi khi duyệt hồ sơ:\n" + ex.Message); }
        }

        private void TraLai()
        {
            string? ma = ChonMa();
            if (ma == null) return;

            string? lyDo = Ui.Prompt("Trả lại hồ sơ", "Nhập lý do / nội dung cần chỉnh sửa:", multiline: true);
            if (lyDo == null) return;
            if (string.IsNullOrWhiteSpace(lyDo)) { Ui.Warn("Vui lòng nhập lý do trả lại."); return; }

            try
            {
                int n = Ui.Exec(@"UPDATE HoSo SET TrangThai = 'TuChoi', LyDoTraLai = @ld
                                  WHERE MaCanBo = @ma AND CoQuan = @cq AND TrangThai = 'ChoDuyet'",
                    ("@ld", lyDo.Trim()), ("@ma", ma), ("@cq", coQuan));

                if (n == 0)
                {
                    Ui.Warn("Hồ sơ không còn ở trạng thái chờ duyệt.");
                }
                else
                {
                    Ui.Exec("INSERT INTO ThongBao (MaCanBo, NoiDung) VALUES (@ma, @nd)",
                        ("@ma", ma),
                        ("@nd", "Hồ sơ của bạn bị Trưởng đơn vị trả lại. Lý do: " + lyDo.Trim()));
                    Ui.Info("Đã trả lại hồ sơ cho nhân viên.");
                }
                TaiDanhSach();
            }
            catch (Exception ex) { Ui.Error("Lỗi khi trả lại hồ sơ:\n" + ex.Message); }
        }
    }
}
