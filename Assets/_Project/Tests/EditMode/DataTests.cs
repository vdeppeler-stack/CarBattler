using System.Linq;
using NUnit.Framework;

namespace IronSyndicate.Tests
{
    /// <summary>Definitions against the handbook tables (charges, roster, menu values, costs).</summary>
    public class DataTests
    {
        static GameDatabase Db => GameDatabase.Instance;

        [Test]
        public void DatabaseHasRosterAndWeapons()
        {
            Assert.IsNotNull(Db, "Run Iron Syndicate/Build/Data first.");
            Assert.AreEqual(14, Db.Playable().Count(), "14 playable vehicles");
            Assert.AreEqual(16, Db.vehicles.Count, "14 playable + end boss in two phases");
            Assert.AreEqual(8, Db.weapons.Count);
        }

        [TestCase("blaze_rocket", 2)]
        [TestCase("seeker_dart", 2)]
        [TestCase("titan_warhead", 1)]
        [TestCase("kinetic_disc", 3)]
        [TestCase("c4_satchel", 1)]
        [TestCase("thermite_gel", 2)]
        [TestCase("spider_mine", 3)]
        [TestCase("tesla_arc", 1)]
        public void WeaponChargesMatchHandbook(string id, int charges)
        {
            var w = Db.Weapon(id);
            Assert.IsNotNull(w, id);
            Assert.AreEqual(charges, w.chargesPerPickup);
        }

        [Test]
        public void WeaponBehaviourRules()
        {
            Assert.Greater(Db.Weapon("seeker_dart").homingDegPerSec, Db.Weapon("blaze_rocket").homingDegPerSec, "Seeker tracks harder");
            Assert.Greater(Db.Weapon("blaze_rocket").directDamage, Db.Weapon("seeker_dart").directDamage, "Blaze hits harder");
            Assert.AreEqual(0f, Db.Weapon("titan_warhead").homingDegPerSec, "Titan is dumbfire");
            Assert.IsTrue(Db.Weapon("seeker_dart").requiresLock, "Seeker needs lock (cloak blocks it)");
            Assert.IsTrue(Db.Weapon("thermite_gel").ignites);
            Assert.IsTrue(Db.Weapon("tesla_arc").environmentOnly);
        }

        [TestCase("goliath_wheel", 3, 3, 3, HandlingClass.Average)]
        [TestCase("sandviper", 1, 4, 3, HandlingClass.Tight)]
        [TestCase("iron_jaw", 4, 2, 5, HandlingClass.Sluggish)]
        [TestCase("nocturne", 2, 3, 4, HandlingClass.Average)]
        [TestCase("vortex_f1", 1, 5, 4, HandlingClass.Tight)]
        [TestCase("bastion_mk4", 4, 2, 3, HandlingClass.Sluggish)]
        [TestCase("interceptor_v8", 3, 4, 3, HandlingClass.Good)]
        [TestCase("chrome_igniter", 3, 4, 5, HandlingClass.Average)]
        [TestCase("phase_gt", 1, 5, 3, HandlingClass.Tight)]
        [TestCase("rustfang", 2, 4, 3, HandlingClass.Good)]
        [TestCase("bonechaser", 1, 5, 5, HandlingClass.Good)]
        [TestCase("mammoth_4x4", 4, 2, 4, HandlingClass.Sluggish)]
        [TestCase("jester_van", 4, 2, 5, HandlingClass.Sluggish)]
        [TestCase("leviathan_apc", 5, 4, 5, HandlingClass.Average)]
        public void MenuValuesMatchProfiles(string id, int armor, int speed, int special, HandlingClass handling)
        {
            var v = Db.Vehicle(id);
            Assert.IsNotNull(v, id);
            Assert.AreEqual(armor, v.armorStars, "armor");
            Assert.AreEqual(speed, v.speedStars, "speed");
            Assert.AreEqual(special, v.specialStars, "special");
            Assert.AreEqual(handling, v.handling, "handling");
            Assert.IsTrue(SpecialRegistry.Ids.Contains(v.specialId), "special registered: " + v.specialId);
        }

        [Test]
        public void StarsOrderPhysicalValues()
        {
            var light = Db.Vehicle("vortex_f1");
            var heavy = Db.Vehicle("leviathan_apc");
            Assert.Greater(heavy.maxHp, light.maxHp);
            Assert.Greater(heavy.mass, light.mass);
            Assert.Greater(light.topSpeed, Db.Vehicle("iron_jaw").topSpeed);
            Assert.Greater(Db.Vehicle("phase_gt").cornerGrip, Db.Vehicle("mammoth_4x4").cornerGrip);
        }

        [Test]
        public void EndBossIsNotSelectable()
        {
            var boss = Db.Vehicle("dread_jester");
            Assert.IsNotNull(boss);
            Assert.IsFalse(boss.playable);
            Assert.IsTrue(boss.boss);
        }

        [Test]
        public void OverdriveCostsMatchHandbook()
        {
            Assert.AreEqual(15f, VehicleArsenal.CostOf(Maneuver.ThrusterHop));
            Assert.AreEqual(30f, VehicleArsenal.CostOf(Maneuver.AegisBarrier));
            Assert.AreEqual(30f, VehicleArsenal.CostOf(Maneuver.QuickMine));
            Assert.AreEqual(30f, VehicleArsenal.CostOf(Maneuver.ThermiteBurst));
            Assert.AreEqual(45f, VehicleArsenal.CostOf(Maneuver.CryoPulse));
            Assert.AreEqual(45f, VehicleArsenal.CostOf(Maneuver.RearCryoPulse));
            Assert.AreEqual(30f, VehicleArsenal.CostOf(Maneuver.RearThermite));
            Assert.AreEqual(75f, VehicleArsenal.CostOf(Maneuver.PhantomCloak));
            Assert.AreEqual(100f, VehicleArsenal.CostOf(Maneuver.CerberusBarrage));
            Assert.AreEqual(0f, VehicleArsenal.CostOf(Maneuver.RearSalvo), "costs one weapon, no energy");
            Assert.AreEqual(8f, VehicleArsenal.EnergyRegenPerSec);
            Assert.AreEqual(50f, VehicleArsenal.EnergyColorThreshold);
        }
    }
}
