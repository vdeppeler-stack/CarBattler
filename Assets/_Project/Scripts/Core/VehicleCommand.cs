using System;

namespace IronSyndicate
{
    public enum Maneuver
    {
        None,
        ThrusterHop,
        AegisBarrier,
        QuickMine,
        ThermiteBurst,
        CryoPulse,
        PhantomCloak,
        RearSalvo,
        RearCryoPulse,
        RearThermite,
        CerberusBarrage,
        ScrapToRepair
    }

    /// <summary>
    /// One physics step worth of intent for a single seat. Keyboard, gamepad and bots all produce this.
    /// Edge fields (WeaponFire, Special, WeaponCycle, Maneuver) are true for exactly one command.
    /// </summary>
    [Serializable]
    public struct VehicleCommand
    {
        public float Steer;
        public float Throttle;
        public float Brake;
        public bool Drift;
        public bool Boost;
        public bool PrimaryFire;
        public bool WeaponFire;
        public bool Special;
        public int WeaponCycle;
        public Maneuver Maneuver;

        public static readonly VehicleCommand Idle = default;
    }

    public interface ISeatInput
    {
        /// <summary>Called once per physics step; consumes any buffered edges.</summary>
        VehicleCommand ReadCommand(float time);
    }
}
