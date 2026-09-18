import collections
import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
DATA = ROOT / 'Assets/_Game/Data/Maps'


class LevelAssetTests(unittest.TestCase):
    def test_every_core_folder_has_markdown(self):
        root = ROOT / 'Assets/_Core'
        for folder in [root] + [p for p in root.rglob('*') if p.is_dir()]:
            with self.subTest(folder=folder.relative_to(ROOT)):
                self.assertTrue(list(folder.glob('*.md')), 'Core folder needs Markdown')

    def test_each_level_contains_map_and_queues(self):
        levels = list(DATA.glob('*.json'))
        self.assertTrue(levels)
        for path in levels:
            with self.subTest(level=path.name):
                data = json.loads(path.read_text(encoding='utf-8-sig'))
                self.assertEqual(len(data['cells']), data['rows'] * data['columns'])
                self.assertTrue('queues' in data, 'Missing combined queues')
                colors = {entry['id'] for entry in data['palette']}
                self.assertTrue(set(data['cells']) <= colors | {0})
                self.assertLessEqual(len(data['queues']), 3)
                for queue in data['queues']:
                    for box in queue['boxes']:
                        self.assertIn(box['colorId'], colors)
                        self.assertGreater(box['antCount'], 0)

    def test_full_levels_match_box_budget_to_cells(self):
        for path in DATA.glob('*.json'):
            with self.subTest(level=path.name):
                data = json.loads(path.read_text(encoding='utf-8-sig'))
                cells = collections.Counter(c for c in data['cells'] if c)
                boxes = collections.Counter()
                for queue in data.get('queues', []):
                    for box in queue['boxes']:
                        boxes[box['colorId']] += box['antCount']
                self.assertEqual(cells, boxes)

    def test_standalone_queue_source_is_removed(self):
        self.assertFalse((ROOT / 'Assets/_Game/Data/BoxQueues').exists())
        self.assertFalse((ROOT / 'Assets/_Game/_GamePlay/Script/Map/Ant/BoxQueueJsonLoader.cs').exists())
        self.assertFalse((ROOT / 'Assets/_Game/_GamePlay/Script/Map/Editor/GameplaySceneGuard.cs').exists())

    def test_owned_scripts_use_no_exceptions_or_hierarchy_search(self):
        forbidden = re.compile(r'\b(?:throw|try|catch)\b|\bGetComponents?In(?:Children|Parent)\s*[<(]|\bGameObject\.Find\s*\(|(?<!Shader)\.Find\s*\(')
        for directory in ('Assets/_Game', 'Assets/_Core'):
            for path in (ROOT / directory).rglob('*.cs'):
                with self.subTest(script=path.relative_to(ROOT)):
                    source = re.sub(r'//[^\n]*|/\*.*?\*/', '', path.read_text(encoding='utf-8-sig'), flags=re.S)
                    self.assertIsNone(forbidden.search(source))

    def test_scene_has_one_json_reference(self):
        scene = (ROOT / 'Assets/_Game/_GamePlay/Scenes/MapDemo.unity').read_text(encoding='utf-8-sig')
        self.assertFalse('boxQueuesJson:' in scene, 'Scene still has separate queue JSON')
        self.assertFalse('\n  boxes:' in scene, 'Scene still has fallback boxes')
        self.assertEqual(len(re.findall(r'^  mapJson:', scene, re.M)), 1)

    def test_project_scripts_have_no_legacy_queue_api(self):
        for path in (ROOT / 'Assets/_Game').rglob('*.cs'):
            with self.subTest(script=path.name):
                source = path.read_text(encoding='utf-8-sig')
                self.assertNotRegex(source, r'\b(?:DemoBoxData|BoxQueueJsonLoader|boxQueuesJson|ConfigureQueues)\b')

    def test_prefabs_have_explicit_component_references(self):
        prefabs = ROOT / 'Assets/_Game/_GamePlay/Prefabs'
        required = {
            'RoundedMapCell.prefab': ('renderers',),
            'ColorBox.prefab': ('countLabel', 'face', 'countCanvas', 'hitCollider', 'bodyRenderers'),
            'Ant.prefab': ('carriedBrick', 'carriedRenderer', 'abdomen'),
        }
        for name, fields in required.items():
            with self.subTest(prefab=name):
                text = (prefabs / name).read_text(encoding='utf-8-sig')
                ids = set(re.findall(r'^--- !u!\d+ &(-?\d+)', text, re.M))
                for field in fields:
                    match = re.search(r'^  ' + field + r':(?:\n  -)? \{fileID: (-?\d+)\}', text, re.M)
                    self.assertIsNotNone(match, 'Missing reference: ' + field)
                    self.assertIn(match[1], ids, 'Missing component fileID: ' + field)

    def test_scene_cell_prefab_points_to_cell_view(self):
        prefabs = ROOT / 'Assets/_Game/_GamePlay/Prefabs'
        text = (prefabs / 'RoundedMapCell.prefab').read_text(encoding='utf-8-sig')
        component = re.search(r'^--- !u!114 &(\d+)\n(?:(?!^---).)*?guid: f7a61779320004744a4c807d8b98e6fd', text, re.M | re.S)
        scene = (ROOT / 'Assets/_Game/_GamePlay/Scenes/MapDemo.unity').read_text(encoding='utf-8-sig')
        reference = re.search(r'^  cellPrefab: \{fileID: (\d+),', scene, re.M)
        self.assertEqual(component[1], reference[1])

    def test_map_and_art_layout_are_preserved(self):
        expected = {'demo-map.json': (20, 20, 202), 'demo-map-small.json': (20, 20, 202),
                    'star-map.json': (13, 13, 99), 'map1.json': (26, 26, 676)}
        for name, (rows, columns, count) in expected.items():
            data = json.loads((DATA / name).read_text(encoding='utf-8-sig'))
            self.assertEqual((rows, columns, count),
                             (data['rows'], data['columns'], sum(c != 0 for c in data['cells'])))


if __name__ == '__main__':
    unittest.main()
