# Рендер модели с нескольких ракурсов (для проверки). Использование:
# python3 render_views.py model.glb out_prefix [grid]
import bpy, sys, math
src, out = sys.argv[1], sys.argv[2]
grid = len(sys.argv) > 3
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for o in meshes:
    if not o.data.materials:
        m = bpy.data.materials.new("Clay"); m.use_nodes = True
        m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.8, 0.8, 0.8, 1)
        o.data.materials.append(m)
sc = bpy.context.scene
sc.render.engine = "CYCLES"; sc.cycles.samples = 24; sc.cycles.device = "CPU"
sc.render.resolution_x = 700; sc.render.resolution_y = 900
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.25, 0.25, 0.28, 1)
w.node_tree.nodes["Background"].inputs["Strength"].default_value = 1.0
bpy.ops.object.light_add(type="SUN", rotation=(math.radians(40), math.radians(10), math.radians(20)))
bpy.context.object.data.energy = 3
if grid:
    gm = bpy.data.materials.new("G"); gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (1, 0, 0, 1)
    gm.node_tree.nodes["Principled BSDF"].inputs["Emission Color"].default_value = (1, 0, 0, 1)
    gm.node_tree.nodes["Principled BSDF"].inputs["Emission Strength"].default_value = 3
    for i in range(-10, 11):
        z = i / 10
        bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, z))
        c = bpy.context.object; c.scale = (2.0, 2.0, 0.003); c.data.materials.append(gm)
        c.visible_shadow = False
cams = {"front": (0, -4, 0, 90, 0), "back": (0, 4, 0, 90, 180), "side": (4, 0, 0, 90, 90)}
for name, (x, y, z, rx, rz) in cams.items():
    bpy.ops.object.camera_add(location=(x, y, z + float(__import__("os").environ.get("CZ", "0"))), rotation=(math.radians(rx), 0, math.radians(rz)))
    cam = bpy.context.object; cam.data.type = "ORTHO"; cam.data.ortho_scale = float(__import__("os").environ.get("OS", "2.3"))
    sc.camera = cam
    sc.render.filepath = f"{out}_{name}.png"
    bpy.ops.render.render(write_still=True)
