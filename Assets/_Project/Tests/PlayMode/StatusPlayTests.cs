using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace IronSyndicate.Tests
{
    /// <summary>Handbook 10: burn transfer, the three extinguish paths, stasis on a ramp, shield end, lock-on vs cloak.</summary>
    public class StatusPlayTests : LabFixture
    {
        [UnityTest]
        public IEnumerator PM01_BurnTransfersOnRam()
        {
            var a = Spawn("mammoth_4x4", new Vector3(0f, 0.3f, 0f));
            var b = Spawn("sandviper", new Vector3(0f, 0.3f, 14f));
            yield return Seconds(0.5f);
            a.Status.Ignite(null, 10f);
            a.Body.linearVelocity = Vector3.forward * 16f;
            yield return Seconds(1.5f);
            TestProtocol.Check("PM-01", "A brennt, B steht 14 m voraus", "A rammt B mit 16 m/s", "B brennt", b.Status.IsBurning ? "B brennt" : "B brennt nicht", b.Status.IsBurning);
        }

        [UnityTest]
        public IEnumerator PM02_NitroExtinguishes()
        {
            var seat = new ScriptedSeat();
            var v = Spawn("vortex_f1", new Vector3(0f, 0.3f, 0f), 0f, seat);
            yield return Seconds(0.5f);
            v.Status.Ignite(null, 10f);
            seat.Cmd = new VehicleCommand { Boost = true, Throttle = 1f };
            yield return Seconds(0.3f);
            TestProtocol.Check("PM-02", "Fahrzeug brennt (10 s)", "Nitro zünden", "Brand gelöscht", v.Status.IsBurning ? "brennt" : "gelöscht", !v.Status.IsBurning);
        }

        [UnityTest]
        public IEnumerator PM03_AegisExtinguishes()
        {
            var v = Spawn("iron_jaw", new Vector3(0f, 0.3f, 0f));
            yield return Seconds(0.5f);
            v.Status.Ignite(null, 10f);
            bool used = v.Arsenal.TryManeuver(Maneuver.AegisBarrier);
            yield return Seconds(0.1f);
            TestProtocol.Check("PM-03", "Fahrzeug brennt, Overdrive voll", "Aegis Barrier", "Schild aktiv, Brand gelöscht",
                $"schild={v.Status.IsShielded};brennt={v.Status.IsBurning}", used && v.Status.IsShielded && !v.Status.IsBurning);
        }

        [UnityTest]
        public IEnumerator PM04_PoolExtinguishes()
        {
            var pool = new GameObject("Pool");
            Created.Add(pool);
            pool.transform.position = new Vector3(0f, 1f, 20f);
            pool.AddComponent<BoxCollider>().size = new Vector3(12f, 2f, 10f);
            pool.AddComponent<HazardZone>().kind = HazardKind.Pool;
            var v = Spawn("bastion_mk4", new Vector3(0f, 0.3f, 0f));
            yield return Seconds(0.5f);
            v.Status.Ignite(null, 10f);
            v.Body.linearVelocity = Vector3.forward * 12f;
            yield return Seconds(2f);
            TestProtocol.Check("PM-04", "Fahrzeug brennt, Pool 20 m voraus", "in den Pool fahren", "Brand gelöscht", v.Status.IsBurning ? "brennt" : "gelöscht", !v.Status.IsBurning);
        }

        [UnityTest]
        public IEnumerator PM05_StasisKeepsGravityOnRamp()
        {
            // 15° ramp rising towards +Z; the vehicle faces uphill and holds full throttle.
            Box("Ramp", new Vector3(0f, 3.5f, 0f), new Vector3(14f, 1f, 34f), Quaternion.Euler(-15f, 0f, 0f));
            var seat = new ScriptedSeat { Cmd = new VehicleCommand { Throttle = 1f } };
            var v = Spawn("interceptor_v8", new Vector3(0f, 4.6f, 0f), 0f, seat);
            seat.Cmd = VehicleCommand.Idle;
            yield return Seconds(0.6f);
            v.Status.Freeze(null);
            seat.Cmd = new VehicleCommand { Throttle = 1f };
            float z0 = v.transform.position.z;
            yield return Seconds(1.5f);
            float dz = v.transform.position.z - z0;
            TestProtocol.Check("PM-05", "Fahrzeug auf 15°-Rampe, bergauf, Vollgas", "Stase auslösen", "rutscht bergab, Stase aktiv",
                $"dz={dz:F2};stase={v.Status.IsFrozen}", dz < -0.3f && v.Status.IsFrozen);
        }

        [UnityTest]
        public IEnumerator PM06_ShieldEndsAfterThreeSeconds()
        {
            var v = Spawn("phase_gt", new Vector3(0f, 0.3f, 0f));
            yield return Seconds(0.3f);
            v.Status.Shield();
            yield return Seconds(2.8f);
            bool during = v.Status.IsShielded;
            float hp = v.Hp;
            v.ReceiveDamage(DamageEvent.Create(null, v, DamageType.Missile, 30f, v.transform.position));
            bool blocked = Mathf.Approximately(hp, v.Hp);
            yield return Seconds(0.4f);
            bool after = v.Status.IsShielded;
            TestProtocol.Check("PM-06", "Schild aktiviert", "2,8 s und 3,2 s prüfen, Treffer bei 2,8 s", "bei 2,8 s aktiv + blockt, bei 3,2 s aus",
                $"2.8s={during};blockt={blocked};3.2s={after}", during && blocked && !after);
        }

        [UnityTest]
        public IEnumerator PM07_LockOnIgnoresCloak()
        {
            var a = Spawn("bastion_mk4", new Vector3(0f, 0.3f, 0f));
            var b = Spawn("nocturne", new Vector3(0f, 0.3f, 30f), 180f);
            yield return Seconds(0.5f);
            var before = Targeting.Find(a, a.Muzzle.position, a.transform.forward, 30f, 120f, true);
            b.Status.Cloak();
            var during = Targeting.Find(a, a.Muzzle.position, a.transform.forward, 30f, 120f, true);
            TestProtocol.Check("PM-07", "B 30 m vor A", "Lock-On vor und während Tarnung", "vorher B, während Tarnung kein Ziel",
                $"vorher={(before != null ? before.name : "-")};getarnt={(during != null ? during.name : "-")}", before == b && during == null);
        }

        [UnityTest]
        public IEnumerator PM08_ReedsConcealBeyondRange()
        {
            var reeds = new GameObject("Reeds");
            Created.Add(reeds);
            reeds.transform.position = new Vector3(0f, 2f, 30f);
            reeds.AddComponent<BoxCollider>().size = new Vector3(10f, 4f, 10f);
            reeds.AddComponent<HazardZone>().kind = HazardKind.Reeds;
            var a = Spawn("bastion_mk4", new Vector3(0f, 0.3f, 0f));
            var b = Spawn("nocturne", new Vector3(0f, 0.3f, 30f), 180f);
            yield return Seconds(0.5f);
            var far = Targeting.Find(a, a.Muzzle.position, a.transform.forward, 30f, 120f, true);
            a.Body.position = new Vector3(0f, 0.5f, 20f);
            a.transform.position = a.Body.position;
            yield return Seconds(0.1f);
            var near = Targeting.Find(a, a.Muzzle.position, a.transform.forward, 30f, 120f, true);
            TestProtocol.Check("PM-08", "B im Schilf", "Lock-On aus 30 m und aus 10 m", "30 m kein Ziel, 10 m B",
                $"30m={(far != null ? far.name : "-")};10m={(near != null ? near.name : "-")}", far == null && near == b);
        }
    }
}
