using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace IronSyndicate
{
    /// <summary>
    /// Runtime-built HUD for the human seat: HP, nitro, Overdrive (blue below / red above threshold), weapon and
    /// ammo, special readiness, status icons that differ in symbol and text (not only colour), opponents and results.
    /// </summary>
    public class MatchHud : MonoBehaviour
    {
        MatchManager _match;
        Font _font;
        Image _hpFill, _nitroFill, _energyFill;
        Text _hpText, _weaponText, _specialText, _statusText, _centerText, _opponentsText, _comboText, _helpText;
        GameObject _results;
        Text _resultsText;

        public void Bind(MatchManager match)
        {
            _match = match;
            match.Ended += ShowResults;
        }

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            var root = canvasGo.transform;

            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var panel = Panel(root, new Vector2(0, 1), new Vector2(16, -16), new Vector2(330, 172));
            _hpFill = Bar(panel, 0, "HP", new Color(0.2f, 0.85f, 0.3f), out _hpText);
            _nitroFill = Bar(panel, 1, "NITRO", new Color(0.2f, 0.6f, 1f), out _);
            _energyFill = Bar(panel, 2, "OVERDRIVE", new Color(0.3f, 0.5f, 1f), out _);
            _weaponText = Label(panel, new Vector2(10, -96), 18, TextAnchor.UpperLeft, new Vector2(310, 24));
            _specialText = Label(panel, new Vector2(10, -120), 16, TextAnchor.UpperLeft, new Vector2(310, 22));
            _statusText = Label(panel, new Vector2(10, -144), 16, TextAnchor.UpperLeft, new Vector2(310, 22));

            var opp = Panel(root, new Vector2(1, 1), new Vector2(-16, -16), new Vector2(280, 220));
            _opponentsText = Label(opp, new Vector2(10, -8), 15, TextAnchor.UpperLeft, new Vector2(260, 210));

            _centerText = Label(root, Vector2.zero, 64, TextAnchor.MiddleCenter, new Vector2(900, 120));
            var ct = _centerText.rectTransform;
            ct.anchorMin = ct.anchorMax = ct.pivot = new Vector2(0.5f, 0.62f);
            ct.anchoredPosition = Vector2.zero;

            _comboText = Label(root, Vector2.zero, 16, TextAnchor.LowerLeft, new Vector2(600, 24));
            var cb = _comboText.rectTransform;
            cb.anchorMin = cb.anchorMax = cb.pivot = new Vector2(0, 0);
            cb.anchoredPosition = new Vector2(16, 40);

            _helpText = Label(root, Vector2.zero, 13, TextAnchor.LowerLeft, new Vector2(1200, 20));
            var hb = _helpText.rectTransform;
            hb.anchorMin = hb.anchorMax = hb.pivot = new Vector2(0, 0);
            hb.anchoredPosition = new Vector2(16, 12);
            _helpText.text = "WASD fahren · Leertaste Drift · Umschalt Nitro · LMB Bordkanone · RMB Waffe · Q/E Waffe wechseln · F Spezial · R Rückblick";
            _helpText.color = new Color(1f, 1f, 1f, 0.6f);

            BuildResults(root);
        }

        void Update()
        {
            if (_match == null) return;
            var v = _match.Human != null ? _match.Human : _match.Participants.FirstOrDefault();

            if (_match.State == MatchManager.Phase.Countdown)
                _centerText.text = Mathf.CeilToInt(Mathf.Max(0f, _match.CountdownLeft)).ToString();
            else if (_match.State == MatchManager.Phase.Running)
                _centerText.text = _match.RoundTime < 0.8f ? "LOS!" : (v != null && !v.IsAlive ? "AUSGESCHIEDEN" : "");

            if (v != null) UpdateSeat(v);
            UpdateOpponents(v);
        }

        void UpdateSeat(Vehicle v)
        {
            _hpFill.fillAmount = v.HpFraction;
            _hpText.text = $"HP {v.Hp:F0}/{v.MaxHp:F0}";
            _nitroFill.fillAmount = v.Motor.Nitro / VehicleMotor.NitroMax;
            var a = v.Arsenal;
            _energyFill.fillAmount = a.Energy / VehicleArsenal.EnergyMax;
            _energyFill.color = a.Energy >= VehicleArsenal.EnergyColorThreshold ? new Color(1f, 0.25f, 0.2f) : new Color(0.3f, 0.5f, 1f);

            var sel = a.Selected;
            _weaponText.text = a.HasPlacedRemote ? "C4 gelegt – Waffe erneut drücken zum Zünden"
                : sel != null && sel.Ammo > 0 ? $"{sel.Def.displayName}  x{sel.Ammo}   (gesamt {a.TotalAmmo})" : "Keine Waffe";
            _specialText.text = a.Special == null ? "Spezial: –"
                : a.SpecialCharges > 0 ? $"Spezial {v.Definition.specialName}: BEREIT ({a.SpecialCharges})"
                : $"Spezial {v.Definition.specialName}: lädt {a.SpecialRechargeProgress:P0}";

            var sb = new StringBuilder();
            var s = v.Status;
            if (s.IsBurning) sb.Append("▲ BRAND  ");
            if (s.IsFrozen) sb.Append($"◆ STASE ({s.IceLeft})  ");
            if (s.IsShielded) sb.Append($"● SCHILD {s.ShieldLeft:F1}s  ");
            if (s.IsCloaked) sb.Append("◌ TARNUNG  ");
            if (s.IsSnared) sb.Append("✕ NETZ  ");
            if (s.IsPhased) sb.Append("≈ PHASE  ");
            _statusText.text = sb.ToString();

            var input = v.GetComponent<PlayerSeatInput>();
            _comboText.text = input != null && input.Combo.Count > 0 ? "Kombo: " + new string('•', input.Combo.Count) : "";
        }

        void UpdateOpponents(Vehicle self)
        {
            var sb = new StringBuilder("Teilnehmer\n");
            foreach (var p in _match.Participants)
            {
                string mark = p == self ? "► " : "  ";
                sb.Append(mark).Append(p.Definition.displayName);
                sb.Append(p.IsAlive ? $"  {p.Hp:F0}" : "  ✕").Append('\n');
            }
            _opponentsText.text = sb.ToString();
        }

        void ShowResults(MatchManager m)
        {
            _results.SetActive(true);
            _centerText.text = "";
            var sb = new StringBuilder();
            sb.Append(m.Draw ? "UNENTSCHIEDEN\n" : $"SIEG: {m.Winner.Definition.displayName}{(m.Winner == m.Human ? " (Du)" : "")}\n");
            sb.Append($"Dauer {m.RoundTime:F0}s · Seed {m.Seed}\n\n");
            foreach (var p in m.Participants.OrderByDescending(p => p.Stats.damageDealt))
                sb.Append($"{p.Definition.displayName,-18} Schaden {p.Stats.damageDealt,5:F0}  erlitten {p.Stats.damageTaken,5:F0}  Spezial {p.Stats.specialHits}/{p.Stats.specialAttempts}\n");
            _resultsText.text = sb.ToString();
            var es = EventSystem.current;
            if (es != null) es.SetSelectedGameObject(_results.GetComponentInChildren<Button>().gameObject);
        }

        void BuildResults(Transform root)
        {
            _results = new GameObject("Results", typeof(RectTransform), typeof(Image));
            _results.transform.SetParent(root, false);
            var rt = (RectTransform)_results.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720, 420);
            _results.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            _resultsText = Label(_results.transform, new Vector2(24, -20), 18, TextAnchor.UpperLeft, new Vector2(672, 300));
            Button(_results.transform, "Wiederholen", new Vector2(-130, 36), () => _match.Restart());
            Button(_results.transform, "Fahrzeug wechseln", new Vector2(130, 36), () => _match.ChangeVehicle());
            _results.SetActive(false);
        }

        // ------------------------------------------------------------------ widgets

        RectTransform Panel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            return rt;
        }

        Image Bar(RectTransform panel, int row, string label, Color color, out Text valueText)
        {
            var l = Label(panel, new Vector2(10, -10 - row * 28), 14, TextAnchor.MiddleLeft, new Vector2(90, 22));
            l.text = label;
            var bg = new GameObject(label + "Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(panel, false);
            var bgr = (RectTransform)bg.transform;
            bgr.anchorMin = bgr.anchorMax = bgr.pivot = new Vector2(0, 1);
            bgr.anchoredPosition = new Vector2(100, -12 - row * 28);
            bgr.sizeDelta = new Vector2(220, 18);
            bg.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            var fill = new GameObject(label + "Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(bg.transform, false);
            var fr = (RectTransform)fill.transform;
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            var img = fill.GetComponent<Image>();
            img.color = color;
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;

            valueText = Label(bg.transform, new Vector2(6, 0), 13, TextAnchor.MiddleLeft, new Vector2(210, 18));
            var vr = valueText.rectTransform;
            vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0, 0.5f);
            vr.anchoredPosition = new Vector2(6, 0);
            return img;
        }

        Text Label(Transform parent, Vector2 pos, int size, TextAnchor align, Vector2 box)
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
            t.alignment = align;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            go.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
            return t;
        }

        void Button(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(230, 48);
            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            var l = Label(go.transform, Vector2.zero, 20, TextAnchor.MiddleCenter, new Vector2(230, 48));
            l.text = text;
        }
    }
}
