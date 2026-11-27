-- Idempotent seed for 30 admin feedback entries. Run against the MomOi PostgreSQL database.
-- The example.com accounts have no passwords and cannot sign in.
WITH feedback(n, full_name, category, page, body) AS (
    VALUES
      ( 1, 'Như',    'Idea',    '/feedback',   'Mong ứng dụng có thêm lịch nhắc uống nước trong ngày.'),
      ( 2, 'Thảo',   'Content', '/feedback',   'Mình muốn xem thêm thực đơn dễ nấu cho mẹ sau sinh.'),
      ( 3, 'Nhi',    'Idea',    '/feedback',   'Sẽ tiện hơn nếu có thể lưu các bài viết chăm sóc bé yêu thích.'),
      ( 4, 'Hương',  'Bug',     '/baby',       'Lịch tiêm chủng đôi khi tải hơi chậm trên điện thoại.'),
      ( 5, 'Linh',   'Content', '/nutrition',  'Các gợi ý bữa ăn theo độ tuổi của bé rất hữu ích.'),
      ( 6, 'Trang',  'Idea',    '/feedback',   'Mong có mục ghi chú câu hỏi để trao đổi với bác sĩ.'),
      ( 7, 'Vy',     'Idea',    '/pregnancy',  'Mình muốn có nhắc lịch khám thai theo từng tuần.'),
      ( 8, 'Mai',    'Content', '/baby',       'Bài viết về giấc ngủ của bé dễ hiểu và thực tế.'),
      ( 9, 'Hà',     'Bug',     '/profile',    'Ảnh đại diện đôi lúc chưa cập nhật ngay sau khi lưu.'),
      (10, 'Lan',    'Idea',    '/nutrition',  'Mong có bộ lọc món ăn theo nguyên liệu sẵn ở nhà.'),
      (11, 'Phương', 'Content', '/pregnancy',  'Phần theo dõi thai kỳ giúp mình nhớ các mốc quan trọng.'),
      (12, 'Yến',    'Idea',    '/baby',       'Mình muốn xuất lịch tiêm của bé ra tệp PDF.'),
      (13, 'Trâm',   'Bug',     '/feedback',   'Nút gửi phản hồi có lúc phản hồi chậm trên mạng di động.'),
      (14, 'Ngọc',   'Content', '/nutrition',  'Các công thức ăn dặm có hướng dẫn khá rõ ràng.'),
      (15, 'Quỳnh',  'Idea',    '/baby',       'Sẽ tốt hơn nếu có biểu đồ chiều cao theo từng tháng.'),
      (16, 'Diễm',   'Idea',    '/postpartum', 'Mong có thêm mẹo nghỉ ngơi cho mẹ sau sinh.'),
      (17, 'Hạnh',   'Content', '/feedback',   'Giao diện dễ dùng và các mục được sắp xếp hợp lý.'),
      (18, 'Thu',    'Bug',     '/nutrition',  'Danh sách món ăn thỉnh thoảng cuộn lại đầu trang.'),
      (19, 'Oanh',   'Idea',    '/baby',       'Mình muốn thêm ghi chú riêng cho từng lần khám của bé.'),
      (20, 'Tiên',   'Content', '/pregnancy',  'Các thông tin theo tuần thai rất dễ theo dõi.'),
      (21, 'My',     'Idea',    '/feedback',   'Mong ứng dụng có chế độ chữ lớn để đọc dễ hơn.'),
      (22, 'Hiền',   'Bug',     '/profile',    'Trang hồ sơ đôi khi mở chậm sau khi đăng nhập.'),
      (23, 'Tâm',    'Content', '/postpartum', 'Phần chăm sóc sau sinh có nhiều thông tin hữu ích.'),
      (24, 'Ánh',    'Idea',    '/nutrition',  'Mình muốn lưu lại thực đơn yêu thích để xem nhanh.'),
      (25, 'Bích',   'Content', '/baby',       'Theo dõi tăng trưởng của bé rất thuận tiện.'),
      (26, 'Uyên',   'Idea',    '/pregnancy',  'Mong có thông báo trước ngày khám thai một hôm.'),
      (27, 'Vân',    'Bug',     '/baby',       'Biểu đồ cân nặng hiển thị hơi nhỏ trên màn hình điện thoại.'),
      (28, 'Huyền',  'Content', '/nutrition',  'Mình thích cách chia gợi ý món ăn theo độ tuổi.'),
      (29, 'Chi',    'Idea',    '/feedback',   'Sẽ tiện nếu có thể theo dõi trạng thái góp ý đã gửi.'),
      (30, 'Loan',   'Content', '/feedback',   'Ứng dụng có nhiều nội dung thiết thực cho mẹ và bé.')
), samples AS (
    SELECT 'demo-feedback-' || lpad(n::text, 2, '0') AS id,
           full_name,
           'demo.feedback.' || lpad(n::text, 2, '0') || '@example.com' AS email,
           category, page, body, n * interval '1 hour' AS age
    FROM feedback
), inserted_users AS (
    INSERT INTO "AspNetUsers" (
        id, full_name, created_at, tier, user_name, normalized_user_name,
        email, normalized_email, email_confirmed, password_hash,
        security_stamp, concurrency_stamp, phone_number_confirmed,
        two_factor_enabled, lockout_enabled, access_failed_count
    )
    SELECT id, full_name, now() - age, 0, email, upper(email),
           email, upper(email), false, NULL,
           id, id, false, false, true, 0
    FROM samples
    ON CONFLICT (id) DO NOTHING
    RETURNING id
)
INSERT INTO notification_alerts (
    user_id, type, severity, message, status, channels,
    created_at, updated_at, is_deleted
)
SELECT s.id, 2, 3,
       '[FEEDBACK] Category=' || s.category || '; Page=' || s.page ||
       '; Message=' || s.body,
       0, ARRAY['AdminDashboard'], now() - s.age, now() - s.age, false
FROM samples s
WHERE (EXISTS (SELECT 1 FROM inserted_users u WHERE u.id = s.id)
       OR EXISTS (SELECT 1 FROM "AspNetUsers" u WHERE u.id = s.id))
  AND NOT EXISTS (
      SELECT 1 FROM notification_alerts a
      WHERE a.user_id = s.id AND a.message LIKE '[FEEDBACK] %'
  );
