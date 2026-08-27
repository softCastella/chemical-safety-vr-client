import math
import os
import sys

import bpy
from mathutils import Vector


GLASS_MATERIAL_NAME = "PPE_Mask_Glass"


def args_after_separator():
    if "--" not in sys.argv:
        raise SystemExit("Expected mask1 FBX, mask2 FBX, output directory, and preview directory")
    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 4:
        raise SystemExit("Expected mask1 FBX, mask2 FBX, output directory, and preview directory")
    return tuple(os.path.abspath(arg) for arg in args)


def reset_and_import(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)


def create_glass_material():
    material = bpy.data.materials.get(GLASS_MATERIAL_NAME)
    if material is None:
        material = bpy.data.materials.new(GLASS_MATERIAL_NAME)
    material.diffuse_color = (0.15, 0.55, 0.75, 0.28)
    material.use_nodes = True
    principled = material.node_tree.nodes.get("Principled BSDF")
    if principled is not None:
        principled.inputs["Base Color"].default_value = (0.15, 0.55, 0.75, 1.0)
        principled.inputs["Roughness"].default_value = 0.08
        if "Alpha" in principled.inputs:
            principled.inputs["Alpha"].default_value = 0.28
    return material


def assign_mask1_visor_material():
    glass = create_glass_material()
    selected = 0
    for object_name in ("tripo_part_5", "tripo_part_29"):
        obj = bpy.data.objects.get(object_name)
        if obj is None or obj.type != "MESH":
            raise RuntimeError(f"mask1 source is missing visor mesh object '{object_name}'")
        obj.data.materials.clear()
        obj.data.materials.append(glass)
        for polygon in obj.data.polygons:
            polygon.material_index = 0
            selected += 1

    if selected != 3524:
        raise RuntimeError(f"mask1 visor polygon count changed unexpectedly: {selected}")
    print(f"MASK1_GLASS_POLYGONS={selected}")


def create_mask2_lens():
    glass = create_glass_material()
    center_z = 0.602
    half_width = 0.292
    half_height = 0.226
    edge_y = -0.318
    curvature = 0.055
    exponent = 4.0
    ring_count = 8
    segment_count = 72

    vertices = [(0.0, edge_y - curvature, center_z)]
    uvs = [(0.5, 0.5)]
    for ring in range(1, ring_count + 1):
        radius = ring / ring_count
        for segment in range(segment_count):
            angle = math.tau * segment / segment_count
            cosine = math.cos(angle)
            sine = math.sin(angle)
            boundary_x = half_width * math.copysign(abs(cosine) ** (2.0 / exponent), cosine)
            boundary_z = half_height * math.copysign(abs(sine) ** (2.0 / exponent), sine)
            x = boundary_x * radius
            z = center_z + boundary_z * radius
            y = edge_y - curvature * (1.0 - radius * radius)
            vertices.append((x, y, z))
            uvs.append((0.5 + x / (half_width * 2.0), 0.5 + (z - center_z) / (half_height * 2.0)))

    faces = []
    for segment in range(segment_count):
        current = 1 + segment
        following = 1 + (segment + 1) % segment_count
        faces.append((0, current, following))

    for ring in range(2, ring_count + 1):
        inner_start = 1 + (ring - 2) * segment_count
        outer_start = 1 + (ring - 1) * segment_count
        for segment in range(segment_count):
            following = (segment + 1) % segment_count
            faces.append(
                (
                    inner_start + segment,
                    outer_start + segment,
                    outer_start + following,
                    inner_start + following,
                )
            )

    mesh = bpy.data.meshes.new("Mask2_Glass_Lens_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    lens = bpy.data.objects.new("Mask2_Glass_Lens", mesh)
    bpy.context.scene.collection.objects.link(lens)
    mesh.materials.append(glass)

    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        polygon.use_smooth = True
        for loop_index in polygon.loop_indices:
            uv_layer.data[loop_index].uv = uvs[mesh.loops[loop_index].vertex_index]

    solidify = lens.modifiers.new("Lens Thickness", "SOLIDIFY")
    solidify.thickness = 0.003
    solidify.offset = 0.0
    bpy.context.view_layer.objects.active = lens
    lens.select_set(True)
    bpy.ops.object.modifier_apply(modifier=solidify.name)
    print(f"MASK2_GLASS_VERTICES={len(mesh.vertices)} POLYGONS={len(mesh.polygons)}")


def aggregate_bounds():
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        points.extend(obj.matrix_world @ vertex.co for vertex in obj.data.vertices)
    minimum = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    maximum = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    return (minimum + maximum) * 0.5, maximum - minimum


def render_preview(path):
    center, size = aggregate_bounds()
    maximum_dimension = max(size)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "BOTH"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = True

    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = maximum_dimension * 1.2
    camera_data.clip_start = maximum_dimension * 0.001
    camera_data.clip_end = maximum_dimension * 10.0
    direction = Vector((0.0, -1.0, 0.0))
    camera.location = center + direction * maximum_dimension * 3.0
    camera.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def export_fbx(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=False,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )


def main():
    mask1_source, mask2_source, output_directory, preview_directory = args_after_separator()
    os.makedirs(output_directory, exist_ok=True)
    os.makedirs(preview_directory, exist_ok=True)

    reset_and_import(mask1_source)
    assign_mask1_visor_material()
    render_preview(os.path.join(preview_directory, "Mask1_GlassReady.png"))
    export_fbx(os.path.join(output_directory, "Mask1_GlassReady.fbx"))

    reset_and_import(mask2_source)
    create_mask2_lens()
    render_preview(os.path.join(preview_directory, "Mask2_GlassReady.png"))
    export_fbx(os.path.join(output_directory, "Mask2_GlassReady.fbx"))


if __name__ == "__main__":
    main()
