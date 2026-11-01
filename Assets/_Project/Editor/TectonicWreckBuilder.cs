using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static IronSyndicate.EditorTools.SceneBuilderUtil;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// Tectonic Wreck (own layout): an industrial district torn by a rift. Two plateaus north and south of an
    /// 8 m deep trench; the trench runs under two roofed tunnel sections and climbs out at both ends.
    /// Crossings: tunnel roofs, end connectors, a jump ramp and an elevated highway whose span only C4 breaks.
    /// Teleporter in a safety cage (north) up onto the highway deck, Tesla reactor with four pillars that drop a
    /// weapon once each, destructible WRECK letters and a depot with hidden weapons behind a breakable door.
    /// Five spawns and a hand-authored drive graph for bots.
    /// </summary>
    public static class TectonicWreckBuilder
    {
        public const string SceneName = "Tectonic_Wreck";

        [MenuItem("Iron Syndicate/Build/Tectonic Wreck Scene")]
        public static void Build()
        {
            var db = GameDatabase.Instance != null ? GameDatabase.Instance : DataBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var env = new GameObject("Environment").transform;
            Lighting(env);
            var ground = Mat("Ground", SceneBuilderUtil.Ground);
            var rift = Mat("RiftFloor", new Color(0.24f, 0.2f, 0.18f));
            var wall = Mat("Wall", SceneBuilderUtil.Wall);
            var cover = Mat("Cover", SceneBuilderUtil.Cover);
            var ramp = Mat("Ramp", SceneBuilderUtil.RampColor);
            var hazard = Mat("Hazard", SceneBuilderUtil.Hazard);
            var steel = Mat("Steel", new Color(0.35f, 0.37f, 0.4f));
            var road = Mat("Road", new Color(0.18f, 0.18f, 0.2f));
            var letterMat = Mat("Letters", new Color(0.85f, 0.35f, 0.1f));
            var cage = Mat("Cage", new Color(0.15f, 0.15f, 0.17f));

            // --- Terrain: plateaus, trench, tunnel roofs, end ramps and connectors
            Block(env, "Plateau_N", new Vector3(0f, -5f, 46f), new Vector3(230f, 10f, 68f), ground);
            Block(env, "Plateau_S", new Vector3(0f, -5f, -46f), new Vector3(230f, 10f, 68f), ground);
            Block(env, "Trench_Floor", new Vector3(0f, -9f, 0f), new Vector3(200f, 2f, 24f), rift);
            Block(env, "Tunnel_W_Roof", new Vector3(-60f, -0.75f, 0f), new Vector3(20f, 1.5f, 24f), steel);
            Block(env, "Tunnel_E_Roof", new Vector3(60f, -0.75f, 0f), new Vector3(20f, 1.5f, 24f), steel);
            Ramp(env, "Trench_Exit_W", new Vector3(-70f, -8f, 0f), 23.8f, 30f, 8f, -90f, ramp);
            Ramp(env, "Trench_Exit_E", new Vector3(70f, -8f, 0f), 23.8f, 30f, 8f, 90f, ramp);
            Block(env, "Connector_W", new Vector3(-107.5f, -5f, 0f), new Vector3(15f, 10f, 24f), ground);
            Block(env, "Connector_E", new Vector3(107.5f, -5f, 0f), new Vector3(15f, 10f, 24f), ground);

            Block(env, "Wall_N", new Vector3(0f, 10f, 81f), new Vector3(234f, 40f, 2f), wall);
            Block(env, "Wall_S", new Vector3(0f, 10f, -81f), new Vector3(234f, 40f, 2f), wall);
            Block(env, "Wall_E", new Vector3(116f, 10f, 0f), new Vector3(2f, 40f, 160f), wall);
            Block(env, "Wall_W", new Vector3(-116f, 10f, 0f), new Vector3(2f, 40f, 160f), wall);

            // Rim markers so the trench edge reads at speed.
            foreach (float z in new[] { 12.3f, -12.3f })
            {
                Block(env, "Rim_W", new Vector3(-30f, 0.05f, z), new Vector3(40f, 0.1f, 0.6f), hazard);
                Block(env, "Rim_E", new Vector3(30f, 0.05f, z), new Vector3(40f, 0.1f, 0.6f), hazard);
            }

            // Cover on both plateaus.
            Block(env, "Cover_N1", new Vector3(-75f, 2f, 40f), new Vector3(8f, 4f, 3f), cover, 20f);
            Block(env, "Cover_N2", new Vector3(30f, 2f, 52f), new Vector3(6f, 4f, 3f), cover, -15f);
            Block(env, "Cover_S1", new Vector3(-75f, 2f, -40f), new Vector3(8f, 4f, 3f), cover, -20f);
            Block(env, "Cover_S2", new Vector3(25f, 2f, -55f), new Vector3(6f, 4f, 3f), cover, 15f);
            Block(env, "Container_S", new Vector3(55f, 1.5f, -70f), new Vector3(12f, 3f, 3f), steel);
            Block(env, "Container_N", new Vector3(-30f, 1.5f, 50f), new Vector3(3f, 3f, 12f), steel);

            // --- Elevated highway: fixed ramps and decks, C4-only span over the trench
            Ramp(env, "Highway_Ramp_N", new Vector3(0f, 0f, 58f), 10f, 30f, 8f, 180f, road);
            Ramp(env, "Highway_Ramp_S", new Vector3(0f, 0f, -58f), 10f, 30f, 8f, 0f, road);
            foreach (float s in new[] { 1f, -1f })
            {
                Block(env, "Highway_Deck", new Vector3(0f, 7.5f, 22f * s), new Vector3(10f, 1f, 12f), road);
                Block(env, "Highway_Pier", new Vector3(0f, 3.5f, 20f * s), new Vector3(3f, 7f, 3f), steel);
                Block(env, "Highway_Rail_L", new Vector3(-5.25f, 8.5f, 22f * s), new Vector3(0.5f, 1f, 12f), hazard);
                Block(env, "Highway_Rail_R", new Vector3(5.25f, 8.5f, 22f * s), new Vector3(0.5f, 1f, 12f), hazard);
            }
            var span = MakeDestructible(env, "Highway_Span", Vector3.zero, 70f);
            span.label = "Hochstrassenbruecke";
            span.onlyCauses = new[] { "c4_satchel" };
            span.debrisSeconds = 14f;
            float seg = 32f / 3f;
            for (int i = 0; i < 3; i++)
            {
                float z = 16f - seg * (i + 0.5f);
                Piece(span, "Span_" + i, new Vector3(0f, 7.5f, z), new Vector3(10f, 1f, seg), road);
                Piece(span, "Span_Rail_L" + i, new Vector3(-5.25f, 8.5f, z), new Vector3(0.5f, 1f, seg), hazard);
                Piece(span, "Span_Rail_R" + i, new Vector3(5.25f, 8.5f, z), new Vector3(0.5f, 1f, seg), hazard);
            }

            // --- Jump across the rift (south to north)
            Ramp(env, "Jump_Rift", new Vector3(30f, 0f, -32f), 8f, 12f, 2.5f, 0f, ramp);

            // --- Watchtower (Core Overcharge on the roof)
            Block(env, "Tower", new Vector3(80f, 5f, -55f), new Vector3(10f, 10f, 10f), steel);
            Ramp(env, "Tower_Ramp", new Vector3(80f, 0f, -20f), 8f, 30f, 10f, 180f, ramp);
            Block(env, "Tower_Rail_S", new Vector3(80f, 10.5f, -59.75f), new Vector3(10f, 1f, 0.5f), hazard);
            Block(env, "Tower_Rail_E", new Vector3(84.75f, 10.5f, -55f), new Vector3(0.5f, 1f, 10f), hazard);
            Block(env, "Tower_Rail_W", new Vector3(75.25f, 10.5f, -55f), new Vector3(0.5f, 1f, 10f), hazard);
            var exit = new GameObject("Teleport_Exit").transform;
            exit.SetParent(env, false);
            exit.localPosition = new Vector3(0f, 8.6f, 25f);
            exit.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // --- Teleporter cage (north)
            var cageRoot = new GameObject("Teleporter_Cage").transform;
            cageRoot.SetParent(env, false);
            const float cx = -60f, cz = 56f, ch = 6f;
            for (float x = -6f; x <= 6.01f; x += 2f)
            {
                Block(cageRoot, "Bar", new Vector3(cx + x, ch * 0.5f, cz + 6f), new Vector3(0.4f, ch, 0.4f), cage);
                if (Mathf.Abs(x) > 3.1f) Block(cageRoot, "Bar", new Vector3(cx + x, ch * 0.5f, cz - 6f), new Vector3(0.4f, ch, 0.4f), cage);
            }
            for (float z = -4f; z <= 4.01f; z += 2f)
            {
                Block(cageRoot, "Bar", new Vector3(cx - 6f, ch * 0.5f, cz + z), new Vector3(0.4f, ch, 0.4f), cage);
                Block(cageRoot, "Bar", new Vector3(cx + 6f, ch * 0.5f, cz + z), new Vector3(0.4f, ch, 0.4f), cage);
            }
            Block(cageRoot, "Roof", new Vector3(cx, ch, cz), new Vector3(12.4f, 0.3f, 12.4f), cage);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Teleporter_Pad";
            pad.transform.SetParent(cageRoot, false);
            pad.transform.localPosition = new Vector3(cx, 0.05f, cz + 1f);
            pad.transform.localScale = new Vector3(5f, 0.05f, 5f);
            pad.GetComponent<Renderer>().sharedMaterial = Mat("Teleporter", new Color(0.65f, 0.35f, 1f));
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            var trig = pad.AddComponent<BoxCollider>();
            trig.size = new Vector3(0.9f, 60f, 0.9f);
            trig.center = new Vector3(0f, 20f, 0f);
            pad.AddComponent<Teleporter>().exit = exit;

            // --- Tesla reactor with four pillars, each drops a weapon once
            var reactor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reactor.name = "TeslaReactor";
            reactor.transform.SetParent(env, false);
            reactor.transform.localPosition = new Vector3(-40f, 3f, -45f);
            reactor.transform.localScale = new Vector3(4f, 3f, 4f);
            reactor.GetComponent<Renderer>().sharedMaterial = Mat("Reactor", new Color(0.3f, 0.75f, 1f));
            reactor.AddComponent<TeslaReactor>().radius = 22f;

            var pickups = new GameObject("Pickups").transform;
            string[] pillarWeapons = { "blaze_rocket", "kinetic_disc", "seeker_dart", "c4_satchel" };
            for (int i = 0; i < 4; i++)
            {
                var dir = new Vector3(i % 2 == 0 ? -1f : 1f, 0f, i < 2 ? -1f : 1f);
                var pos = new Vector3(-40f, 0f, -45f) + dir * 9f;
                var pillar = MakeDestructible(env, "Reactor_Pillar_" + i, pos, 90f);
                pillar.label = "Reaktorsaeule";
                Piece(pillar, "Pillar", new Vector3(0f, 3.5f, 0f), new Vector3(1.6f, 7f, 1.6f), Mat("ReactorPillar", new Color(0.25f, 0.55f, 0.8f)), true);
                var p = Pickup(pickups, PickupKind.Weapon, pos + dir * 3f, db.Weapon(pillarWeapons[i]));
                p.startHidden = true;
                p.oneShot = true;
                pillar.revealPickups = new[] { p };
            }

            // --- WRECK letters on the north rim (E and K hide weapons)
            string word = "WRECK";
            for (int i = 0; i < word.Length; i++)
            {
                var letter = MakeDestructible(env, "Letter_" + word[i], new Vector3(-16f + i * 8f, 0f, 72f), 90f);
                letter.label = "Buchstabe " + word[i];
                var rows = Glyph(word[i]);
                for (int r = 0; r < 5; r++)
                    for (int c = 0; c < 3; c++)
                        if (rows[r][c] == '#')
                            Piece(letter, "Px", new Vector3((c - 1) * 1.8f, (4 - r) * 1.5f + 0.75f, 0f), new Vector3(1.8f, 1.5f, 1.2f), letterMat, true);
                string hidden = word[i] == 'E' ? "thermite_gel" : word[i] == 'K' ? "spider_mine" : null;
                if (hidden == null) continue;
                var hp = Pickup(pickups, PickupKind.Weapon, new Vector3(-16f + i * 8f, 0f, 66f), db.Weapon(hidden));
                hp.startHidden = true;
                letter.revealPickups = new[] { hp };
            }

            // --- Depot (north-east): breakable door, two hidden weapons inside
            Block(env, "Depot_N", new Vector3(75f, 3f, 61.5f), new Vector3(20f, 6f, 1f), wall);
            Block(env, "Depot_E", new Vector3(84.5f, 3f, 55f), new Vector3(1f, 6f, 14f), wall);
            Block(env, "Depot_W", new Vector3(65.5f, 3f, 55f), new Vector3(1f, 6f, 14f), wall);
            Block(env, "Depot_S_L", new Vector3(68f, 3f, 48.5f), new Vector3(6f, 6f, 1f), wall);
            Block(env, "Depot_S_R", new Vector3(82f, 3f, 48.5f), new Vector3(6f, 6f, 1f), wall);
            Block(env, "Depot_Roof", new Vector3(75f, 6.25f, 55f), new Vector3(20f, 0.5f, 14f), steel);
            var door = MakeDestructible(env, "Depot_Door", new Vector3(75f, 0f, 48.5f), 120f);
            door.label = "Depottor";
            Piece(door, "Door", new Vector3(0f, 3f, 0f), new Vector3(8f, 6f, 1f), hazard, true);
            var d1 = Pickup(pickups, PickupKind.Weapon, new Vector3(70f, 0f, 56f), db.Weapon("titan_warhead"));
            var d2 = Pickup(pickups, PickupKind.Weapon, new Vector3(80f, 0f, 56f), db.Weapon("seeker_dart"));
            d1.startHidden = d2.startHidden = true;
            door.revealPickups = new[] { d1, d2 };

            // --- Visible pickups
            Pickup(pickups, PickupKind.Weapon, new Vector3(-30f, 0f, 30f), db.Weapon("blaze_rocket"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(45f, 0f, -38f), db.Weapon("seeker_dart"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(0f, -8f, -5f), db.Weapon("titan_warhead"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(-80f, 0f, -30f), db.Weapon("kinetic_disc"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(15f, 0f, 45f), db.Weapon("c4_satchel"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(-15f, 0f, -40f), db.Weapon("c4_satchel"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(60f, 0f, 25f), db.Weapon("thermite_gel"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(-60f, 0f, -20f), db.Weapon("spider_mine"));
            Pickup(pickups, PickupKind.Weapon, new Vector3(-25f, 0f, -45f), db.Weapon("tesla_arc"));
            Pickup(pickups, PickupKind.NanoPatch, new Vector3(-107f, 0f, 0f));
            Pickup(pickups, PickupKind.NanoPatch, new Vector3(0f, -8f, 6f));
            Pickup(pickups, PickupKind.NanoPatch, new Vector3(95f, 0f, 70f));
            Pickup(pickups, PickupKind.NitroCell, new Vector3(107f, 0f, 0f));
            Pickup(pickups, PickupKind.NitroCell, new Vector3(-90f, 0f, 70f));
            Pickup(pickups, PickupKind.CoreOvercharge, new Vector3(80f, 10f, -56f));

            // --- Five spawns
            var spawns = new GameObject("SpawnPoints").transform;
            Spawn(spawns, 0, new Vector3(-95f, 0.2f, 65f), Vector3.zero);
            Spawn(spawns, 1, new Vector3(95f, 0.2f, -70f), Vector3.zero);
            Spawn(spawns, 2, new Vector3(-95f, 0.2f, -65f), Vector3.zero);
            Spawn(spawns, 3, new Vector3(40f, 0.2f, 40f), new Vector3(0f, 0.2f, 0f));
            Spawn(spawns, 4, new Vector3(-20f, -7.8f, 0f), new Vector3(20f, -7.8f, 0f));

            BuildGraph(span, door);
            MatchRig(null, 4);
            SaveScene(scene, SceneName);
        }

        static void BuildGraph(Destructible span, Destructible door)
        {
            var graph = new GameObject("DriveGraph").AddComponent<DriveGraph>();
            var g = new GraphBuilder(graph);

            // North plateau
            g.N("N0", -95, 0, 62); g.N("N1", -45, 0, 62); g.N("HN0", 0, 0, 66); g.N("N3", 45, 0, 66); g.N("N4", 100, 0, 66);
            g.N("N5", -95, 0, 28); g.N("N6", -45, 0, 28); g.N("N7", -18, 0, 30); g.N("N8", 18, 0, 30); g.N("N9", 45, 0, 28); g.N("N10", 100, 0, 28);
            g.N("XWn", -60, 0, 18); g.N("XEn", 60, 0, 18); g.N("DropN", -30, 0, 15); g.N("Land", 30, 0, 32);
            g.N("Gate", -60, 0, 43); g.N("Pad", -60, 0, 57); g.N("DepotDoor", 75, 0, 42); g.N("DepotIn", 75, 0, 55);
            // South plateau
            g.N("S0", -95, 0, -62); g.N("S1", -45, 0, -62); g.N("HS0", 0, 0, -66); g.N("S3", 45, 0, -66); g.N("S4", 100, 0, -66);
            g.N("S5", -95, 0, -28); g.N("S6", -45, 0, -28); g.N("S7", -18, 0, -30); g.N("S8", 18, 0, -30); g.N("S9", 50, 0, -28); g.N("S10", 100, 0, -28);
            g.N("XWs", -60, 0, -18); g.N("XEs", 60, 0, -18); g.N("DropS", -10, 0, -15);
            g.N("J0", 30, 0, -44); g.N("J1", 30, 2.5f, -21); g.N("TR0", 80, 0, -14); g.N("TR1", 80, 10, -55);
            // Connectors, trench, highway
            g.N("W", -107, 0, 0); g.N("E", 107, 0, 0);
            g.N("T0", -62, -8, 0); g.N("T1", -30, -8, 0); g.N("T2", 0, -8, 0); g.N("T3", 30, -8, 0); g.N("T4", 62, -8, 0);
            g.N("HN1", 0, 8, 24); g.N("HM", 0, 8, 0); g.N("HS1", 0, 8, -24);

            g.Road("N0", "N1"); g.Road("N1", "HN0"); g.Road("HN0", "N3"); g.Road("N3", "N4");
            g.Road("N5", "N6"); g.Road("N6", "N7"); g.Road("N7", "N8"); g.Road("N8", "N9"); g.Road("N9", "N10");
            g.Road("N0", "N5"); g.Road("N1", "N6"); g.Road("N3", "N9"); g.Road("N4", "N10");
            g.Road("N6", "XWn"); g.Road("XWn", "N5"); g.Road("N9", "XEn"); g.Road("XEn", "N10");
            g.Road("Land", "N8"); g.Road("Land", "N9");
            g.Road("Gate", "N1"); g.Road("Gate", "N6"); g.Road("Gate", "Pad");
            g.Road("DepotDoor", "N3"); g.Road("DepotDoor", "N4"); g.Road("DepotDoor", "N9");
            g.Road("DepotDoor", "DepotIn", requiresDestroyed: door);
            g.Road("N6", "DropN"); g.Road("N7", "DropN");

            g.Road("S0", "S1"); g.Road("S1", "HS0"); g.Road("HS0", "S3"); g.Road("S3", "S4");
            g.Road("S5", "S6"); g.Road("S6", "S7"); g.Road("S7", "S8"); g.Road("S8", "S9"); g.Road("S9", "S10");
            g.Road("S0", "S5"); g.Road("S1", "S6"); g.Road("S3", "S9"); g.Road("S4", "S10");
            g.Road("S5", "XWs"); g.Road("S6", "XWs"); g.Road("S9", "XEs"); g.Road("XEs", "TR0"); g.Road("S10", "TR0");
            g.Road("TR0", "TR1");
            g.Road("S7", "DropS"); g.Road("S8", "DropS");
            g.Road("S8", "J0"); g.Road("S3", "J0");

            // Crossings
            g.Road("XWn", "XWs"); g.Road("XEn", "XEs");
            g.Road("W", "N5"); g.Road("W", "S5"); g.Road("E", "N10"); g.Road("E", "S10");
            g.Road("W", "T0"); g.Road("E", "T4");
            g.Road("T0", "T1"); g.Road("T1", "T2"); g.Road("T2", "T3"); g.Road("T3", "T4");
            g.Road("HN0", "HN1"); g.Road("HS0", "HS1");
            g.Road("HN1", "HM", requiresIntact: span); g.Road("HM", "HS1", requiresIntact: span);

            g.One("J0", "J1", EdgeKind.Road);
            g.One("J1", "Land", EdgeKind.Jump, 22f);
            g.One("DropN", "T1", EdgeKind.Drop);
            g.One("DropS", "T2", EdgeKind.Drop);
            g.One("Pad", "HN1", EdgeKind.Teleport);
        }

        sealed class GraphBuilder
        {
            readonly DriveGraph _g;
            readonly Dictionary<string, int> _idx = new Dictionary<string, int>();

            public GraphBuilder(DriveGraph g) => _g = g;

            public void N(string name, float x, float y, float z)
            {
                _idx[name] = _g.nodes.Count;
                _g.nodes.Add(new Vector3(x, y, z));
                _g.names.Add(name);
            }

            public void Road(string a, string b, Destructible requiresIntact = null, Destructible requiresDestroyed = null)
            {
                Add(a, b, EdgeKind.Road, 0f, requiresIntact, requiresDestroyed);
                Add(b, a, EdgeKind.Road, 0f, requiresIntact, requiresDestroyed);
            }

            public void One(string a, string b, EdgeKind kind, float minSpeed = 0f) => Add(a, b, kind, minSpeed, null, null);

            void Add(string a, string b, EdgeKind kind, float minSpeed, Destructible intact, Destructible destroyed)
            {
                _g.edges.Add(new DriveEdge { from = _idx[a], to = _idx[b], kind = kind, minSpeed = minSpeed, requiresIntact = intact, requiresDestroyed = destroyed });
            }
        }

        static Destructible MakeDestructible(Transform parent, string name, Vector3 pos, float hp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var d = go.AddComponent<Destructible>();
            d.maxHp = hp;
            return d;
        }

        /// <summary>Non-static piece of a destructible (it becomes debris); rams are forwarded to the owner.</summary>
        static GameObject Piece(Destructible owner, string name, Vector3 pos, Vector3 size, Material mat, bool localToOwner = false)
        {
            var go = Block(owner.transform, name, Vector3.zero, size, mat);
            go.isStatic = false;
            if (localToOwner) go.transform.localPosition = pos;
            else go.transform.position = pos;
            go.AddComponent<DestructiblePiece>().owner = owner;
            return go;
        }

        static string[] Glyph(char c)
        {
            switch (c)
            {
                case 'W': return new[] { "#.#", "#.#", "#.#", "###", "#.#" };
                case 'R': return new[] { "##.", "#.#", "##.", "#.#", "#.#" };
                case 'E': return new[] { "###", "#..", "##.", "#..", "###" };
                case 'C': return new[] { "###", "#..", "#..", "#..", "###" };
                case 'K': return new[] { "#.#", "#.#", "##.", "#.#", "#.#" };
                default: return new[] { "###", "###", "###", "###", "###" };
            }
        }
    }
}
