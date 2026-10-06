# Накладывает на референс карты миссии 1 метки: пути врагов, спавны, зоны, ресурсы.
# Координаты — в пикселях исходной картинки 1536x1024.
# Запуск: python3 game/tools/annotate_map.py <вход.webp> <выход.png>
import sys, math
from PIL import Image, ImageDraw, ImageFont

SRC, OUT = sys.argv[1], sys.argv[2]
K = 2  # суперсэмплинг для гладких линий
base = Image.open(SRC).convert("RGBA")
W, H = base.size
LEG = 220
ov = Image.new("RGBA", (W * K, (H + LEG) * K), (0, 0, 0, 0))
d = ImageDraw.Draw(ov)
FB = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
def font(s): return ImageFont.truetype(FB, int(s * K))
P = lambda pts: [(x * K, y * K) for x, y in pts]

C = dict(main=(235, 50, 50), bridge=(255, 150, 20), ford=(255, 210, 40), forest=(30, 190, 70),
         secret=(170, 70, 235), death=(230, 30, 30), build=(40, 190, 230), res=(255, 235, 60), egg=(255, 200, 0))

def arrow(pts, col, w=9, dash=False):
    pts = P(pts)
    if dash:
        for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
            L = math.hypot(x2 - x1, y2 - y1); n = int(L / (26 * K))
            for i in range(0, n, 2):
                a, b = i / max(n, 1), min((i + 1) / max(n, 1), 1)
                d.line([(x1 + (x2 - x1) * a, y1 + (y2 - y1) * a), (x1 + (x2 - x1) * b, y1 + (y2 - y1) * b)], fill=col + (255,), width=w * K)
    else:
        d.line(pts, fill=col + (255,), width=w * K, joint="curve")
    (x1, y1), (x2, y2) = pts[-2], pts[-1]
    a = math.atan2(y2 - y1, x2 - x1); s = 26 * K
    d.polygon([(x2 + math.cos(a) * s * .6, y2 + math.sin(a) * s * .6),
               (x2 + math.cos(a + 2.5) * s, y2 + math.sin(a + 2.5) * s),
               (x2 + math.cos(a - 2.5) * s, y2 + math.sin(a - 2.5) * s)], fill=col + (255,))

def label(xy, text, col=(30, 30, 30), size=21, anchor="mm", fg=(255, 255, 255)):
    f = font(size); x, y = xy[0] * K, xy[1] * K
    l, t, r, b = d.textbbox((x, y), text, font=f, anchor=anchor)
    pad = 8 * K
    d.rounded_rectangle([l - pad, t - pad // 2, r + pad, b + pad // 2], radius=10 * K, fill=col + (225,), outline=(255, 255, 255, 230), width=2 * K)
    d.text((x, y), text, font=f, fill=fg + (255,), anchor=anchor)

def marker(xy, col, text, r=17, size=20, off=(0, -48)):
    x, y = xy
    d.ellipse([(x - r) * K, (y - r) * K, (x + r) * K, (y + r) * K], fill=col + (255,), outline=(255, 255, 255, 255), width=4 * K)
    label((x + off[0], y + off[1]), text, col=col, size=size)

def zone(poly, col, alpha=70, dash_outline=True):
    d.polygon(P(poly), fill=col + (alpha,))
    pts = P(poly) + [P(poly)[0]]
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        L = math.hypot(x2 - x1, y2 - y1); n = max(int(L / (22 * K)), 1)
        for i in range(0, n, 2):
            a, b = i / n, min((i + 1) / n, 1)
            d.line([(x1 + (x2 - x1) * a, y1 + (y2 - y1) * a), (x1 + (x2 - x1) * b, y1 + (y2 - y1) * b)], fill=col + (255,), width=4 * K)

# --- Зоны смерти яйца ---
river_L = [(0, 100), (200, 125), (480, 155), (695, 172), (695, 262), (520, 276), (250, 270), (100, 250), (0, 185)]
river_M = [(806, 168), (1014, 168), (1014, 262), (806, 256)]
river_R = [(1070, 150), (1250, 162), (1420, 130), (1536, 70), (1536, 190), (1420, 255), (1250, 250), (1070, 266)]
cliff = [(430, 885), (580, 880), (840, 862), (1060, 802), (1100, 742), (1536, 736), (1536, 1024), (430, 1024)]
for z in (river_L, river_M, river_R, cliff): zone(z, C["death"], 85)

# --- Зоны ресурсов / стройки ---
zone([(8, 270), (470, 270), (470, 760), (8, 760)], C["res"], 28)
zone([(1090, 335), (1520, 335), (1520, 700), (1090, 700)], C["build"], 40)
zone([(556, 640), (700, 640), (700, 725), (556, 725)], C["res"], 55)

# --- Пути врагов ---
arrow([(745, 45), (752, 140), (755, 262), (755, 322)], C["main"], 11)
arrow([(1380, 40), (1250, 108), (1042, 148), (1046, 280), (940, 292), (800, 322)], C["bridge"], 9)
arrow([(1380, 60), (1366, 180), (1300, 300), (1210, 400), (1068, 450)], C["ford"], 8, dash=True)
arrow([(70, 505), (250, 512), (400, 516), (518, 498)], C["forest"], 10)
arrow([(235, 840), (330, 795), (480, 790), (690, 812), (815, 770), (800, 650), (775, 535)], C["secret"], 9, dash=True)

# --- Маркеры ---
marker((745, 38), C["main"], "SpawnCave (пещера)", off=(0, 52))
marker((1385, 34), C["bridge"], "SpawnBridge (восток)", off=(-10, 52))
marker((62, 500), C["forest"], "SpawnForest (лес)", off=(60, -48))
marker((235, 845), C["secret"], "SpawnSecret (тайный путь, c 3-й волны)", off=(110, 50), size=19)
marker((770, 487), C["egg"], "ЯЙЦО (гнездо)", off=(0, -52))
ov_text_fg = None
label((885, 372), "ВЕРСТАК (кузница)", col=(60, 60, 60), size=19)
label((690, 352), "ГОНГ (ранний старт)", col=(120, 60, 20), size=19)
label((620, 735), "Каменоломня: камень", col=(110, 85, 20), size=18)
label((240, 405), "ЛЕС: дерево (рубить часть деревьев)", col=(110, 85, 20), size=19)
label((1300, 520), "ВОСТОЧНАЯ ПОЛЯНА\nзона ловушек и стен", col=(20, 120, 150), size=22)
label((300, 205), "РЕКА = смерть яйца", col=C["death"], size=21)
label((1232, 215), "РЕКА / БРОД\n(замедляет врагов)", col=(150, 120, 20), size=17)
label((960, 905), "ОБРЫВ = смерть яйца", col=C["death"], size=22)
label((755, 188), "ПУТЬ БОССА (кабан):\nблоки и ловушки на мост", col=(150, 30, 30), size=17, anchor="mm")
label((1185, 300), "ВЕТХИЙ МОСТ\n(можно сломать)", col=(150, 90, 15), size=17)
label((755, 292), "С. ВОРОТА", col=(90, 40, 20), size=17)
label((482, 470), "З. калитка", col=(25, 110, 50), size=17)
label((800, 826), "ПРОЛОМ в стене\n(тайный вход)", col=(110, 50, 160), size=17)
label((400, 748), "намёки: следы,\nсломанная изгородь", col=(110, 50, 160), size=16)
label((1160, 625), "← стена без ворот:\nломают или обходят", col=(70, 70, 70), size=16)
label((690, 592), "↑ уклон ≈4°\nк С. воротам", col=(60, 60, 60), size=15)

# --- Легенда ---
y0 = H
d.rectangle([0, y0 * K, W * K, (H + LEG) * K], fill=(32, 34, 44, 255))
d.text((24 * K, (y0 + 14) * K), "Миссия 1 «Форт у реки» — карта путей и зон", font=font(26), fill=(255, 255, 255, 255))
items = [(C["main"], "Главный путь: пещера → каменный мост → С. ворота"), (C["bridge"], "Ветхий деревянный мост (восток)"),
         (C["ford"], "Брод (штрих.): мелко, замедляет"), (C["forest"], "Лесная тропа → З. калитка"),
         (C["secret"], "Тайный путь (штрих.): пролом в южной стене"), (C["death"], "Смерть яйца: река и обрыв"),
         (C["build"], "Зона ловушек и стен"), (C["res"], "Ресурсы: лес, каменоломня")]
for i, (col, txt) in enumerate(items):
    cx, cy = 24 + (i % 2) * 760, y0 + 64 + (i // 2) * 30
    d.rounded_rectangle([cx * K, cy * K, (cx + 34) * K, (cy + 20) * K], radius=5 * K, fill=col + (255,))
    d.text(((cx + 46) * K, (cy - 2) * K), txt, font=font(20), fill=(255, 255, 255, 255))
d.text((24 * K, (y0 + 196) * K), "Координаты приблизительные: ориентир для расстановки, точные размеры — в LEVEL_DESIGN.md", font=font(15), fill=(180, 185, 200, 255))

canvas = Image.new("RGBA", (W * K, (H + LEG) * K), (32, 34, 44, 255))
canvas.paste(base.resize((W * K, H * K), Image.LANCZOS), (0, 0))
canvas = Image.alpha_composite(canvas, ov).resize((W, H + LEG), Image.LANCZOS).convert("RGB")
canvas.save(OUT)
