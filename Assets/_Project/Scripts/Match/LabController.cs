using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IronSyndicate
{
    /// <summary>
    /// Test lab: presets survive a reset (vehicle, seed, station, inventory, arena state); opponents, dummies
    /// and the boss are spawned on demand. Every action is logged so lab runs are reproducible.
    /// Keys: 1-9 station, Bild↑/↓ vehicle, O bot, P dummy, B boss, J simulated kill (boss spawner), I inventory, H refill, K clear,
    /// U arena state, R reset, Y new seed, F1 help.
    /// </summary>
    public class LabController : MonoBehaviour
    {
        public enum Inventory { Empty, AllWeapons }
        public enum ArenaState { Intact, AllBroken }

        // Presets kept across lab resets (scene reloads).
        public static int Station = 1;
        public static Inventory InventoryPreset = Inventory.AllWeapons;
        public static ArenaState ArenaPreset = ArenaState.Intact;
        static int _nextOpponent;

        Text _panel;
        bool _help = true;

        void Awake()
        {
            MatchSetup.Configured = true;
            MatchSetup.HumanPlaying = true;
            MatchSetup.BotCount = 0;
            MatchSetup.BotVehicleIds.Clear();
            MatchSetup.Label = "lab";
            MatchSetup.SpawnOrder.Clear();
            MatchSetup.SpawnOrder.Add(Station);
        }

        void Start()
        {
            var canvas = new GameObject("Lab Canvas", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            var go = new GameObject("Lab Panel", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-16f, 60f);
            rt.sizeDelta = new Vector2(420f, 330f);
            _panel = go.GetComponent<Text>();
            _panel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _panel.fontSize = 14;
            _panel.alignment = TextAnchor.LowerRight;
            _panel.color = new Color(1f, 1f, 0.85f);

            if (ArenaPreset == ArenaState.AllBroken) BreakAll();
            Invoke(nameof(ApplyInventory), 0.1f);
            TestLog.Write("lab", "-", "preset", $"vehicle={MatchSetup.PlayerVehicleId};station={Station};inventory={InventoryPreset};arena={ArenaPreset};seed={MatchSetup.Seed}");
        }

        static Vehicle Player => MatchManager.Instance != null ? MatchManager.Instance.Human : null;

        void ApplyInventory()
        {
            var p = Player;
            if (p == null || InventoryPreset != Inventory.AllWeapons) return;
            foreach (var w in GameDatabase.Instance.weapons) p.Arsenal.AddAmmo(w, w.chargesPerPickup);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            var mm = MatchManager.Instance;

            for (int i = 1; i <= 9; i++)
            {
                var key = (Key)((int)Key.Digit1 + i - 1);
                if (kb[key].wasPressedThisFrame) GoToStation(i);
            }
            if (kb.pageUpKey.wasPressedThisFrame) SwapVehicle(-1);
            if (kb.pageDownKey.wasPressedThisFrame) SwapVehicle(1);
            if (kb.oKey.wasPressedThisFrame) SpawnOpponent(false);
            if (kb.pKey.wasPressedThisFrame) SpawnOpponent(true);
            if (kb.bKey.wasPressedThisFrame) SpawnBoss();
            if (kb.jKey.wasPressedThisFrame) FindAnyObjectByType<BossSpawner>()?.SimulateKill();
            if (kb.iKey.wasPressedThisFrame)
            {
                InventoryPreset = InventoryPreset == Inventory.Empty ? Inventory.AllWeapons : Inventory.Empty;
                ApplyInventory();
                TestLog.Write("lab", "-", "inventory", InventoryPreset.ToString());
            }
            if (kb.hKey.wasPressedThisFrame && Player != null)
            {
                Player.Heal(Player.MaxHp, "lab-refill");
                Player.Arsenal.Energy = VehicleArsenal.EnergyMax;
                Player.Motor.Nitro = VehicleMotor.NitroMax;
                Player.Status.Extinguish("lab");
            }
            if (kb.kKey.wasPressedThisFrame && mm != null)
                foreach (var v in mm.Participants.ToArray())
                    if (v != mm.Human) mm.Despawn(v);
            if (kb.uKey.wasPressedThisFrame)
            {
                ArenaPreset = ArenaPreset == ArenaState.Intact ? ArenaState.AllBroken : ArenaState.Intact;
                TestLog.Write("lab", "-", "arena-preset", ArenaPreset + " (applies on reset)");
            }
            if (kb.rKey.wasPressedThisFrame && kb.leftCtrlKey.isPressed) ResetLab(false);
            if (kb.yKey.wasPressedThisFrame) ResetLab(true);
            if (kb.f1Key.wasPressedThisFrame) _help = !_help;

            _panel.text = Panel();
        }

        string Panel()
        {
            var p = Player;
            string head = $"TESTLABOR · Seed {MatchSetup.Seed} · Station {Station}\n" +
                          $"{(p != null ? p.Definition.displayName : "-")} · Inventar {InventoryPreset} · Arena {ArenaPreset}\n";
            if (!_help) return head + "F1 Hilfe";
            return head +
                   "1-9 Station · Bild↑/↓ Fahrzeug\n" +
                   "O Bot · P Dummy · B Endboss · J Abschuss simulieren · K leeren\n" +
                   "I Inventar · H auffüllen · U Arena-Zustand\n" +
                   "Strg+R zurücksetzen · Y neuer Seed · F1 Hilfe";
        }

        void GoToStation(int index)
        {
            var sp = SpawnPoint.All.FirstOrDefault(s => s.index == index);
            if (sp == null || Player == null) return;
            Station = index;
            var v = Player;
            v.Body.linearVelocity = Vector3.zero;
            v.Body.angularVelocity = Vector3.zero;
            v.Body.position = sp.transform.position + Vector3.up * 0.3f;
            v.Body.rotation = sp.transform.rotation;
            v.transform.SetPositionAndRotation(sp.transform.position + Vector3.up * 0.3f, sp.transform.rotation);
            TestLog.Write("lab", v.name, "station", $"{index}:{sp.name}");
        }

        void SwapVehicle(int step)
        {
            var list = GameDatabase.Instance.Playable().ToList();
            int idx = list.FindIndex(d => d.id == MatchSetup.PlayerVehicleId);
            MatchSetup.PlayerVehicleId = list[(idx + step + list.Count) % list.Count].id;
            var mm = MatchManager.Instance;
            var old = Player;
            if (mm == null || old == null) return;
            var pos = old.transform.position + Vector3.up * 0.5f;
            var rot = Quaternion.Euler(0f, old.transform.eulerAngles.y, 0f);
            mm.Despawn(old);
            var v = mm.SpawnExtra(GameDatabase.Instance.Vehicle(MatchSetup.PlayerVehicleId), pos, rot, true);
            var cam = FindAnyObjectByType<ChaseCamera>();
            if (cam != null) cam.target = v;
            ApplyInventory();
        }

        void SpawnOpponent(bool dummy)
        {
            var mm = MatchManager.Instance;
            if (mm == null || Player == null) return;
            var list = GameDatabase.Instance.Playable().ToList();
            var def = list[_nextOpponent++ % list.Count];
            var t = Player.transform;
            var pos = t.position + t.forward * 22f + Vector3.up * 0.5f;
            var v = mm.SpawnExtra(def, pos, Quaternion.LookRotation(-t.forward), false, dummy ? def.displayName + " (Dummy)" : null);
            if (dummy)
            {
                var brain = v.GetComponent<BotBrain>();
                if (brain != null) Destroy(brain);
                v.Input = null;
            }
        }

        void SpawnBoss()
        {
            var mm = MatchManager.Instance;
            var pad = GameObject.Find("Boss_Pad");
            if (mm == null) return;
            var pos = pad != null ? pad.transform.position + Vector3.up * 0.5f : Vector3.up;
            var rot = pad != null ? pad.transform.rotation : Quaternion.identity;
            mm.SpawnExtra(GameDatabase.Instance.Vehicle("dread_jester"), pos, rot, false);
        }

        void BreakAll()
        {
            foreach (var d in Destructible.All.ToArray()) d.Break(null, "lab-preset");
        }

        void ResetLab(bool newSeed)
        {
            MatchSetup.RandomizeSeedOnStart = newSeed;
            Projectile.DespawnAll();
            TestLog.Write("lab", "-", "reset", newSeed ? "new-seed" : "same-seed");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
