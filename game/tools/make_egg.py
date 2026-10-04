# Генерирует модель Золотого Яйца в Blender (bpy) и экспортирует в FBX/GLB + рендер превью.
# Запуск: python3 game/tools/make_egg.py
import bpy, math, os

OUT = os.path.join(os.path.dirname(__file__), "..", "models")
R, HALF_H, K = 0.85, 1.1, 0.14   # радиус, половина высоты, "яйцевидность" (шире снизу)

bpy.ops.wm.read_factory_settings(use_empty=True)

def egg_radius(z):          # z от -1 до 1
    return R * math.sqrt(max(0.0, 1 - z * z)) * (1 - K * z)

def mat(name, color, rough):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes["Principled BSDF"]
    p.inputs["Base Color"].default_value = (*color, 1)
    p.inputs["Metallic"].default_value = 1.0
    p.inputs["Roughness"].default_value = rough
    return m

gold = mat("Gold", (1.0, 0.55, 0.06), 0.25)
gold_dark = mat("GoldBand", (0.9, 0.42, 0.04), 0.3)

# --- Корпус: UV-сфера, деформированная в яйцо ---
bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=32, radius=1)
egg = bpy.context.object
egg.name = "GoldenEgg"
for v in egg.data.vertices:
    x, y, z = v.co
    rxy = math.hypot(x, y)
    target = egg_radius(z)
    s = target / rxy if rxy > 1e-6 else 0
    v.co = (x * s, y * s, z * HALF_H)
egg.data.materials.append(gold)

# Где яйцо самое широкое — туда ставим поясок
zb = -0.12  # чуть ниже середины, как на эскизе
band_z, band_r = zb * HALF_H, egg_radius(zb)

parts = []
# Лента пояска
bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=band_r + 0.025, depth=0.26, location=(0, 0, band_z))
parts.append(bpy.context.object)
# Два валика по краям
for dz in (-0.14, 0.14):
    bpy.ops.mesh.primitive_torus_add(major_radius=band_r + 0.03, minor_radius=0.025,
                                     major_segments=64, minor_segments=8, location=(0, 0, band_z + dz))
    parts.append(bpy.context.object)
# Ромбы-пирамидки
N = 12
for i in range(N):
    a = 2 * math.pi * i / N
    r = band_r + 0.025
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.1, radius2=0, depth=0.07,
                                    location=(r * math.cos(a), r * math.sin(a), band_z))
    c = bpy.context.object
    # остриё наружу, ромб "стоит на углу"
    c.rotation_euler = (0, math.pi / 2, a)
    c.scale = (1.3, 1.0, 1.0)
    parts.append(c)
for p in parts:
    p.data.materials.append(gold_dark)

# Объединяем в один объект
bpy.ops.object.select_all(action="DESELECT")
for o in parts + [egg]:
    o.select_set(True)
bpy.context.view_layer.objects.active = egg
bpy.ops.object.join()
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
bpy.ops.object.shade_auto_smooth(angle=math.radians(40))

tris = sum(len(p.vertices) - 2 for p in egg.data.polygons)
print("Треугольников:", tris, " высота м:", round(2 * HALF_H, 2))

# --- Экспорт ---
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, "golden_egg.fbx"), use_selection=True,
                         apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y")
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, "golden_egg.glb"), use_selection=True)

# --- Превью-рендер ---
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.samples = 64
scene.cycles.device = "CPU"
scene.render.resolution_x = scene.render.resolution_y = 900
world = bpy.data.worlds.new("W"); scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.55, 0.5, 0.45, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, -HALF_H * (1)))
ground = bpy.context.object
gm = bpy.data.materials.new("Grass"); gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.35, 0.3, 0.25, 1)
ground.data.materials.append(gm)
bpy.ops.object.light_add(type="SUN", rotation=(math.radians(50), 0, math.radians(30)))
bpy.context.object.data.energy = 4
bpy.ops.object.light_add(type="AREA", location=(3, -3, 3))
bpy.context.object.data.energy = 400; bpy.context.object.data.size = 3
bpy.context.object.rotation_euler = (math.radians(55), 0, math.radians(45))
bpy.ops.object.camera_add(location=(0, -5.2, 0.6), rotation=(math.radians(88), 0, 0))
scene.camera = bpy.context.object
scene.render.filepath = os.path.join(OUT, "golden_egg_preview.png")
bpy.ops.render.render(write_still=True)
