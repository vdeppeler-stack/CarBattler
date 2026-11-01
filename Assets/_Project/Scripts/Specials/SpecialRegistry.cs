using UnityEngine;

namespace IronSyndicate
{
    public static class SpecialRegistry
    {
        public static readonly string[] Ids =
        {
            "gyro_crush", "dust_devil", "wall_press", "wraith_lantern", "slipstream_lance", "mortar_grid", "spike_net",
            "exhaust_inferno", "phase_shift", "chain_harpoon", "scythe_sweep", "battering_charge", "fireworks_finale", "siege_lock",
            "dread_ram", "hellfire_carousel"
        };

        public static SpecialAbility Attach(GameObject go, string id)
        {
            switch (id)
            {
                case "gyro_crush": return go.AddComponent<GyroCrush>();
                case "dust_devil": return go.AddComponent<DustDevil>();
                case "wall_press": return go.AddComponent<WallPress>();
                case "wraith_lantern": return go.AddComponent<WraithLantern>();
                case "slipstream_lance": return go.AddComponent<SlipstreamLance>();
                case "mortar_grid": return go.AddComponent<MortarGrid>();
                case "spike_net": return go.AddComponent<SpikeNet>();
                case "exhaust_inferno": return go.AddComponent<ExhaustInferno>();
                case "phase_shift": return go.AddComponent<PhaseShift>();
                case "chain_harpoon": return go.AddComponent<ChainHarpoon>();
                case "scythe_sweep": return go.AddComponent<ScytheSweep>();
                case "battering_charge": return go.AddComponent<BatteringCharge>();
                case "fireworks_finale": return go.AddComponent<FireworksFinale>();
                case "siege_lock": return go.AddComponent<SiegeLock>();
                case "dread_ram": return go.AddComponent<DreadRam>();
                case "hellfire_carousel": return go.AddComponent<HellfireCarousel>();
                default:
                    if (!string.IsNullOrEmpty(id)) Debug.LogWarning($"[IS] Unknown special '{id}'");
                    return null;
            }
        }
    }
}
