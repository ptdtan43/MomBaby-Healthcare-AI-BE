"""Map free-text allergy names (Vietnamese from the web/mobile apps, or English) onto the
allergen codes used in Recipe.allergens ("egg", "fish", "shellfish", ...).

Matching is on whole words with diacritics kept: in Vietnamese "cá" (fish) and "cà"
(eggplant), or "mè" (sesame) and "me" (tamarind), are different words.
"""
import re
import unicodedata

ALLERGEN_KEYWORDS: dict[str, tuple[str, ...]] = {
    # Vietnamese
    "trứng": ("egg",),
    "sữa": ("milk",),
    "cá": ("fish",),
    "hải sản": ("fish", "shellfish"),
    "tôm": ("shellfish",),
    "cua": ("shellfish",),
    "ghẹ": ("shellfish",),
    "sò": ("shellfish",),
    "nghêu": ("shellfish",),
    "ngao": ("shellfish",),
    "hến": ("shellfish",),
    "ốc": ("shellfish",),
    "mực": ("shellfish",),
    "bạch tuộc": ("shellfish",),
    "giáp xác": ("shellfish",),
    "nhuyễn thể": ("shellfish",),
    "đậu phộng": ("peanut",),
    "lạc": ("peanut",),
    "đậu nành": ("soy",),
    "lúa mì": ("wheat",),
    "bột mì": ("wheat",),
    "gluten": ("wheat",),
    "hạt điều": ("tree_nut",),
    "óc chó": ("tree_nut",),
    "hạnh nhân": ("tree_nut",),
    "mắc ca": ("tree_nut",),
    "vừng": ("sesame",),
    "mè": ("sesame",),
    # English words that are not themselves codes
    "seafood": ("fish", "shellfish"),
    "shrimp": ("shellfish",),
    "prawn": ("shellfish",),
    "crab": ("shellfish",),
    "lobster": ("shellfish",),
    "peanuts": ("peanut",),
    "eggs": ("egg",),
    "dairy": ("milk",),
}

_PATTERNS = [
    (re.compile(rf"(?<!\w){re.escape(keyword)}(?!\w)"), codes)
    for keyword, codes in ALLERGEN_KEYWORDS.items()
]


def normalize_allergies(raw: list[str] | None) -> set[str]:
    """Return the allergen codes named by `raw`. Each original value is kept too
    (lower-cased), so inputs that are already codes keep working."""
    codes: set[str] = set()
    for item in raw or []:
        # NFC: phone keyboards may send decomposed diacritics.
        text = unicodedata.normalize("NFC", str(item)).strip().lower()
        if not text:
            continue
        codes.add(text)
        for pattern, mapped in _PATTERNS:
            if pattern.search(text):
                codes.update(mapped)
    return codes
