using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IronSyndicate
{
    /// <summary>
    /// Vehicle select: roster with menu values, bot count, start. Secret vehicles appear when unlocked by
    /// code or when the developer roster is enabled.
    /// </summary>
    public class VehicleSelectMenu : MonoBehaviour
    {
        public string matchScene = "Proto_Arena";

        static readonly HashSet<string> Unlocked = new HashSet<string>();

        Font _font;
        Text _title, _details, _info;
        Transform _list;
        readonly MenuCodes _codes = new MenuCodes(MenuCodes.VehicleSelect);
        List<VehicleDefinition> _roster;
        int _index;

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            var root = canvasGo.transform;

            _title = Label(root, new Vector2(40, -30), 40, new Vector2(1200, 50));
            _title.text = "IRON SYNDICATE · Fahrzeugwahl";
            _list = new GameObject("List", typeof(RectTransform)).transform;
            _list.SetParent(root, false);
            var lr = (RectTransform)_list;
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0, 1);
            lr.anchoredPosition = new Vector2(40, -100);
            _details = Label(root, new Vector2(520, -100), 20, new Vector2(700, 400));
            _info = Label(root, new Vector2(40, -660), 16, new Vector2(1200, 40));
            Rebuild();
        }

        void Rebuild()
        {
            var db = GameDatabase.Instance;
            _roster = db.Playable().Where(v => !v.secret || MatchSetup.RosterUnlocked || Unlocked.Contains(v.id)).ToList();
            _index = Mathf.Clamp(_roster.FindIndex(v => v.id == MatchSetup.PlayerVehicleId), 0, _roster.Count - 1);
            foreach (Transform c in _list) Destroy(c.gameObject);
            for (int i = 0; i < _roster.Count; i++)
            {
                int idx = i;
                var go = new GameObject(_roster[i].id, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_list, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(0, -i * 36);
                rt.sizeDelta = new Vector2(440, 32);
                go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
                go.GetComponent<Button>().onClick.AddListener(() => { _index = idx; Refresh(); });
                var l = Label(go.transform, new Vector2(10, -4), 18, new Vector2(420, 26));
                l.text = _roster[i].displayName + (_roster[i].secret ? "  ★" : "");
            }
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < _list.childCount; i++)
                _list.GetChild(i).GetComponent<Image>().color = i == _index ? new Color(0.9f, 0.55f, 0.1f, 0.95f) : new Color(0.2f, 0.2f, 0.25f, 0.9f);
            var v = _roster[_index];
            string Stars(int n) => new string('■', n) + new string('□', 5 - n);
            _details.text =
                $"{v.displayName}\nFahrer: {v.driver} · {v.vehicleType}\n\n" +
                $"Panzerung     {Stars(v.armorStars)}\nTempo         {Stars(v.speedStars)}\nSpezial       {Stars(v.specialStars)}\nHandling      {v.handling}\n\n" +
                $"Spezial: {v.specialName}\n{v.specialDescription}\n\nHP {v.maxHp:F0} · {v.topSpeed * 3.6f:F0} km/h";
            _info.text = $"↑/↓ wählen · Enter/Start: Gefecht gegen {MatchSetup.BotCount} Bots · ←/→ Botanzahl · Esc: Beenden";
        }

        void Update()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            float t = Time.unscaledTime;

            if (Pressed(kb?.upArrowKey, pad?.dpad.up)) { Code(MenuKey.Up, t); _index = (_index - 1 + _roster.Count) % _roster.Count; Refresh(); }
            if (Pressed(kb?.downArrowKey, pad?.dpad.down)) { Code(MenuKey.Down, t); _index = (_index + 1) % _roster.Count; Refresh(); }
            if (Pressed(kb?.leftArrowKey, pad?.dpad.left)) { Code(MenuKey.Left, t); MatchSetup.BotCount = Mathf.Max(1, MatchSetup.BotCount - 1); Refresh(); }
            if (Pressed(kb?.rightArrowKey, pad?.dpad.right)) { Code(MenuKey.Right, t); MatchSetup.BotCount = Mathf.Min(9, MatchSetup.BotCount + 1); Refresh(); }
            if (Pressed(kb?.qKey, pad?.leftShoulder)) Code(MenuKey.L1, t);
            if (Pressed(kb?.eKey, pad?.rightShoulder)) Code(MenuKey.R1, t);
            if (Pressed(kb?.tKey, pad?.buttonNorth)) Code(MenuKey.Triangle, t);
            if (Pressed(kb?.enterKey, pad?.startButton) || Pressed(kb?.numpadEnterKey, pad?.buttonSouth)) StartMatch();
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Application.Quit();
        }

        void Code(MenuKey key, float t)
        {
            var id = _codes.Push(key, t);
            if (id == null) return;
            Unlocked.Add(id);
            TestLog.Write("menu", "-", "code-unlock", id);
            Rebuild();
            _info.text = "Freigeschaltet: " + GameDatabase.Instance.Vehicle(id).displayName;
        }

        static bool Pressed(params UnityEngine.InputSystem.Controls.ButtonControl[] controls) =>
            controls.Any(c => c != null && c.wasPressedThisFrame);

        void StartMatch()
        {
            MatchSetup.Configured = true;
            MatchSetup.PlayerVehicleId = _roster[_index].id;
            MatchSetup.HumanPlaying = true;
            MatchSetup.BotVehicleIds.Clear();
            MatchSetup.SpawnOrder.Clear();
            MatchSetup.RandomizeSeedOnStart = true;
            MatchSetup.Label = "match";
            SceneManager.LoadScene(matchScene);
        }

        Text Label(Transform parent, Vector2 pos, int size, Vector2 box)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.color = Color.white;
            return t;
        }
    }
}
