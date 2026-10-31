#!/usr/bin/env python3
"""Draw the original Chess AR image target and its printable paper pieces.

All coordinates are millimetres from the top-left of an A4 sheet. The image
tracking target is the entire A4 page, not the checkerboard alone. Its grid is
176 mm square at the exact page centre; ChessAR.Core BoardLayout uses this
same geometry. Assets are generated from geometry and bundled open fonts.
"""
from __future__ import annotations

import argparse
import io
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from reportlab.lib.utils import ImageReader
from reportlab.pdfgen import canvas

ROOT = Path(__file__).resolve().parents[1]
SCALE = 10  # 254 dpi: exactly ten pixels per millimetre.
PAGE_W, PAGE_H = 210.0, 297.0
GRID_W = 176.0
CELL = GRID_W / 8
GRID_X = (PAGE_W - GRID_W) / 2
GRID_Y = (PAGE_H - GRID_W) / 2
PAPER = '#FFFCF5'
LIGHT = '#F4EFE3'
DARK = '#454846'
INK = '#262C2A'
MUTED = '#666D66'
COPPER = '#A85534'
RULE = '#D8D0C1'
WHITE = '#FFFFFF'
FONT_ROOT = ROOT / 'Assets/SeongbukAR/Resources/Fonts'
FONT_REGULAR = FONT_ROOT / 'SUIT-Regular.otf'
FONT_BOLD = FONT_ROOT / 'SUIT-SemiBold.otf'


def px(mm: float) -> int:
    return round(mm * SCALE)


def font(mm: float, bold=False):
    return ImageFont.truetype(str(FONT_BOLD if bold else FONT_REGULAR), px(mm))


def text(draw, x, y, value, size, fill=INK, bold=False, anchor='mm'):
    draw.text((px(x), px(y)), value, font=font(size, bold), fill=fill, anchor=anchor)


def line(draw, points, fill=INK, width=.28):
    draw.line([(px(x), px(y)) for x, y in points], fill=fill, width=max(1, px(width)), joint='curve')


def ellipse(draw, bounds, fill=None, outline=None, width=.28):
    draw.ellipse(tuple(px(v) for v in bounds), fill=fill, outline=outline, width=max(1, px(width)))


def rect(draw, bounds, fill=None, outline=None, width=.28):
    draw.rectangle(tuple(px(v) for v in bounds), fill=fill, outline=outline, width=max(1, px(width)))


def icon(draw, kind, cx, cy, r=3.5, color=COPPER):
    """Small, deliberately different silhouettes and paths around the grid."""
    if kind == 'rook':
        line(draw, [(cx-r, cy+r), (cx+r, cy+r)], color, .6)
        line(draw, [(cx-r*.7, cy+r), (cx-r*.7, cy-r*.2),
                    (cx-r, cy-r*.2), (cx-r, cy-r),
                    (cx-r*.35, cy-r), (cx-r*.35, cy-r*.45),
                    (cx+r*.3, cy-r*.45), (cx+r*.3, cy-r),
                    (cx+r, cy-r), (cx+r, cy-r*.2),
                    (cx+r*.7, cy-r*.2), (cx+r*.7, cy+r)], color, .55)
    elif kind == 'knight':
        line(draw, [(cx-r, cy+r*.7), (cx+r*.6, cy+r*.7),
                    (cx+r*.6, cy-r*.65), (cx-r*.55, cy-r*.65)], color, .6)
        ellipse(draw, (cx-r-1, cy+r*.7-1, cx-r+1, cy+r*.7+1), fill=color)
        draw.polygon([(px(cx-r*.65), px(cy-r*.65)),
                      (px(cx-r*.05), px(cy-r*1.12)),
                      (px(cx-r*.05), px(cy-r*.18))], fill=color)
    elif kind == 'queen':
        line(draw, [(cx-r,cy-r*.35), (cx-r*.65,cy+r*.55),
                    (cx+r*.65,cy+r*.55), (cx+r,cy-r*.35),
                    (cx+r*.4,cy), (cx,cy-r),
                    (cx-r*.4,cy), (cx-r,cy-r*.35)], color, .55)
        line(draw, [(cx-r*.75,cy+r), (cx+r*.75,cy+r)], color, .65)
        for ox, oy in [(-1,-.35),(0,-1),(1,-.35)]:
            ellipse(draw, (cx+r*ox-.5,cy+r*oy-.5,cx+r*ox+.5,cy+r*oy+.5),fill=color)
    else:
        ellipse(draw, (cx-r*.5,cy-r,cx+r*.5,cy), outline=color, width=.55)
        line(draw, [(cx,cy), (cx,cy+r*.7)], color, .55)
        line(draw, [(cx-r*.65,cy+r*.75),(cx+r*.65,cy+r*.75)], color, .6)
        line(draw, [(cx-r,cy+r*1.1),(cx+r,cy+r*1.1)], color, .6)


def feature_trace(draw, x, y, seed):
    # Stable, non-repeating route ornaments strengthen the image perimeter
    # without filling the playing area or imitating a QR code.
    rng = random.Random(seed)
    rows = list(range(3))
    rng.shuffle(rows)
    nodes = [(x + i*3.2, y + rows[i%3]*3.0) for i in range(4)]
    route = [nodes[0]]
    for previous, current in zip(nodes,nodes[1:]):
        route.extend([(current[0],previous[1]),current])
    line(draw,route,fill='#B6A08D',width=.27)
    for i,(nx,ny) in enumerate(nodes):
        if i==rng.randrange(4):
            rect(draw,(nx-.65,ny-.65,nx+.65,ny+.65),fill=COPPER)
        else:
            ellipse(draw,(nx-.45,ny-.45,nx+.45,ny+.45),fill=INK)


def panel(draw, x, y, width, number, heading, detail, kind, accent):
    rect(draw, (x,y,x+width,y+14), fill=accent, outline=RULE, width=.18)
    icon(draw, kind, x+7.0, y+6.5, r=3.1)
    text(draw, x+14,y+4.6,number+'  '+heading,3.05,bold=True,anchor='lm')
    text(draw, x+14,y+9.8,detail,2.5,fill=MUTED,anchor='lm')
    feature_trace(draw,x+width-15,y+4,730+int(number))


def draw_target() -> Image.Image:
    page = Image.new('RGB', (px(PAGE_W), px(PAGE_H)), PAPER)
    d = ImageDraw.Draw(page)
    # These asymmetric title/pictogram areas provide distinctive image
    # features beyond the repetitive 8×8 checker pattern.
    line(d, [(17,9),(45,9)], COPPER, .75)
    text(d, 105,20,'CHESS FIRST MOVE',10.2,bold=True)
    text(d, 105,31.5,'그림 위에서 배우는 체스의 첫 배치와 이동',3.6,fill=MUTED)
    panel(d,17,39,84,'01','SET UP','처음 놓는 자리 배우기','rook','#F4E5D7')
    panel(d,109,39,84,'02','MOVE','말의 이동 방향 살펴보기','knight','#EAECE4')

    # Pixel ranges are half-open: exact 1760×1760 grid with 220-pixel cells.
    for rank in range(8):
        for file in range(8):
            left = px(GRID_X + file*CELL)
            top = px(GRID_Y + (7-rank)*CELL)
            color = DARK if (file+rank)%2 == 0 else LIGHT
            d.rectangle((left,top,left+px(CELL)-1,top+px(CELL)-1),fill=color)
    # A thin outside keyline does not alter the square centres.
    rect(d,(GRID_X-.4,GRID_Y-.4,GRID_X+GRID_W+.4,GRID_Y+GRID_W+.4),outline=INK,width=.3)
    for file in range(8):
        x=GRID_X+(file+.5)*CELL
        text(d,x,GRID_Y-5.2,chr(97+file),3.5,bold=True)
        text(d,x,GRID_Y+GRID_W+5.2,chr(97+file),3.5,bold=True)
    for rank in range(8):
        y=GRID_Y+(7-rank+.5)*CELL
        text(d,9.8,y,str(rank+1),3.5,bold=True)
        text(d,200.2,y,str(rank+1),3.5,bold=True)

    text(d,105,249.5,'WHITE STARTS HERE  ·  흰 말이 시작하는 쪽',3.2,bold=True)
    panel(d,17,258,84,'03','LOOK','판 전체를 비춰 주세요','queen','#EEE9E0')
    panel(d,109,258,84,'04','TRY','종이 말로 직접 따라 놓기','pawn','#F4E5D7')
    line(d,[(17,279),(193,279)],RULE,.2)
    text(d,17,285,'A4 · 100% / 실제 크기로 인쇄',2.55,fill=MUTED,anchor='lm')
    text(d,193,285,'전체 210 × 297 mm / 체스판 176 × 176 mm',2.55,fill=MUTED,anchor='rm')
    text(d,105,291,'AR 인식 대상: 이 페이지 전체  |  오른쪽 아래 h1 칸은 밝은색입니다.',2.55,fill=MUTED)
    return page


PIECES = {
    'K':('킹','KING'), 'Q':('퀸','QUEEN'), 'R':('룩','ROOK'),
    'B':('비숍','BISHOP'), 'N':('나이트','KNIGHT'), 'P':('폰','PAWN')
}
BACK_RANK = list('RNBQKBNR')


def draw_pieces() -> Image.Image:
    page=Image.new('RGB',(px(PAGE_W),px(PAGE_H)),PAPER)
    d=ImageDraw.Draw(page)
    line(d,[(17,9),(45,9)],COPPER,.75)
    text(d,105,23,'PAPER CHESS PIECES',9.4,bold=True)
    text(d,105,36,'흰 말 16개 + 검은 말 16개 · 총 32개',4,fill=MUTED)
    text(d,105,47,'22mm 사각형 바깥선을 따라 자르세요. 글자가 위로 보이게 사용합니다.',3.2,fill=MUTED)
    top=65.0
    rows=[('WHITE',BACK_RANK),('WHITE',['P']*8),('BLACK',['P']*8),('BLACK',BACK_RANK)]
    for row,(side,types) in enumerate(rows):
        y=top+row*26
        for col,letter in enumerate(types):
            x=17+col*22
            rect(d,(x,y,x+22,y+22),outline=RULE,width=.18)
            black=side=='BLACK'
            fill=DARK if black else LIGHT
            ink=WHITE if black else INK
            ellipse(d,(x+1,y+1,x+21,y+21),fill=fill,outline=INK,width=.3)
            text(d,x+11,y+8.2,letter,8.2,fill=ink,bold=True)
            korean,english=PIECES[letter]
            text(d,x+11,y+15.1,korean,3.0,fill=ink,bold=True)
            text(d,x+11,y+18.7,english,2.1,fill=ink)
    line(d,[(17,177),(193,177)],RULE,.25)
    text(d,17,185,'처음 배치할 때',4.4,bold=True,anchor='lm')
    lines=[
        '흰 말은 1·2줄, 검은 말은 7·8줄에서 시작합니다.',
        '뒷줄은 왼쪽부터 R  N  B  Q  K  B  N  R 순서입니다.',
        '퀸(Q)은 자기 색 칸에, 킹(K)은 바로 옆 칸에 놓습니다.',
        '화이트: 퀸 d1 · 킹 e1  /  블랙: 퀸 d8 · 킹 e8',
    ]
    for index,value in enumerate(lines):
        text(d,17,195+index*8,value,3.4,fill=MUTED,anchor='lm')
    text(d,17,240,'작은 종이 말을 잡기 어렵다면',4.1,bold=True,anchor='lm')
    text(d,17,250,'두꺼운 종이에 붙여 자르거나, 같은 크기의 동전·병뚜껑 위에 올려 사용하세요.',3.1,fill=MUTED,anchor='lm')
    text(d,17,259,'추가 보드게임을 살 필요 없이 체스판 한 장과 이 종이 말로 시연할 수 있습니다.',3.1,fill=MUTED,anchor='lm')
    line(d,[(17,279),(193,279)],RULE,.2)
    text(d,17,286,'A4 · 100% / 실제 크기로 인쇄',2.7,fill=MUTED,anchor='lm')
    text(d,193,286,'종이 말은 AR 인식 대상이 아닙니다.',2.7,fill=MUTED,anchor='rm')
    return page


def write_a4_pdf(image: Image.Image, path: Path, title: str):
    mm_to_pt=72/25.4
    page_w,page_h=PAGE_W*mm_to_pt,PAGE_H*mm_to_pt
    buffer=io.BytesIO()
    image.save(buffer,format='PNG')
    buffer.seek(0)
    out=canvas.Canvas(str(path),pagesize=(page_w,page_h),pageCompression=1,invariant=1)
    out.setTitle(title)
    out.setAuthor('Chess AR Learning')
    out.drawImage(ImageReader(buffer),0,0,width=page_w,height=page_h,mask='auto')
    out.showPage()
    out.save()


def verify_target(image):
    assert image.size==(2100,2970)
    assert GRID_X==17 and GRID_Y==60.5
    assert GRID_X+GRID_W/2==105 and GRID_Y+GRID_W/2==148.5
    # Every sample cell centre must agree with the runtime's file/rank model.
    for rank in range(8):
        for file in range(8):
            p=(px(GRID_X+(file+.5)*CELL),px(GRID_Y+(7-rank+.5)*CELL))
            expected=tuple(bytes.fromhex((DARK if (file+rank)%2==0 else LIGHT)[1:]))
            assert image.getpixel(p)==expected,(file,rank,p,image.getpixel(p))
    assert len(BACK_RANK)*2+16==32
    assert BACK_RANK[3:5]==['Q','K']


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--preview-dir',type=Path,help='Optional preview output directory, outside the source assets.')
    args=parser.parse_args()
    target_dir=ROOT/'Assets/ChessAR/Resources/Chess'
    docs_dir=ROOT/'Docs/ChessAR'
    target_dir.mkdir(parents=True,exist_ok=True)
    docs_dir.mkdir(parents=True,exist_ok=True)
    board=draw_target()
    pieces=draw_pieces()
    verify_target(board)
    board.save(target_dir/'BoardTarget.png',dpi=(254,254),optimize=True)
    write_a4_pdf(board,docs_dir/'ChessBoard.pdf','Chess AR — full A4 image target')
    write_a4_pdf(pieces,docs_dir/'PaperPieces.pdf','Chess AR — 32 paper pieces')
    if args.preview_dir:
        args.preview_dir.mkdir(parents=True,exist_ok=True)
        for name,image in [('board',board),('pieces',pieces)]:
            copy=image.copy()
            copy.thumbnail((1050,1485))
            copy.save(args.preview_dir/(name+'.png'))
    print('Generated 2100×2970 target, exact full-A4 board PDF and 32 paper pieces.')
    print('Geometry verified: 176mm grid centred at (105,148.5)mm; a1 dark, h1 light; 22mm cells.')


if __name__=='__main__':
    main()
