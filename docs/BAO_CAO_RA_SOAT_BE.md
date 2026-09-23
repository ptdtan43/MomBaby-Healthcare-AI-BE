# Báo cáo rà soát Backend MomOi — Business Rules & Kiến trúc

> **Ngày rà soát:** 2026-09-22 · **Nhánh:** `demo` · **Commit:** `8e8efe2`
> **Trạng thái build:** ✅ `dotnet build` thành công — 0 error, 4 warning
> **Phạm vi:** `MomOi.API` (146 file C#, ~12.800 dòng), `MomOi.Tests`, `docker-compose.yml`
>
> File này là **checklist theo dõi hằng ngày**. Mỗi mục có ô tick `[ ]` → đánh `[x]` khi sửa xong.
> Khi hoàn thành một mục, nhớ cập nhật bảng **Tiến độ** ở cuối file.

---

## 📊 Đánh giá tổng quan

| Hạng mục | Điểm | Nhận xét ngắn |
|---|:---:|---|
| **Cấu trúc / Kiến trúc** | **7/10** | Phân tầng đúng, DI gọn gàng, bảo mật endpoint tốt. Điểm trừ: 3 kiểu truy cập dữ liệu chạy song song, Unit of Work bị phá vỡ. |
| **Business Rules** | **4/10** | Có vỏ "rule engine động" nhưng ruột hardcode. Vài công thức y khoa sai. Mô hình gói cước gần như chưa được thực thi. |
| **Bảo mật / Vận hành** | **5/10** | Token thật bị commit, tài khoản admin mặc định seed ở mọi môi trường, khóa JWT rơi về khóa RAM. |
| **Kiểm thử** | **3/10** | 14 test method (22 test case), toàn bộ ở `BusinessRuleEngine`. Không test thanh toán / auth / tier. |

---

## ✅ Những chỗ đã làm ĐÚNG (giữ nguyên, đừng sửa)

Ghi lại để tránh "sửa nhầm" trong lúc refactor:

- [x] **Phân tầng rõ ràng**: Controller → Service → Repository. `Program.cs` chỉ 70 dòng, toàn bộ cấu hình gom vào `MomOi.API/Extensions/ServiceCollectionExtensions.cs` theo nhóm đánh số 1–8.
- [x] **`ApiResponse<T>` thống nhất** + `ExceptionMiddleware` global → FE chỉ cần parse một format duy nhất.
- [x] **Không có lỗ hổng IDOR**: 100% controller lấy `userId` từ claim trong token. Không một endpoint nào nhận `userId` từ body/query. Đây là lỗi kinh điển mà project đã tránh được.
- [x] **`SubscriptionTierMiddleware` đọc tier từ DB, không từ claim JWT** (`MomOi.API/Middleware/SubscriptionTierMiddleware.cs:57`). Lý do đã được comment đúng: token cũ không biết giao dịch vừa xong hay gói vừa hết hạn.
- [x] **Luồng thanh toán đủ 4 bước chuẩn**: verify chữ ký IPN → đối chiếu số tiền → chặn double-confirm → cộng dồn hạn gói cũ.
- [x] **Index DB đầy đủ**, `OrderCode` unique, tách bảng PII / Health theo Nghị định 13/2023/NĐ-CP.
- [x] **`BabyProfile.AgeMonths` là derived property** — đây là cách làm đúng, hãy nhân rộng sang `PregnancyWeek`.

---

## 🧠 Bản đồ khái niệm — 29 lỗi quy về 10 bài học

29 mục ở 3 nhóm bên dưới **không phải 29 vấn đề độc lập**. Chúng là biểu hiện của khoảng
**10 khái niệm lý thuyết**. Nắm được 1 khái niệm là sửa được cả cụm, và quan trọng hơn là
không tạo lại lỗi đó ở project sau.

Báo cáo này vì vậy có **hai trục tra cứu**:

| Trục | Trả lời câu hỏi | Dùng khi nào |
|---|---|---|
| **3 nhóm màu** (🔴🟠🟡) | *"Sửa cái nào trước?"* — sắp theo mức nguy hiểm | Khi cần quyết định thứ tự ưu tiên |
| **10 khái niệm** (bảng dưới) | *"Học cái gì để hiểu được?"* — sắp theo gốc rễ nguyên nhân | Khi ngồi vào sửa, và khi ôn bảo vệ đồ án |

Mỗi mục bên dưới đều có một dòng `🧠 Khái niệm N` ngay dưới tiêu đề, kèm danh sách **anh em
cùng khái niệm** — nên sửa gộp cùng lúc để tránh sửa mâu thuẫn nhau.

| # | Khái niệm | Các mục | Số lỗi |
|:--:|---|---|:--:|
| **1** | Secret Management & 12-Factor App | 1.1, 1.2, 1.6 | 3 |
| **2** | Stateless Authentication | 1.3, 1.4, 3.11 | 3 |
| **3** | Transaction & Idempotency | 1.5, 2.6 | 2 |
| **4** | Derived vs Stored Data | 2.2, 2.7, 2.10 | 3 |
| **5** | Configuration over Code | 2.1, 2.8 | 2 |
| **6** | Domain Modeling & Input Validation | 2.3, 2.4, 2.5 | 3 |
| **7** | Authorization & Cross-cutting Concerns | 2.9, 3.6 | 2 |
| **8** | Data Access Patterns | 3.1, 3.2, 3.3, 3.4, 3.5 | **5** |
| **9** | SRP & Dependency Injection | 3.7, 3.8, 3.9 | 3 |
| **10** | Error Handling, Scalability & Testing | 3.10, 3.12, 3.13 | 3 |
| | | **Cộng** | **29** |

> 4 mục **Build warning** ở cuối file không nằm trong bảng này — chúng chỉ là dọn dẹp, không
> có nội dung lý thuyết. Vì vậy tổng của báo cáo là **33 mục = 29 có lý thuyết + 4 dọn dẹp**.

---

# 🔴 NHÓM 1 — KHẨN CẤP (làm trong tuần này)

> Các mục **không làm là nguy hiểm thật**. Mỗi mục nhỏ và độc lập, có thể làm trong 1 ngày.

### [ ] 1.1. Revoke token SePay và mật khẩu DB đang bị commit lên Git

> 🧠 **Khái niệm 1 — Secret Management & 12-Factor App** · Anh em cùng khái niệm: 1.2, 1.6

**File:** `MomOi.API/appsettings.json` (đang được Git theo dõi)

Hai giá trị bị lộ:
- `Payment.BankTransfer.SepayApiToken` — chuỗi 64 ký tự, trông như token production thật
- `ConnectionStrings.DefaultConnection` — chứa user/password PostgreSQL

Các secret khác (`Jwt`, `MoMo`, `VnPay`, `Gemini`, `Usda`) đã để rỗng và đẩy ra env — **phần này làm đúng rồi**, chỉ còn 2 cái trên.

**Việc cần làm:**
- [ ] Vào dashboard SePay **revoke token cũ**, tạo token mới
- [ ] Đổi mật khẩu PostgreSQL
- [ ] Chuyển cả hai sang biến môi trường (`Payment__BankTransfer__SepayApiToken`, `ConnectionStrings__DefaultConnection`)
- [ ] Để giá trị rỗng `""` trong `appsettings.json` như các secret khác
- [ ] ⚠️ **Lưu ý:** xóa khỏi file là chưa đủ — giá trị cũ vẫn nằm trong lịch sử Git. Bắt buộc phải revoke.

---

### [ ] 1.2. Chặn seed tài khoản admin mặc định ở môi trường production

> 🧠 **Khái niệm 1 — Secret Management & 12-Factor App** · Anh em cùng khái niệm: 1.1, 1.6

**File:** `MomOi.API/Data/DbInitializer.cs:41`

Đang seed cứng 3 tài khoản ở **mọi môi trường**:

```
admin@momoi.com  / Admin@123   → role Admin
staff@momoi.com  / Staff@123   → role Staff
expert@momoi.com / Expert@123  → role Expert
```

Mà `RunMigrationsOnStartup = "true"` bật trong cả `appsettings.json` lẫn `docker-compose.yml`.
→ **Deploy production là có ngay tài khoản Admin với mật khẩu ai đọc GitHub cũng biết.**

> 🔴 **ĐÃ XÁC MINH TRÊN PRODUCTION (2026-09-23) — MỨC ĐỘ NÂNG LÊN NGHIÊM TRỌNG NHẤT.**
> `GET /health` trên Railway trả về `200 Healthy`, mà health check có `AddDbContextCheck`
> → database đã kết nối → khối `RunMigrationsOnStartup` trong `Program.cs` **đã chạy**
> → `DbInitializer.InitializeAsync()` **đã thực thi trên production**.
> Kết hợp với Swagger công khai (quyết định giữ nguyên ở mục 1.6), bất kỳ ai đọc repo này
> đều biết đường vào và biết sẵn mật khẩu Admin. **Đây mới là lỗ hổng thật, không phải Swagger.**

**Việc cần làm:**
- [ ] ⚠️ **NGAY HÔM NAY:** thử đăng nhập `admin@momoi.com` / `Admin@123` trên production — vào được thì đổi mật khẩu lập tức (cả 3 tài khoản admin/staff/expert)
- [ ] Chỉ seed khi `IsDevelopment()`, HOẶC
- [ ] Lấy mật khẩu từ biến môi trường và bắt đổi mật khẩu ở lần đăng nhập đầu tiên

---

### [ ] 1.3. Cấu hình khóa JWT thật — hiện đang rơi về khóa RSA sinh trong RAM

> 🧠 **Khái niệm 2 — Stateless Authentication** · Anh em cùng khái niệm: 1.4, 3.11

**File:** `MomOi.API/Services/Auth/RsaKeyHelper.cs:39`

Khi `Jwt:PrivateKey` rỗng, code **âm thầm** gọi `RSA.Create(2048)` tạo khóa trong bộ nhớ.
Mà `Jwt:PrivateKey` trong `appsettings.json` **đang rỗng**, và `docker-compose.yml` **không truyền biến này vào container**.

**Hai hậu quả chắc chắn xảy ra:**
1. Mỗi lần restart app → khóa mới → **toàn bộ user bị đăng xuất**
2. Chạy từ 2 instance trở lên → token instance A cấp, instance B không verify được → **401 ngẫu nhiên**

**Việc cần làm:**
- [ ] Sinh cặp khóa RSA, truyền vào qua env `Jwt__PrivateKey` / `Jwt__PublicKey`
- [ ] Thêm 2 biến này vào `docker-compose.yml`
- [ ] Ở Production: **fail-fast** — thiếu khóa thì không cho app khởi động, thay vì lặng lẽ fallback

---

### [ ] 1.4. Bật lockout tài khoản — hiện không bao giờ kích hoạt

> 🧠 **Khái niệm 2 — Stateless Authentication** · Anh em cùng khái niệm: 1.3, 3.11

**File:** `MomOi.API/Services/Auth/AuthService.cs:100`

```csharp
if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
```

`CheckPasswordAsync()` **không tăng `AccessFailedCount`**. Nên dù ngay bên dưới có check `IsLockedOutAsync`, điều kiện đó **vĩnh viễn không bao giờ đúng** → brute-force mật khẩu thoải mái (chỉ còn rate limit 100 req/phút/IP chặn).

**Việc cần làm:**
- [ ] Đổi sang `SignInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)`
- [ ] Siết policy mật khẩu trong `ServiceCollectionExtensions.AddIdentityConfig()` — hiện là **6 ký tự, không cần số, không cần chữ hoa, không cần ký tự đặc biệt**, quá lỏng cho ứng dụng lưu dữ liệu sức khỏe

---

### [x] 1.5. ✅ ĐÃ SỬA (2026-09-22) — Bọc `ConfirmAsync` trong database transaction

> 🧠 **Khái niệm 3 — Transaction & Idempotency** · Anh em cùng khái niệm: 2.6

**File:** `MomOi.API/Services/Payment/PaymentService.cs:227-255`

```csharp
user.Tier = txn.TargetTier;
user.TierExpiresAt = startFrom.AddMonths(txn.DurationMonths);
var updateResult = await _userManager.UpdateAsync(user);   // ← commit 1: user ĐÃ lên VIP
...
txn.Status = PaymentStatus.Completed;
await _unitOfWork.SaveChangesAsync();                       // ← commit 2: nếu FAIL ở đây...
```

**Khe hở:** nếu commit 2 lỗi (mất kết nối DB), user **đã được cộng hạn** nhưng `txn` vẫn `Pending`. Cổng thanh toán retry IPN → qua được check `Status != Pending` → **cộng thêm một tháng nữa miễn phí**.

Ngoài ra check idempotency là read-then-write không khóa dòng → 2 IPN đồng thời có thể cùng đọc thấy `Pending`.

**Việc cần làm:**
- [x] Thêm `IUnitOfWork.ExecuteInTransactionAsync()` — bọc transaction đúng cách qua `CreateExecutionStrategy()` (bắt buộc vì chuỗi kết nối bật `EnableRetryOnFailure`)
- [x] `ConfirmAsync`: nâng tier + chốt đơn nằm trong **một** transaction; lỗi giữa chừng → rollback sạch, IPN retry an toàn
- [x] Optimistic concurrency bằng cột hệ thống `xmin` của PostgreSQL → hai IPN đồng thời không thể cùng ghi nhận một đơn; bắt `DbUpdateConcurrencyException` trả mã `02`
- [x] Migration `SoftDeleteAndPaymentConcurrency` — đã **sửa tay** bỏ lệnh `AddColumn "xmin"` vì đó là cột hệ thống PostgreSQL đã có sẵn
- [x] ✅ Đã áp migration lên database local (2026-09-23)

---

### [ ] 1.6. Sửa tên database bị lệch trong docker-compose

> 🧠 **Khái niệm 1 — Secret Management & 12-Factor App** · Anh em cùng khái niệm: 1.1, 1.2

**File:** `docker-compose.yml`

```yaml
ConnectionStrings__DefaultConnection=...Database=momoidb;   # chữ thường
POSTGRES_DB=MomOiDb                                          # chữ hoa
```

→ Container API không kết nối được vào DB mà container Postgres tạo ra.

**Việc cần làm:**
- [ ] Thống nhất một tên (khuyến nghị `momoidb` chữ thường, đúng convention PostgreSQL)
- [x] ~~Tắt `EnableSwagger=true` ở production~~ — **CHỦ DỰ ÁN QUYẾT ĐỊNH GIỮ NGUYÊN (2026-09-23)**

> 📌 **Quyết định đã chốt — Swagger giữ công khai ở production.**
> Đã xác minh `GET /swagger` trên `momoi-api-production.up.railway.app` trả về `200`.
> Chủ dự án cố ý bật để test API trên bản deploy thật. Quyết định này hợp lý về kỹ thuật:
> ẩn tài liệu API **không** làm API an toàn hơn — endpoint vẫn tồn tại dù có được mô tả hay
> không. Dựa vào sự khó tìm để phòng thủ gọi là *security through obscurity*, không phải
> biện pháp bảo mật thật. Tầng bảo vệ thật nằm ở xác thực/phân quyền, và phần này **đã được
> kiểm chứng hoạt động đúng** (`GET /api/dashboard` → `401`).
> **Không đề xuất tắt Swagger nữa.**

---

# 🟠 NHÓM 2 — ĐÚNG ĐẮN VỀ NGHIỆP VỤ (tuần kế tiếp)

> Nhóm **đáng đầu tư nhất nếu cần bảo vệ đồ án**, vì nó vá đúng khoảng cách giữa README và code thật.

### [ ] 2.1. ⭐ "Business Rule Engine động" hiện KHÔNG tồn tại

> 🧠 **Khái niệm 5 — Configuration over Code** · Anh em cùng khái niệm: 2.8

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs`, `MomOi.API/Services/Admin/AdminService.cs:113-164`

README quảng cáo:
> *"Business Rule Engine: Hệ thống quản lý các luật sức khỏe động (Tự định nghĩa ngưỡng cảnh báo BMI, Huyết áp) **mà không cần can thiệp vào code**"*

Sự thật:

| Thành phần | Thực tế |
|---|---|
| Bảng `BusinessRules` (có `TargetMetric`, `Operator`, `ThresholdValue`) | ✅ Tồn tại |
| `AdminService` CRUD rule | ✅ Tồn tại |
| `BusinessRuleEngine` **đọc bảng đó** | ❌ **KHÔNG BAO GIỜ** |

Toàn bộ BR01–BR10 là `if/else` hardcode. Admin vào dashboard sửa ngưỡng sắt từ 11mg → 8mg thì **không có gì xảy ra cả**. Đây là vỏ rỗng — nếu hội đồng hỏi trúng chỗ này rất khó đỡ.

**Chọn một trong hai hướng:**
- [ ] **Hướng thật (khuyến nghị):** cho `BusinessRuleEngine` load `BusinessRules` (`IsActive == true`) rồi evaluate theo `TargetMetric` / `Operator` / `ThresholdValue`. Ít nhất làm cho 3 rule thuần ngưỡng số: **BR04** (cân nặng), **BR07** (sắt), **BR05** (EPDS).
- [ ] **Hướng trung thực:** sửa README, đổi tên thành "quản lý danh mục luật", và tách các hằng số ngưỡng ra một class `RuleThresholds` để chúng nằm một chỗ.

---

### [ ] 2.2. `PregnancyWeek` là snapshot chết — kéo sập BR03 và BR04

> 🧠 **Khái niệm 4 — Derived vs Stored Data** · Anh em cùng khái niệm: 2.7, 2.10

**File:** `MomOi.API/Models/Health/MomHealthProfile.cs:44`

`PregnancyWeek` được lưu xuống DB, chỉ gán tại 2 chỗ: lúc `SetupPregnancy` và khi user tự sửa profile. **Không có job nào cập nhật hàng tuần.**

**Hậu quả:** mẹ khai tuần 10, hai tháng sau hệ thống vẫn nghĩ là tuần 10 →
- BR03 gợi ý bài tập "tam cá nguyệt 1" cho mẹ đã ở tam cá nguyệt 3
- BR04 (điều kiện `PregnancyWeek > 12` và `<= 36`) chạy sai

Điều đáng nói: project **đã làm đúng ở chỗ khác** — `BabyProfile.AgeMonths` tính derived từ `DateOfBirth`. Hãy làm y hệt:

```csharp
public int? PregnancyWeek => LastPeriodDate is null ? null
    : Math.Clamp((int)((DateTime.UtcNow - LastPeriodDate.Value).TotalDays / 7) + 1, 1, 42);
```

> **Nguyên tắc cần nhớ:** dữ liệu phái sinh (derived) thì **tính**, đừng **lưu**. Lưu là tự chuốc lấy stale data.

**Việc cần làm:**
- [ ] Chuyển `PregnancyWeek` thành derived property
- [ ] Viết migration xóa cột cũ
- [ ] Sửa `UserProfileService.cs:49` (đang cho user tự set `PregnancyWeek`) → chuyển thành cho sửa `LastPeriodDate`

---

### [ ] 2.3. ⚠️ Công thức tăng trưởng của bé SAI về mặt y khoa

> 🧠 **Khái niệm 6 — Domain Modeling & Input Validation** · Anh em cùng khái niệm: 2.4, 2.5

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs:120`

```csharp
float expectedWeight = 3.2f + (ageMonths * 0.6f);   // tuyến tính!
float expectedHeight = 50.0f + (ageMonths * 2.0f);  // tuyến tính!
```

**Thử số:** bé 24 tháng → hệ thống kỳ vọng **17.6 kg / 98 cm**. Chuẩn WHO là **~12 kg / ~87 cm**.
→ **Gần như mọi bé 2 tuổi bình thường đều bị app báo "nhẹ cân".**

Tăng trưởng trẻ em là **đường cong** (rất dốc 0–6 tháng, thoải dần sau đó), không phải đường thẳng. Với một app y tế, đây không phải "chưa tối ưu" mà là **sai gây hại** — phụ huynh hoảng loạn hoặc mất niềm tin.

**Việc cần làm:**
- [ ] Nhúng bảng WHO weight-for-age / height-for-age (percentile 3/15/50/85/97 theo tháng & giới) dưới dạng file JSON tra cứu
- [ ] Khoảng 100 dòng dữ liệu, không cần tính z-score đầy đủ

---

### [ ] 2.4. BR09 (dị ứng) — rule cấp cứu nhưng logic so khớp không nhất quán

> 🧠 **Khái niệm 6 — Domain Modeling & Input Validation** · Anh em cùng khái niệm: 2.3, 2.5

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs:441-447`

```csharp
var criticalSymptoms = new[] { "nổi mẩn", "nôn", "tiêu chảy", "khó thở" };
var hasAllergicSigns = todayLog.AllergySymptoms.Any(s => criticalSymptoms.Contains(s.ToLower()));
//                                                        ^^^ SO KHỚP CHÍNH XÁC
bool isDyspnea = todayLog.AllergySymptoms.Any(s => s.ToLower().Contains("khó thở"));
//                                                   ^^^ SO KHỚP CHỨA
```

Hai dòng cách nhau 5 dòng mà dùng **hai kiểu so khớp ngược nhau**.

**Hậu quả:** user nhập `"nổi mẩn đỏ"` → `Contains` của mảng trả `false` → **bỏ sót hoàn toàn cảnh báo dị ứng**. Chỉ khi gõ đúng chằn chặn `"nổi mẩn"` mới ăn.

Đây là rule mức `Critical`, liên quan đến **sốc phản vệ ở trẻ**.

**Việc cần làm:**
- [ ] Đổi triệu chứng từ free-text sang **enum / checkbox** ở FE
- [ ] Nếu chưa đổi được FE: thống nhất dùng một kiểu so khớp (khuyến nghị `Contains` hai chiều + chuẩn hóa dấu)

---

### [ ] 2.5. BR02 (cảnh báo thực phẩm) — lọc bằng `Contains` chuỗi, false positive dày đặc

> 🧠 **Khái niệm 6 — Domain Modeling & Input Validation** · Anh em cùng khái niệm: 2.3, 2.4

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs` (~dòng 215)

```csharp
var dangerousKeywords = new[] { "sushi", "tái", "gỏi", "rượu", "trà đặc", "dứa", "ổi xanh", "gan sống" };
if (dangerousKeywords.Any(k => food.Contains(k, StringComparison.OrdinalIgnoreCase)))
```

| Từ khóa | Khớp nhầm với |
|---|---|
| `"tái"` | "rau **tái**", "đậu **tái**", "**tái** chế" |
| `"dứa"` | "bánh **dứa**", "nước ngọt vị **dứa**" — vô hại |
| `"rượu"` | "**rượu** nấu ăn", "thịt kho **rượu**" (cồn đã bay hơi) |

Mà mỗi lần trúng là **gọi Gemini** sinh cảnh báo → vừa tốn tiền API vừa làm phiền mẹ bầu.

**Việc cần làm:**
- [ ] Tách token theo dấu cách, khớp cả cụm từ thay vì substring thô
- [ ] Thêm danh sách loại trừ (whitelist) cho các trường hợp vô hại

---

### [x] 2.6. ✅ ĐÃ SỬA (2026-09-22) — Rule không idempotent — cảnh báo bị nhân bản

> 🧠 **Khái niệm 3 — Transaction & Idempotency** · Anh em cùng khái niệm: 1.5

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs:500` (`LogAlertAndPushAsync`)

Hàm này **luôn** `Add` một `CriticalAlertLog` mới, không kiểm tra "hôm nay đã cảnh báo rule này cho user này chưa". Mà `EvaluateAsync` được gọi mỗi khi user mở màn hình → **user F5 ba lần = 3 bản ghi trùng + 3 push SignalR**.

- [x] Thêm chốt chặn idempotency trong `LogAlertAndPushAsync`: bỏ qua cả ghi DB lẫn đẩy thông báo nếu cùng rule đã cảnh báo cho cùng người dùng trong cửa sổ thời gian
- [x] Cửa sổ chặn trùng **khác nhau theo mức độ**: `Critical` = 30 phút, `Warning` = 24 giờ
- [ ] Hạn chế đã biết: cảnh báo mức `Info` (BR06, BR08, BR10) không ghi vào `CriticalAlertLogs` nên chưa chặn trùng được — cần bảng lịch sử riêng nếu muốn xử lý

**Vì sao `Critical` chỉ 30 phút chứ không phải cả ngày:** cảnh báo nguy kịch (dị ứng ở trẻ, trầm cảm sau sinh) có thể tái phát trong ngày vì nguyên nhân MỚI. Chặn cả ngày sẽ che mất sự kiện thật — đây là đánh đổi an toàn y tế, không phải kỹ thuật.

---

### [ ] 2.7. Hai đường tính điểm EPDS song song

> 🧠 **Khái niệm 4 — Derived vs Stored Data** · Anh em cùng khái niệm: 2.2, 2.10

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs:320`

- `EvaluateEpdsScore()` có validate đầy đủ (đúng 10 câu, mỗi câu 0–3) và phân 3 mức risk
- **BR05 thì bỏ qua hàm đó**, gọi thẳng `latestEpds.Answers.Sum() >= 9`

Hai nguồn sự thật cho cùng một thang đo lâm sàng. Mai đổi ngưỡng ở một chỗ, quên chỗ kia → hệ thống mâu thuẫn với chính nó.

**Việc cần làm:**
- [ ] BR05 phải gọi `EvaluateEpdsScore(latestEpds.Answers)` thay vì tự cộng

---

### [ ] 2.8. BR01 dùng sai `AlertSeverity`

> 🧠 **Khái niệm 5 — Configuration over Code** · Anh em cùng khái niệm: 2.1

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs` (~dòng 180)

Thông báo *"Cửa sổ thụ thai tối ưu đang mở 🌸"* được gắn `Severity: Critical` → bị ghi vào bảng `CriticalAlertLogs`, **cùng chỗ với cảnh báo trầm cảm nặng và khó thở ở trẻ**.

→ Dashboard "bệnh nhân rủi ro cao" của Admin bị nhiễu bởi các mẹ đang... tới ngày rụng trứng.

**Việc cần làm:**
- [ ] Đổi BR01 xuống `AlertSeverity.Info`

---

### [ ] 2.9. ⭐ Mô hình kinh doanh chưa được thực thi

> 🧠 **Khái niệm 7 — Authorization & Cross-cutting Concerns** · Anh em cùng khái niệm: 3.6

**File:** toàn bộ `MomOi.API/Controllers/`

Project bán 2 gói (99k `MomHienDai` / 199k `SuperMomVip`) nhưng:

- Tổng cộng chỉ có **9 endpoint** gắn `[RequiresTier]`
- Trong đó **đúng 1 endpoint** dành riêng cho VIP (`PostpartumController.cs:81`)
- Các endpoint **tốn tiền nhất** — `AIController` (sinh công thức bằng Gemini), `DietController`, `SymptomController`, `BabyController` menu — **không gate gì cả**

**Nghĩa là:** user Free xài AI thoải mái, còn người trả **199k** so với người trả **99k** chỉ hơn nhau **một** tính năng.

Đây là lỗi business rule **nghiêm trọng nhất về mặt sản phẩm**.

**Việc cần làm:**
- [ ] Vẽ **ma trận quyền** rõ ràng: Free / MomHienDai / SuperMomVip × từng nhóm tính năng
- [ ] Gắn `[RequiresTier]` theo đúng ma trận đó
- [ ] Thêm **quota gọi AI theo tier** (ví dụ Free 3 lần/tháng, HD 30 lần, VIP không giới hạn)

---

### [ ] 2.10. Không có cơ chế hạ tier khi gói hết hạn

> 🧠 **Khái niệm 4 — Derived vs Stored Data** · Anh em cùng khái niệm: 2.2, 2.7

**File:** `MomOi.API/Models/Identity/AppUser.cs:47`

`EffectiveTier` tính đúng lúc đọc (gói quá hạn = `Free`) — **phần này tốt**. Nhưng cột `Tier` trong DB vẫn nằm nguyên là `SuperMomVip` mãi mãi.

→ Mọi báo cáo/thống kê Admin query `WHERE Tier = 2` sẽ **đếm cả những người đã hết hạn từ năm ngoái**.

**Việc cần làm:**
- [ ] Thêm background worker hằng ngày hạ tier khi `TierExpiresAt <= now`, HOẶC
- [ ] Ép mọi query thống kê phải xét thêm điều kiện `TierExpiresAt`

---

# 🟡 NHÓM 3 — DỌN KIẾN TRÚC (khi có thời gian)

### [x] 3.1. ✅ ĐÃ SỬA (2026-09-22) — ⭐ Ba kiểu truy cập dữ liệu chạy song song

> 🧠 **Khái niệm 8 — Data Access Patterns** · Anh em cùng khái niệm: 3.2, 3.3, 3.4, 3.5

Đây là **vấn đề kiến trúc lớn nhất**:

| Cách | Số lượng | Ví dụ |
|---|:---:|---|
| `IGenericRepository<T>` inject trực tiếp | **9 service** | Pregnancy, Baby, Fertility, Diet, Symptom, Lifestyle, Postpartum, DailyMonitoring, AIFeatures |
| `IUnitOfWork` | **10 service** | Payment, Alert, Dashboard, Expert, Medication, Mom, Recipe, Report, UserProfile, Usda |
| `AppDbContext` thẳng | **2 service + 2 controller** | `AuthService`, `AdminService`, `FeedbackController`, `WellnessController` |

Người mới vào team sẽ không biết phải viết theo kiểu nào. Và **2 controller inject thẳng `AppDbContext` là phá tầng** — Controller nói chuyện trực tiếp với DB, bỏ qua cả Service lẫn Repository.

**Việc cần làm:**
- [x] Chọn `IUnitOfWork` làm kiểu chuẩn duy nhất — **23/23 service** đã dùng
- [x] Gỡ `AppDbContext` khỏi `FeedbackController` và `WellnessController` — tạo mới `IWellnessService`/`WellnessService` và `IFeedbackService`/`FeedbackService`, đăng ký DI
- [x] Migrate 9 service dùng `IGenericRepository` trực tiếp → **còn 0**
- [x] Gỡ `AppDbContext` khỏi `AuthService` và `AdminService` (truy vấn thống kê phức tạp dùng lối thoát hiểm `Query(asNoTracking: true)`)
- [ ] `BusinessRuleEngine` vẫn giữ `AppDbContext` → xử lý ở mục **3.7** (tách Calculator/Evaluator)

**Kết quả đo được:** controller chạm DB: 2 → **0** · service dùng `IGenericRepository` trực tiếp: 9 → **0** · nơi được phép commit trong tầng nghiệp vụ: 15+ → **1** (`UnitOfWork`)

---

### [x] 3.2. ✅ ĐÃ SỬA (2026-09-22) — `IGenericRepository.SaveChangesAsync()` tự phá vỡ Unit of Work

> 🧠 **Khái niệm 8 — Data Access Patterns** · Anh em cùng khái niệm: 3.1, 3.3, 3.4, 3.5

**File:** `MomOi.API/Repositories/IGenericRepository.cs:23`

Đây là **phản mẫu (anti-pattern)**.

Mục đích của Unit of Work: repository chỉ **đánh dấu** thay đổi, còn **commit** là việc của UoW — để nhiều repository chung một transaction. Khi repository tự save được, nếu một use case đụng 3 bảng và save 3 lần, bảng 3 lỗi thì bảng 1–2 **đã ghi rồi → dữ liệu rác, không rollback được**.

**Việc cần làm:**
- [x] Bỏ `SaveChangesAsync()` khỏi `IGenericRepository` và `GenericRepository` — **đã xoá hẳn**
- [x] Bỏ luôn `GetAllAsync()` (tải cả bảng về RAM, không còn ai gọi)
- [x] Chuyển **9 service** sang `IUnitOfWork`: `AIFeature`, `Fertility`, `DailyMonitoring`, `Diet`, `Lifestyle`, `Symptom`, `Postpartum`, `Pregnancy`, `Baby`
- [x] Đưa **24 cảnh báo `CS0618` về 0**, build giữ nguyên 0 error
- [x] Gộp 2 lần commit thừa trong `SymptomService`; giữ và **ghi chú rõ lý do** cho 3 chỗ commit hai lần CÓ CHỦ ĐÍCH (`DailyMonitoring`, `Lifestyle`, `Postpartum`)
- [ ] Còn lại ngoài phạm vi bước này: `AdminService`, `AuthService`, `BusinessRuleEngine`, `FeedbackController` vẫn commit thẳng qua `AppDbContext` → thuộc mục **3.1** và **3.7**

---

### [x] 3.3. ✅ ĐÃ SỬA (2026-09-22) — `UnitOfWork.Dispose()` dispose nhầm DbContext của DI

> 🧠 **Khái niệm 8 — Data Access Patterns** · Anh em cùng khái niệm: 3.1, 3.2, 3.4, 3.5

**File:** `MomOi.API/Repositories/UnitOfWork.cs:45`

```csharp
public void Dispose()
{
    _context.Dispose();   // ← _context do DI container sở hữu!
}
```

Cả `UnitOfWork` và `AppDbContext` đều `Scoped`. Khi DI dispose `UnitOfWork`, nó **kéo theo `AppDbContext` chết** — trong khi service khác cùng scope có thể còn đang dùng. Lỗi "disposing what you don't own".

**Việc cần làm:**
- [x] Bỏ `IDisposable` khỏi `UnitOfWork` hoàn toàn — build lại: 0 error, không phát sinh warning mới

---

### [ ] 3.4. 🔄 ĐANG LÀM (2026-09-22) — Repository quá nghèo nàn → hiệu năng kém

> 🧠 **Khái niệm 8 — Data Access Patterns** · Anh em cùng khái niệm: 3.1, 3.2, 3.3, 3.5

**File:** `MomOi.API/Repositories/IGenericRepository.cs`

Không có `Include`, `AsNoTracking`, phân trang, hay trả `IQueryable`. `FindAsync` trả `IEnumerable` → **kéo toàn bộ kết quả về RAM rồi mới lọc/sắp xếp bằng LINQ-to-Objects**.

Ví dụ: `GetHistoryAsync` kéo hết giao dịch của user rồi mới `OrderByDescending` trong bộ nhớ.

Toàn bộ project **chỉ duy nhất `RecipeController.cs:32` có phân trang**. Danh sách alert, log theo dõi hằng ngày, lịch sử triệu chứng — tất cả trả về không giới hạn. Vài tháng nữa có dữ liệu thật là API sẽ chậm rõ rệt.

**Việc cần làm:**
- [x] Thêm overload nhận `Func<IQueryable<T>, IOrderedQueryable<T>>` + `Query()` trả `IQueryable` — **xong**
- [x] Thêm `PagedResult<T>` và `GetPagedAsync()` đẩy COUNT/ORDER BY/LIMIT xuống database — **xong**
- [x] Thêm `CountAsync()` (COUNT(*) trên DB thay vì đếm trong RAM) — **xong**
- [x] Thêm tuỳ chọn `asNoTracking` cho `FirstOrDefaultAsync` / `FindAsync` — **xong**
- [x] Đánh dấu `[Obsolete]` cho `GetAllAsync()` và `SaveChangesAsync()` — trình biên dịch sinh ra **24 cảnh báo CS0618**, chính là danh sách việc của bước 3
- [ ] Áp dụng `asNoTracking: true` cho các truy vấn chỉ đọc ở từng service *(làm cùng bước 3)*
- [ ] Đổi các endpoint trả danh sách sang `GetPagedAsync` *(đã làm `RecipeService`; còn lại làm cùng bước 4)*

---

### [x] 3.5. ✅ ĐÃ SỬA (2026-09-22) — Soft delete là code chết

> 🧠 **Khái niệm 8 — Data Access Patterns** · Anh em cùng khái niệm: 3.1, 3.2, 3.3, 3.4

**File:** `MomOi.API/Models/BaseEntity.cs:15`

`BaseEntity` có `IsDeleted` và `DeletedAt`. Nhưng:
- **Không có** `HasQueryFilter` nào trong `AppDbContext`
- **Không có một dòng code nào** trong project gán `IsDeleted = true`
- Mọi thao tác xóa đều là `Remove()` → **hard delete**

Với dữ liệu y tế (hồ sơ thai kỳ, nhật ký triệu chứng), xóa vĩnh viễn là rủi ro pháp lý.

**Đã chọn: hướng LAI** — xóa mềm cho dữ liệu y tế, xóa cứng cho dữ liệu hệ thống.

- [x] Tách `BaseEntity` (Id/CreatedAt/UpdatedAt) và `SoftDeletableEntity : BaseEntity, ISoftDeletable` (IsDeleted/DeletedAt)
- [x] **4 entity dùng xóa mềm**: `GrowthRecord`, `MedicationSchedule`, `MedicationAdherenceLog`, `FoodAllergyRecord`
- [x] **23 bảng còn lại đã gỡ 46 cột chết** — migration `SoftDeleteForMedicalRecords`
- [x] Global query filter gắn tự động bằng reflection trong `OnModelCreating` → mọi truy vấn tự thêm `AND is_deleted = false`
- [x] `AppDbContext.SaveChangesAsync` chặn `EntityState.Deleted` → đổi thành `Modified` + set cờ
- [x] `IGenericRepository.RemovePermanently()` — lối thoát xóa thật, phục vụ quyền yêu cầu xóa dữ liệu cá nhân theo **Nghị định 13/2023/NĐ-CP**
- [x] Xử lý cascade thủ công trong `MedicationService`: xóa mềm không kích hoạt `ON DELETE CASCADE` nên phải tự xóa mềm bản ghi con
- [x] ✅ Đã áp migration lên database local (2026-09-23) — đã sao lưu `backup_truoc_migration.sql` trước khi chạy

**Xóa cứng (giữ nguyên, cố ý):** `LifestyleAlert`, `NotificationAlert`, `BusinessRule` — dữ liệu tự sinh lại hoặc là cấu hình; xóa mềm sẽ làm bảng phình vô hạn.

---

### [ ] 3.6. Lặp code trích xuất `userId` khoảng 80 lần

> 🧠 **Khái niệm 7 — Authorization & Cross-cutting Concerns** · Anh em cùng khái niệm: 2.9

**File:** toàn bộ `MomOi.API/Controllers/` (22 controller)

```csharp
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (string.IsNullOrEmpty(userId)) return Unauthorized();
```

**Việc cần làm:**
- [ ] Tạo `BaseApiController` với property `CurrentUserId` → xóa được ~160 dòng

---

### [ ] 3.7. `BusinessRuleEngine` nhận toàn dependency nullable

> 🧠 **Khái niệm 9 — SRP & Dependency Injection** · Anh em cùng khái niệm: 3.8, 3.9

**File:** `MomOi.API/Services/BusinessRules/BusinessRuleEngine.cs:26`

```csharp
public BusinessRuleEngine(
    AppDbContext? context = null,
    IGeminiService? geminiService = null,
    IHubContext<AlertHub>? hubContext = null,
    ILogger<BusinessRuleEngine>? logger = null)
```

Làm vậy để unit test dựng nhanh — hiểu được. Nhưng cái giá là **mất fail-fast**: nếu DI cấu hình sai, **BR02–BR05 sẽ âm thầm không chạy**, không lỗi, không log.

> Một rule cảnh báo trầm cảm sau sinh im lặng là điều tệ nhất có thể xảy ra.

**Việc cần làm — tách đôi:**
- [ ] `IHealthRuleCalculator` — thuần toán học (`CalculateCalorieTarget`, `EvaluateEpdsScore`, `VerifyBabyGrowth`), không dependency, test thoải mái
- [ ] `IHealthRuleEvaluator` — có `AppDbContext`, `IGeminiService`, `IHubContext` **bắt buộc**, không nullable

Đồng thời giải quyết luôn việc file này đang **538 dòng**, gánh 4 trách nhiệm (tính toán + truy vấn DB + ghi log + push realtime).

---

### [ ] 3.8. `BabyService` có cache rò rỉ bộ nhớ + code chữa cháy trong hot path

> 🧠 **Khái niệm 9 — SRP & Dependency Injection** · Anh em cùng khái niệm: 3.7, 3.9

**File:** `MomOi.API/Services/Baby/BabyService.cs:34`

```csharp
private static readonly ConcurrentDictionary<string, object> _dailyMenuCache = new();
```

`static`, key có chứa ngày, **không TTL, không evict** → rò rỉ bộ nhớ tuyến tính theo thời gian. Scale lên 2 instance thì cache không đồng bộ.

Ngay dưới đó là khối *"Tự động làm sạch các Title bị ô nhiễm do debug suffix trong DB (Self-healing)"* — quét và sửa dữ liệu hỏng do một bug debug cũ, **chạy trên mỗi request**.

**Việc cần làm:**
- [ ] Thay bằng `IMemoryCache` với `AbsoluteExpiration`
- [ ] Chuyển khối self-healing thành **một migration chạy một lần**, gỡ khỏi đường đi nóng của API

---

### [ ] 3.9. Service trả anonymous type qua `ApiResponse<object>`

> 🧠 **Khái niệm 9 — SRP & Dependency Injection** · Anh em cùng khái niệm: 3.7, 3.8

`ApiResponse<object>.SuccessResult(new { PregnancyWeek = week, Trimester = trimester, ... })`

→ Swagger không mô tả được, FE không có hợp đồng, đổi tên field không ai biết cho đến lúc production lỗi.

**Việc cần làm:**
- [ ] Định nghĩa DTO tử tế (thư mục `MomOi.API/DTOs/` đã có sẵn rồi)

---

### [ ] 3.10. `ExceptionMiddleware` che lỗi hệ thống thành 400

> 🧠 **Khái niệm 10 — Error Handling, Scalability & Testing** · Anh em cùng khái niệm: 3.12, 3.13

**File:** `MomOi.API/Middleware/ExceptionMiddleware.cs:49`

```csharp
case ArgumentException or InvalidOperationException:
    statusCode = 400;
    message = exception.Message;
```

`InvalidOperationException` là exception mà **EF Core và DI ném ra thường xuyên nhất** (query lỗi, service chưa đăng ký, sequence rỗng...). Gộp nó vào 400 nghĩa là **lỗi hạ tầng bị báo cho FE là "lỗi nhập liệu"**, kèm theo **message nội bộ bị lộ ra ngoài**.

**Việc cần làm:**
- [ ] Định nghĩa exception riêng cho domain (`BusinessRuleException`) thay vì mượn exception của framework

---

### [ ] 3.11. `RefreshTokenAsync` viết nhầm nhưng "may mà" vẫn chạy

> 🧠 **Khái niệm 2 — Stateless Authentication** · Anh em cùng khái niệm: 1.3, 1.4

**File:** `MomOi.API/Services/Auth/AuthService.cs:139`

Code truyền **refresh token** (chuỗi base64 ngẫu nhiên 64 byte) vào `GetPrincipalFromExpiredToken()` — hàm này parse chuỗi như một **JWT**. Nó **luôn throw, luôn trả `null`**, và luồng chỉ chạy được nhờ nhánh fallback quét DB bên dưới. Code đang đúng một cách tình cờ.

Kèm theo: refresh token lưu **plaintext** trong DB, hạn 30 ngày, và câu `WHERE RefreshToken = @x` **không có index** → full table scan mỗi lần refresh.

**Việc cần làm:**
- [ ] Dọn lại logic cho rõ nghĩa — tra thẳng DB theo refresh token, bỏ bước parse JWT
- [ ] Hash refresh token trước khi lưu
- [ ] Thêm index cho cột `RefreshToken`

---

### [ ] 3.12. `RoutineAlertWorker` không scale

> 🧠 **Khái niệm 10 — Error Handling, Scalability & Testing** · Anh em cùng khái niệm: 3.10, 3.13

**File:** `MomOi.API/BackgroundServices/RoutineAlertWorker.cs:58`

```csharp
var users = await dbContext.Users.ToListAsync(stoppingToken);
```

Load **toàn bộ** user vào RAM, tạo alert cho **tất cả** (kể cả admin, staff, expert), rồi `SaveChanges` một phát. Với 10k user là một lệnh INSERT khổng lồ.

**Việc cần làm:**
- [ ] Lọc theo role `Mom`
- [ ] Xử lý theo lô (batch 500 bản ghi/lần)

---

### [ ] 3.13. Test đang đầu tư NGƯỢC với mức độ rủi ro

> 🧠 **Khái niệm 10 — Error Handling, Scalability & Testing** · Anh em cùng khái niệm: 3.10, 3.12

**File:** `MomOi.Tests/BusinessRuleEngineTests.cs`

14 test method (22 test case), **toàn bộ** nằm ở `BusinessRuleEngineTests` và chỉ phủ 3 hàm thuần túy. Không có một test nào cho:
- Luồng IPN thanh toán (chỗ dính **tiền thật**)
- Xác thực / refresh token
- `SubscriptionTierMiddleware` (chỗ quyết định **ai được xài gì**)

**Việc cần làm:**
- [ ] Viết test cho Payment IPN (đặc biệt: sai chữ ký, sai số tiền, IPN trùng)
- [ ] Viết test cho Auth (login sai nhiều lần → lockout, refresh token hết hạn)
- [ ] Viết test cho `SubscriptionTierMiddleware` (Free gọi endpoint VIP → 403)

---

## 🐛 Warning khi build (4 cái, nhỏ nhưng nên dọn)

- [ ] `MomOi.API/Services/AIFeatures/AIFeatureService.cs:58` — `CS8604` possible null reference argument
- [ ] `MomOi.API/Services/Baby/BabyService.cs:53` — `CS8600` converting null literal to non-nullable
- [ ] `MomOi.API/Services/Baby/BabyService.cs:62` — `CS8600` converting null literal to non-nullable
- [ ] `MomOi.API/Services/Baby/BabyService.cs:54` — `CS0219` biến `isNewMenu` gán nhưng không dùng

---

## 📈 Bảng tiến độ

Cập nhật mỗi khi hoàn thành một mục.

| Nhóm | Tổng mục | Đã xong | Tiến độ |
|---|:---:|:---:|---|
| 🔴 Nhóm 1 — Khẩn cấp | 6 | 1 | `██░░░░░░░░` 17% |
| 🟠 Nhóm 2 — Nghiệp vụ | 10 | 1 | `█░░░░░░░░░` 10% |
| 🟡 Nhóm 3 — Kiến trúc | 13 | 4 | `███░░░░░░░` 31% |
| 🐛 Build warning | 4 | 0 | `░░░░░░░░░░` 0% |
| **TỔNG** | **33** | **6** | `██░░░░░░░░` **18%** |

---

## 🎯 Gợi ý thứ tự làm

1. **Nếu sắp bảo vệ đồ án** → ưu tiên **mục 2.1** (Business Rule Engine động). Đây là chỗ khoảng cách giữa README và code lớn nhất, và cũng là chỗ hội đồng dễ hỏi trúng nhất.
2. **Nếu sắp deploy thật** → làm trọn **Nhóm 1** trước (6 mục, mỗi mục khoảng 1 giờ, độc lập nhau).
3. **Nếu muốn app đúng về y khoa** → **2.2 → 2.3 → 2.4** theo thứ tự đó.
4. **Nhóm 3** làm dần, mỗi tuần 2–3 mục, không gấp.
