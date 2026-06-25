<div align="center">
  <img src="https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL"/>
  <img src="https://img.shields.io/badge/Entity_Framework-0078D4?style=for-the-badge&logo=.net&logoColor=white" alt="EF Core"/>
  <img src="https://img.shields.io/badge/Gemini_AI-8E75B2?style=for-the-badge&logo=googlebard&logoColor=white" alt="Gemini AI"/>
  
  <h1>MomOi - Hệ sinh thái chăm sóc sức khỏe Mẹ & Bé</h1>
  <p>Dự án Capstone: Nền tảng Y tế Ứng dụng Trí tuệ Nhân tạo (AI)</p>
</div>

## 📖 Bảng Mục lục
- [Giới thiệu dự án](#-giới-thiệu-dự-án)
- [Tính năng nổi bật (Theo Role)](#-tính-năng-nổi-bật-theo-role)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Hướng dẫn cài đặt (Getting Started)](#-hướng-dẫn-cài-đặt-getting-started)
- [Biến môi trường (Environment Variables)](#-biến-môi-trường-environment-variables)
- [Cấu trúc thư mục](#-cấu-trúc-thư-mục)

---

## 🌟 Giới thiệu dự án
**MomOi** là một hệ sinh thái chăm sóc sức khỏe toàn diện dành cho phụ nữ mang thai, sau sinh và trẻ sơ sinh. Với sự hỗ trợ đắc lực từ **Trí tuệ nhân tạo (Google Gemini)**, hệ thống không chỉ cung cấp kiến thức chuẩn y khoa mà còn tự động phân tích tâm lý, cảnh báo nguy cơ trầm cảm (EPDS) và gợi ý thực đơn cá nhân hóa.

Hệ thống được thiết kế linh hoạt với kiến trúc **Role-Based Access Control (RBAC)** phục vụ 4 nhóm đối tượng chính: *Admin, Staff, Expert (Chuyên gia/Bác sĩ), và Mom (Mẹ)*.

---

## ✨ Tính năng nổi bật (Theo Role)

### 👩‍👧 Dành cho Mẹ (Mom)
- **Hồ sơ sức khỏe:** Lưu trữ chỉ số thai kỳ, cân nặng, nhật ký ăn dặm.
- **Thực đơn AI (Diet Plan):** Tự động sinh thực đơn dinh dưỡng 7 ngày dựa vào độ tuổi, cân nặng và **Khai báo dị ứng thực phẩm** của trẻ thông qua AI.
- **Trợ lý Tâm lý:** Đánh giá trầm cảm sau sinh (EPDS Score) và phân tích cảm xúc qua giọng nói (Voice Journal).
- **Tư vấn trực tiếp:** Khởi tạo chat 1-1 với Bác sĩ/Chuyên gia dinh dưỡng.
- **Premium (SuperMomVip):** Nâng cấp tài khoản qua Cổng thanh toán (MoMo/VNPAY).

### 👨‍⚕️ Dành cho Chuyên gia (Expert)
- **Duyệt công thức AI:** Kiểm tra và phê duyệt/từ chối các công thức ăn dặm do AI sinh ra để đảm bảo chuẩn y khoa.
- **Tư vấn trực tuyến:** Nhắn tin tư vấn sức khỏe trực tiếp với các Mẹ.

### 🛡️ Dành cho Quản trị viên (Admin & Staff)
- **Quản lý tài khoản:** Thêm/sửa/khóa tài khoản người dùng, phân quyền hệ thống.
- **Business Rule Engine:** Hệ thống quản lý các luật sức khỏe động (Ví dụ: Tự định nghĩa ngưỡng cảnh báo BMI, Huyết áp) mà không cần can thiệp vào code.
- **Tích hợp USDA:** Đồng bộ dữ liệu dinh dưỡng (Calories, Protein, Carbs, Fat) từ Bộ Nông nghiệp Hoa Kỳ.
- **Dashboard & Báo cáo:** Theo dõi biểu đồ sức khỏe và danh sách bệnh nhân có rủi ro cao.

---

## 💻 Công nghệ sử dụng
- **Backend Framework:** ASP.NET Core 8.0 (Web API)
- **Cơ sở dữ liệu:** PostgreSQL
- **ORM:** Entity Framework Core (Code-First Migration)
- **Xác thực (Authentication):** JWT (JSON Web Tokens) & ASP.NET Core Identity
- **Trí tuệ nhân tạo:** Google Gemini 1.5 Pro REST API
- **Real-time Communication:** SignalR (Chat)
- **Third-party API:** USDA FoodData Central

---

## 🚀 Hướng dẫn cài đặt (Getting Started)

### Yêu cầu hệ thống
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (Hoặc sử dụng Docker)
- Visual Studio 2022 / JetBrains Rider / VS Code

### Các bước khởi chạy
1. **Clone repository:**
   ```bash
   git clone https://github.com/your-username/MomBaby-Healthcare-AI-BE.git
   cd MomBaby-Healthcare-AI-BE/MomOi.API
   ```

2. **Cấu hình chuỗi kết nối (Connection String):**
   Mở file `appsettings.Development.json` và thay đổi thông tin database PostgreSQL của bạn:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=momoidb;Username=postgres;Password=your_password;"
   }
   ```

3. **Chạy Migration:**
   ```bash
   dotnet ef database update
   ```

4. **Khởi động server:**
   ```bash
   dotnet run
   ```
   Hệ thống sẽ tự động Seed (khởi tạo) các tài khoản mặc định và mở Swagger tại: `https://localhost:port/swagger/index.html`

---

## 🔑 Biến môi trường (Environment Variables)
Để sử dụng toàn bộ tính năng (AI, Đồng bộ thực phẩm, Thanh toán), bạn cần bổ sung các API Key vào `appsettings.json`:

| Biến | Chức năng | Lấy ở đâu? |
|:---|:---|:---|
| `Gemini:ApiKey` | Sử dụng AI sinh thực đơn, phân tích trầm cảm | Google AI Studio |
| `UsdaApiKey` | Kéo dữ liệu dinh dưỡng thực phẩm | USDA FoodData Central |
| `Payment:MoMo` | Cổng thanh toán (Mock/Real) | MoMo Developer |
| `Payment:VnPay` | Cổng thanh toán (Mock/Real) | VNPay Sandbox |

---

<div align="center">
  <i>Được phát triển với ❤️ cho dự án Capstone Y tế thông minh.</i>
</div>