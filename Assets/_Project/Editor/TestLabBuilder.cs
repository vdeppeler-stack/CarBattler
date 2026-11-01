using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static IronSyndicate.EditorTools.SceneBuilderUtil;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// Test lab: nine stations along a shared corridor (z &lt; 5), each module north of it behind low dividers.
    /// 1 start + boss pad, 2 falling bridge, 3 tower becomes road, 4 statue opens tunnel, 5 glass opens room,
    /// 6 lava, 7 pool (with a lava strip to test extinguishing), 8 abyss with ice plate and warnings,
    /// 9 train crossing and reeds. Presets and spawns are driven by <see cref="LabController"/>.
    /// </summary>
    public static class TestLabBuilder
    {
        public const string SceneName = "Test_Lab";
        const float ModuleWidth = 60f;
        const float FirstX = -270f;

        static float Cx(int station) => FirstX + (station - 1) * ModuleWidth;

        [MenuItem("Iron Syndicate/Build/Test Lab Scene")]
        public static void Build()
        {
            var db = GameDatabase.Instance != null ? GameDatabase.Instance : DataBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var env = new GameObject("Environment").transform;
            Lighting(env);
            var ground = Mat("Ground", SceneBuilderUtil.Ground);
            var wall = Mat("Wall", SceneBuilderUtil.Wall);
            var hazard = Mat("Hazard", SceneBuilderUtil.Hazard);
            var ramp = Mat("Ramp", SceneBuilderUtil.RampColor);
            var stone = Mat("Stone", new Color(0.5f, 0.48f, 0.45f));
            var steel = Mat("Steel", new Color(0.35f, 0.37f, 0.4f));
            var road = Mat("Road", new Color(0.18f, 0.18f, 0.2f));

            float minX = Cx(1) - ModuleWidth * 0.5f, maxX = Cx(9) + ModuleWidth * 0.5f;
            float len = maxX - minX, mid = (minX + maxX) * 0.5f;
            // Corridor ground, shared by every station.
            Block(env, "Ground_Corridor", new Vector3(mid, -0.5f, -32.5f), new Vector3(len, 1f, 75f), ground);
            Block(env, "Wall_S", new Vector3(mid, 5f, -71f), new Vector3(len + 4f, 10f, 2f), wall);
            Block(env, "Wall_N", new Vector3(mid, 5f, 71f), new Vector3(len + 4f, 10f, 2f), wall);
            Block(env, "Wall_W", new Vector3(minX - 1f, 5f, 0f), new Vector3(2f, 10f, 140f), wall);
            Block(env, "Wall_E", new Vector3(maxX + 1f, 5f, 0f), new Vector3(2f, 10f, 140f), wall);
            for (int s = 1; s < 9; s++)
                Block(env, "Divider_" + s, new Vector3(Cx(s) + ModuleWidth * 0.5f, 1.5f, 37.5f), new Vector3(1f, 3f, 65f), wall);

            var spawns = new GameObject("Stations").transform;
            var pickups = new GameObject("Pickups").transform;
            string[] names = { "Start", "Fallende Bruecke", "Turm wird Strasse", "Statue oeffnet Tunnel", "Glas oeffnet Raum", "Lava", "Pool", "Abgrund + Eisplatte", "Zug + Schilf" };
            for (int s = 1; s <= 9; s++)
            {
                var sp = Spawn(spawns, s, new Vector3(Cx(s), 0.2f, -10f), new Vector3(Cx(s), 0.2f, 30f));
                sp.name = $"Station_{s}_{names[s - 1]}";
                Label(env, names[s - 1], new Vector3(Cx(s), 0.02f, -3f));
            }

            // 1 Start: flat ground, jump ramp, boss pad.
            float x = Cx(1);
            ModuleGround(env, x, ground);
            Ramp(env, "Jump", new Vector3(x - 12f, 0f, 15f), 8f, 10f, 2.2f, 0f, ramp);
            var pad = Block(env, "Boss_Pad", new Vector3(x + 10f, 0.03f, 45f), new Vector3(12f, 0.06f, 12f), Mat("BossPad", new Color(0.7f, 0.1f, 0.15f)));
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            pad.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var spawner = new GameObject("Boss_Spawner").AddComponent<BossSpawner>();
            spawner.pad = pad.transform;
            spawner.killsRequired = 8;

            // 2 Falling bridge over the abyss: any damage source can break it.
            x = Cx(2);
            ModuleGround(env, x, ground, 10f, 30f);
            Stripes(env, x, 10f, 30f, hazard);
            var bridge = Destructible(env, "Falling_Bridge", new Vector3(x, 0f, 20f), 60f);
            for (int i = 0; i < 4; i++)
                Piece(bridge, "Plank_" + i, new Vector3(0f, -0.5f, -7.5f + i * 5f), new Vector3(8f, 1f, 5f), road);

            // 3 Tower becomes road: a slab tower topples across the abyss and stays as a bridge.
            x = Cx(3);
            ModuleGround(env, x, ground, 8f, 32f);
            Stripes(env, x, 8f, 32f, hazard);
            var tower = Destructible(env, "Signal_Tower", new Vector3(x, 0f, 6f), 100f);
            tower.keepPieces = true;
            Piece(tower, "Tower", new Vector3(0f, 13f, 0.75f), new Vector3(8f, 26f, 1.5f), steel);
            Piece(tower, "Tower_Cap", new Vector3(0f, 26.5f, 0.75f), new Vector3(9f, 1f, 1.5f), hazard);
            var topple = tower.gameObject.AddComponent<ToppleOnDestroy>();
            topple.axis = Vector3.right;
            topple.angle = 90f;
            var reward3 = Pickup(pickups, PickupKind.CoreOvercharge, new Vector3(x, 0f, 45f));
            reward3.respawnSeconds = 10f;

            // 4 Statue opens tunnel: the plug behind the statue disappears when the statue falls.
            x = Cx(4);
            ModuleGround(env, x, ground);
            Block(env, "Rock_L", new Vector3(x - 17.5f, 5f, 34f), new Vector3(25f, 10f, 8f), stone);
            Block(env, "Rock_R", new Vector3(x + 17.5f, 5f, 34f), new Vector3(25f, 10f, 8f), stone);
            Block(env, "Rock_Top", new Vector3(x, 8f, 34f), new Vector3(10f, 4f, 8f), stone);
            var plug = Block(env, "Tunnel_Plug", new Vector3(x, 3f, 34f), new Vector3(10f, 6f, 8f), Mat("Plug", new Color(0.4f, 0.38f, 0.36f)));
            var statue = Destructible(env, "Statue", new Vector3(x, 0f, 26f), 150f);
            Piece(statue, "Pedestal", new Vector3(0f, 1f, 0f), new Vector3(5f, 2f, 5f), stone);
            Piece(statue, "Body", new Vector3(0f, 4.5f, 0f), new Vector3(2.5f, 5f, 2f), stone);
            Piece(statue, "Head", new Vector3(0f, 7.8f, 0f), new Vector3(1.6f, 1.6f, 1.6f), stone);
            statue.disableOnDestroy = new[] { plug };
            Pickup(pickups, PickupKind.Weapon, new Vector3(x, 0f, 50f), db.Weapon("titan_warhead"));

            // 5 Glass opens room.
            x = Cx(5);
            ModuleGround(env, x, ground);
            Block(env, "Room_Back", new Vector3(x, 3f, 44.5f), new Vector3(20f, 6f, 1f), wall);
            Block(env, "Room_L", new Vector3(x - 9.5f, 3f, 37f), new Vector3(1f, 6f, 16f), wall);
            Block(env, "Room_R", new Vector3(x + 9.5f, 3f, 37f), new Vector3(1f, 6f, 16f), wall);
            Block(env, "Room_Roof", new Vector3(x, 6.25f, 37f), new Vector3(20f, 0.5f, 16f), steel);
            var glass = Destructible(env, "Glass_Front", new Vector3(x, 0f, 29.5f), 25f);
            glass.debrisSeconds = 3f;
            for (int i = 0; i < 3; i++)
                Piece(glass, "Pane_" + i, new Vector3(-6f + i * 6f, 3f, 0f), new Vector3(6f, 6f, 0.3f), GlassMat());
            Pickup(pickups, PickupKind.NanoPatch, new Vector3(x - 4f, 0f, 38f));
            Pickup(pickups, PickupKind.Weapon, new Vector3(x + 4f, 0f, 38f), db.Weapon("seeker_dart"));

            // 6 Lava field.
            x = Cx(6);
            ModuleGround(env, x, ground);
            Zone(env, "Lava", HazardKind.Lava, new Vector3(x, 0f, 25f), new Vector3(24f, 2f, 20f), EmissiveMat("Lava", new Color(1f, 0.35f, 0.05f)));

            // 7 Pool: lava strip first, then water to put the fire out.
            x = Cx(7);
            ModuleGround(env, x, ground);
            Zone(env, "Lava_Strip", HazardKind.Lava, new Vector3(x, 0f, 10f), new Vector3(16f, 2f, 4f), EmissiveMat("Lava", new Color(1f, 0.35f, 0.05f)));
            Zone(env, "Pool", HazardKind.Pool, new Vector3(x, 0f, 32f), new Vector3(24f, 2f, 20f), Mat("Water", new Color(0.15f, 0.4f, 0.8f)));

            // 8 Abyss with an ice plate and warnings.
            x = Cx(8);
            ModuleGround(env, x, ground, 12f, 36f);
            Stripes(env, x, 12f, 36f, hazard);
            var sign = Block(env, "Warning_Sign", new Vector3(x - 10f, 2f, 9f), new Vector3(3f, 3f, 0.3f), hazard);
            sign.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            // Ice - safe island - ice: the island holds, the plates break after the warning.
            foreach (float z in new[] { 16f, 32f })
            {
                var ice = Block(env, "Ice_Plate", new Vector3(x, -0.15f, z), new Vector3(16f, 0.3f, 8f), Mat("Ice", new Color(0.75f, 0.9f, 1f)));
                ice.isStatic = false;
                ice.AddComponent<IcePlate>();
            }
            Block(env, "Safe_Island", new Vector3(x, -1f, 24f), new Vector3(16f, 2f, 8f), Mat("Island", new Color(0.35f, 0.5f, 0.3f)));

            // 9 Train crossing and reeds.
            x = Cx(9);
            ModuleGround(env, x, ground);
            Block(env, "Track", new Vector3(x, 0.02f, 25f), new Vector3(ModuleWidth - 2f, 0.04f, 4f), steel);
            var train = Block(env, "Train", new Vector3(x - 38f, 2.2f, 25f), new Vector3(18f, 4f, 3.6f), Mat("Train", new Color(0.55f, 0.12f, 0.1f)));
            train.isStatic = false;
            var a = new GameObject("Train_A").transform; a.SetParent(env, false); a.localPosition = new Vector3(x - 38f, 2.2f, 25f);
            var b = new GameObject("Train_B").transform; b.SetParent(env, false); b.localPosition = new Vector3(x + 38f, 2.2f, 25f);
            train.AddComponent<Rigidbody>();
            var th = train.AddComponent<TrainHazard>();
            th.from = a; th.to = b;
            var lampL = Block(env, "Lamp_L", new Vector3(x - 6f, 2.5f, 21f), new Vector3(0.8f, 0.8f, 0.8f), Mat("Lamp", new Color(0.2f, 0.05f, 0.05f)));
            var lampR = Block(env, "Lamp_R", new Vector3(x + 6f, 2.5f, 21f), new Vector3(0.8f, 0.8f, 0.8f), Mat("Lamp", new Color(0.2f, 0.05f, 0.05f)));
            th.lamps = new[] { lampL.GetComponent<Renderer>(), lampR.GetComponent<Renderer>() };
            Zone(env, "Reeds", HazardKind.Reeds, new Vector3(x, 0f, 50f), new Vector3(30f, 4f, 16f), null);
            var reedMat = Mat("Reeds", new Color(0.45f, 0.55f, 0.2f));
            var rng = new System.Random(9);
            for (int i = 0; i < 70; i++)
            {
                var r = Block(env, "Reed", new Vector3(x - 15f + (float)rng.NextDouble() * 30f, 1.6f, 42f + (float)rng.NextDouble() * 16f), new Vector3(0.25f, 3.2f, 0.25f), reedMat);
                Object.DestroyImmediate(r.GetComponent<Collider>());
            }

            var mm = MatchRig(null, 0);
            mm.labMode = true;
            mm.countdownSeconds = 1f;
            new GameObject("Lab").AddComponent<LabController>();
            SaveScene(scene, SceneName);
        }

        static void ModuleGround(Transform env, float x, Material mat, float holeFrom = 0f, float holeTo = 0f)
        {
            if (holeTo <= holeFrom)
            {
                Block(env, "Ground_M", new Vector3(x, -0.5f, 37.5f), new Vector3(ModuleWidth, 1f, 65f), mat);
                return;
            }
            Block(env, "Ground_Near", new Vector3(x, -0.5f, (5f + holeFrom) * 0.5f), new Vector3(ModuleWidth, 1f, holeFrom - 5f), mat);
            Block(env, "Ground_Far", new Vector3(x, -0.5f, (holeTo + 70f) * 0.5f), new Vector3(ModuleWidth, 1f, 70f - holeTo), mat);
        }

        static void Stripes(Transform env, float x, float holeFrom, float holeTo, Material mat)
        {
            Block(env, "Edge_Warning", new Vector3(x, 0.03f, holeFrom - 0.6f), new Vector3(ModuleWidth, 0.06f, 1.2f), mat);
            Block(env, "Edge_Warning", new Vector3(x, 0.03f, holeTo + 0.6f), new Vector3(ModuleWidth, 0.06f, 1.2f), mat);
        }

        static void Label(Transform env, string text, Vector3 pos)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(env, false);
            go.transform.localPosition = pos + Vector3.up * 3f;
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.text = text;
            tm.characterSize = 0.5f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
        }

        static void Zone(Transform env, string name, HazardKind kind, Vector3 groundCenter, Vector3 size, Material visual)
        {
            if (visual != null)
            {
                var v = Block(env, name + "_Visual", groundCenter + Vector3.up * 0.03f, new Vector3(size.x, 0.06f, size.z), visual);
                Object.DestroyImmediate(v.GetComponent<Collider>());
            }
            var go = new GameObject(name);
            go.transform.SetParent(env, false);
            go.transform.localPosition = groundCenter + Vector3.up * size.y * 0.5f;
            go.AddComponent<BoxCollider>().size = size;
            go.AddComponent<HazardZone>().kind = kind;
        }

        static Destructible Destructible(Transform parent, string name, Vector3 pos, float hp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var d = go.AddComponent<IronSyndicate.Destructible>();
            d.maxHp = hp;
            return d;
        }

        static void Piece(IronSyndicate.Destructible owner, string name, Vector3 local, Vector3 size, Material mat)
        {
            var go = Block(owner.transform, name, local, size, mat);
            go.isStatic = false;
            go.AddComponent<DestructiblePiece>().owner = owner;
        }

        static Material EmissiveMat(string name, Color c)
        {
            var m = Mat(name, c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material GlassMat()
        {
            var m = Mat("Glass", new Color(0.7f, 0.9f, 1f, 0.35f));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", new Color(0.7f, 0.9f, 1f, 0.35f));
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
