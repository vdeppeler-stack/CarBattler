using UnityEngine;

namespace IronSyndicate
{
    public enum HandlingClass { Sluggish, Average, Good, Tight }

    public enum BodyStyle { Wheel, Buggy, Loader, Hearse, Formula, Humvee, Sedan, Lowrider, Coupe, Wasteland, Bike, MonsterTruck, ShowTruck, Apc, BossRig }

    [CreateAssetMenu(menuName = "Iron Syndicate/Vehicle Definition")]
    public class VehicleDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public string driver;
        public string vehicleType;
        public bool playable = true;
        public bool secret;
        public bool boss;

        [Header("Menu values (stars 1-5)")]
        [Range(1, 5)] public int armorStars = 3;
        [Range(1, 5)] public int speedStars = 3;
        [Range(1, 5)] public int specialStars = 3;
        public HandlingClass handling = HandlingClass.Average;

        [Header("Physics (derived from stars, tweakable)")]
        public float mass = 1600f;
        public float maxHp = 220f;
        public float acceleration = 16f;
        public float topSpeed = 38f;
        public float cornerGrip = 1f;
        public float turnRate = 110f;

        [Header("Special")]
        public string specialId;
        public string specialName;
        [TextArea] public string specialDescription;
        public int specialCharges = 2;
        public float specialRecharge = 12f;
        public float specialPower = 1f;

        [Header("Placeholder body")]
        public BodyStyle bodyStyle = BodyStyle.Sedan;
        public Vector3 bodySize = new Vector3(2.0f, 1.0f, 4.2f);
        public float wheelRadius = 0.45f;
        public Color bodyColor = Color.gray;
        public GameObject bodyPrefab;

        /// <summary>Derives the tunable physical values from the menu stars. Called by the data builder.</summary>
        public void ApplyStarDefaults()
        {
            maxHp = 100f + 40f * armorStars;
            mass = 1000f + 350f * armorStars;
            topSpeed = 28f + 4f * speedStars;
            acceleration = 11f + 2.2f * speedStars;
            float h = HandlingFactor(handling);
            cornerGrip = h;
            turnRate = 95f * h;
            specialPower = 0.7f + 0.15f * specialStars;
            specialRecharge = 18f - 2f * specialStars;
        }

        public static float HandlingFactor(HandlingClass c)
        {
            switch (c)
            {
                case HandlingClass.Sluggish: return 0.78f;
                case HandlingClass.Good: return 1.12f;
                case HandlingClass.Tight: return 1.25f;
                default: return 1f;
            }
        }
    }
}
