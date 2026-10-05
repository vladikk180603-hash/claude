# Раскрашивает серую модель гоблина однотонными цветами по зонам и экспортирует в GLB/FBX.
# Зоны заданы координатами (модель нормализована: рост ~2, лицо смотрит в -Y, Z вверх).
# Запуск: python3 game/tools/paint_goblin.py
import bpy, math, os
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "..", "models", "goblin", "goblin_white_mesh.glb")
OUT = os.path.join(HERE, "..", "models", "goblin")
TARGET_HEIGHT = 1.2  # метров: гоблин мелкий, игрок ~1.8

def hex2lin(h, a=1.0):
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    c = [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c]
    return (*c, a)

PALETTE = {  # имя: (цвет, metallic, roughness)
    "Skin":    ("#7C8B34", 0.0, 0.6),
    "Belly":   ("#A6B25C", 0.0, 0.6),
    "Ear":     ("#C98A6A", 0.0, 0.6),
    "Nose":    ("#B85A4A", 0.0, 0.6),
    "Eye":     ("#FFE01A", 0.0, 0.3),
    "Pupil":   ("#151515", 0.0, 0.3),
    "Socket":  ("#4E5A22", 0.0, 0.6),
    "Teeth":   ("#F2EBD0", 0.0, 0.4),
    "Helmet":  ("#8C8E92", 0.6, 0.45),
    "Bandana": ("#C8322B", 0.0, 0.7),
    "Vest":    ("#5A3A24", 0.0, 0.8),
    "Rope":    ("#B88E55", 0.0, 0.8),
    "Shorts":  ("#3F2B1F", 0.0, 0.8),
}

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH")
obj.name = "Goblin"
mesh = obj.data
mesh.materials.clear()
idx = {}
for name, (col, met, rough) in PALETTE.items():
    m = bpy.data.materials.new("Goblin_" + name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = hex2lin(col)
    p.inputs["Metallic"].default_value = met
    p.inputs["Roughness"].default_value = rough
    idx[name] = len(mesh.materials)
    mesh.materials.append(m)

EYES = [Vector((-0.13, 0.635)), Vector((0.14, 0.635))]  # (x, z) центры глаз
EYE_R = 0.075

def classify(c, n):
    x, y, z = c
    ax = abs(x)
    front = n.y < -0.25          # грань смотрит вперёд (к зрителю)
    # Котелок: выше линии полей (поля наклонены — спереди выше, сзади ниже)
    if z > 0.79 - 0.2 * (y + 0.08) and ax < 0.33:
        return "Helmet"
    # Уши: широко по бокам головы
    if 0.36 < z < 0.76 and ax > 0.24:
        return "Ear" if n.y < -0.5 and ax > 0.31 else "Skin"
    # Глаза
    if front and y < 0.05:
        for e in EYES:
            if (Vector((x, z)) - e).length < EYE_R + 0.012:
                return "Socket"   # тёмные круги вокруг глаз (сами глаза — отдельные сферы)
        if 0.415 < z < 0.445 and ax < 0.09:
            return "Teeth"
    # Бандана: кольцо на шее + треугольник спереди
    if 0.32 < z < 0.405 and ax < 0.19:
        if z > 0.335 or y > 0.0 or front:
            if z < 0.42:
                return "Bandana"
    if front and 0.12 < z <= 0.36 and ax < 0.17 * (z - 0.12) / 0.24 and y < 0.02:
        return "Bandana"
    # Руки (снаружи торса и кисти, выступающие вперёд у бёдер) — кожа
    if ax > 0.205 or (ax > 0.17 and y < -0.02):
        return "Skin"
    # Жилет (спереди открыт — виден живот)
    if -0.06 < z <= 0.34:
        if front and ax < 0.06 and y < -0.02:
            return "Belly"
        return "Vest"
    # Верёвочный пояс
    if -0.21 < z <= -0.06:
        return "Rope"
    # Свисающий узел пояса спереди
    if -0.32 < z <= -0.21 and y < -0.03 and ax < 0.15:
        return "Rope"
    # Шорты
    if -0.52 < z <= -0.21 and ax < 0.2:
        return "Shorts"
    return "Skin"

mw = obj.matrix_world
nm = mw.to_3x3().inverted().transposed()
for poly in mesh.polygons:
    c = mw @ poly.center
    n = (nm @ poly.normal).normalized()
    poly.material_index = idx[classify(c, n)]

bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.shade_smooth()

# Глаза — аккуратные сферы поверх лица (чистая форма, потом легко сделать моргание)
parts = []
for e in EYES:
    hit, loc, nrm, _ = obj.ray_cast(Vector((e.x, -2.0, e.y)), Vector((0, 1, 0)))
    ys = loc.y if hit else -0.1
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=EYE_R,
                                         location=(e.x, ys + EYE_R * 0.15, e.y))
    eye = bpy.context.object; eye.scale = (1, 0.55, 1)
    eye.data.materials.append(mesh.materials[idx["Eye"]])
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=EYE_R * 0.38,
                                         location=(e.x + (0.012 if e.x < 0 else -0.012), ys - EYE_R * 0.32, e.y - 0.005))
    pup = bpy.context.object; pup.scale = (0.8, 0.35, 1.25)  # вертикальный зрачок
    pup.data.materials.append(mesh.materials[idx["Pupil"]])
    parts += [eye, pup]
for p_ in parts:
    bpy.context.view_layer.objects.active = p_
    bpy.ops.object.shade_smooth()
bpy.ops.object.select_all(action="DESELECT")
for o in parts + [obj]:
    o.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.object.join()
mesh = obj.data

# Масштаб и опора: ноги на Z=0, центр по X/Y
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
zs = [v.co.z for v in mesh.vertices]
xs = [v.co.x for v in mesh.vertices]
s = TARGET_HEIGHT / (max(zs) - min(zs))
cx = (max(xs) + min(xs)) / 2
for v in mesh.vertices:
    v.co = Vector(((v.co.x - cx) * s, v.co.y * s, (v.co.z - min(zs)) * s))
mesh.update()

print("Треугольников:", sum(len(p.vertices) - 2 for p in mesh.polygons))
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, "goblin.glb"), use_selection=True)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "goblin.fbx"), use_selection=True,
                         apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y")
