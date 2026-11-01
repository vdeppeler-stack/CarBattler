using UnityEngine;

namespace IronSyndicate
{
    public class SpawnPoint : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<SpawnPoint> All = new System.Collections.Generic.List<SpawnPoint>();

        public int index;
        [Tooltip("Locked spawns (e.g. on a destroyed bridge segment) are skipped.")]
        public bool blocked;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool IsFree(float clearance = 4f)
        {
            if (blocked) return false;
            foreach (var v in Vehicle.All)
                if (Vector3.Distance(v.transform.position, transform.position) < clearance) return false;
            return true;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = blocked ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(2.2f, 1.5f, 4.5f));
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 4f);
        }
    }
}
