from __future__ import annotations

import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT / "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity"
TEMPLATE = ROOT / "Assets/Materials/PPE/Scene Unlit/rubber_boots_3d_model_Unlit.mat"
MATERIAL_ROOT = ROOT / "Assets/Materials/PPE/Scene Unlit"
METAL_FBX_GUID = "f62f096ede7f4f0438815bc7c647cf24"
METAL_ROOT_TRANSFORM = "6466164252249718520"
BOOTS_TEX_GUID = "5b4e7a852e926504bb9303925535a6f4"
NS = uuid.UUID("8f3a1c2e-4b5d-6789-abcd-ef0123456789")
BOOTS_NAME = "rubber_boots_3d_model_Unlit"


def guid_for(name: str) -> str:
    return uuid.uuid5(NS, name).hex


def write_text(path: Path, text: str) -> None:
    path.write_bytes(text.replace("\r\n", "\n").encode("utf-8"))


def read_texture_guid(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    match = re.search(r"^guid: ([0-9a-f]{32})$", text, re.MULTILINE)
    if not match:
        raise SystemExit(f"Missing guid in {path}")
    return match.group(1)


def folder_meta(guid: str) -> str:
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "folderAsset: yes\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def material_meta(guid: str) -> str:
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 2100000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )


def write_unlit(folder: Path, name: str, tex_guid: str, mat_guid: str) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    template = TEMPLATE.read_text(encoding="utf-8")
    text = template.replace(f"m_Name: {BOOTS_NAME}", f"m_Name: {name}")
    text = text.replace(f"guid: {BOOTS_TEX_GUID}", f"guid: {tex_guid}")
    write_text(folder / f"{name}.mat", text)
    write_text(folder / f"{name}.mat.meta", material_meta(mat_guid))


def remap_block(entries: list[tuple[str, str]]) -> str:
    lines = ["  externalObjects:"]
    for source_name, mat_guid in entries:
        lines.extend(
            [
                "  - first:",
                "      type: UnityEngine:Material",
                "      assembly: UnityEngine.CoreModule",
                f"      name: {source_name}",
                f"    second: {{fileID: 2100000, guid: {mat_guid}, type: 2}}",
            ]
        )
    return "\n".join(lines) + "\n"


def patch_fbx_meta(path: Path, entries: list[tuple[str, str]]) -> None:
    text = path.read_text(encoding="utf-8")
    if "externalObjects: {}" not in text:
        return
    text = re.sub(
        r"  externalObjects: \{\}\n",
        remap_block(entries),
        text,
        count=1,
    )
    if "externalObjects: {}" in text:
        raise SystemExit(f"Failed to insert remaps in {path}")
    write_text(path, text)


def metal_texture_path(part: int) -> Path:
    folder = ROOT / "Assets/FBX/PPE_B_MetalShelving"
    tripo = folder / f"PPE_B_MetalShelving_tripo_part_{part}_basecolor.JPEG.meta"
    short = folder / f"PPE_B_MetalShelving_part_{part}_basecolor.JPEG.meta"
    if tripo.exists():
        return tripo
    if short.exists():
        return short
    raise SystemExit(f"Missing metal shelving texture meta for part {part}")


def parse_unity_objects(text: str) -> tuple[str, list[tuple[str, str, str]]]:
    pattern = re.compile(r"^--- !u!(\d+) &(-?\d+)\r?\n", re.MULTILINE)
    matches = list(pattern.finditer(text))
    if not matches:
        raise SystemExit("Scene YAML has no Unity objects")
    header = text[: matches[0].start()]
    objects = []
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        objects.append((match.group(1), match.group(2), text[match.start() : end]))
    return header, objects


def patch_scene(part_mat_guids: dict[int, str]) -> int:
    text = SCENE.read_text(encoding="utf-8")
    _, objects = parse_unity_objects(text)
    go_by_id = {}
    for class_id, file_id, block in objects:
        if class_id != "1":
            continue
        name_match = re.search(r"^  m_Name: (tripo_part_\d+)\s*$", block, re.MULTILINE)
        if not name_match:
            continue
        components = re.findall(r"- component: \{fileID: (-?\d+)\}", block)
        go_by_id[file_id] = (name_match.group(1), components)

    renderer_part = {}
    for class_id, file_id, block in objects:
        if class_id != "23":
            continue
        go_match = re.search(r"^  m_GameObject: \{fileID: (-?\d+)\}\s*$", block, re.MULTILINE)
        if not go_match:
            continue
        go_id = go_match.group(1)
        if go_id not in go_by_id:
            continue
        name, components = go_by_id[go_id]
        if file_id not in components:
            continue
        father_ok = False
        for class2, fid2, block2 in objects:
            if class2 != "4" or fid2 not in components:
                continue
            if f"m_Father: {{fileID: {METAL_ROOT_TRANSFORM}}}" in block2:
                father_ok = True
                break
        if not father_ok:
            continue
        renderer_part[file_id] = int(name.split("_")[-1])

    replaced = 0
    new_text = text
    for class_id, file_id, block in objects:
        if class_id != "23" or file_id not in renderer_part:
            continue
        part = renderer_part[file_id]
        mat_guid = part_mat_guids[part]
        new_block, count = re.subn(
            rf"^  - \{{fileID: -?\d+, guid: {METAL_FBX_GUID}, type: 3\}}\s*$",
            f"  - {{fileID: 2100000, guid: {mat_guid}, type: 2}}",
            block,
            count=1,
            flags=re.MULTILINE,
        )
        if count == 0:
            if f"guid: {mat_guid}, type: 2" in block:
                continue
            raise SystemExit(
                f"MeshRenderer {file_id} (tripo_part_{part}) had no FBX material to replace"
            )
        if block not in new_text:
            raise SystemExit(f"MeshRenderer block {file_id} was not found in scene text")
        new_text = new_text.replace(block, new_block, 1)
        replaced += count

    if replaced == 0:
        return 0

    write_text(SCENE, new_text)
    verify = SCENE.read_text(encoding="utf-8")
    leftover = len(re.findall(
        rf"m_Materials:\r?\n  - \{{fileID: -?\d+, guid: {METAL_FBX_GUID}, type: 3\}}",
        verify,
    ))
    if leftover:
        raise SystemExit(
            f"Wrote scene but {leftover} MetalShelving FBX materials remain. "
            "Unity may have the scene open; run Tools > PPE > Convert SCBA Shelving Plank Materials to URP Unlit."
        )
    return replaced


def main() -> None:
    scba_tex = read_texture_guid(
        ROOT / "Assets/FBX/PPE_A_SCBA_Cylinder/PPE_A_SCBA_Cylinder_basecolor.jpg.meta"
    )
    plank_tex = read_texture_guid(
        ROOT / "Assets/FBX/PPE_B_WoodenPlank/PPE_B_WoodenPlank.jpg.meta"
    )
    scba_guid = guid_for("PPE_A_SCBA_Cylinder_Unlit")
    plank_guid = guid_for("PPE_B_WoodenPlank_Unlit")

    folders = {
        "SCBA": guid_for("folder:SCBA"),
        "WoodenPlank": guid_for("folder:WoodenPlank"),
        "MetalShelving": guid_for("folder:MetalShelving"),
    }
    for name, folder_guid in folders.items():
        folder = MATERIAL_ROOT / name
        folder.mkdir(parents=True, exist_ok=True)
        write_text(MATERIAL_ROOT / f"{name}.meta", folder_meta(folder_guid))

    write_unlit(MATERIAL_ROOT / "SCBA", "PPE_A_SCBA_Cylinder_Unlit", scba_tex, scba_guid)
    write_unlit(
        MATERIAL_ROOT / "WoodenPlank",
        "PPE_B_WoodenPlank_Unlit",
        plank_tex,
        plank_guid,
    )

    part_mat_guids: dict[int, str] = {}
    metal_entries: list[tuple[str, str]] = []
    for part in range(30):
        tex_guid = read_texture_guid(metal_texture_path(part))
        mat_guid = guid_for(f"PPE_B_MetalShelving_part_{part}_Unlit")
        part_mat_guids[part] = mat_guid
        write_unlit(
            MATERIAL_ROOT / "MetalShelving",
            f"PPE_B_MetalShelving_part_{part}_Unlit",
            tex_guid,
            mat_guid,
        )
        for source_name in (
            f"tripo_part_{part}",
            f"tripo_part_{part}_material",
            f"Material_tripo_part_{part}",
            f"PPE_B_MetalShelving_tripo_part_{part}_basecolor",
            f"PPE_B_MetalShelving_part_{part}_basecolor",
        ):
            metal_entries.append((source_name, mat_guid))

    patch_fbx_meta(
        ROOT / "Assets/FBX/PPE_A_SCBA_Cylinder/PPE_A_SCBA_Cylinder.fbx.meta",
        [
            ("Material", scba_guid),
            ("No Name", scba_guid),
            ("ppe_a_scba_cylinder_basecolor", scba_guid),
            ("PPE_A_SCBA_Cylinder_basecolor", scba_guid),
            (
                "tripo_node_a2a2c560-8eca-45fb-bfc4-954d48823762_material",
                scba_guid,
            ),
        ],
    )
    patch_fbx_meta(
        ROOT / "Assets/FBX/PPE_B_WoodenPlank/PPE_B_WoodenPlank.fbx.meta",
        [
            ("Material", plank_guid),
            ("No Name", plank_guid),
            ("tripo_mat_68cb379c", plank_guid),
            ("PPE_B_WoodenPlank", plank_guid),
            ("tripo_image_68cb379c_0", plank_guid),
        ],
    )
    patch_fbx_meta(
        ROOT / "Assets/FBX/PPE_B_MetalShelving/PPE_B_MetalShelving.fbx.meta",
        metal_entries,
    )

    replaced = patch_scene(part_mat_guids)
    print(f"Created Unlit materials and remapped FBX files.")
    print(f"Patched MetalShelving MeshRenderers: {replaced}")
    print(f"SCBA material guid: {scba_guid}")
    print(f"WoodenPlank material guid: {plank_guid}")


if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1 and sys.argv[1] == "--scene-only":
        part_mat_guids = {
            part: guid_for(f"PPE_B_MetalShelving_part_{part}_Unlit")
            for part in range(30)
        }
        replaced = patch_scene(part_mat_guids)
        print(f"Patched MetalShelving MeshRenderers: {replaced}")
    else:
        main()
