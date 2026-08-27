# One-shot renderer: overwrite WorkPlan_Leak.png at the confined document canvas size.
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "client" / "Assets" / "UIs" / "Things" / "Docs" / "WorkPlan_Leak.png"
REF = ROOT / "client" / "Assets" / "UIs" / "Things" / "Docs" / "WorkPlan.png"

NAVY = (3, 37, 93)
NAVY_TITLE = (8, 44, 101)
LINE = (198, 204, 214)
LABEL_BG = (3, 37, 93)
PAGE = (255, 255, 255)
TEXT = (28, 32, 38)
MUTED = (90, 98, 110)
CHECK = (160, 168, 180)

W, H = 1055, 1491
LEFT, RIGHT = 64, 989
CONTENT_W = RIGHT - LEFT


def font(path: str, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(path, size)


def load_fonts() -> dict[str, ImageFont.FreeTypeFont]:
    regular = r"C:\Windows\Fonts\malgun.ttf"
    bold = r"C:\Windows\Fonts\malgunbd.ttf"
    return {
        "title": font(bold, 42),
        "subtitle": font(regular, 22),
        "tag": font(regular, 16),
        "header": font(bold, 20),
        "label": font(bold, 15),
        "body": font(regular, 18),
        "small": font(regular, 16),
        "confirm": font(regular, 18),
        "date": font(regular, 17),
    }


def text_w(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont) -> int:
    box = draw.textbbox((0, 0), text, font=fnt)
    return box[2] - box[0]


def wrap(draw: ImageDraw.ImageDraw, text: str, fnt: ImageFont.FreeTypeFont, max_width: int) -> list[str]:
    lines: list[str] = []
    for paragraph in text.split("\n"):
        current = ""
        for ch in paragraph:
            trial = current + ch
            if text_w(draw, trial, fnt) <= max_width:
                current = trial
            else:
                if current:
                    lines.append(current)
                current = ch
        lines.append(current)
    return lines or [""]


def draw_header_bar(draw: ImageDraw.ImageDraw, y: int, label: str, fonts: dict) -> int:
    h = 36
    draw.rectangle((LEFT, y, RIGHT, y + h), fill=NAVY)
    draw.text((LEFT + 14, y + 5), label, font=fonts["header"], fill=(255, 255, 255))
    return y + h


def draw_checkbox(draw: ImageDraw.ImageDraw, x: int, y: int, size: int = 16) -> None:
    draw.rectangle((x, y, x + size, y + size), outline=CHECK, width=2)
    draw.rectangle((x + 1, y + 1, x + size - 1, y + size - 1), outline=(230, 234, 238))


def draw_check_columns(
    draw: ImageDraw.ImageDraw,
    y: int,
    left_items: list[str],
    right_items: list[str],
    fonts: dict,
) -> int:
    gap = 12
    col_w = (CONTENT_W - gap) // 2
    row_h = 36
    rows = max(len(left_items), len(right_items))
    bottom = y + rows * row_h + 8
    draw.rectangle((LEFT, y, RIGHT, bottom), outline=LINE, width=1)
    mid = LEFT + col_w
    draw.line((mid + gap // 2, y, mid + gap // 2, bottom), fill=LINE, width=1)

    def column(items: list[str], x0: int, x1: int) -> None:
        text_x = x0 + 28
        for i, item in enumerate(items):
            cy = y + 10 + i * row_h
            draw_checkbox(draw, x0 + 8, cy + 2)
            draw.text((text_x, cy), item, font=fonts["small"], fill=TEXT)

    column(left_items, LEFT, LEFT + col_w)
    column(right_items, LEFT + col_w + gap, RIGHT)
    return bottom


def draw_info_table(draw: ImageDraw.ImageDraw, y: int, cells: list[tuple[str, str]], fonts: dict) -> int:
    gap = 8
    col_w = (CONTENT_W - gap) // 2
    label_h = 20
    value_h = 26
    cell_h = label_h + value_h
    rows = (len(cells) + 1) // 2
    for i, (label, value) in enumerate(cells):
        row, col = divmod(i, 2)
        x0 = LEFT if col == 0 else LEFT + col_w + gap
        x1 = x0 + col_w
        cy = y + row * (cell_h + 4)
        draw.rectangle((x0, cy, x1, cy + label_h), fill=LABEL_BG)
        draw.text((x0 + 10, cy + 1), label, font=fonts["label"], fill=(255, 255, 255))
        draw.rectangle((x0, cy + label_h, x1, cy + cell_h), outline=LINE, width=1)
        draw.text((x0 + 10, cy + label_h + 3), value, font=fonts["body"], fill=TEXT)
    return y + rows * (cell_h + 4)


def main() -> None:
    ref = Image.open(REF)
    if ref.size != (W, H):
        raise SystemExit(f"Confined canvas changed: {ref.size}, expected {(W, H)}")

    fonts = load_fonts()
    img = Image.new("RGB", (W, H), PAGE)
    draw = ImageDraw.Draw(img)

    title = "산성 세정제 누출 대응 작업계획서"
    subtitle = "혼합기 A 산성 세정제 누출 시나리오"
    tag = "작업 전 확인 및 PPE 착용용"

    title_w = text_w(draw, title, fonts["title"])
    draw.text(((W - title_w) / 2, 40), title, font=fonts["title"], fill=NAVY_TITLE)
    sub_w = text_w(draw, subtitle, fonts["subtitle"])
    draw.text(((W - sub_w) / 2, 96), subtitle, font=fonts["subtitle"], fill=MUTED)
    tag_w = text_w(draw, tag, fonts["tag"])
    draw.text(((W - tag_w) / 2, 130), tag, font=fonts["tag"], fill=MUTED)

    # Header Y matches WorkPlan.png so both documents fill the same tablet plane.
    y = draw_header_bar(draw, 200, "1  |  작업 기본정보", fonts)
    draw_info_table(
        draw,
        y + 8,
        [
            ("문서번호", "CS-MIX-A-SPILL-001"),
            ("작업명", "산성 세정제 누출 대응"),
            ("작업장소", "산성 세정제 제조공정 / 혼합기 A 외부구역"),
            ("작업대상", "혼합기 A 하부 배관 연결부"),
            ("작업자", "김도윤"),
            ("감시인", "박성훈"),
        ],
        fonts,
    )

    y = draw_header_bar(draw, 394, "2  |  작업 내용", fonts)
    body = (
        "혼합기 A 외부구역 하부 배관 연결부에서 산성 세정제가 소량 누출된 상황을 가정하여, "
        "누출원 차단, 작업구역 통제, 환기, 흡착포 포설, 오염물 수거 및 폐기 절차를 수행한다."
    )
    box_top = y
    lines = wrap(draw, body, fonts["body"], CONTENT_W - 28)
    box_bottom = 500
    draw.rectangle((LEFT, box_top, RIGHT, box_bottom), outline=LINE, width=1)
    for i, line in enumerate(lines):
        draw.text((LEFT + 14, box_top + 10 + i * 26), line, font=fonts["body"], fill=TEXT)

    y = draw_header_bar(draw, 502, "3  |  주요 위험요인", fonts)
    draw_check_columns(
        draw,
        y,
        ["산성 세정제 피부·안구 접촉", "유해증기 흡입", "바닥 미끄러짐·전도"],
        ["누출 확산 및 2차 오염", "설비 잔압 재분출", "주변 설비 부식"],
        fonts,
    )

    y = draw_header_bar(draw, 686, "4  |  지정 보호구", fonts)
    draw_check_columns(
        draw,
        y,
        ["내화학성 방호복", "내화학성 장화", "내화학성 내부장갑"],
        ["내화학성 외부장갑", "화학보안경", "안면보호구", "호흡보호구"],
        fonts,
    )

    y = draw_header_bar(draw, 910, "5  |  현장 작업 전 실시사항", fonts)
    draw_check_columns(
        draw,
        y,
        ["설비 정지 및 잔압 차단(LOTO)", "누출원 확인", "작업구역 통제", "흡착포·중화제 준비"],
        ["강제환기 실시", "오염물 수거·폐기 준비", "감시인 배치"],
        fonts,
    )

    y = draw_header_bar(draw, 1126, "6  |  확인", fonts)
    confirm_top = y
    confirm_bottom = 1418
    draw.rectangle((LEFT, confirm_top, RIGHT, confirm_bottom), outline=LINE, width=1)
    mid = LEFT + CONTENT_W // 2
    split = confirm_top + (confirm_bottom - confirm_top) // 2
    draw.line((LEFT, split, RIGHT, split), fill=LINE, width=1)
    draw.line((mid, confirm_top, mid, confirm_bottom), fill=LINE, width=1)

    def confirm_block(x: int, role: str, name: str, top: int) -> None:
        draw.text((x + 14, top + 16), role, font=fonts["label"], fill=NAVY)
        draw.text((x + 14, top + 48), f"성명  {name}", font=fonts["confirm"], fill=TEXT)
        draw.text((x + 14, top + 84), "서명", font=fonts["confirm"], fill=TEXT)
        line_x = x + 58
        draw.line((line_x, top + 100, x + CONTENT_W // 2 - 24, top + 100), fill=CHECK, width=1)

    confirm_block(LEFT, "작업자 확인", "김도윤", confirm_top)
    confirm_block(mid, "감시인 확인", "박성훈", confirm_top)
    draw.text((LEFT + 14, split + 28), "확인일자  2026년 08월 18일", font=fonts["date"], fill=TEXT)

    img.save(OUT, format="PNG")
    written = Image.open(OUT)
    print(f"wrote {OUT} {written.size} mode={written.mode}")


if __name__ == "__main__":
    main()
