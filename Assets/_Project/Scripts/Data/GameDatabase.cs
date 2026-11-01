using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>Single entry point for all definitions. Lives in Resources so builds and the lab find it.</summary>
    [CreateAssetMenu(menuName = "Iron Syndicate/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "IronSyndicate/GameDatabase";

        public List<VehicleDefinition> vehicles = new List<VehicleDefinition>();
        public List<WeaponDefinition> weapons = new List<WeaponDefinition>();

        static GameDatabase _instance;

        public static GameDatabase Instance
        {
            get
            {
                if (_instance == null) _instance = Resources.Load<GameDatabase>(ResourcePath);
                return _instance;
            }
        }

        public VehicleDefinition Vehicle(string id) => vehicles.Find(v => v != null && v.id == id);
        public WeaponDefinition Weapon(string id) => weapons.Find(w => w != null && w.id == id);

        public IEnumerable<VehicleDefinition> Playable()
        {
            foreach (var v in vehicles)
                if (v != null && v.playable) yield return v;
        }
    }
}
