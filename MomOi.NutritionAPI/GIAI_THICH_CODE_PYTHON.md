# Giải thích code Python — MomOi.NutritionAPI

> Tài liệu giải thích từng phần code của **Nutrition API (Python FastAPI)** — microservice
> dinh dưỡng trong hệ thống MomBaby Healthcare AI. Dùng để ôn tập khi thuyết trình / trả lời hội đồng.

---

## 1. Vai trò trong kiến trúc tổng thể

```
React SPA ──REST──> .NET API (MomOi.API :5265) ──HTTP/REST──> Python FastAPI (MomOi.NutritionAPI :8001)
                                                                    │
                                                                    ├── SQLite / PostgreSQL (bảng nutrition_*)
                                                                    └── USDA FoodData Central API (dữ liệu dinh dưỡng gốc)
```

- **.NET API** là backend chính (auth, hồ sơ, nghiệp vụ). Nó **không tự tính dinh dưỡng** mà ủy quyền
  cho Python qua `NutritionProxyService` (HttpClient).
- **Python NutritionAPI** là *nutrition engine*: nạp dữ liệu USDA, tính mục tiêu WHO,
  đề xuất thực đơn cho bé (lọc dị ứng) và thực đơn mẹ bầu theo tuần thai.
- Frontend **không bao giờ gọi thẳng** Python — mọi request đi qua .NET (một cổng duy nhất, có JWT).

---

## 2. Cấu trúc thư mục

| File / thư mục | Vai trò |
|---|---|
| `main.py` | Khai báo FastAPI app + toàn bộ endpoint + seed dữ liệu khi khởi động |
| `database.py` | Kết nối DB qua SQLAlchemy (SQLite cho dev, PostgreSQL cho docker) |
| `schemas.py` | Pydantic models — hợp đồng dữ liệu (request/response) |
| `models/` | ORM models — các bảng `nutrition_*` |
| `services/usda_service.py` | Gọi USDA FoodData Central lấy dinh dưỡng nguyên liệu |
| `services/who_nutrition_goals.py` | Bảng mục tiêu dinh dưỡng WHO theo tuổi bé |
| `services/recommendation_engine.py` | Thuật toán đề xuất thực đơn bé (lọc dị ứng, texture, đủ chất) |
| `services/prenatal_meal_plans.py` | Thực đơn mẹ bầu 7 ngày theo tam cá nguyệt + xoay vòng theo tuần thai |
| `seed/seed_recipes.py` | 20 nguyên liệu + 21 công thức ăn dặm mẫu (offline resiliency) |

---

## 3. `database.py` — kết nối DB linh hoạt

```python
DATABASE_URL = os.getenv("DATABASE_URL", "sqlite:///./nutrition.db")

if DATABASE_URL.startswith("sqlite"):
    engine = create_engine(DATABASE_URL, connect_args={"check_same_thread": False})
else:
    engine = create_engine(DATABASE_URL, pool_pre_ping=True)
```

**Giải thích:**
- Đọc chuỗi kết nối từ **biến môi trường** → dev local dùng SQLite (không cần cài gì),
  docker-compose truyền `DATABASE_URL=postgresql://...@pgdb:5432/momoidb` để dùng chung PostgreSQL.
- `check_same_thread=False`: SQLite mặc định chặn đa luồng, FastAPI chạy nhiều request song song nên phải tắt.
- `pool_pre_ping=True`: kiểm tra kết nối còn sống trước khi dùng (tránh lỗi connection stale trên Postgres).

**Điểm hay để nói với hội đồng:** các bảng Python đều có tiền tố **`nutrition_`**
(`nutrition_ingredients`, `nutrition_recipes`, ...) để **không xung đột schema** với các bảng
EF Core của .NET (`recipes`, `baby_profiles`) khi hai service dùng chung một database PostgreSQL.

---

## 4. `models/` — các bảng ORM

### `models/ingredient.py` — bảng `nutrition_ingredients`
Mỗi dòng = 1 nguyên liệu với **giá trị dinh dưỡng trên 100g** (nguồn USDA):
`calories_kcal, protein_g, fat_g, carbs_g, iron_mg, zinc_mg, calcium_mg, vitamin_c_mg, omega3_mg`.

### `models/recipe.py` — bảng `nutrition_recipes` + `nutrition_recipe_ingredients`
- `Recipe`: công thức ăn dặm cho bé — có `meal_type` (breakfast/lunch/dinner/snack),
  `texture` (độ thô theo tuổi), `min_age_months`/`max_age_months`, `allergens` (JSON),
  và tổng dinh dưỡng đã tính sẵn (`total_calories`, `total_protein_g`, ...).
- `RecipeIngredient`: bảng nối N-N — công thức gồm nguyên liệu nào, bao nhiêu gram
  (FK → `nutrition_recipes.id`, `nutrition_ingredients.id`).

### `models/baby_profile.py` — bảng `nutrition_baby_profiles`
Hồ sơ bé phía Python (ít dùng — luồng chính là .NET gửi thông tin bé qua request body, xem mục 6).

---

## 5. `main.py` — endpoints & seed

### Khởi động: tạo bảng + tự seed
```python
@app.on_event("startup")
def on_startup():
    Base.metadata.create_all(bind=engine)          # tạo bảng nếu chưa có
    db = SessionLocal()
    if db.query(Ingredient).count() == 0:          # DB trống → seed
        from seed.seed_recipes import seed_data
        seed_data(db)                              # 20 nguyên liệu + 21 công thức
```
**Giải thích:** đảm bảo service chạy được ngay cả khi offline/không có USDA key
(offline resiliency) — dữ liệu mẫu đã kèm giá trị dinh dưỡng chuẩn.

### Các endpoint chính

| Endpoint | Chức năng | Ai gọi |
|---|---|---|
| `POST /api/nutrition/ingest` | Nạp nguyên liệu từ USDA vào DB | Admin (use case *Ingest USDA data*) |
| `GET /api/nutrition/ingredient/{name}` | Tra cứu dinh dưỡng 1 nguyên liệu (USDA live) | Admin/hệ thống |
| `GET /api/nutrition/meal-plan?week=X` | **Thực đơn mẹ bầu 7 ngày** theo tuần thai | .NET `PregnancyService` |
| `POST /api/menu/daily` | **Thực đơn bé 1 ngày** (nhận age/weight/allergies trong body) | .NET `BabyService` |
| `POST /api/menu/weekly` | Thực đơn bé 7 ngày | .NET `BabyService` |
| `POST /api/recipes` + `/recalculate` | CRUD công thức + tính lại dinh dưỡng | Nội bộ |

### Vì sao menu bé nhận thông tin qua body thay vì baby_id?
```python
def _transient_baby(req: MenuRecommendRequest):
    return SimpleNamespace(
        age_months=req.age_months,
        current_weight_kg=req.weight_kg,
        allergies=req.allergies or [],
    )

@app.post("/api/menu/daily", response_model=DailyMenu)
async def recommend_daily_for_payload(req: MenuRecommendRequest, db=Depends(get_db)):
    return recommend_daily_menu(_transient_baby(req), db)
```
**Giải thích:** hồ sơ bé do **.NET sở hữu** (bảng `baby_profiles` của EF có schema khác
với model Python). Thay vì để Python đọc bảng của .NET (rủi ro xung đột schema),
.NET đọc hồ sơ → gửi `{age_months, weight_kg, allergies}` sang → Python dựng một
"transient baby" (đối tượng tạm, không lưu DB) rồi chạy engine.
Đây là nguyên tắc **mỗi service sở hữu dữ liệu của mình** trong microservice.

---

## 6. `services/who_nutrition_goals.py` — mục tiêu WHO

```python
WHO_GOALS = {
    (0,  5):  {"calories": 550.0,  "protein_g": 9.1,  "iron_mg": 0.27, "fat_g": 31.0},
    (6,  8):  {"calories": 615.0,  "protein_g": 11.0, "iron_mg": 11.0, "fat_g": 30.0},
    (9, 11):  {"calories": 686.0,  "protein_g": 14.0, "iron_mg": 11.0, "fat_g": 30.0},
    (12, 23): {"calories": 894.0,  "protein_g": 13.0, "iron_mg": 7.0,  "fat_g": 25.0},
    (24, 35): {"calories": 1000.0, "protein_g": 13.0, "iron_mg": 7.0,  "fat_g": 25.0},
}
```
- Tra mục tiêu dinh dưỡng/ngày theo **độ tuổi (tháng)** của bé, chuẩn WHO 2023.
- Bé < 12 tháng có cân nặng → calories được **cá nhân hóa**: `weight_kg × 90 kcal/kg`.

---

## 7. `services/recommendation_engine.py` — thuật toán đề xuất thực đơn bé

Luồng `recommend_daily_menu(baby, db)` — 5 bước:

```python
# 1. Lấy công thức phù hợp ĐỘ TUỔI
all_recipes = db.query(Recipe).filter(
    Recipe.min_age_months <= age, Recipe.max_age_months >= age).all()

# 2. LOẠI CÔNG THỨC CHỨA CHẤT GÂY DỊ ỨNG (use case: Exclude allergens)
baby_allergies = [a.lower() for a in (baby.allergies or [])]
safe_recipes = [r for r in all_recipes
                if not any(a in [al.lower() for al in (r.allergens or [])]
                           for a in baby_allergies)]

# 3. Gom nhóm theo bữa (breakfast / lunch / dinner / snack)
# 4. Chọn món: ưu tiên đúng TEXTURE theo tuổi (bột → cháo rây → cháo hạt → cơm nát),
#    tránh lặp món đã chọn các ngày trước (seen_ids) → thực đơn tuần đa dạng
chosen = random.choice(final_pool)

# 5. Cộng tổng dinh dưỡng các món đã chọn, so với mục tiêu WHO
#    → coverage_pct từng chất + cờ meets_80pct (đạt ≥80% mục tiêu chưa)
```

**Điểm nhấn khi thuyết trình:**
- Dị ứng được lọc **phía server, trong engine** — không phải ẩn trên giao diện.
- `meets_80pct` là tiêu chí "greedy WHO-compliant": menu đề xuất phải phủ ≥80% nhu cầu.
- `recommend_weekly_menu` gọi lại hàm daily 7 lần, truyền `seen_ids` để **7 ngày không trùng món**.

---

## 8. `services/prenatal_meal_plans.py` — thực đơn mẹ bầu

### Dữ liệu: 3 bộ thực đơn theo tam cá nguyệt
- `_T1` (tuần 1–12): dễ tiêu, giàu folate.
- `_T2` (tuần 13–27): tăng sắt & canxi.
- `_T3` (tuần 28+): omega-3, chất xơ, năng lượng.
- Mỗi ngày ghi rõ món 4 bữa **và** danh sách `(nguyên_liệu, số_gram)` — ví dụ
  `("Salmon", 130)` = 130g cá hồi — tên tiếng Anh khớp cột `english_name` trong DB.

### Cá nhân hóa theo tuần thai (xoay vòng)
```python
offset = week % len(days)                       # tuần thai làm seed
weekday_labels = [d["day"] for d in days]       # giữ nhãn Thứ Hai → Chủ Nhật
rotated = days[offset:] + days[:offset]         # xoay nội dung
days = [{**content, "day": label} for label, content in zip(weekday_labels, rotated)]
```
- Tuần 5 ≠ tuần 6 ≠ tuần 7... (mỗi tuần một cách sắp xếp khác).
- **Deterministic**: cùng tuần luôn trả cùng thực đơn → tái lập được, cache được.
- Sang tam cá nguyệt mới (tuần 13, 28) đổi cả bộ món.

### Dinh dưỡng tính THẬT từ USDA (không hardcode con số)
```python
f = grams / 100.0                               # dữ liệu USDA là per-100g
cal     += ing.calories_kcal * f
protein += ing.protein_g * f
iron    += ing.iron_mg * f
```
- Cộng dồn theo gram thực tế của từng nguyên liệu trong ngày → `dailyNutrients`
  (calories, protein, carbs, fat, iron) hiển thị trên FE là **kết quả tính toán**.
- Nguyên liệu chưa có trong DB thì **bỏ qua chứ không bịa số** (`if ing is None: continue`).
- Key trả về dạng camelCase (`dailyNutrients`) vì .NET pass-through nguyên văn cho React.

---

## 9. `services/usda_service.py` — tích hợp USDA FoodData Central

```python
url = "https://api.nal.usda.gov/fdc/v1/foods/search"
params = {"query": name, "pageSize": 1, "api_key": USDA_API_KEY}
async with httpx.AsyncClient(timeout=15.0) as client:
    ...
```
- Gọi **bất đồng bộ** (httpx async) tới USDA, parse các nutrient ID chuẩn
  (protein, sắt, kẽm, canxi...) về model `Ingredient`.
- `USDA_VN_MAP`: ánh xạ tên tiếng Anh ↔ tiếng Việt (Salmon ↔ Cá hồi) để hiển thị.
- `bulk_ingest_ingredients`: Admin nạp hàng loạt (use case *Import & Update USDA Data*).
- Key đọc từ env `USDA_API_KEY` (docker-compose truyền vào).

---

## 10. `schemas.py` — hợp đồng dữ liệu (Pydantic)

Đáng chú ý:
```python
class MenuRecommendRequest(BaseModel):   # .NET gửi sang
    age_months: int
    weight_kg: Optional[float] = None
    allergies: List[str] = []

class DailyMenu(BaseModel):              # trả về cho .NET
    meals: Dict[str, RecipeResponse]     # breakfast / lunch / dinner / snack / supplementary_snack
    nutrition_totals: Dict[str, float]
    coverage_pct: Dict[str, float]       # % đạt mục tiêu WHO từng chất
    meets_80pct: bool
```
Pydantic **tự validate** kiểu dữ liệu — request sai định dạng bị chặn với lỗi 422 trước khi vào logic.

---

## 11. Phía .NET gọi sang như thế nào (để trả lời liên kết 2 service)

`MomOi.API/Services/Nutrition/NutritionProxyService.cs`:
```csharp
// Thực đơn mẹ bầu
GET  {NutritionApiUrl}/api/nutrition/meal-plan?week={week}

// Thực đơn bé — gửi thông tin bé trong body (không gửi baby_id)
POST {NutritionApiUrl}/api/menu/daily
body: { "age_months": 8, "weight_kg": 8.5, "allergies": ["fish"] }
```
- `NutritionApiUrl` cấu hình trong `appsettings.json` (local: `http://localhost:8001`,
  docker: `http://nutrition-api:8001`).
- Python chết → .NET trả `FailureResult("Dịch vụ dinh dưỡng hiện không khả dụng")`
  — **không có dữ liệu giả** (đã gỡ toàn bộ fallback hardcode).

---

## 12. Cách chạy (dev local)

```powershell
cd MomOi.NutritionAPI
pip install -r requirements.txt        # cần Python 3.10–3.12 (khuyến nghị 3.12)
set PYTHONIOENCODING=utf-8             # BẮT BUỘC: console Windows in được tiếng Việt khi seed
set DATABASE_URL=sqlite:///./nutrition.db
py -m uvicorn main:app --port 8001
```
- Kiểm tra sống: mở `http://localhost:8001/docs` (Swagger tự sinh của FastAPI).
- Lần chạy đầu tự tạo `nutrition.db` + seed 20 nguyên liệu / 21 công thức.

---

## 13. Câu hỏi hội đồng thường gặp — gợi ý trả lời

**Q: Vì sao tách Python riêng thay vì viết hết trong .NET?**
A: Tách *nutrition engine* thành microservice độc lập: thuật toán dinh dưỡng (WHO goals,
lọc dị ứng, tính toán USDA) thay đổi độc lập với nghiệp vụ chính; Python có hệ sinh thái
data tốt; hai service scale và deploy riêng (2 container trong docker-compose).

**Q: Dữ liệu dinh dưỡng lấy từ đâu?**
A: USDA FoodData Central (per-100g) — nạp qua endpoint ingest, có bộ seed offline 20 nguyên liệu.
Tổng dinh dưỡng mỗi ngày/mỗi công thức được **tính** từ gram nguyên liệu, không nhập tay.

**Q: Thực đơn có cá nhân hóa không?**
A: Bé — theo tuổi (texture + WHO goals theo tháng), cân nặng (<12 tháng), và **dị ứng** (lọc server-side).
Mẹ — theo **tam cá nguyệt** (3 bộ menu) và **xoay vòng theo tuần thai** (mỗi tuần một sắp xếp, deterministic).

**Q: Hai service dùng chung database có xung đột không?**
A: Không — bảng Python đều mang tiền tố `nutrition_`, và Python không đọc bảng của EF;
thông tin bé được .NET gửi qua request body (mỗi service sở hữu dữ liệu của mình).
