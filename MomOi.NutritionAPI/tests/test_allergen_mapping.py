import unicodedata
from datetime import date

import pytest
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker

from database import Base
from models.baby_profile import BabyProfile
from models.recipe import Recipe
from services.allergen_mapping import normalize_allergies
from services.recommendation_engine import recommend_daily_menu

engine = create_engine("sqlite:///:memory:", connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)


@pytest.fixture
def db_session():
    Base.metadata.create_all(bind=engine)
    session = SessionLocal()
    try:
        yield session
    finally:
        session.close()
        Base.metadata.drop_all(bind=engine)


@pytest.mark.parametrize("raw, expected", [
    (["Hải sản"], {"fish", "shellfish"}),
    (["Trứng gà"], {"egg"}),
    (["Sữa bò"], {"milk"}),
    (["Cá"], {"fish"}),
    (["Tôm, cua"], {"shellfish"}),
    (["Đậu phộng"], {"peanut"}),
    (["Lạc"], {"peanut"}),
    (["Đậu nành"], {"soy"}),
    (["Gluten"], {"wheat"}),
    (["egg"], {"egg"}),
    (["Seafood"], {"fish", "shellfish"}),
])
def test_normalize_maps_names_to_codes(raw, expected):
    assert expected <= normalize_allergies(raw)


@pytest.mark.parametrize("raw", [["Cà chua"], ["Me"], ["Cam"]])
def test_normalize_keeps_diacritics_and_whole_words(raw):
    # "cà" (eggplant/tomato), "me" (tamarind) and "cam" must not be read as fish/sesame.
    assert normalize_allergies(raw) & {"fish", "sesame"} == set()


def test_normalize_handles_decomposed_unicode():
    decomposed = unicodedata.normalize("NFD", "Hải sản")
    assert {"fish", "shellfish"} <= normalize_allergies([decomposed])


def _recipe(name, meal_type, allergens):
    return Recipe(
        name_vi=name, name_en=name, meal_type=meal_type, texture="soft_solid",
        min_age_months=6, max_age_months=36, total_calories=100, total_protein_g=5,
        total_fat_g=3, total_carbs_g=12, total_iron_mg=1, total_zinc_mg=1, allergens=allergens,
    )


def test_vietnamese_allergy_filters_menu_and_never_falls_back_to_allergens(db_session):
    safe_lunch = _recipe("Cháo bí đỏ", "lunch", [])
    fish_lunch = _recipe("Cơm nát cá ngừ đại dương", "lunch", ["fish"])
    shrimp_dinner = _recipe("Cháo tôm", "dinner", ["shellfish"])
    db_session.add_all([safe_lunch, fish_lunch, shrimp_dinner])
    db_session.commit()

    today = date.today()
    baby = BabyProfile(
        user_id="u1", baby_name="Bin", date_of_birth=date(today.year - 1, today.month, 1),
        gender="male", current_weight_kg=11.0, allergies=["Hải sản"], food_history=[],
    )

    for _ in range(10):
        menu = recommend_daily_menu(baby, db_session)
        assert menu.meals["lunch"].id == safe_lunch.id
        # The only dinner contains shellfish: the slot must stay empty rather than serve it.
        assert "dinner" not in menu.meals
