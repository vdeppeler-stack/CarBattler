using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace IronSyndicate.Tests
{
    /// <summary>Handbook 10 and hazard modules: teleport loop, pickup respawn, lava, abyss, ice plate, train, C4 ramp vs flat.</summary>
    public class ArenaPlayTests : LabFixture
    {
        Teleporter Pad(string name, Vector3 pos, Transform exit)
        {
            var go = new GameObject(name);
            Created.Add(go);
            go.transform.position = pos + Vector3.up;
            go.AddComponent<BoxCollider>().size = new Vector3(5f, 2f, 5f);
            var t = go.AddComponent<Teleporter>();
            t.exit = exit;
            return t;
        }

        [UnityTest]
        public IEnumerator PM09_TeleportWithoutReturnLoop()
        {
            // Worst case: A's exit lies on pad B, whose exit leads back to A.
            var exitA = new GameObject("ExitA").transform; Created.Add(exitA.gameObject);
            var exitB = new GameObject("ExitB").transform; Created.Add(exitB.gameObject);
            exitA.position = new Vector3(60f, 0.4f, 0f);
            exitB.position = new Vector3(0f, 0.4f, 0f);
            var a = Pad("PadA", new Vector3(0f, 0f, 10f), exitA);
            var b = Pad("PadB", new Vector3(60f, 0f, 0f), exitB);
            var v = Spawn("sandviper", new Vector3(0f, 0.3f, 0f));
            yield return Seconds(0.3f);
            v.Body.linearVelocity = Vector3.forward * 8f;
            yield return Seconds(3f);
            int uses = a.Uses + b.Uses;
            TestProtocol.Check("PM-09", "Pad A führt auf Pad B, B zurück zu A", "über Pad A fahren, 3 s warten", "genau ein Teleport",
                $"teleports={uses};x={v.transform.position.x:F0}", uses == 1 && v.transform.position.x > 40f);
        }

        [UnityTest]
        public IEnumerator PM10_PickupRespawns()
        {
            var go = new GameObject("Pickup_Nano");
            Created.Add(go);
            go.transform.position = new Vector3(0f, 0f, 12f);
            go.AddComponent<SphereCollider>();
            var p = go.AddComponent<PickupSpawner>();
            p.kind = PickupKind.NanoPatch;
            p.respawnSeconds = 2f;
            var v = Spawn("goliath_wheel", new Vector3(0f, 0.3f, 0f));
            yield return Seconds(0.3f);
            v.ReceiveDamage(DamageEvent.Create(null, v, DamageType.Missile, 60f, v.transform.position));
            float hurt = v.Hp;
            v.Body.linearVelocity = Vector3.forward * 10f;
            yield return Seconds(1.5f);
            bool taken = !p.Available && v.Hp > hurt;
            yield return Seconds(2.5f);
            TestProtocol.Check("PM-10", "Nano-Patch, Respawn 2 s (Spiel: 20 s)", "überfahren, 4 s warten", "geheilt, danach wieder verfügbar",
                $"aufgenommen={taken};verfügbar={p.Available};standard={PickupSpawner.DefaultRespawn}", taken && p.Available && PickupSpawner.DefaultRespawn == 20f);
        }

        [UnityTest]
        public IEnumerator PM11_LavaTicksOnlyInside()
        {
            var lava = new GameObject("Lava");
            Created.Add(lava);
            lava.transform.position = new Vector3(0f, 1f, 0f);
            lava.AddComponent<BoxCollider>().size = new Vector3(8f, 2f, 8f);
            lava.AddComponent<HazardZone>().kind = HazardKind.Lava;
            var inside = Spawn("mammoth_4x4", new Vector3(0f, 0.3f, 0f));
            var outside = Spawn("mammoth_4x4", new Vector3(12f, 0.3f, 0f));
            yield return Seconds(2f);
            bool ok = inside.Hp < inside.MaxHp && Mathf.Approximately(outside.Hp, outside.MaxHp);
            TestProtocol.Check("PM-11", "ein Fahrzeug in, eines neben der Lava", "2 s warten", "nur das innere verliert HP",
                $"innen={inside.Hp:F0}/{inside.MaxHp:F0};aussen={outside.Hp:F0}", ok);
        }

        [UnityTest]
        public IEnumerator PM12_AbyssEliminates()
        {
            var v = Spawn("phase_gt", new Vector3(500f, -5f, 0f));
            string cause = null;
            v.Died += (_, __) => cause = "eliminated";
            yield return Seconds(3f);
            TestProtocol.Check("PM-12", "Fahrzeug über dem Abgrund", "fallen lassen", "Lebensverlust gemeldet", cause ?? "lebt", !v.IsAlive && cause != null);
        }

        [UnityTest]
        public IEnumerator PM13_IcePlateWarnsThenBreaks()
        {
            Created[0].transform.position = new Vector3(0f, -0.5f, -250f); // no ground under the plate
            var ice = Box("Ice", new Vector3(0f, -0.15f, 0f), new Vector3(10f, 0.3f, 10f), Quaternion.identity);
            var plate = ice.AddComponent<IcePlate>();
            var v = Spawn("rustfang", new Vector3(0f, 0.3f, 0f));
            yield return Seconds(1.6f);
            bool standing = v.transform.position.y > -1f;
            yield return Seconds(2.5f);
            bool fell = v.transform.position.y < -3f;
            TestProtocol.Check("PM-13", "Fahrzeug auf Eisplatte über Abgrund", "stehen bleiben", "nach Vorwarnung bricht die Platte",
                $"nach1.6s_steht={standing};nach4.1s_gefallen={fell}", standing && fell);
        }

        [UnityTest]
        public IEnumerator PM14_TrainHitsOnceAndWreckIsHarmless()
        {
            var a = new GameObject("TrainA").transform; Created.Add(a.gameObject);
            var b = new GameObject("TrainB").transform; Created.Add(b.gameObject);
            a.position = new Vector3(-40f, 2.2f, 0f);
            b.position = new Vector3(40f, 2.2f, 0f);
            var train = Box("Train", a.position, new Vector3(18f, 4f, 3.6f), Quaternion.identity);
            train.AddComponent<Rigidbody>();
            var th = train.AddComponent<TrainHazard>();
            th.from = a; th.to = b;
            th.interval = 0.4f;
            th.warningSeconds = 0.3f;
            var victim = Spawn("mammoth_4x4", new Vector3(0f, 0.3f, 0f), 90f);
            yield return Seconds(3f);
            float hpAfterHit = victim.Hp;
            bool hitOnce = victim.MaxHp - hpAfterHit >= th.damage - 1f && victim.MaxHp - hpAfterHit < th.damage * 2f - 1f;

            // Second run: destroy the train while it is on its way; the parked target stays untouched.
            var second = Spawn("mammoth_4x4", new Vector3(-10f, 0.3f, 0f), 90f);
            while (th.IsDestroyed == false)
            {
                th.ReceiveDamage(DamageEvent.Create(null, null, DamageType.Missile, 999f, train.transform.position));
                yield return new WaitForFixedUpdate();
            }
            yield return Seconds(3f);
            bool wreckHarmless = Mathf.Approximately(second.Hp, second.MaxHp);
            TestProtocol.Check("PM-14", "Zug fährt über Gleis mit Fahrzeug", "Durchfahrt, dann Zug zerstören", "ein Treffer, zerstörter Zug trifft nicht mehr",
                $"schaden={victim.MaxHp - hpAfterHit:F0};zweites={second.Hp:F0}/{second.MaxHp:F0}", hitOnce && wreckHarmless);
        }

        [UnityTest]
        public IEnumerator PM15_C4SameDamageOnRampAndFlat()
        {
            var flat = Spawn("mammoth_4x4", new Vector3(0f, 0.3f, 0f));
            Box("Ramp", new Vector3(60f, 3f, 0f), new Vector3(14f, 1f, 30f), Quaternion.Euler(-15f, 0f, 0f));
            var onRamp = Spawn("mammoth_4x4", new Vector3(60f, 3.9f, 0f));
            yield return Seconds(1f);
            var c4 = GameDatabase.Instance.Weapon("c4_satchel");
            Explosion.Detonate(new ExplosionSpec { Position = flat.transform.position + flat.transform.forward * 4f + Vector3.up * 0.3f, Radius = c4.splashRadius, Damage = c4.splashDamage, Type = DamageType.Explosion, Cause = c4.id });
            Explosion.Detonate(new ExplosionSpec { Position = onRamp.transform.position + onRamp.transform.forward * 4f + onRamp.transform.up * 0.3f, Radius = c4.splashRadius, Damage = c4.splashDamage, Type = DamageType.Explosion, Cause = c4.id });
            yield return null;
            float d1 = flat.MaxHp - flat.Hp, d2 = onRamp.MaxHp - onRamp.Hp;
            bool ok = d1 > 0f && Mathf.Abs(d1 - d2) <= d1 * 0.15f;
            TestProtocol.Check("PM-15", "C4 4 m vor Fahrzeug, Ebene und 15°-Rampe", "zünden", "Schaden gleich (±15 %)", $"ebene={d1:F1};rampe={d2:F1}", ok);
        }
    }
}
