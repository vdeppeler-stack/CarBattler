using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Burning ground left by Thermite Gel; ignites vehicles inside and anything flagged burnable.</summary>
    public class BurnZone : MonoBehaviour
    {
        static readonly Collider[] Hits = new Collider[16];

        float _radius, _until;
        Vehicle _source;

        public static void Spawn(Vector3 at, float radius, float duration, Vehicle source)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "BurnZone";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = at + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            go.GetComponent<Renderer>().sharedMaterial = Fx.UnlitColor(new Color(1f, 0.35f, 0.05f));
            Fx.NoShadows(go.GetComponent<Renderer>());
            var z = go.AddComponent<BurnZone>();
            z._radius = radius;
            z._until = Time.time + duration;
            z._source = source;
        }

        void FixedUpdate()
        {
            if (Time.time > _until) { Destroy(gameObject); return; }
            int n = Physics.OverlapSphereNonAlloc(transform.position, _radius, Hits, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var c = Hits[i];
                var v = Vehicle.Of(c.attachedRigidbody);
                if (v != null && v.IsAlive && !v.Status.IsBurning) v.Status.Ignite(_source);
                else if (v == null && c.TryGetComponent<IBurnable>(out var b)) b.Burn(_source);
            }
        }
    }

    public interface IBurnable
    {
        void Burn(Vehicle source);
    }
}
