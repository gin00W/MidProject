#!/usr/bin/env python3
"""Draw the Chess AR board and paper pieces as a simple practice handout.

Coordinates are millimetres from the top-left of an A4 sheet. The registered
image is the whole page. Its 176 mm grid remains at the exact page centre,
matching ChessAR.Core.BoardLayout; changing the surrounding print layout must
not change the positions of any square.
"""
from __future__ import annotations

import argparse
import io
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from reportlab.lib.utils import ImageReader
from reportlab.pdfgen import canvas

ROOT = Path(__file__).resolve().parents[1]
SCALE = 10  # 254 dpi, exactly ten pixels per millimetre.
PAGE_W, PAGE_H = 210.0, 297.0
GRID_W = 176.0
CELL = GRID_W / 8
GRID_X = (PAGE_W - GRID_W) / 2
GRID_Y = (PAGE_H - GRID_W) / 2
PAPER = '#FFFFFF'
LIGHT = '#FFFFFF'
DARK = '#565656'
INK = '#252525'
MUTED = '#666666'
RULE = '#BBBBBB'
WHITE = '#FFFFFF'
FONT_ROOT = ROOT / 'Assets/SeongbukAR/Resources/Fonts'
FONT_REGULAR = FONT_ROOT / 'SUIT-Regular.otf'
FONT_BOLD = FONT_ROOT / 'SUIT-SemiBold.otf'


def px(mm: float) -> int:
    return round(mm * SCALE)


def font(mm: float, bold=False):
    return ImageFont.truetype(str(FONT_BOLD if bold else FONT_REGULAR), px(mm))


def text(draw, x, y, value, size, fill=INK, bold=False, anchor='lm'):
    draw.text((px(x), px(y)), value, font=font(size, bold), fill=fill, anchor=anchor)


def line(draw, points, fill=RULE, width=.18):
    draw.line([(px(x), px(y)) for x, y in points], fill=fill, width=max(1, px(width)))


def ellipse(draw, bounds, fill=None, outline=None, width=.25):
    draw.ellipse(tuple(px(v) for v in bounds), fill=fill, outline=outline, width=max(1, px(width)))


def rect(draw, bounds, fill=None, outline=None, width=.18):
    draw.rectangle(tuple(px(v) for v in bounds), fill=fill, outline=outline, width=max(1, px(width)))


def draw_target() -> Image.Image:
    page = Image.new('RGB', (px(PAGE_W), px(PAGE_H)), PAPER)
    d = ImageDraw.Draw(page)
    # Different functional headings and notation distinguish the page's edges
    # for image tracking without adding decorative marker patterns.
    text(d, 17, 22, '체스 연습판', 6.2, bold=True)
    text(d, 193, 22, '배치 · 이동 연습', 3.0, fill=MUTED, anchor='rm')
    text(d, 17, 34, '가상 말의 위치를 보고 종이 말을 놓아 보세요.', 3.2)
    text(d, 17, 46, 'K  킹     Q  퀸     R  룩     B  비숍     N  나이트     P  폰', 3.0, fill=MUTED)

    # Exact half-open 1760×1760 pixel region, with 220-pixel cells.
    for rank in range(8):
        for file in range(8):
            left = px(GRID_X + file * CELL)
            top = px(GRID_Y + (7 - rank) * CELL)
            color = DARK if (file + rank) % 2 == 0 else LIGHT
            d.rectangle((left, top, left + px(CELL) - 1, top + px(CELL) - 1), fill=color)
    rect(d, (GRID_X - .3, GRID_Y - .3, GRID_X + GRID_W + .3, GRID_Y + GRID_W + .3),
         outline=INK, width=.22)
    for file in range(8):
        x = GRID_X + (file + .5) * CELL
        text(d, x, GRID_Y - 5.0, chr(97 + file), 3.5, anchor='mm')
        text(d, x, GRID_Y + GRID_W + 5.0, chr(97 + file), 3.5, anchor='mm')
    for rank in range(8):
        y = GRID_Y + (7 - rank + .5) * CELL
        text(d, 9.8, y, str(rank + 1), 3.5, anchor='mm')
        text(d, 200.2, y, str(rank + 1), 3.5, anchor='mm')

    text(d, 105, 251, '흰 말이 놓이는 쪽', 3.3, anchor='mm')
    text(d, 17, 264, '처음 배치', 3.4, bold=True)
    text(d, 17, 272, '흰 말은 1·2줄, 검은 말은 7·8줄에서 시작합니다.', 3.0)
    text(d, 17, 279, '퀸은 d1·d8, 킹은 e1·e8에 놓습니다.', 3.0)
    text(d, 17, 290, 'A4 · 실제 크기 100%로 인쇄', 2.6, fill=MUTED)
    text(d, 193, 290, '한 칸 22mm · 페이지 전체를 비춰 주세요.', 2.6, fill=MUTED, anchor='rm')
    return page


PIECES = {'K': '킹', 'Q': '퀸', 'R': '룩', 'B': '비숍', 'N': '나이트', 'P': '폰'}
BACK_RANK = list('RNBQKBNR')


def draw_pieces() -> Image.Image:
    page = Image.new('RGB', (px(PAGE_W), px(PAGE_H)), PAPER)
    d = ImageDraw.Draw(page)
    text(d, 17, 22, '체스 말', 6.2, bold=True)
    text(d, 193, 22, '흰 말 16개 · 검은 말 16개', 3.0, fill=MUTED, anchor='rm')
    text(d, 17, 34, '사각형 바깥선을 따라 잘라 주세요. 글자가 위로 오도록 놓습니다.', 3.2)

    rows = [
        ('WHITE', BACK_RANK, 58.0), ('WHITE', ['P'] * 8, 84.0),
        ('BLACK', ['P'] * 8, 128.0), ('BLACK', BACK_RANK, 154.0),
    ]
    text(d, 17, 50, '흰 말', 3.4, bold=True)
    text(d, 17, 120, '검은 말', 3.4, bold=True)
    for side, types, y in rows:
        for col, letter in enumerate(types):
            x = 17 + col * 22
            rect(d, (x, y, x + 22, y + 22), outline=RULE, width=.16)
            black = side == 'BLACK'
            fill = DARK if black else WHITE
            ink = WHITE if black else INK
            ellipse(d, (x + 1, y + 1, x + 21, y + 21), fill=fill, outline=INK, width=.25)
            text(d, x + 11, y + 8.7, letter, 8.4, fill=ink, anchor='mm')
            text(d, x + 11, y + 15.6, PIECES[letter], 3.2, fill=ink, anchor='mm')

    text(d, 17, 197, '처음 놓는 순서', 3.6, bold=True)
    text(d, 17, 208, '뒷줄은 왼쪽부터 R  N  B  Q  K  B  N  R 순서입니다.', 3.2)
    text(d, 17, 217, '폰(P)은 뒷줄 바로 앞에 여덟 개씩 놓습니다.', 3.2)
    text(d, 17, 226, '퀸(Q)은 자기 색 칸에, 킹(K)은 바로 옆 칸에 놓습니다.', 3.2)
    text(d, 17, 235, '흰 퀸 d1 · 흰 킹 e1  /  검은 퀸 d8 · 검은 킹 e8', 3.0, fill=MUTED)
    text(d, 17, 255, '종이가 얇으면 두꺼운 종이에 붙여서 사용하세요.', 3.0, fill=MUTED)
    text(d, 17, 264, '가상 말의 위치와 비교하면서 한 단계씩 놓아 보세요.', 3.0, fill=MUTED)
    text(d, 17, 290, 'A4 · 실제 크기 100%로 인쇄', 2.6, fill=MUTED)
    text(d, 193, 290, '총 32개 · 한 변 22mm', 2.6, fill=MUTED, anchor='rm')
    return page


def write_a4_pdf(image: Image.Image, path: Path, title: str):
    mm_to_pt = 72 / 25.4
    buffer = io.BytesIO()
    image.save(buffer, format='PNG')
    buffer.seek(0)
    out = canvas.Canvas(str(path), pagesize=(PAGE_W * mm_to_pt, PAGE_H * mm_to_pt),
                        pageCompression=1, invariant=1)
    out.setTitle(title)
    out.setAuthor('Chess AR Learning')
    out.drawImage(ImageReader(buffer), 0, 0, width=PAGE_W * mm_to_pt, height=PAGE_H * mm_to_pt, mask='auto')
    out.showPage()
    out.save()


def verify_target(image):
    assert image.size == (2100, 2970)
    assert GRID_X == 17 and GRID_Y == 60.5
    assert GRID_X + GRID_W / 2 == 105 and GRID_Y + GRID_W / 2 == 148.5
    for rank in range(8):
        for file in range(8):
            p = (px(GRID_X + (file + .5) * CELL), px(GRID_Y + (7 - rank + .5) * CELL))
            expected = tuple(bytes.fromhex((DARK if (file + rank) % 2 == 0 else LIGHT)[1:]))
            assert image.getpixel(p) == expected, (file, rank, p, image.getpixel(p))
    assert len(BACK_RANK) * 2 + 16 == 32
    assert BACK_RANK[3:5] == ['Q', 'K']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--preview-dir', type=Path, help='Optional preview output outside the source assets.')
    args = parser.parse_args()
    target_dir = ROOT / 'Assets/ChessAR/Resources/Chess'
    docs_dir = ROOT / 'Docs/ChessAR'
    target_dir.mkdir(parents=True, exist_ok=True)
    docs_dir.mkdir(parents=True, exist_ok=True)
    board, pieces = draw_target(), draw_pieces()
    verify_target(board)
    board.save(target_dir / 'BoardTarget.png', dpi=(254, 254), optimize=True)
    write_a4_pdf(board, docs_dir / 'ChessBoard.pdf', '체스 연습판')
    write_a4_pdf(pieces, docs_dir / 'PaperPieces.pdf', '체스 말')
    if args.preview_dir:
        args.preview_dir.mkdir(parents=True, exist_ok=True)
        for name, image in [('board', board), ('pieces', pieces)]:
            preview = image.copy()
            preview.thumbnail((1050, 1485))
            preview.save(args.preview_dir / (name + '.png'))
    print('Generated simple A4 practice board and 32 paper pieces.')
    print('Verified: centred 176mm grid, 22mm cells, a1 dark and h1 light.')


if __name__ == '__main__':
    main()
