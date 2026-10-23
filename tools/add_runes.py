# PotatoRunes icon (Valheim mod): draws exact Elder Futhark runes, glowing gold, onto the potato artwork.
# Image tools mangle runes, so the art is made without them and the runes are added here.
#   python3 -I tools/add_runes.py in.png out.png
import sys
from PIL import Image, ImageDraw, ImageFilter

# Each rune as straight strokes in a box 0.6 wide and 1 tall (y down).
RUNES = {
    "R": [[(0, 1), (0, 0), (0.5, 0.25), (0, 0.5), (0.5, 1)]],                  # raido
    "U": [[(0, 1), (0, 0), (0.55, 0.35), (0.55, 1)]],                           # uruz
    "N": [[(0.3, 0), (0.3, 1)], [(0.0, 0.26), (0.6, 0.7)]],                   # naudiz
    "E": [[(0, 1), (0, 0), (0.3, 0.35), (0.6, 0), (0.6, 1)]],                   # ehwaz
    "F": [[(0.1, 0), (0.1, 1)], [(0.1, 0.32), (0.55, 0.04)], [(0.1, 0.62), (0.55, 0.34)]],   # fehu
}
GOLD, CORE, OUTLINE = (255, 196, 70), (255, 238, 170), (70, 40, 15)

def place(name, x, y, h, tilt=0.0):
    import math
    c, s = math.cos(tilt), math.sin(tilt)
    out = []
    for stroke in RUNES[name]:
        pts = []
        for px, py in stroke:
            dx, dy = (px - 0.3) * h, (py - 0.5) * h
            pts.append((x + dx * c - dy * s, y + dx * s + dy * c))
        out.append(pts)
    return out

def draw(img, strokes, width):
    W, H = img.size
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0)); g = ImageDraw.Draw(glow)
    line = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(line)
    for pts in strokes:
        g.line(pts, fill=GOLD + (200,), width=int(width * 3.2), joint="curve")
    glow = glow.filter(ImageFilter.GaussianBlur(width * 1.6))
    for pts in strokes:   # dark outline, gold body, pale hot core
        d.line(pts, fill=OUTLINE + (255,), width=int(width * 1.55), joint="curve")
        for p in (pts[0], pts[-1]): d.ellipse((p[0] - width * 0.77, p[1] - width * 0.77, p[0] + width * 0.77, p[1] + width * 0.77), fill=OUTLINE + (255,))
    for pts in strokes:
        d.line(pts, fill=GOLD + (255,), width=int(width), joint="curve")
        for p in (pts[0], pts[-1]): d.ellipse((p[0] - width / 2, p[1] - width / 2, p[0] + width / 2, p[1] + width / 2), fill=GOLD + (255,))
    for pts in strokes:
        d.line(pts, fill=CORE + (255,), width=max(2, int(width * 0.35)))
    img.alpha_composite(glow); img.alpha_composite(line)

if __name__ == "__main__":
    img = Image.open(sys.argv[1]).convert("RGBA")
    k = img.width / 1024
    # R U N E in a gentle arc between the horns, above the helmet.
    word = [("R", 395, 228, -0.12), ("U", 470, 205, -0.04), ("N", 548, 205, 0.04), ("E", 625, 228, 0.12)]
    strokes = []
    for name, x, y, t in word:
        strokes += place(name, x * k, y * k, 62 * k, t)
    draw(img, strokes, 11 * k)
    # One rune carved into the stone in his arm.
    draw(img, place("F", 712 * k, 675 * k, 70 * k, 0.18), 10 * k)
    img.save(sys.argv[2])
