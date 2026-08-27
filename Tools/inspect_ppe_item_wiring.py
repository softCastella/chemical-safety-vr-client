"""Read-only comparison of PPE item wiring between Unity scenes.

Parses scene YAML and prints one row per GameObject that owns a
PPEActionPanelController, so helmet and mask configuration can be compared
against the other PPE items. This script never writes to any file.

Usage:
    python Tools/inspect_ppe_item_wiring.py <scene> [<scene> ...]

A scene argument may be a path in the working tree, or "git:<rev>:<path>" to
read a committed version instead.
"""
import re
import subprocess
import sys


def load(spec):
    if spec.startswith('git:'):
        raw = subprocess.run(['git', 'show', spec[4:]], capture_output=True, check=True).stdout
        return raw.decode('utf-8', errors='replace')
    with open(spec, 'r', encoding='utf-8', errors='replace') as handle:
        return handle.read()


class Scene:
    def __init__(self, text):
        self.lines = text.split('\n')
        self.starts = []
        for i, line in enumerate(self.lines):
            m = re.match(r'--- !u!(\d+) &(\d+)', line)
            if m:
                self.starts.append((i, m.group(1), m.group(2)))

        self.blocks, self.go, self.tr, self.tr_of_go, self.comps = {}, {}, {}, {}, {}
        for k, (ln, kind, fid) in enumerate(self.starts):
            t = self._body(k)
            self.blocks[fid] = (kind, t)
            if kind == '1':
                n = re.search(r'm_Name: ([^\n]*)', t)
                a = re.search(r'm_IsActive: (\d)', t)
                if n:
                    self.go[fid] = (n.group(1).strip(), a.group(1) if a else '?')
                    self.comps[fid] = re.findall(r'- component: \{fileID: (\d+)\}', t)
            elif kind in ('4', '224'):
                g = re.search(r'm_GameObject: \{fileID: (\d+)\}', t)
                f = re.search(r'm_Father: \{fileID: (\d+)\}', t)
                if g:
                    self.tr[fid] = (g.group(1), f.group(1) if f else '0')
                    self.tr_of_go[g.group(1)] = fid

    def _body(self, k):
        s = self.starts[k][0]
        e = self.starts[k + 1][0] if k + 1 < len(self.starts) else len(self.lines)
        return '\n'.join(self.lines[s + 1:e])

    def path(self, gid):
        parts, seen = [], set()
        tid = self.tr_of_go.get(gid)
        while tid and tid in self.tr and tid not in seen:
            seen.add(tid)
            parts.append(self.go.get(self.tr[tid][0], ('?', '?'))[0])
            tid = self.tr[tid][1]
            if tid == '0':
                break
        return '/'.join(reversed(parts))

    def alive(self, gid):
        tid = self.tr_of_go.get(gid)
        seen = set()
        while tid and tid in self.tr and tid not in seen:
            seen.add(tid)
            if self.go.get(self.tr[tid][0], ('?', '?'))[1] == '0':
                return False
            tid = self.tr[tid][1]
            if tid == '0':
                break
        return True

    def cls(self, fid):
        _, t = self.blocks.get(fid, ('', ''))
        m = re.search(r'm_EditorClassIdentifier: [^:]*::(\w+)', t)
        return m.group(1) if m else ''

    def owner(self, fid):
        _, t = self.blocks.get(fid, ('', ''))
        m = re.search(r'm_GameObject: \{fileID: (\d+)\}', t)
        return m.group(1) if m else None


def field(text, name):
    m = re.search(rf'\n  {name}: ([^\n]*)', text)
    return m.group(1).strip() if m else '-'


def describe_ref(scene, value):
    m = re.match(r'\{fileID: (\d+)\}$', value)
    if not m:
        return value or '(empty)'
    if m.group(1) == '0':
        return 'none'
    target = m.group(1)
    gid = scene.owner(target) or (target if target in scene.go else None)
    if gid is None:
        return 'MISSING'
    return 'set' if scene.alive(gid) else 'DEAD'


def table(label, spec):
    scene = Scene(load(spec))
    print(f'===== {label} =====')
    print(f'{"item":26} {"alive":5} {"insp":4} {"anim":5} {"req":3} '
          f'{"useSfx":9} {"voice":6} {"panel":5} {"appearance":28} {"grab":4}')
    rows = []
    for fid in scene.blocks:
        if scene.cls(fid) != 'PPEActionPanelController':
            continue
        gid = scene.owner(fid)
        _, text = scene.blocks[fid]
        siblings = {scene.cls(c) for c in scene.comps.get(gid, [])}
        appearance = ','.join(sorted(c for c in siblings if 'Appearance' in c)) or '-'
        rows.append((
            scene.path(gid).replace('PPE/', ''),
            'yes' if scene.alive(gid) else 'no',
            field(text, 'enableInspectChoice'),
            describe_ref(scene, field(text, 'inspectAnimation')),
            field(text, 'requireInspectBeforeUseOrDiscard'),
            field(text, 'useSfxId') or '(empty)',
            describe_ref(scene, field(text, 'voiceFlowDirector')),
            describe_ref(scene, field(text, 'panelRoot')),
            appearance,
            'yes' if 'PPEMarkerToggleGrab' in siblings else 'NO'))

    for r in sorted(rows):
        print(f'{r[0]:26} {r[1]:5} {r[2]:4} {r[3]:5} {r[4]:3} '
              f'{r[5]:9} {r[6]:6} {r[7]:5} {r[8]:28} {r[9]:4}')
    print()


def main():
    scenes = sys.argv[1:]
    if not scenes:
        print(__doc__)
        return 1
    for spec in scenes:
        table(spec, spec)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
