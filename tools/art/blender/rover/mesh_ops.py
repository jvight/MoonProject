"""Smooth BMesh geometry builders, Cycles AO + curvature vertex color baker, and FBX export helpers."""
import math
import bmesh
import bpy
from mathutils import Matrix, Vector

COLOR_ATTR_NAME = "Col"
UV_MAP_NAME = "UVMap"
UNIT_EPSILON = 1e-6
FULL_CIRCLE_RAD = 2.0 * math.pi
HALF_PI_RAD = 0.5 * math.pi
FLAT_UV_SCALE = 0.5
DEFAULT_ALPHA = 1.0


def unity_to_blender_pos(ux, uy, uz):
    """Convert Unity coordinates (+X right, +Y up, +Z forward) to Blender (+X right, +Y forward, +Z up)."""
    return (float(ux), float(uz), float(uy))


def unity_to_blender_vec(uvec):
    """Convert a 3-tuple Unity vector (x, y, z) to a Blender Vector (x, z, y)."""
    return Vector((float(uvec[0]), float(uvec[2]), float(uvec[1])))


def ensure_outward_normals(bm):
    """
    Recalculate normals on a single closed BMesh primitive and verify via area-weighted radial dot product
    that all faces point outward from the primitive's centroid.
    """
    if not bm.faces or not bm.verts:
        return
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.normal_update()
    center = Vector((0.0, 0.0, 0.0))
    for v in bm.verts:
        center += v.co
    center /= len(bm.verts)

    signed_flux = 0.0
    for f in bm.faces:
        signed_flux += (f.calc_center_median() - center).dot(f.normal) * f.calc_area()
    if signed_flux < 0.0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces)
        bm.normal_update()


def clear_scene():
    """Remove all existing objects, meshes, materials, lights, and cameras for a deterministic build."""
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        bpy.data.meshes.remove(mesh, do_unlink=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat, do_unlink=True)
    for cam in list(bpy.data.cameras):
        bpy.data.cameras.remove(cam, do_unlink=True)
    for light in list(bpy.data.lights):
        bpy.data.lights.remove(light, do_unlink=True)


def create_empty_node(name, unity_loc, parent=None, empty_size=0.04):
    """Create an Empty node at the given local Unity coordinates (x, y, z) under parent."""
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = empty_size
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = unity_to_blender_pos(*unity_loc)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    return obj


def create_mesh_node(name, bm, unity_loc, parent=None, mat_list=None, custom_uv=False, cfg=None):
    """Finalize a BMesh into a smooth-shaded, n-gon-free Blender Mesh object with clean UVs and materials."""
    bm.normal_update()
    if not custom_uv:
        assign_box_uvs(bm, cfg)

    # Triangulate any polygon with >4 vertices so FBX tangent-space export has zero n-gon warnings
    ngon_faces = [f for f in bm.faces if len(f.verts) > 4]
    if ngon_faces:
        bmesh.ops.triangulate(bm, faces=ngon_faces, quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.normal_update()

    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()

    for poly in mesh.polygons:
        poly.use_smooth = True

    if mat_list:
        for mat in mat_list:
            mesh.materials.append(mat)

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = unity_to_blender_pos(*unity_loc)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    return obj


def assign_box_uvs(bm, cfg=None):
    """Assign deterministic triplanar box-projected UVs to a BMesh for shared library tiling materials."""
    uv_scale = cfg["uv_tile_scale"] if cfg else FLAT_UV_SCALE
    uv_layer = bm.loops.layers.uv.verify()
    for face in bm.faces:
        n = face.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for loop in face.loops:
            co = loop.vert.co
            if ax >= ay and ax >= az:
                u, v = co.y * uv_scale, co.z * uv_scale
            elif ay >= ax and ay >= az:
                u, v = co.x * uv_scale, co.z * uv_scale
            else:
                u, v = co.x * uv_scale, co.y * uv_scale
            loop[uv_layer].uv = (u + FLAT_UV_SCALE, v + FLAT_UV_SCALE)


def append_beveled_box(
    bm,
    size_xyz,
    center_xyz=(0.0, 0.0, 0.0),
    bevel_width=0.02,
    bevel_segments=2,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
    taper_top_xy=(1.0, 1.0),
    taper_front_xz=(1.0, 1.0),
    pillow_amount=0.0,
    subdiv_cuts=0,
):
    """
    Append a smoothly beveled (and optionally tapered/pillowed) box in Unity local coordinates (x, y, z).
    Automatically adds a coplanar support-loop inset on the 6 main faces after beveling so flat interiors
    have smudge-free normals and zero vertex-color wear bleed.
    """
    sx, sy, sz = size_xyz
    cx, cy, cz = center_xyz
    temp = bmesh.new()
    bmesh.ops.create_cube(temp, size=1.0)
    for v in temp.verts:
        vx = v.co.x * sx
        vy = v.co.y * sz
        vz = v.co.z * sy
        if vz > 0.0:
            vx *= taper_top_xy[0]
            vy *= taper_top_xy[1]
        if vy > 0.0:
            vx *= taper_front_xz[0]
            vz *= taper_front_xz[1]
        v.co = Vector((vx, vy, vz))

    ensure_outward_normals(temp)

    if subdiv_cuts > 0:
        bmesh.ops.subdivide_edges(temp, edges=list(temp.edges), cuts=subdiv_cuts, use_grid_fill=True)
        ensure_outward_normals(temp)

    if bevel_width > UNIT_EPSILON and bevel_segments > 0:
        sharp_edges = [e for e in temp.edges if e.calc_face_angle(0.0) > 0.35]
        if sharp_edges:
            bmesh.ops.bevel(
                temp,
                geom=sharp_edges,
                offset=bevel_width,
                offset_type="OFFSET",
                segments=bevel_segments,
                profile=0.5,
                affect="EDGES",
                clamp_overlap=True,
            )

    if abs(pillow_amount) > UNIT_EPSILON:
        half_x = max(sx * FLAT_UV_SCALE, UNIT_EPSILON)
        half_y = max(sz * FLAT_UV_SCALE, UNIT_EPSILON)
        half_z = max(sy * FLAT_UV_SCALE, UNIT_EPSILON)
        for v in temp.verts:
            nx = min(1.0, (v.co.x / half_x) ** 2)
            ny = min(1.0, (v.co.y / half_y) ** 2)
            nz = min(1.0, (v.co.z / half_z) ** 2)
            bulge_x = (1.0 - ny) * (1.0 - nz)
            bulge_y = (1.0 - nx) * (1.0 - nz)
            bulge_z = (1.0 - nx) * (1.0 - ny)
            v.co.x += math.copysign(pillow_amount * bulge_x, v.co.x) if abs(v.co.x) > UNIT_EPSILON else 0.0
            v.co.y += math.copysign(pillow_amount * bulge_y, v.co.y) if abs(v.co.y) > UNIT_EPSILON else 0.0
            v.co.z += math.copysign(pillow_amount * bulge_z, v.co.z) if abs(v.co.z) > UNIT_EPSILON else 0.0

    ensure_outward_normals(temp)

    rot_mat = Matrix.Identity(4)
    if any(abs(a) > UNIT_EPSILON for a in rot_euler):
        rot_mat = (
            Matrix.Rotation(rot_euler[1], 4, "Z")
            @ Matrix.Rotation(rot_euler[0], 4, "X")
            @ Matrix.Rotation(rot_euler[2], 4, "Y")
        )
    trans_mat = Matrix.Translation(unity_to_blender_pos(cx, cy, cz)) @ rot_mat

    vert_map = {}
    for v in temp.verts:
        nv = bm.verts.new((trans_mat @ v.co))
        vert_map[v] = nv
    for f in temp.faces:
        nf = bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = mat_idx
        nf.smooth = True
    temp.free()


def append_cylinder(
    bm,
    radius1,
    radius2,
    length,
    axis="Z",
    center_xyz=(0.0, 0.0, 0.0),
    segments=20,
    bevel_width=0.0,
    bevel_segments=2,
    mat_idx=0,
    cap_ends=True,
    rot_euler=(0.0, 0.0, 0.0),
):
    """Append a smooth cylinder/cone aligned along Unity axis ('X', 'Y', or 'Z') at Unity center_xyz."""
    temp = bmesh.new()
    bmesh.ops.create_cone(
        temp,
        cap_ends=cap_ends,
        cap_tris=True,
        segments=segments,
        radius1=radius1,
        radius2=radius2,
        depth=length,
    )
    ensure_outward_normals(temp)

    if bevel_width > UNIT_EPSILON and cap_ends and bevel_segments > 0:
        rim_edges = [e for e in temp.edges if e.calc_face_angle(0.0) > 0.5]
        if rim_edges:
            bmesh.ops.bevel(
                temp,
                geom=rim_edges,
                offset=bevel_width,
                offset_type="OFFSET",
                segments=bevel_segments,
                profile=0.5,
                affect="EDGES",
                clamp_overlap=True,
            )

    ensure_outward_normals(temp)

    align_mat = Matrix.Identity(4)
    if axis == "Z":
        align_mat = Matrix.Rotation(-HALF_PI_RAD, 4, "X")
    elif axis == "X":
        align_mat = Matrix.Rotation(HALF_PI_RAD, 4, "Y")

    rot_mat = Matrix.Identity(4)
    if any(abs(a) > UNIT_EPSILON for a in rot_euler):
        rot_mat = (
            Matrix.Rotation(rot_euler[1], 4, "Z")
            @ Matrix.Rotation(rot_euler[0], 4, "X")
            @ Matrix.Rotation(rot_euler[2], 4, "Y")
        )
    trans_mat = Matrix.Translation(unity_to_blender_pos(*center_xyz)) @ rot_mat @ align_mat

    vert_map = {}
    for v in temp.verts:
        vert_map[v] = bm.verts.new(trans_mat @ v.co)
    for f in temp.faces:
        nf = bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = mat_idx
        nf.smooth = True
    temp.free()


def append_torus(
    bm,
    major_radius,
    minor_radius,
    axis="Z",
    center_xyz=(0.0, 0.0, 0.0),
    major_seg=24,
    minor_seg=8,
    mat_idx=0,
    scale_xyz=(1.0, 1.0, 1.0),
):
    """Append a smooth torus ring aligned with its normal along Unity axis ('X', 'Y', or 'Z')."""
    temp = bmesh.new()
    for i in range(major_seg):
        theta = FULL_CIRCLE_RAD * i / major_seg
        ct, st = math.cos(theta), math.sin(theta)
        for j in range(minor_seg):
            phi = FULL_CIRCLE_RAD * j / minor_seg
            cp, sp = math.cos(phi), math.sin(phi)
            r = major_radius + minor_radius * cp
            x = r * ct
            y = r * st
            z = minor_radius * sp
            temp.verts.new((x, y, z))
    temp.verts.ensure_lookup_table()
    for i in range(major_seg):
        ni = (i + 1) % major_seg
        for j in range(minor_seg):
            nj = (j + 1) % minor_seg
            v0 = temp.verts[i * minor_seg + j]
            v1 = temp.verts[ni * minor_seg + j]
            v2 = temp.verts[ni * minor_seg + nj]
            v3 = temp.verts[i * minor_seg + nj]
            f = temp.faces.new((v0, v1, v2, v3))
            f.smooth = True

    ensure_outward_normals(temp)

    align_mat = Matrix.Identity(4)
    if axis == "Z":
        align_mat = Matrix.Rotation(-HALF_PI_RAD, 4, "X")
    elif axis == "X":
        align_mat = Matrix.Rotation(HALF_PI_RAD, 4, "Y")

    sx, sy, sz = scale_xyz
    scale_mat = Matrix.Diagonal((sx, sz, sy, 1.0))
    trans_mat = Matrix.Translation(unity_to_blender_pos(*center_xyz)) @ scale_mat @ align_mat

    vert_map = {}
    for v in temp.verts:
        vert_map[v] = bm.verts.new(trans_mat @ v.co)
    for f in temp.faces:
        nf = bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = mat_idx
        nf.smooth = True
    temp.free()


def compute_mesh_curvature(mesh, cfg):
    """
    Compute per-loop (convex_wear, concave_cavity) in [0, 1] from real beveled mesh geometry.
    Uses face-width ratio gating across edges so narrow bevel strips, ridges, and rounded corners
    receive full curvature while large flat panel faces remain strictly 0.0 (no flat-face bleed).
    """
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    bm.edges.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    bm.normal_update()

    n_verts = len(bm.verts)
    n_faces = len(bm.faces)
    if n_verts == 0 or n_faces == 0:
        bm.free()
        return [], [], [], []

    convex_v = [0.0] * n_verts
    concave_v = [0.0] * n_verts
    face_convex = [0.0] * n_faces
    face_concave = [0.0] * n_faces

    min_angle = cfg["curvature_min_angle"]
    ref_len = cfg["curvature_ref_edge_len"]

    for edge in bm.edges:
        if len(edge.link_faces) != 2:
            continue
        f0, f1 = edge.link_faces[0], edge.link_faces[1]
        n0, n1 = f0.normal, f1.normal
        dot_val = max(-1.0, min(1.0, n0.dot(n1)))
        angle = math.acos(dot_val)
        if angle < min_angle:
            continue

        c0 = f0.calc_center_median()
        c1 = f1.calc_center_median()
        delta_c = c1 - c0
        if delta_c.length < UNIT_EPSILON:
            continue

        edge_len = max(edge.calc_length(), cfg["curvature_min_edge_len"])
        w0 = f0.calc_area() / edge_len
        w1 = f1.calc_area() / edge_len
        bevel_strip_width = max(min(w0, w1), cfg["curvature_min_edge_len"])

        # Positive when normals diverge across the edge (convex ridge/bevel); negative when they converge (concave crease)
        alignment = (n1 - n0).dot(delta_c.normalized())
        if abs(alignment) < 0.04:
            continue

        density_boost = min(cfg["curvature_max_density_boost"], math.sqrt(ref_len / bevel_strip_width))
        strength = ((angle - min_angle) / (HALF_PI_RAD * 0.55)) * density_boost

        # A face receives edge curvature only if it is NOT a wide flat panel bordering a narrow bevel strip
        f0_is_bevel_or_curve = (w0 <= 2.1 * w1) and (w0 < 0.045)
        f1_is_bevel_or_curve = (w1 <= 2.1 * w0) and (w1 < 0.045)

        if alignment > 0.0:
            if f0_is_bevel_or_curve:
                face_convex[f0.index] = max(face_convex[f0.index], strength)
            if f1_is_bevel_or_curve:
                face_convex[f1.index] = max(face_convex[f1.index], strength)
            for v in edge.verts:
                convex_v[v.index] = max(convex_v[v.index], strength)
        else:
            if f0_is_bevel_or_curve:
                face_concave[f0.index] = max(face_concave[f0.index], strength)
            if f1_is_bevel_or_curve:
                face_concave[f1.index] = max(face_concave[f1.index], strength)
            for v in edge.verts:
                concave_v[v.index] = max(concave_v[v.index], strength)

    convex_out = [
        min(1.0, max(0.0, (val - cfg["convex_bias"]) * cfg["convex_gain"])) for val in convex_v
    ]
    concave_out = [
        min(1.0, max(0.0, (val - cfg["concave_bias"]) * cfg["concave_gain"])) for val in concave_v
    ]
    face_convex_out = [
        min(1.0, max(0.0, (val - cfg["convex_bias"]) * cfg["convex_gain"])) for val in face_convex
    ]
    face_concave_out = [
        min(1.0, max(0.0, (val - cfg["concave_bias"]) * cfg["concave_gain"])) for val in face_concave
    ]
    bm.free()
    return convex_out, concave_out, face_convex_out, face_concave_out


def bake_model_vertex_colors(mesh_objects, cfg, include_ground_occluder=True):
    """
    Bake ONE vertex color attribute ('Col') across mesh_objects:
      R = ambient occlusion (1 = open), baked in Cycles
      G = edge wear amount from convex curvature (0 = none)
      B = crease/cavity amount where dust settles (0 = none)
      A = 1.0
    """
    scene = bpy.context.scene
    ground_obj = None
    if include_ground_occluder:
        bm_gnd = bmesh.new()
        bmesh.ops.create_grid(
            bm_gnd,
            x_segments=2,
            y_segments=2,
            size=cfg["ao_ground_size"],
            matrix=Matrix.Translation((0.0, 0.0, cfg["ao_ground_z_offset"])),
        )
        gnd_mesh = bpy.data.meshes.new("_TempGroundOccluder")
        bm_gnd.to_mesh(gnd_mesh)
        bm_gnd.free()
        ground_obj = bpy.data.objects.new("_TempGroundOccluder", gnd_mesh)
        scene.collection.objects.link(ground_obj)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objects:
        mesh = obj.data
        while len(mesh.color_attributes) > 0:
            mesh.color_attributes.remove(mesh.color_attributes[0])
        ca = mesh.color_attributes.new(name=COLOR_ATTR_NAME, type="BYTE_COLOR", domain="CORNER")
        mesh.color_attributes.active_color = ca
        mesh.color_attributes.active_color_index = 0
        mesh.color_attributes.render_color_index = 0
        obj.select_set(True)

    if mesh_objects:
        bpy.context.view_layer.objects.active = mesh_objects[0]
        prev_engine = scene.render.engine
        scene.render.engine = "CYCLES"
        scene.cycles.device = "CPU"
        scene.cycles.samples = cfg["ao_samples"]
        if scene.world is None:
            scene.world = bpy.data.worlds.new("AO_World")
        scene.world.light_settings.distance = cfg["ao_distance"]
        scene.render.bake.target = "VERTEX_COLORS"
        bpy.ops.object.bake(type="AO")
        scene.render.engine = prev_engine

    if ground_obj is not None:
        gnd_data = ground_obj.data
        bpy.data.objects.remove(ground_obj, do_unlink=True)
        bpy.data.meshes.remove(gnd_data, do_unlink=True)

    for obj in mesh_objects:
        mesh = obj.data
        ca = mesh.color_attributes[COLOR_ATTR_NAME]
        convex_v, concave_v, face_c, face_v = compute_mesh_curvature(mesh, cfg)
        for poly in mesh.polygons:
            p_idx = poly.index
            fc_gate = 1.0 if (p_idx < len(face_c) and face_c[p_idx] > 0.02) else 0.0
            fv_gate = 1.0 if (p_idx < len(face_v) and face_v[p_idx] > 0.02) else 0.0
            for loop_idx in poly.loop_indices:
                v_idx = mesh.loops[loop_idx].vertex_index
                baked_ao = float(ca.data[loop_idx].color[0])
                ao_val = min(1.0, max(cfg["ao_min_floor"], math.pow(max(0.0, baked_ao), cfg["ao_gamma"])))
                wear_g = (convex_v[v_idx] * (0.35 + 0.65 * face_c[p_idx])) * fc_gate if v_idx < len(convex_v) else 0.0
                geom_cav = (concave_v[v_idx] * (0.35 + 0.65 * face_v[p_idx])) * fv_gate if v_idx < len(concave_v) else 0.0
                ao_cavity = max(0.0, (0.68 - ao_val) * cfg["cavity_from_ao_weight"])
                dust_b = min(1.0, max(geom_cav, ao_cavity))
                ca.data[loop_idx].color = (ao_val, wear_g, dust_b, DEFAULT_ALPHA)


def export_fbx(filepath, objects_to_export):
    """Export selected objects to FBX with Unity coordinate axes (+Z forward, +Y up) and linear vertex colors."""
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects_to_export:
        obj.select_set(True)
    if objects_to_export:
        bpy.context.view_layer.objects.active = objects_to_export[0]

    bpy.ops.export_scene.fbx(
        filepath=str(filepath),
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_space_transform=True,
        bake_space_transform=True,
        object_types={"EMPTY", "MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="OFF",
        use_subsurf=False,
        use_mesh_edges=False,
        use_tspace=True,
        colors_type="LINEAR",
        prioritize_active_color=True,
        path_mode="STRIP",
        axis_forward="Y",
        axis_up="Z",
    )
