using System.IO;
using UnityEditor;
using UnityEngine;

namespace IronSyndicate.EditorTools
{
    /// <summary>
    /// Writes all vehicle and weapon definitions from one table. Re-running updates existing assets in place
    /// (GUIDs stay stable), so tuning done here is the reference; tweak-in-inspector is overwritten on rebuild.
    /// </summary>
    public static class DataBuilder
    {
        const string DataDir = "Assets/_Project/Data";
        const string ResourcesDir = "Assets/_Project/Resources/IronSyndicate";

        [MenuItem("Iron Syndicate/Build/Data (Vehicles + Weapons)")]
        public static GameDatabase Build()
        {
            Directory.CreateDirectory(DataDir + "/Vehicles");
            Directory.CreateDirectory(DataDir + "/Weapons");
            Directory.CreateDirectory(ResourcesDir);

            var db = LoadOrCreate<GameDatabase>(ResourcesDir + "/GameDatabase.asset");
            db.weapons.Clear();
            db.vehicles.Clear();

            db.weapons.Add(Weapon("blaze_rocket", "Blaze Rocket", WeaponBehaviour.Rocket, 2, 0.5f, 32, 8, 2.5f, 70, 140, 45, false, 25, 0, 0, 0, false, false, new Color(1f, 0.5f, 0.1f), 0.45f));
            db.weapons.Add(Weapon("seeker_dart", "Seeker Dart", WeaponBehaviour.Rocket, 2, 0.5f, 20, 5, 2f, 55, 150, 180, true, 35, 0, 0, 0, false, false, new Color(0.2f, 0.5f, 1f), 0.4f));
            db.weapons.Add(Weapon("titan_warhead", "Titan Warhead", WeaponBehaviour.Rocket, 1, 1f, 70, 55, 7f, 45, 160, 0, false, 0, 0, 0, 0, false, false, new Color(0.9f, 0.1f, 0.1f), 0.9f));
            db.weapons.Add(Weapon("kinetic_disc", "Kinetic Disc", WeaponBehaviour.Bounce, 3, 0.4f, 25, 15, 3f, 50, 150, 0, false, 0, 0, 2.5f, 0, false, false, new Color(0.8f, 0.8f, 0.85f), 0.5f));
            db.weapons.Add(Weapon("c4_satchel", "C4 Satchel", WeaponBehaviour.Remote, 1, 0.3f, 0, 80, 8f, 3, 999, 0, false, 0, 20, 0, 0, false, false, new Color(0.55f, 0.3f, 0.15f), 0.6f));
            db.weapons.Add(Weapon("thermite_gel", "Thermite Gel", WeaponBehaviour.Lob, 2, 0.6f, 12, 10, 3.5f, 30, 120, 0, false, 0, 20, 0, 0, true, false, new Color(1f, 0.6f, 0.05f), 0.55f));
            db.weapons.Add(Weapon("spider_mine", "Spider Mine", WeaponBehaviour.Mine, 3, 0.4f, 0, 35, 4.5f, 3, 999, 0, false, 0, 20, 0, 3.5f, false, false, new Color(0.6f, 0.1f, 0.8f), 0.5f));
            var tesla = Weapon("tesla_arc", "Tesla Arc", WeaponBehaviour.TeslaArc, 1, 1f, 0, 45, 22f, 0, 0, 0, false, 0, 0, 0, 0, false, false, new Color(0.4f, 0.9f, 1f), 0.6f);
            tesla.environmentOnly = true;
            db.weapons.Add(tesla);

            db.vehicles.Add(Vehicle("goliath_wheel", "Goliath Wheel", "Vance Korr", "Zweirad-Koloss", 3, 3, 3, HandlingClass.Average,
                "gyro_crush", "Gyro Crush", "Räder drehen hoch, gerader Sturmlauf; alles im Weg wird beschädigt und weggestossen.",
                BodyStyle.Wheel, new Vector3(2.0f, 1.3f, 2.8f), 0.5f, new Color(0.7f, 0.45f, 0.2f)));
            db.vehicles.Add(Vehicle("sandviper", "Sandviper", "Kira Vance", "Wüsten-Buggy", 1, 4, 3, HandlingClass.Tight,
                "dust_devil", "Dust Devil", "Sandwolke: Sandviper ist nicht erfassbar, Verfolger in der Wolke verlieren Grip.",
                BodyStyle.Buggy, new Vector3(1.9f, 0.9f, 3.4f), 0.5f, new Color(0.9f, 0.8f, 0.45f)));
            db.vehicles.Add(Vehicle("iron_jaw", "Iron Jaw", "Viktor Krane", "Schwerer Radlader", 4, 2, 5, HandlingClass.Sluggish,
                "wall_press", "Wall Press", "Schaufel fasst und schiebt; Schaden steigt, solange das Ziel gegen eine Wand gepresst wird.",
                BodyStyle.Loader, new Vector3(2.4f, 1.5f, 4.6f), 0.65f, new Color(0.95f, 0.75f, 0.1f)));
            db.vehicles.Add(Vehicle("nocturne", "Nocturne", "Malakor Graves", "Goth-Leichenwagen", 2, 3, 4, HandlingClass.Average,
                "wraith_lantern", "Wraith Lantern", "Laterne bindet den nächsten Gegner und entzieht ihm HP zugunsten von Nocturne.",
                BodyStyle.Hearse, new Vector3(2.0f, 1.3f, 5.2f), 0.45f, new Color(0.15f, 0.1f, 0.2f)));
            db.vehicles.Add(Vehicle("vortex_f1", "Vortex F1", "Elena Volt", "Formel-1-Rennwagen", 1, 5, 4, HandlingClass.Tight,
                "slipstream_lance", "Slipstream Lance", "Hochgeschwindigkeitsstoss mit Energiespur, die querende Fahrzeuge beschädigt.",
                BodyStyle.Formula, new Vector3(1.9f, 0.7f, 4.6f), 0.4f, new Color(0.1f, 0.8f, 0.9f)));
            db.vehicles.Add(Vehicle("bastion_mk4", "Bastion MK-IV", "Major Riggs", "Militär-Humvee", 4, 2, 3, HandlingClass.Sluggish,
                "mortar_grid", "Mortar Grid", "Markiert eine Zone voraus, nach 1,5 s schlagen sechs Granaten ein.",
                BodyStyle.Humvee, new Vector3(2.4f, 1.5f, 4.6f), 0.55f, new Color(0.35f, 0.4f, 0.2f)));
            db.vehicles.Add(Vehicle("interceptor_v8", "Interceptor V8", "Lt. Valeria Cross", "Polizei-Streifenwagen", 3, 4, 3, HandlingClass.Good,
                "spike_net", "Spike Net", "Zielsuchendes Krallennetz: Ziel verliert Grip und kann kurz kein Nitro zünden.",
                BodyStyle.Sedan, new Vector3(2.0f, 1.2f, 4.6f), 0.45f, new Color(0.9f, 0.9f, 0.95f)));
            db.vehicles.Add(Vehicle("chrome_igniter", "Chrome Igniter", "Dante Reyes", "64er Lowrider", 3, 4, 5, HandlingClass.Average,
                "exhaust_inferno", "Exhaust Inferno", "Auspuff-Flammenstoss nach hinten, setzt Verfolger in Brand und hinterlässt eine Brandspur.",
                BodyStyle.Lowrider, new Vector3(2.1f, 1.0f, 5.2f), 0.42f, new Color(0.55f, 0.15f, 0.45f)));
            db.vehicles.Add(Vehicle("phase_gt", "Phase GT", "Julian Vance", "Sportwagen / Coupe", 1, 5, 3, HandlingClass.Tight,
                "phase_shift", "Phase Shift", "1,5 s nicht greifbar für Fahrzeuge und Geschosse, Austritt verursacht eine Schockwelle.",
                BodyStyle.Coupe, new Vector3(2.0f, 1.0f, 4.4f), 0.42f, new Color(0.5f, 0.75f, 0.75f)));
            db.vehicles.Add(Vehicle("rustfang", "Rustfang", "Jax Mercer", "Post-Apokalypsen-Auto", 2, 4, 3, HandlingClass.Good,
                "chain_harpoon", "Chain Harpoon", "Harpune hakt ein Ziel ein und schleppt es nach; Wandkontakt verursacht Schaden.",
                BodyStyle.Wasteland, new Vector3(2.1f, 1.2f, 4.4f), 0.5f, new Color(0.55f, 0.3f, 0.15f)));
            db.vehicles.Add(Vehicle("bonechaser", "Bonechaser", "Mordecai", "Chopper-Motorrad", 1, 5, 5, HandlingClass.Good,
                "scythe_sweep", "Scythe Sweep", "Seitliche Klingen schädigen Fahrzeuge an beiden Flanken im Vorbeifahren.",
                BodyStyle.Bike, new Vector3(0.9f, 1.1f, 2.8f), 0.45f, new Color(0.9f, 0.88f, 0.8f)));
            db.vehicles.Add(Vehicle("mammoth_4x4", "Mammoth 4x4", "Zack & Rocco", "Monster-Truck", 4, 2, 4, HandlingClass.Sluggish,
                "battering_charge", "Battering Charge", "Rammschild für 4 s; der nächste Aufprall verursacht tempoabhängig hohen Schaden und kippt das Ziel.",
                BodyStyle.MonsterTruck, new Vector3(2.8f, 1.6f, 4.8f), 0.95f, new Color(0.8f, 0.1f, 0.1f)));
            var jester = Vehicle("jester_van", "Jester Van", "Grinlock", "Gepanzerter Schaustellerwagen", 4, 2, 5, HandlingClass.Sluggish,
                "fireworks_finale", "Fireworks Finale", "Fächer aus fünf Feuerwerksraketen mit leichter Zielsuche.",
                BodyStyle.ShowTruck, new Vector3(2.4f, 2.0f, 5.0f), 0.55f, new Color(0.85f, 0.2f, 0.6f));
            jester.secret = true;
            db.vehicles.Add(jester);
            var leviathan = Vehicle("leviathan_apc", "Leviathan APC", "KI Cerberus", "Schwerer Radpanzer", 5, 4, 5, HandlingClass.Average,
                "siege_lock", "Siege Lock", "Stützen ausfahren, geladener Schienenstrahl auf lange Distanz.",
                BodyStyle.Apc, new Vector3(2.8f, 1.8f, 6.0f), 0.6f, new Color(0.25f, 0.27f, 0.3f));
            leviathan.boss = true;
            leviathan.secret = true;
            db.vehicles.Add(leviathan);
            var dread = Vehicle("dread_jester", "Dread Jester", "Grinlock Prime", "Gigantischer Kirmes-Schwertransporter", 5, 3, 5, HandlingClass.Average,
                "dread_ram", "Rammfahrt", "Endboss. Phase 1: Rammtransporter mit Rammfahrt. Phase 2: Hellfire Carousel löst sich als rotierender Geschützturm.",
                BodyStyle.BossRig, new Vector3(3.6f, 3.0f, 9.0f), 0.9f, new Color(0.1f, 0.05f, 0.08f));
            dread.playable = false;
            dread.boss = true;
            dread.maxHp = 900f;
            dread.mass = 6000f;
            EditorUtility.SetDirty(dread);
            db.vehicles.Add(dread);
            var carousel = Vehicle("hellfire_carousel", "Hellfire Carousel", "Grinlock Prime", "Karussell-Geschützturm (Bossphase 2)", 4, 1, 5, HandlingClass.Sluggish,
                "hellfire_carousel", "Feuerkarussell", "Rotiert langsam und feuert Raketensalven im Ring und gezielt.",
                BodyStyle.BossRig, new Vector3(5.0f, 2.4f, 5.0f), 0.7f, new Color(0.6f, 0.1f, 0.05f));
            carousel.playable = false;
            carousel.boss = true;
            carousel.maxHp = 450f;
            carousel.mass = 4000f;
            carousel.topSpeed = 9f;
            EditorUtility.SetDirty(carousel);
            db.vehicles.Add(carousel);

            ApplyBalancing(db);
            EditorUtility.SetDirty(db);
            EnsureBaseMaterial(BaseMaterials.LitPath, "Universal Render Pipeline/Lit");
            EnsureBaseMaterial(BaseMaterials.UnlitPath, "Universal Render Pipeline/Unlit");
            AssetDatabase.SaveAssets();
            Debug.Log($"[IS] Data built: {db.vehicles.Count} vehicles, {db.weapons.Count} weapons.");
            return db;
        }

        /// <summary>
        /// HP corrections from the Stage 1 duel runs (Docs/Balancing/Auswertung.md). Stars and menu values stay
        /// as in the GDD; only the derived hit points move.
        /// </summary>
        static readonly (string id, float hp)[] HpTuning =
        {
            ("vortex_f1", 40f),      // 15–18 % win rate, mostly lost by knock-out
            ("goliath_wheel", 30f),  // 27–32 %
            ("sandviper", 25f),      // 31 %
            ("leviathan_apc", -40f), // 67–85 %
        };

        static void ApplyBalancing(GameDatabase db)
        {
            foreach (var (id, hp) in HpTuning)
            {
                var v = db.Vehicle(id);
                if (v == null) continue;
                v.maxHp += hp;
                EditorUtility.SetDirty(v);
            }
        }

        /// <summary>Runtime colour templates live in Resources so their shaders ship with the player.</summary>
        static void EnsureBaseMaterial(string resourcePath, string shader)
        {
            string path = $"Assets/_Project/Resources/{resourcePath}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(new Material(Shader.Find(shader)), path);
        }

        static WeaponDefinition Weapon(string id, string name, WeaponBehaviour b, int charges, float cooldown, float direct, float splash, float radius,
            float speed, float range, float homing, bool requiresLock, float cone, float gravity, float fuse, float trigger, bool ignites, bool freezes, Color color, float scale)
        {
            var w = LoadOrCreate<WeaponDefinition>($"{DataDir}/Weapons/{id}.asset");
            w.id = id;
            w.displayName = name;
            w.behaviour = b;
            w.chargesPerPickup = charges;
            w.cooldown = cooldown;
            w.directDamage = direct;
            w.splashDamage = splash;
            w.splashRadius = radius;
            w.speed = speed;
            w.range = range;
            w.homingDegPerSec = homing;
            w.requiresLock = requiresLock;
            w.lockConeDeg = cone;
            w.gravity = gravity;
            w.fuseTime = fuse;
            w.triggerRadius = trigger;
            w.ignites = ignites;
            w.freezes = freezes;
            w.color = color;
            w.visualScale = scale;
            w.environmentOnly = false;
            EditorUtility.SetDirty(w);
            return w;
        }

        static VehicleDefinition Vehicle(string id, string name, string driver, string type, int armor, int speed, int special, HandlingClass handling,
            string specialId, string specialName, string specialDesc, BodyStyle style, Vector3 size, float wheelRadius, Color color)
        {
            var v = LoadOrCreate<VehicleDefinition>($"{DataDir}/Vehicles/{id}.asset");
            v.id = id;
            v.displayName = name;
            v.driver = driver;
            v.vehicleType = type;
            v.playable = true;
            v.secret = false;
            v.boss = false;
            v.armorStars = armor;
            v.speedStars = speed;
            v.specialStars = special;
            v.handling = handling;
            v.specialId = specialId;
            v.specialName = specialName;
            v.specialDescription = specialDesc;
            v.specialCharges = 2;
            v.bodyStyle = style;
            v.bodySize = size;
            v.wheelRadius = wheelRadius;
            v.bodyColor = color;
            v.ApplyStarDefaults();
            EditorUtility.SetDirty(v);
            return v;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
