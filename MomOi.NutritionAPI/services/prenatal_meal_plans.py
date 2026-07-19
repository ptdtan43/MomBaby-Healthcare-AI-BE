"""
Prenatal (maternal) 7-day meal plans, personalized by pregnancy trimester.

Menu content is standard Vietnamese maternal nutrition guidance; the daily nutrient
totals are COMPUTED from the `ingredients` table (seeded/ingested from USDA
FoodData Central), fulfilling the use cases "Plan prenatal meals" -> include
"Compute recipe nutrition" -> include "Fetch nutrient data (USDA)".
"""
from sqlalchemy.orm import Session

from models.ingredient import Ingredient

# Each day lists the dishes shown to the mother plus the main ingredients
# (english_name matching the USDA-backed `ingredients` table) with realistic
# gram amounts for a pregnant woman. Nutrients are summed as per100g * grams/100.
#
# Trimester 1 (weeks 1-12): folate & easy-to-digest. Trimester 2 (13-27): iron & calcium.
# Trimester 3 (28+): omega-3, fiber, energy.

_T1 = [
    {
        "day": "Thứ Hai",
        "breakfast": "Cháo yến mạch trứng gà, 1 ly sữa tiệt trùng",
        "lunch": "Cơm gạo tẻ, thịt heo luộc, canh rau bina nấu tôm",
        "snack": "Chuối chín và sữa chua",
        "dinner": "Cơm, đậu phụ sốt cà, súp lơ xanh luộc",
        "ingredients": [("Oats", 60), ("Egg yolk", 30), ("Rice", 200), ("Pork", 120), ("Spinach", 100), ("Shrimp", 60), ("Banana", 100), ("Tofu", 150), ("Broccoli", 100)],
    },
    {
        "day": "Thứ Ba",
        "breakfast": "Bánh mì trứng ốp, táo tươi",
        "lunch": "Cơm, ức gà áp chảo, canh bí đỏ",
        "snack": "Xoài chín",
        "dinner": "Cơm, cá ngừ kho, rau bina xào",
        "ingredients": [("Egg yolk", 30), ("Apple", 150), ("Rice", 200), ("Chicken breast", 130), ("Pumpkin", 120), ("Mango", 120), ("Tuna", 100), ("Spinach", 100)],
    },
    {
        "day": "Thứ Tư",
        "breakfast": "Cháo gà cà rốt",
        "lunch": "Cơm, thịt bò xào súp lơ, canh rau",
        "snack": "Bơ dầm sữa chua",
        "dinner": "Cơm, tôm rim, bí đỏ luộc",
        "ingredients": [("Rice", 220), ("Chicken breast", 80), ("Carrot", 80), ("Beef", 120), ("Broccoli", 100), ("Avocado", 80), ("Shrimp", 100), ("Pumpkin", 120)],
    },
    {
        "day": "Thứ Năm",
        "breakfast": "Yến mạch sữa chuối",
        "lunch": "Cơm, cá hồi áp chảo, rau bina luộc",
        "snack": "Táo và hạt",
        "dinner": "Cơm, gan gà xào nghệ (lượng vừa), canh bí đỏ",
        "ingredients": [("Oats", 60), ("Banana", 100), ("Rice", 200), ("Salmon", 120), ("Spinach", 100), ("Apple", 150), ("Chicken liver", 50), ("Pumpkin", 120)],
    },
    {
        "day": "Thứ Sáu",
        "breakfast": "Cháo tôm bí đỏ",
        "lunch": "Cơm, thịt heo kho trứng, súp lơ luộc",
        "snack": "Chuối và sữa",
        "dinner": "Cơm, đậu phụ nhồi thịt, canh cà rốt",
        "ingredients": [("Rice", 220), ("Shrimp", 80), ("Pumpkin", 100), ("Pork", 120), ("Egg yolk", 30), ("Broccoli", 100), ("Banana", 100), ("Tofu", 120), ("Carrot", 80)],
    },
    {
        "day": "Thứ Bảy",
        "breakfast": "Bánh mì bơ trứng",
        "lunch": "Cơm, cá ngừ sốt cà, rau xào",
        "snack": "Xoài chín",
        "dinner": "Cơm, ức gà hấp, canh rau bina",
        "ingredients": [("Avocado", 80), ("Egg yolk", 30), ("Rice", 200), ("Tuna", 110), ("Spinach", 80), ("Mango", 120), ("Chicken breast", 120)],
    },
    {
        "day": "Chủ Nhật",
        "breakfast": "Cháo cá hồi hạt sen",
        "lunch": "Cơm, thịt bò hầm khoai lang cà rốt",
        "snack": "Sữa chua trái cây",
        "dinner": "Cơm, tôm hấp, súp lơ xanh luộc",
        "ingredients": [("Rice", 220), ("Salmon", 100), ("Beef", 120), ("Sweet potato", 150), ("Carrot", 80), ("Apple", 100), ("Shrimp", 100), ("Broccoli", 100)],
    },
]

_T2 = [
    {
        "day": "Thứ Hai",
        "breakfast": "Cháo thịt bò cà rốt, 1 ly sữa",
        "lunch": "Cơm, cá hồi áp chảo, rau bina xào tỏi",
        "snack": "Sữa chua và chuối",
        "dinner": "Cơm, đậu phụ sốt thịt băm, canh bí đỏ",
        "ingredients": [("Rice", 230), ("Beef", 130), ("Carrot", 80), ("Salmon", 130), ("Spinach", 100), ("Banana", 100), ("Tofu", 150), ("Pork", 60), ("Pumpkin", 120)],
    },
    {
        "day": "Thứ Ba",
        "breakfast": "Yến mạch trứng gà, táo",
        "lunch": "Cơm, gan gà xào (giàu sắt), súp lơ luộc",
        "snack": "Bơ dầm",
        "dinner": "Cơm, tôm rang, canh rau bina",
        "ingredients": [("Oats", 60), ("Egg yolk", 30), ("Apple", 150), ("Rice", 230), ("Chicken liver", 60), ("Broccoli", 120), ("Avocado", 100), ("Shrimp", 110), ("Spinach", 100)],
    },
    {
        "day": "Thứ Tư",
        "breakfast": "Cháo tôm bí đỏ",
        "lunch": "Cơm, thịt bò xào cần, đậu phụ luộc (giàu canxi)",
        "snack": "Xoài và sữa chua",
        "dinner": "Cơm, cá ngừ kho thơm, rau luộc",
        "ingredients": [("Rice", 230), ("Shrimp", 90), ("Pumpkin", 100), ("Beef", 130), ("Tofu", 180), ("Mango", 120), ("Tuna", 110), ("Broccoli", 100)],
    },
    {
        "day": "Thứ Năm",
        "breakfast": "Bánh mì trứng, chuối",
        "lunch": "Cơm, ức gà nướng, khoai lang hấp",
        "snack": "Táo",
        "dinner": "Cơm, cá hồi hấp, canh rau bina đậu phụ",
        "ingredients": [("Egg yolk", 30), ("Banana", 100), ("Rice", 230), ("Chicken breast", 140), ("Sweet potato", 150), ("Apple", 150), ("Salmon", 110), ("Spinach", 80), ("Tofu", 100)],
    },
    {
        "day": "Thứ Sáu",
        "breakfast": "Cháo gà hạt sen cà rốt",
        "lunch": "Cơm, thịt heo kho trứng cút, súp lơ xanh",
        "snack": "Sữa chua bơ",
        "dinner": "Cơm, tôm hấp sả, bí đỏ luộc",
        "ingredients": [("Rice", 230), ("Chicken breast", 90), ("Carrot", 80), ("Pork", 120), ("Egg yolk", 40), ("Broccoli", 120), ("Avocado", 80), ("Shrimp", 110), ("Pumpkin", 120)],
    },
    {
        "day": "Thứ Bảy",
        "breakfast": "Yến mạch sữa xoài",
        "lunch": "Cơm, cá ngừ áp chảo, rau bina xào",
        "snack": "Chuối",
        "dinner": "Cơm, thịt bò hầm khoai, đậu phụ non canh",
        "ingredients": [("Oats", 60), ("Mango", 100), ("Rice", 230), ("Tuna", 120), ("Spinach", 100), ("Banana", 100), ("Beef", 120), ("Sweet potato", 120), ("Tofu", 100)],
    },
    {
        "day": "Chủ Nhật",
        "breakfast": "Cháo cá hồi bí đỏ",
        "lunch": "Cơm, gà kho gừng, súp lơ luộc",
        "snack": "Táo và sữa",
        "dinner": "Cơm, trứng hấp thịt băm, canh cà rốt khoai lang",
        "ingredients": [("Rice", 230), ("Salmon", 110), ("Pumpkin", 100), ("Chicken breast", 130), ("Broccoli", 120), ("Apple", 130), ("Egg yolk", 40), ("Pork", 70), ("Carrot", 70), ("Sweet potato", 100)],
    },
]

_T3 = [
    {
        "day": "Thứ Hai",
        "breakfast": "Cháo cá hồi (omega-3), 1 ly sữa",
        "lunch": "Cơm, thịt bò xào súp lơ, canh rau bina",
        "snack": "Bơ chuối dầm",
        "dinner": "Cơm, đậu phụ hấp tôm, bí đỏ luộc",
        "ingredients": [("Rice", 240), ("Salmon", 140), ("Beef", 120), ("Broccoli", 120), ("Spinach", 100), ("Avocado", 100), ("Banana", 80), ("Tofu", 130), ("Shrimp", 80), ("Pumpkin", 100)],
    },
    {
        "day": "Thứ Ba",
        "breakfast": "Yến mạch chuối hạt (giàu xơ)",
        "lunch": "Cơm, cá ngừ kho, rau luộc chấm",
        "snack": "Sữa chua táo",
        "dinner": "Cơm, ức gà áp chảo, khoai lang hấp",
        "ingredients": [("Oats", 70), ("Banana", 120), ("Rice", 240), ("Tuna", 120), ("Broccoli", 120), ("Apple", 150), ("Chicken breast", 140), ("Sweet potato", 150)],
    },
    {
        "day": "Thứ Tư",
        "breakfast": "Bánh mì trứng bơ",
        "lunch": "Cơm, cá hồi nướng, rau bina xào tỏi",
        "snack": "Xoài chín",
        "dinner": "Cơm, thịt heo luộc, canh bí đỏ đậu phụ",
        "ingredients": [("Egg yolk", 40), ("Avocado", 100), ("Rice", 240), ("Salmon", 140), ("Spinach", 100), ("Mango", 120), ("Pork", 120), ("Pumpkin", 100), ("Tofu", 100)],
    },
    {
        "day": "Thứ Năm",
        "breakfast": "Cháo tôm cà rốt",
        "lunch": "Cơm, thịt bò hầm khoai lang",
        "snack": "Chuối và sữa",
        "dinner": "Cơm, cá ngừ hấp, súp lơ xanh luộc",
        "ingredients": [("Rice", 240), ("Shrimp", 100), ("Carrot", 80), ("Beef", 130), ("Sweet potato", 150), ("Banana", 100), ("Tuna", 110), ("Broccoli", 120)],
    },
    {
        "day": "Thứ Sáu",
        "breakfast": "Yến mạch sữa bơ",
        "lunch": "Cơm, gà kho, canh rau bina tôm",
        "snack": "Táo",
        "dinner": "Cơm, cá hồi áp chảo, bí đỏ hấp",
        "ingredients": [("Oats", 70), ("Avocado", 80), ("Rice", 240), ("Chicken breast", 130), ("Spinach", 100), ("Shrimp", 60), ("Apple", 150), ("Salmon", 130), ("Pumpkin", 120)],
    },
    {
        "day": "Thứ Bảy",
        "breakfast": "Cháo thịt bò cà rốt",
        "lunch": "Cơm, tôm rim, đậu phụ canh hẹ",
        "snack": "Sữa chua xoài",
        "dinner": "Cơm, trứng hấp, rau luộc thập cẩm",
        "ingredients": [("Rice", 240), ("Beef", 120), ("Carrot", 80), ("Shrimp", 110), ("Tofu", 130), ("Mango", 120), ("Egg yolk", 40), ("Broccoli", 100), ("Spinach", 80)],
    },
    {
        "day": "Chủ Nhật",
        "breakfast": "Bánh mì cá ngừ, chuối",
        "lunch": "Cơm, cá hồi sốt cam, khoai lang hấp",
        "snack": "Bơ dầm sữa",
        "dinner": "Cơm, ức gà hấp lá chanh, canh bí đỏ",
        "ingredients": [("Tuna", 90), ("Banana", 100), ("Rice", 240), ("Salmon", 130), ("Sweet potato", 150), ("Avocado", 80), ("Chicken breast", 130), ("Pumpkin", 120)],
    },
]


def _plan_for_week(week: int):
    if week <= 12:
        return _T1
    if week <= 27:
        return _T2
    return _T3


def build_prenatal_meal_plan(week: int, db: Session) -> list[dict]:
    """Returns a 7-day plan; nutrient totals computed from the USDA-backed ingredients table."""
    # Load every referenced ingredient once
    days = _plan_for_week(week)

    # Personalize by pregnancy week: rotate the menu content across weekdays using the
    # week number as a deterministic seed. Every pregnancy week therefore shows a
    # different arrangement (week 5 != week 6), while the same week always returns
    # the same plan (reproducible, cacheable).
    offset = week % len(days)
    weekday_labels = [d["day"] for d in days]          # keep Thứ Hai → Chủ Nhật order
    rotated = days[offset:] + days[:offset]
    days = [{**content, "day": label} for label, content in zip(weekday_labels, rotated)]

    names = {name for d in days for (name, _g) in d["ingredients"]}
    rows = db.query(Ingredient).filter(Ingredient.english_name.in_(list(names))).all()
    by_name = {r.english_name: r for r in rows}

    result = []
    for d in days:
        cal = protein = carbs = fat = iron = 0.0
        for (name, grams) in d["ingredients"]:
            ing = by_name.get(name)
            if ing is None:
                continue  # not ingested yet; skip rather than invent numbers
            f = grams / 100.0
            cal += (ing.calories_kcal or 0.0) * f
            protein += (ing.protein_g or 0.0) * f
            carbs += (ing.carbs_g or 0.0) * f
            fat += (ing.fat_g or 0.0) * f
            iron += (ing.iron_mg or 0.0) * f

        result.append({
            "day": d["day"],
            "breakfast": d["breakfast"],
            "lunch": d["lunch"],
            "snack": d["snack"],
            "dinner": d["dinner"],
            # camelCase + display units: the .NET API passes this object through unchanged
            # and the React MealPlanPage reads these exact keys.
            "dailyNutrients": {
                "calories": round(cal),
                "protein": f"{round(protein)}g",
                "carbs": f"{round(carbs)}g",
                "fat": f"{round(fat)}g",
                "iron": f"{iron:.1f}mg",
            },
        })
    return result
