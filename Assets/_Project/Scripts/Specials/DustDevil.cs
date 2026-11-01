using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Sandviper: sand cloud trail; Sandviper cannot be locked, pursuers inside lose grip.</summary>
    public class DustDevil : SpecialAbility
    {
        const float Duration = 4f;
        const float CloudLife = 3f;
        const float CloudRadius = 3.5f;
        static readonly Collider[] Hits = new Collider[16];

        float _until, _nextCloud;
        readonly System.Collections.Generic.List<(Vector3 pos, float until, GameObject fx)> _clouds = new System.Collections.Generic.List<(Vector3, float, GameObject)>();

        public override bool IsActive => Time.time < _until;

        public override bool TryActivate(out string reason)
        {
            _until = Time.time + Duration;
            V.Status.Cloak(Duration);
            reason = null;
            return true;
        }

        public override void Step(in VehicleCommand c, float dt)
        {
            if (IsActive && Time.time >= _nextCloud)
            {
                _nextCloud = Time.time + 0.25f;
                var pos = V.RearMuzzle.position;
                var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(fx.GetComponent<Collider>());
                fx.transform.position = pos;
                fx.transform.localScale = Vector3.one * CloudRadius * 1.6f;
                fx.GetComponent<Renderer>().sharedMaterial = Fx.UnlitColor(new Color(0.85f, 0.75f, 0.5f));
                Fx.NoShadows(fx.GetComponent<Renderer>());
                _clouds.Add((pos, Time.time + CloudLife, fx));
            }
            for (int i = _clouds.Count - 1; i >= 0; i--)
            {
                var cl = _clouds[i];
                if (Time.time > cl.until) { Destroy(cl.fx); _clouds.RemoveAt(i); continue; }
                int n = Physics.OverlapSphereNonAlloc(cl.pos, CloudRadius, Hits, LayerMask.GetMask("Vehicle"));
                for (int k = 0; k < n; k++)
                {
                    var other = Vehicle.Of(Hits[k].attachedRigidbody);
                    if (other == null || other == V || !other.IsAlive) continue;
                    if (!other.Status.IsSnared) ReportHit(other);
                    other.Status.Snare(0.6f * Power);
                }
            }
        }

        public override void OnOwnerDeath()
        {
            foreach (var cl in _clouds) Destroy(cl.fx);
            _clouds.Clear();
        }

        public override bool BotShouldUse(Vehicle target, float distance, bool los, out string reason)
        {
            if (Behind(V, target, 40f, 30f)) { reason = "pursuer-behind"; return true; }
            if (V.HpFraction < 0.5f && distance < 35f) { reason = "low-hp-escape"; return true; }
            reason = "no-pursuer";
            return false;
        }
    }
}
