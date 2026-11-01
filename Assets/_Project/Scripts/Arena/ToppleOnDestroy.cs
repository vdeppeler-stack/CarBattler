using System.Collections;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// "Tower becomes road": when the owning destructible breaks, the tower falls around its base pivot and
    /// comes to rest as a drivable bridge. Uses a kinematic body so vehicles in the way are shoved.
    /// </summary>
    [RequireComponent(typeof(Destructible))]
    public class ToppleOnDestroy : MonoBehaviour
    {
        [Tooltip("Rotation axis in world space (falls towards the side this axis points around).")]
        public Vector3 axis = Vector3.right;
        public float angle = 90f;
        public float seconds = 1.6f;

        void Awake() => GetComponent<Destructible>().Destroyed += _ => StartCoroutine(Fall());

        IEnumerator Fall()
        {
            foreach (var c in GetComponentsInChildren<Transform>()) c.gameObject.isStatic = false;
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var start = transform.rotation;
            var end = Quaternion.AngleAxis(angle, axis) * start;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.fixedDeltaTime / seconds);
                // Ease-in: a toppling tower accelerates.
                rb.MoveRotation(Quaternion.Slerp(start, end, t * t));
                yield return new WaitForFixedUpdate();
            }
            Fx.Burst(transform.position, 6f, new Color(0.7f, 0.65f, 0.55f), 0.6f);
            TestLog.Write("arena", name, "toppled", "road");
        }
    }
}
