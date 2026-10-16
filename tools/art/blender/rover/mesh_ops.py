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


def apply_weighted_normals(mesh, sharp_angle_deg=42.0, area_power=1.6):
    """
    Compute and assign face-area-weighted custom split normals so large flat/gently-curved panels
    have 100% flat interior shading while 1-2 cm 2-segment bevels and turned cylinders shade smoothly.
    """
    mesh.polygons.foreach_set("use_smooth", [True] * len(mesh.polygons))
    n_polys = len(mesh.polygons)
    n_loops = len(mesh.loops)
    if n_polys == 0 or n_loops == 0:
        return

    cos_sharp = math.cos(math.radians(sharp_angle_deg))
    # 17.5 deg groups both coplanar panel triangles (0 deg) and >=22-seg cylinder strips (<=16.4 deg)
    cos_coplanar = math.cos(math.radians(17.5))

    poly_normals = [Vector(p.normal) for p in mesh.polygons]
    poly_areas = [max(p.area, 1e-8) for p in mesh.polygons]

    vert_to_polys = [[] for _ in range(len(mesh.vertices))]
    for p in mesh.polygons:
        p_idx = p.index
        for v_idx in p.vertices:
            vert_to_polys[v_idx].append(p_idx)

    loop_normals = [None] * n_loops
    for p in mesh.polygons:
        p_idx = p.index
        n_this = poly_normals[p_idx]
        for loop_idx in p.loop_indices:
            v_idx = mesh.loops[loop_idx].vertex_index
            inc_polys = vert_to_polys[v_idx]
            accum = Vector((0.0, 0.0, 0.0))
            for other_p in inc_polys:
                n_other = poly_normals[other_p]
                if n_this.dot(n_other) < cos_sharp:
                    continue
                coplanar_area = 0.0
                for cp_poly in inc_polys:
                    if n_other.dot(poly_normals[cp_poly]) >= cos_coplanar:
                        coplanar_area += poly_areas[cp_poly]
                weight = poly_areas[other_p] * (coplanar_area ** area_power)
                accum += n_other * weight
            if accum.length > UNIT_EPSILON:
                accum.normalize()
                loop_normals[loop_idx] = (accum.x, accum.y, accum.z)
            else:
                loop_normals[loop_idx] = (n_this.x, n_this.y, n_this.z)

    mesh.normals_split_custom_set(loop_normals)


def create_mesh_node(name, bm, unity_loc, parent=None, mat_list=None, custom_uv=False, cfg=None):
    """Finalize a BMesh into a weighted-normal smooth-shaded Blender Mesh object with clean UVs and materials."""
    bm.normal_update()
    if not custom_uv:
        assign_box_uvs(bm, cfg, mat_list=mat_list)

    # Triangulate any polygon with >4 vertices so FBX tangent-space export has zero n-gon warnings
    ngon_faces = [f for f in bm.faces if len(f.verts) > 4]
    if ngon_faces:
        bmesh.ops.triangulate(bm, faces=ngon_faces, quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.normal_update()

    mesh = bpy.data.meshes.new(name)
    if mat_list:
        for mat in mat_list:
            mesh.materials.append(mat)
    bm.to_mesh(mesh)
    bm.free()

    sharp_deg = cfg.get("weighted_normal_sharp_deg", 42.0) if cfg else 42.0
    area_pow = cfg.get("weighted_normal_area_power", 1.6) if cfg else 1.6
    apply_weighted_normals(mesh, sharp_angle_deg=sharp_deg, area_power=area_pow)

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    obj.location = unity_to_blender_pos(*unity_loc)
    obj.rotation_euler = (0.0, 0.0, 0.0)
    obj.scale = (1.0, 1.0, 1.0)
    return obj


def merge_child_meshes_into_host(host_obj, child_names, cfg=None):
    """
    Merge each named child MESH object in child_names into host_obj's Mesh data in world space,
    converting the child object in-place into an EMPTY node at the exact same pivot and preserving
    all parent-child relationships and hierarchy node names.
    """
    bpy.context.view_layer.update()
    host_mesh = host_obj.data
    mat_list = [m for m in host_mesh.materials if m is not None]
    bm_host = bmesh.new()
    bm_host.from_mesh(host_mesh)
    uv_host = bm_host.loops.layers.uv.verify()
    host_inv = host_obj.matrix_world.inverted()

    for child_name in child_names:
        child_obj = bpy.data.objects.get(child_name)
        if child_obj is None or child_obj.type != "MESH" or child_obj == host_obj:
            continue

        rel_mat = host_inv @ child_obj.matrix_world
        child_mesh = child_obj.data
        child_mats = list(child_mesh.materials)
        mat_remap = {}
        for idx, c_mat in enumerate(child_mats):
            if c_mat is None:
                mat_remap[idx] = 0
                continue
            existing_idx = next((i for i, m in enumerate(mat_list) if m.name == c_mat.name), None)
            if existing_idx is None:
                existing_idx = len(mat_list)
                mat_list.append(c_mat)
            mat_remap[idx] = existing_idx

        bm_child = bmesh.new()
        bm_child.from_mesh(child_mesh)
        uv_child = bm_child.loops.layers.uv.verify()

        vert_map = {}
        for v in bm_child.verts:
            vert_map[v] = bm_host.verts.new(rel_mat @ v.co)
        for f in bm_child.faces:
            nf = bm_host.faces.new([vert_map[v] for v in f.verts])
            nf.material_index = mat_remap.get(f.material_index, 0)
            nf.smooth = True
            for loop_h, loop_c in zip(nf.loops, f.loops):
                loop_h[uv_host].uv = tuple(loop_c[uv_child].uv)
        bm_child.free()

        parent_obj = child_obj.parent
        loc = Vector(child_obj.location)
        rot = tuple(child_obj.rotation_euler)
        scl = Vector(child_obj.scale)
        direct_children = list(child_obj.children)

        child_obj.name = f"__obsolete_{child_name}"
        empty_obj = bpy.data.objects.new(child_name, None)
        empty_obj.empty_display_type = "PLAIN_AXES"
        empty_obj.empty_display_size = 0.03
        bpy.context.scene.collection.objects.link(empty_obj)
        if parent_obj is not None:
            empty_obj.parent = parent_obj
        empty_obj.location = loc
        empty_obj.rotation_euler = rot
        empty_obj.scale = scl

        for ch in direct_children:
            ch_loc = Vector(ch.location)
            ch_rot = tuple(ch.rotation_euler)
            ch_scl = Vector(ch.scale)
            ch.parent = empty_obj
            ch.location = ch_loc
            ch.rotation_euler = ch_rot
            ch.scale = ch_scl

        bpy.data.objects.remove(child_obj, do_unlink=True)
        if child_mesh.users == 0:
            bpy.data.meshes.remove(child_mesh, do_unlink=True)
        bpy.context.view_layer.update()

    bm_host.normal_update()
    host_mesh.materials.clear()
    for mat in mat_list:
        host_mesh.materials.append(mat)
    bm_host.to_mesh(host_mesh)
    bm_host.free()

    sharp_deg = cfg.get("weighted_normal_sharp_deg", 42.0) if cfg else 42.0
    area_pow = cfg.get("weighted_normal_area_power", 1.6) if cfg else 1.6
    apply_weighted_normals(host_mesh, sharp_angle_deg=sharp_deg, area_power=area_pow)


def prune_unused_materials(mesh_objects):
    """Remove any material slots that have zero polygons assigned to them to minimize draw calls."""
    for obj in mesh_objects:
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        if len(mesh.polygons) == 0 or len(mesh.materials) <= 1:
            continue
        used_indices = sorted({p.material_index for p in mesh.polygons if 0 <= p.material_index < len(mesh.materials)})
        if not used_indices or len(used_indices) == len(mesh.materials):
            continue
        remap = {old_idx: new_idx for new_idx, old_idx in enumerate(used_indices)}
        new_mats = [mesh.materials[old_idx] for old_idx in used_indices]
        remapped_indices = [remap.get(p.material_index, 0) for p in mesh.polygons]
        mesh.materials.clear()
        for m in new_mats:
            mesh.materials.append(m)
        mesh.polygons.foreach_set("material_index", remapped_indices)


def assign_box_uvs(bm, cfg=None, mat_list=None):
    """
    Assign deterministic triplanar box-projected UVs to a BMesh for shared library tiling materials,
    and center each contiguous R1_LampAmber island on UV (0.5, 0.5) so T_R1_LampAmber_Emission's
    radial hot core illuminates every lamp dome, indicator bar segment, and vent slot.
    """
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

    if not mat_list:
        return

    amber_indices = {
        idx for idx, mat in enumerate(mat_list) if mat is not None and mat.name.startswith("R1_LampAmber")
    }
    if not amber_indices:
        return

    amber_faces = [f for f in bm.faces if f.material_index in amber_indices]
    if not amber_faces:
        return

    visited = set()
    amber_set = set(amber_faces)
    for start_f in amber_faces:
        if start_f in visited:
            continue
        island = []
        queue = [start_f]
        visited.add(start_f)
        while queue:
            cur = queue.pop()
            island.append(cur)
            for v in cur.verts:
                for lf in v.link_faces:
                    if lf in amber_set and lf not in visited:
                        visited.add(lf)
                        queue.append(lf)

        avg_n = Vector((0.0, 0.0, 0.0))
        for f in island:
            avg_n += f.normal * max(f.calc_area(), 1e-8)
        ax, ay, az = abs(avg_n.x), abs(avg_n.y), abs(avg_n.z)

        us, vs = [], []
        for f in island:
            for loop in f.loops:
                co = loop.vert.co
                if ax >= ay and ax >= az:
                    u_val, v_val = co.y, co.z
                elif ay >= ax and ay >= az:
                    u_val, v_val = co.x, co.z
                else:
                    u_val, v_val = co.x, co.y
                us.append(u_val)
                vs.append(v_val)

        u_min, u_max = min(us), max(us)
        v_min, v_max = min(vs), max(vs)
        u_span = max(u_max - u_min, 1e-5)
        v_span = max(v_max - v_min, 1e-5)
        u_mid = 0.5 * (u_min + u_max)
        v_mid = 0.5 * (v_min + v_max)

        for f in island:
            for loop in f.loops:
                co = loop.vert.co
                if ax >= ay and ax >= az:
                    u_val, v_val = co.y, co.z
                elif ay >= ax and ay >= az:
                    u_val, v_val = co.x, co.z
                else:
                    u_val, v_val = co.x, co.y
                u_norm = 0.5 + ((u_val - u_mid) / u_span) * 0.44
                v_norm = 0.5 + ((v_val - v_mid) / v_span) * 0.44
                loop[uv_layer].uv = (u_norm, v_norm)


def _unity_rot_matrix(rot_euler):
    """Build a 4x4 Blender rotation Matrix from Unity Euler angles (rx, ry, rz) in radians."""
    if not any(abs(a) > UNIT_EPSILON for a in rot_euler):
        return Matrix.Identity(4)
    return (
        Matrix.Rotation(rot_euler[1], 4, "Z")
        @ Matrix.Rotation(rot_euler[0], 4, "X")
        @ Matrix.Rotation(rot_euler[2], 4, "Y")
    )


def _axis_align_matrix(axis):
    """Return a 4x4 rotation Matrix mapping local +Z onto Unity axis ('X', 'Y', or 'Z') in Blender space."""
    if axis == "Z":
        return Matrix.Rotation(-HALF_PI_RAD, 4, "X")
    if axis == "X":
        return Matrix.Rotation(HALF_PI_RAD, 4, "Y")
    return Matrix.Identity(4)


def _copy_temp_to_bm(temp, bm, trans_mat, default_mat_idx=0, preserve_face_mats=False):
    """Transform all verts/faces from temp into bm and free temp."""
    vert_map = {}
    for v in temp.verts:
        vert_map[v] = bm.verts.new(trans_mat @ v.co)
    for f in temp.faces:
        nf = bm.faces.new([vert_map[v] for v in f.verts])
        nf.material_index = f.material_index if preserve_face_mats else default_mat_idx
        nf.smooth = True
    temp.free()


def append_beveled_box(
    bm,
    size_xyz,
    center_xyz=(0.0, 0.0, 0.0),
    bevel_width=0.015,
    bevel_segments=2,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
    taper_top_xy=(1.0, 1.0),
    taper_front_xz=(1.0, 1.0),
    pillow_amount=0.0,
    subdiv_cuts=0,
    guard_inset=True,
):
    """
    Append a crisply beveled (and optionally tapered) box in Unity local coordinates (x, y, z).
    Automatically adds a coplanar support-loop inset on large panel faces after beveling so flat
    interiors have smudge-free normals and zero vertex-color wear bleed.
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

    orig_faces = list(temp.faces)
    if bevel_width > UNIT_EPSILON and bevel_segments > 0:
        eff_segments = 1 if max(sx, sy, sz) < 0.065 else bevel_segments
        sharp_edges = [e for e in temp.edges if e.calc_face_angle(0.0) > 0.30]
        if sharp_edges:
            bmesh.ops.bevel(
                temp,
                geom=sharp_edges,
                offset=bevel_width,
                offset_type="OFFSET",
                segments=eff_segments,
                profile=0.5,
                affect="EDGES",
                clamp_overlap=True,
            )

    if (
        guard_inset
        and subdiv_cuts == 0
        and abs(pillow_amount) <= UNIT_EPSILON
        and max(sx, sy, sz) >= 0.08
        and min(sx, sy, sz) > bevel_width * 3.8
    ):
        inset_t = min(bevel_width * 0.95, 0.015)
        valid_main = [
            f
            for f in orig_faces
            if f.is_valid and min((e.calc_length() for e in f.edges), default=0.0) > inset_t * 3.2
        ]
        if valid_main:
            bmesh.ops.inset_individual(
                temp,
                faces=valid_main,
                thickness=inset_t,
                depth=0.0,
                use_even_offset=True,
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
    trans_mat = Matrix.Translation(unity_to_blender_pos(cx, cy, cz)) @ _unity_rot_matrix(rot_euler)
    _copy_temp_to_bm(temp, bm, trans_mat, default_mat_idx=mat_idx)


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
        # Select ONLY cap-to-side rim edges (never vertical side edges between circumference segments)
        rim_edges = [
            e
            for e in temp.edges
            if len(e.link_faces) == 2
            and abs(abs(e.link_faces[0].normal.z) - abs(e.link_faces[1].normal.z)) > 0.45
        ]
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
    trans_mat = (
        Matrix.Translation(unity_to_blender_pos(*center_xyz))
        @ _unity_rot_matrix(rot_euler)
        @ _axis_align_matrix(axis)
    )
    _copy_temp_to_bm(temp, bm, trans_mat, default_mat_idx=mat_idx)


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
    rot_euler=(0.0, 0.0, 0.0),
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
    sx, sy, sz = scale_xyz
    scale_mat = Matrix.Diagonal((sx, sz, sy, 1.0))
    trans_mat = (
        Matrix.Translation(unity_to_blender_pos(*center_xyz))
        @ _unity_rot_matrix(rot_euler)
        @ scale_mat
        @ _axis_align_matrix(axis)
    )
    _copy_temp_to_bm(temp, bm, trans_mat, default_mat_idx=mat_idx)


def _chamfer_lathe_profile(profile_rz, ring_mat_indices, default_mat_idx, chamfer=0.0024):
    """
    Insert a crisp 2-point axial micro-chamfer at sharp interior corners (>28 deg) of a lathe polyline
    so turned brass rings and wheel rims have clean beveled specular highlights and no flat-cap normal bleed.
    """
    n = len(profile_rz)
    if n < 3 or chamfer <= UNIT_EPSILON:
        mats = [
            ring_mat_indices[k] if (ring_mat_indices is not None and k < len(ring_mat_indices)) else default_mat_idx
            for k in range(max(0, n - 1))
        ]
        return list(profile_rz), mats

    orig_mats = [
        ring_mat_indices[k] if (ring_mat_indices is not None and k < len(ring_mat_indices)) else default_mat_idx
        for k in range(n - 1)
    ]
    new_pts = [profile_rz[0]]
    new_mats = []

    for k in range(1, n - 1):
        r_prev, z_prev = profile_rz[k - 1]
        r_curr, z_curr = profile_rz[k]
        r_next, z_next = profile_rz[k + 1]
        dr0, dz0 = r_curr - r_prev, z_curr - z_prev
        dr1, dz1 = r_next - r_curr, z_next - z_curr
        len0 = math.hypot(dr0, dz0)
        len1 = math.hypot(dr1, dz1)
        m_prev = orig_mats[k - 1]
        m_next = orig_mats[k]

        if r_curr > 0.004 and len0 > chamfer * 2.6 and len1 > chamfer * 2.6:
            dot_dir = (dr0 * dr1 + dz0 * dz1) / (len0 * len1)
            if dot_dir < math.cos(math.radians(28.0)):
                c0 = min(chamfer, len0 * 0.28)
                c1 = min(chamfer, len1 * 0.28)
                p_a = (r_curr - (dr0 / len0) * c0, z_curr - (dz0 / len0) * c0)
                p_b = (r_curr + (dr1 / len1) * c1, z_curr + (dz1 / len1) * c1)
                new_pts.append(p_a)
                new_mats.append(m_prev)
                new_pts.append(p_b)
                new_mats.append(m_prev if m_prev == m_next else m_next)
                continue

        new_pts.append((r_curr, z_curr))
        new_mats.append(m_prev)

    new_pts.append(profile_rz[-1])
    new_mats.append(orig_mats[-1])
    return new_pts, new_mats


def append_lathe_profile(
    bm,
    profile_rz,
    axis="Z",
    center_xyz=(0.0, 0.0, 0.0),
    segments=24,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
    ring_mat_indices=None,
    auto_chamfer=True,
):
    """
    Revolve a 2D (radius, axial_z) polyline around Unity axis ('X', 'Y', or 'Z').
    Points with r <= 1e-6 become single pole vertices (triangle fan caps); rings connect with quads.
    """
    if len(profile_rz) < 2:
        return
    if auto_chamfer:
        pts_rz, seg_mats = _chamfer_lathe_profile(profile_rz, ring_mat_indices, mat_idx)
    else:
        pts_rz = list(profile_rz)
        seg_mats = [
            ring_mat_indices[k] if (ring_mat_indices is not None and k < len(ring_mat_indices)) else mat_idx
            for k in range(len(pts_rz) - 1)
        ]

    temp = bmesh.new()
    rings = []
    for r_val, z_val in pts_rz:
        if r_val <= UNIT_EPSILON:
            rings.append([temp.verts.new((0.0, 0.0, float(z_val)))])
        else:
            ring_v = []
            for i in range(segments):
                theta = FULL_CIRCLE_RAD * i / segments
                ring_v.append(
                    temp.verts.new((r_val * math.cos(theta), r_val * math.sin(theta), float(z_val)))
                )
            rings.append(ring_v)

    for k in range(len(rings) - 1):
        r0 = rings[k]
        r1 = rings[k + 1]
        m_idx = seg_mats[k] if k < len(seg_mats) else mat_idx
        if len(r0) == 1 and len(r1) == 1:
            continue
        if len(r0) == 1:
            pole = r0[0]
            for i in range(segments):
                ni = (i + 1) % segments
                f = temp.faces.new((pole, r1[ni], r1[i]))
                f.material_index = m_idx
                f.smooth = True
        elif len(r1) == 1:
            pole = r1[0]
            for i in range(segments):
                ni = (i + 1) % segments
                f = temp.faces.new((r0[i], r0[ni], pole))
                f.material_index = m_idx
                f.smooth = True
        else:
            for i in range(segments):
                ni = (i + 1) % segments
                if r0[i].co == r1[i].co and r0[ni].co == r1[ni].co:
                    continue
                f = temp.faces.new((r0[i], r0[ni], r1[ni], r1[i]))
                f.material_index = m_idx
                f.smooth = True

    ensure_outward_normals(temp)
    trans_mat = (
        Matrix.Translation(unity_to_blender_pos(*center_xyz))
        @ _unity_rot_matrix(rot_euler)
        @ _axis_align_matrix(axis)
    )
    _copy_temp_to_bm(temp, bm, trans_mat, default_mat_idx=mat_idx, preserve_face_mats=True)


def append_spherical_dome(
    bm,
    radius,
    dome_height,
    axis="Z",
    center_xyz=(0.0, 0.0, 0.0),
    segments=24,
    rings=5,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
    collar_depth=0.008,
):
    """
    Append a smooth convex spherical glass dome (with a short cylindrical seating skirt)
    oriented along Unity axis ('X', 'Y', or 'Z').
    """
    h = max(0.004, min(dome_height, radius * 0.95))
    sph_r = (radius * radius + h * h) / (2.0 * h)
    z_sph_center = h - sph_r
    alpha_max = math.asin(min(1.0, radius / sph_r))

    profile = [(radius * 0.96, -collar_depth), (radius, 0.0)]
    for k in range(1, rings):
        alpha = alpha_max * (1.0 - k / rings)
        rk = sph_r * math.sin(alpha)
        zk = z_sph_center + sph_r * math.cos(alpha)
        profile.append((rk, zk))
    profile.append((0.0, h))

    append_lathe_profile(
        bm,
        profile,
        axis=axis,
        center_xyz=center_xyz,
        segments=segments,
        mat_idx=mat_idx,
        rot_euler=rot_euler,
        auto_chamfer=False,
    )


def append_helical_coil(
    bm,
    coil_radius,
    wire_radius,
    length,
    turns=5.5,
    axis="Y",
    center_xyz=(0.0, 0.0, 0.0),
    segs_per_turn=10,
    wire_segs=5,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
):
    """Append a real 3D helical spring or wound wire coil along Unity axis ('X', 'Y', or 'Z')."""
    temp = bmesh.new()
    total_steps = max(4, int(round(turns * segs_per_turn)))
    ring_verts = []

    for step in range(total_steps + 1):
        t = step / total_steps
        theta = FULL_CIRCLE_RAD * turns * t
        z_c = (-0.5 + t) * length
        cx = coil_radius * math.cos(theta)
        cy = coil_radius * math.sin(theta)
        center_pt = Vector((cx, cy, z_c))
        radial = Vector((math.cos(theta), math.sin(theta), 0.0))
        tangent = Vector((-coil_radius * FULL_CIRCLE_RAD * turns * math.sin(theta),
                          coil_radius * FULL_CIRCLE_RAD * turns * math.cos(theta),
                          length)).normalized()
        binorm = tangent.cross(radial).normalized()

        ring = []
        for j in range(wire_segs):
            phi = FULL_CIRCLE_RAD * j / wire_segs
            pt = center_pt + wire_radius * (math.cos(phi) * radial + math.sin(phi) * binorm)
            ring.append(temp.verts.new(pt))
        ring_verts.append(ring)

    for step in range(total_steps):
        r0 = ring_verts[step]
        r1 = ring_verts[step + 1]
        for j in range(wire_segs):
            nj = (j + 1) % wire_segs
            f = temp.faces.new((r0[j], r0[nj], r1[nj], r1[j]))
            f.smooth = True

    f_start = temp.faces.new(list(reversed(ring_verts[0])))
    f_start.smooth = True
    f_end = temp.faces.new(ring_verts[-1])
    f_end.smooth = True

    ensure_outward_normals(temp)
    trans_mat = (
        Matrix.Translation(unity_to_blender_pos(*center_xyz))
        @ _unity_rot_matrix(rot_euler)
        @ _axis_align_matrix(axis)
    )
    _copy_temp_to_bm(temp, bm, trans_mat, default_mat_idx=mat_idx)


def append_prism_extrusion(
    bm,
    poly_2d,
    depth,
    axis="X",
    center_xyz=(0.0, 0.0, 0.0),
    bevel_width=0.008,
    bevel_segments=2,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
):
    """
    Extrude a 2D polygon [(u, v), ...] along Unity axis ('X', 'Y', or 'Z') by depth:
      - axis='X': (u, v) = (uz, uy), extruded along ux in [-depth/2, +depth/2]
      - axis='Z': (u, v) = (ux, uy), extruded along uz in [-depth/2, +depth/2]
      - axis='Y': (u, v) = (ux, uz), extruded along uy in [-depth/2, +depth/2]
    """
    if len(poly_2d) < 3:
        return
    temp = bmesh.new()
    half_d = depth * FLAT_UV_SCALE
    front_ring = []
    back_ring = []

    for u_val, v_val in poly_2d:
        if axis == "X":
            p_neg = unity_to_blender_pos(-half_d, v_val, u_val)
            p_pos = unity_to_blender_pos(half_d, v_val, u_val)
        elif axis == "Z":
            p_neg = unity_to_blender_pos(u_val, v_val, -half_d)
            p_pos = unity_to_blender_pos(u_val, v_val, half_d)
        else:
            p_neg = unity_to_blender_pos(u_val, -half_d, v_val)
            p_pos = unity_to_blender_pos(u_val, half_d, v_val)
        back_ring.append(temp.verts.new(p_neg))
        front_ring.append(temp.verts.new(p_pos))

    n_pts = len(poly_2d)
    for i in range(n_pts):
        ni = (i + 1) % n_pts
        f = temp.faces.new((back_ring[i], back_ring[ni], front_ring[ni], front_ring[i]))
        f.smooth = True

    f_b = temp.faces.new(list(reversed(back_ring)))
    f_b.smooth = True
    f_f = temp.faces.new(front_ring)
    f_f.smooth = True

    ensure_outward_normals(temp)

    if bevel_width > UNIT_EPSILON and bevel_segments > 0:
        eff_segments = 1 if depth < 0.045 else bevel_segments
        sharp_edges = [e for e in temp.edges if e.calc_face_angle(0.0) > 0.45]
        if sharp_edges:
            bmesh.ops.bevel(
                temp,
                geom=sharp_edges,
                offset=bevel_width,
                offset_type="OFFSET",
                segments=eff_segments,
                profile=0.5,
                affect="EDGES",
                clamp_overlap=True,
            )
            ensure_outward_normals(temp)

    trans_mat = Matrix.Translation(unity_to_blender_pos(*center_xyz)) @ _unity_rot_matrix(rot_euler)
    _copy_temp_to_bm(temp, bm, trans_mat, default_mat_idx=mat_idx)


def append_hex_bolt(
    bm,
    radius,
    height,
    axis="Z",
    center_xyz=(0.0, 0.0, 0.0),
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
    include_washer=True,
):
    """Append a compact 6-sided hex bolt head with a chamfered crown and optional washer base."""
    hh = height * 0.5
    profile = (
        [(radius * 1.24, -hh), (radius * 1.24, -hh * 0.35), (radius, -hh * 0.35)]
        if include_washer
        else [(radius, -hh)]
    )
    profile.extend([
        (radius, hh * 0.65),
        (radius * 0.78, hh),
        (0.0, hh),
    ])
    append_lathe_profile(
        bm,
        profile,
        axis=axis,
        center_xyz=center_xyz,
        segments=6,
        mat_idx=mat_idx,
        rot_euler=rot_euler,
        auto_chamfer=False,
    )


def append_dome_rivet(
    bm,
    radius,
    height,
    axis="X",
    center_xyz=(0.0, 0.0, 0.0),
    segments=8,
    mat_idx=0,
    rot_euler=(0.0, 0.0, 0.0),
):
    """Append a compact flanged hemispherical button-head rivet oriented along Unity axis ('X', 'Y', or 'Z')."""
    profile = [
        (radius * 1.15, -height * 0.20),
        (radius, 0.0),
        (radius * 0.78, height * 0.56),
        (radius * 0.42, height * 0.88),
        (0.0, height),
    ]
    append_lathe_profile(
        bm,
        profile,
        axis=axis,
        center_xyz=center_xyz,
        segments=segments,
        mat_idx=mat_idx,
        rot_euler=rot_euler,
        auto_chamfer=False,
    )


def compute_mesh_curvature(mesh, cfg):
    """
    Compute per-vertex and per-face (convex_wear, concave_cavity) in [0, 1] from real beveled mesh geometry.
    Uses face-width gating across edges so narrow bevel strips, ridges, arch lips, visor brims, and
    scoop lips receive strong convex edge wear (0.88..1.00) while flat panel interiors remain strictly 0.0.
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
    face_touching_flat = [False] * n_faces

    min_angle = cfg["curvature_min_angle"]
    ref_len = cfg["curvature_ref_edge_len"]

    # Precompute approximate strip width of every face: area / max_edge_len
    face_widths = [0.0] * n_faces
    for f in bm.faces:
        max_elen = max((e.calc_length() for e in f.edges), default=cfg["curvature_min_edge_len"])
        face_widths[f.index] = f.calc_area() / max(max_elen, cfg["curvature_min_edge_len"])

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

        w0 = face_widths[f0.index]
        w1 = face_widths[f1.index]
        bevel_strip_width = max(min(w0, w1), cfg["curvature_min_edge_len"])

        # Positive when normals diverge across the edge (convex ridge/bevel); negative when they converge (concave crease)
        alignment = (n1 - n0).dot(delta_c.normalized())
        if abs(alignment) < 0.03:
            continue

        density_boost = min(cfg["curvature_max_density_boost"], math.sqrt(ref_len / bevel_strip_width))
        strength = ((angle - min_angle) / (HALF_PI_RAD * 0.42)) * density_boost

        # A face is a bevel/lip/rim strip if its strip width is narrow (< 1.65 cm, or < 3.8 cm on sharp >35 deg edges)
        f0_is_bevel = (w0 <= 2.2 * w1) and (w0 < 0.0165 or (w0 < 0.038 and angle > 0.62))
        f1_is_bevel = (w1 <= 2.2 * w0) and (w1 < 0.0165 or (w1 < 0.038 and angle > 0.62))

        if alignment > 0.0:
            if f0_is_bevel:
                face_convex[f0.index] = max(face_convex[f0.index], strength)
            else:
                face_touching_flat[f0.index] = True
            if f1_is_bevel:
                face_convex[f1.index] = max(face_convex[f1.index], strength)
            else:
                face_touching_flat[f1.index] = True
            if f0_is_bevel or f1_is_bevel:
                for v in edge.verts:
                    convex_v[v.index] = max(convex_v[v.index], strength)
        else:
            # Concave seams and grooves always register strong cavity curvature on narrow groove walls/bottoms
            if w0 < 0.065:
                face_concave[f0.index] = max(face_concave[f0.index], strength * 1.25)
            if w1 < 0.065:
                face_concave[f1.index] = max(face_concave[f1.index], strength * 1.25)
            for v in edge.verts:
                concave_v[v.index] = max(concave_v[v.index], strength * 1.25)

    # Ensure flat panel interiors and broad smooth contour fillets (>= 1.65 cm wide) stay strictly at 0.0 convex wear
    for f_idx in range(n_faces):
        if face_widths[f_idx] >= 0.0165 and face_touching_flat[f_idx]:
            face_convex[f_idx] = 0.0

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
      R = ambient occlusion (1 = open, dark under visor, inside wheel arches, inside scoop, in seams)
      G = edge wear amount (strong on exposed convex bevels/rims, 0.0 on panel interiors & smooth contours)
      B = crease/cavity dust (strong in seams, wheel arches, tread grooves, and lower body)
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

    optical_mats = {"R1_LampAmber", "R1_DarkGlass", "R1_Screen", "R1_SolarCell"}
    enamel_mats = {"R1_CreamEnamel", "R1_OrangeStripe", "R1_SagePatch"}

    for obj in mesh_objects:
        mesh = obj.data
        ca = mesh.color_attributes[COLOR_ATTR_NAME]
        convex_v, concave_v, face_c, face_v = compute_mesh_curvature(mesh, cfg)
        world_mat = obj.matrix_world
        rot_mat = world_mat.to_3x3()

        # Precompute world-space Unity position (ux, uy, uz) per vertex
        v_unity_pos = []
        for v in mesh.vertices:
            wp = world_mat @ v.co
            v_unity_pos.append((wp.x, wp.z, wp.y))

        for poly in mesh.polygons:
            p_idx = poly.index
            fc_val = face_c[p_idx] if p_idx < len(face_c) else 0.0
            fv_val = face_v[p_idx] if p_idx < len(face_v) else 0.0
            wn = (rot_mat @ poly.normal).normalized()
            un_x, un_y, un_z = wn.x, wn.z, wn.y

            mat_name = ""
            if poly.material_index < len(mesh.materials) and mesh.materials[poly.material_index] is not None:
                mat_name = mesh.materials[poly.material_index].name

            is_optical = mat_name in optical_mats
            is_enamel = mat_name in enamel_mats

            for loop_idx in poly.loop_indices:
                v_idx = mesh.loops[loop_idx].vertex_index
                ux, uy, uz = v_unity_pos[v_idx]
                baked_ao = float(ca.data[loop_idx].color[0])
                ao_val = min(1.0, max(cfg["ao_min_floor"], math.pow(max(0.0, baked_ao), cfg["ao_gamma"])))

                if is_optical:
                    ca.data[loop_idx].color = (max(ao_val, 0.92), 0.0, 0.0, DEFAULT_ALPHA)
                    continue

                # Deepen AO under visor brow, inside wheel arches, inside scoop bucket, and in concave seams
                if include_ground_occluder:
                    # 1. Under visor overhang / inner visor soffit
                    if 1.00 <= uy <= 1.52 and 0.24 <= uz <= 0.54 and abs(ux) <= 0.40:
                        if un_y < -0.15 or abs(un_x) > 0.65:
                            ao_val = min(ao_val, 0.22)
                    # 2. Inside wheel arches & suspension pockets
                    for axle_z in (-cfg["wheel_base"], cfg["wheel_base"]):
                        if abs(uz - axle_z) < 0.36 and 0.28 <= uy <= 0.64 and 0.34 <= abs(ux) <= 0.56:
                            ao_val = min(ao_val, 0.24)
                    # 3. Inside hollow scoop bucket
                    if uz > 0.78 and uy < 0.38 and abs(ux) < 0.32 and (un_y > 0.15 or un_z < -0.15):
                        ao_val = min(ao_val, 0.25)

                if fv_val > 0.35:
                    ao_val = min(ao_val, max(cfg["ao_min_floor"], 1.0 - 0.68 * fv_val))

                # Edge wear (G): crisp chipped paint on narrow 1-2 cm enamel bevels (0.60..0.82)
                # and strong metal edge wear (0.80..1.00) on bumper bars, arch lips, visor brim & scoop lip
                cv_val = convex_v[v_idx] if v_idx < len(convex_v) else 0.0
                if fc_val > 0.08 and cv_val > 0.10:
                    raw_wear = 0.55 * fc_val + 0.45 * cv_val
                    if is_enamel:
                        wear_g = min(0.82, 0.58 + 0.24 * raw_wear)
                    else:
                        wear_g = min(1.0, 0.78 + 0.22 * raw_wear)
                else:
                    wear_g = 0.0

                # Cavity dust (B): strong in seams, wheel arches, tread grooves, and lower body
                cv_concave = concave_v[v_idx] if v_idx < len(concave_v) else 0.0
                geom_cav = max(fv_val, cv_concave * (0.45 + 0.55 * fv_val)) if fv_val > 0.04 else 0.0
                ao_cavity = max(0.0, (0.74 - ao_val) * cfg["cavity_from_ao_weight"])
                lower_body_dust = 0.0
                if include_ground_occluder and uy < 0.62:
                    lower_body_dust = max(0.0, min(0.78, (0.62 - uy) / 0.58 * 0.72))
                dust_b = min(1.0, max(geom_cav, ao_cavity, lower_body_dust * (0.55 + 0.45 * (1.0 - ao_val))))

                ca.data[loop_idx].color = (ao_val, wear_g, dust_b, DEFAULT_ALPHA)


def export_fbx(filepath, objects_to_export):
    """Export selected objects to FBX with Unity coordinate axes (+Z forward, +Y up) and linear vertex colors."""
    import sys
    from pathlib import Path

    blender_dir = Path(__file__).resolve().parent.parent
    if str(blender_dir) not in sys.path:
        sys.path.insert(0, str(blender_dir))
    import r1kit

    r1kit._install_deterministic_fbx_hooks()
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

