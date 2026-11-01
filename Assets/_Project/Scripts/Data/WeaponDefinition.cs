using UnityEngine;

namespace IronSyndicate
{
    public enum WeaponBehaviour { Rocket, Bounce, Remote, Lob, Mine, TeslaArc }

    [CreateAssetMenu(menuName = "Iron Syndicate/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public WeaponBehaviour behaviour = WeaponBehaviour.Rocket;
        public int chargesPerPickup = 2;
        public float cooldown = 0.6f;

        [Header("Damage")]
        public float directDamage = 30f;
        public float splashDamage = 10f;
        public float splashRadius = 3f;
        public bool ignites;
        public bool freezes;

        [Header("Flight")]
        public float speed = 60f;
        public float range = 120f;
        public float homingDegPerSec = 0f;
        public bool requiresLock;
        public float lockConeDeg = 25f;
        public float gravity = 0f;
        public float fuseTime = 0f;
        public float triggerRadius = 0f;
        public Color color = Color.white;
        public float visualScale = 0.5f;

        /// <summary>Only offered in arenas with a Tesla reactor.</summary>
        public bool environmentOnly;

        public float Lifetime => speed > 0.01f ? range / speed : 30f;
    }
}
