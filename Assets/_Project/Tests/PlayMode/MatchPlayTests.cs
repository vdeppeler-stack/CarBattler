using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IronSyndicate.Tests
{
    /// <summary>Round restart, boss phases, boss spawner.</summary>
    public class MatchPlayTests : LabFixture
    {
        [UnityTest]
        public IEnumerator PM16_BossPhaseTwoExactlyOnce()
        {
            var mm = Match(false);
            yield return null;
            var db = GameDatabase.Instance;
            var boss = mm.SpawnExtra(db.Vehicle("dread_jester"), new Vector3(0f, 0.5f, 0f), Quaternion.identity, false);
            var other = mm.SpawnExtra(db.Vehicle("sandviper"), new Vector3(40f, 0.5f, 0f), Quaternion.identity, false);
            Object.Destroy(boss.GetComponent<BotBrain>()); boss.Input = null;
            Object.Destroy(other.GetComponent<BotBrain>()); other.Input = null;
            yield return Seconds(0.5f);
            boss.Kill(DamageType.Special, "test");
            yield return Seconds(0.3f);
            var carousels = mm.Participants.Where(p => p.Definition.id == "hellfire_carousel").ToList();
            bool phase2 = carousels.Count == 1 && carousels[0].IsAlive && mm.State == MatchManager.Phase.Running;
            var carousel = carousels.FirstOrDefault();
            if (carousel != null) carousel.Kill(DamageType.Special, "test");
            yield return Seconds(0.3f);
            int total = mm.Participants.Count(p => p.Definition.id == "hellfire_carousel");
            bool ended = mm.State == MatchManager.Phase.Ended && mm.Winner == other;
            TestProtocol.Check("PM-16", "Dread Jester gegen ein Fahrzeug", "Truck zerstören, dann Karussell zerstören",
                "genau eine Phase 2, Kampf endet erst mit dem Karussell", $"phase2={phase2};karussells={total};ende={ended}", phase2 && total == 1 && ended);
        }

        [UnityTest]
        public IEnumerator PM17_BossSpawnerStartsFightExactlyOnce()
        {
            var mm = Match(true);
            var spawner = new GameObject("Boss_Spawner").AddComponent<BossSpawner>();
            Created.Add(spawner.gameObject);
            spawner.transform.position = new Vector3(0f, 0.5f, 30f);
            spawner.killsRequired = 8;
            yield return null;
            for (int i = 0; i < 7; i++) spawner.SimulateKill();
            bool noneAfter7 = !spawner.Spawned;
            spawner.SimulateKill();
            bool oneAfter8 = spawner.Spawned && spawner.Boss != null;
            spawner.SimulateKill();
            yield return null;
            int bosses = Vehicle.All.Count(v => v.Definition.id == "dread_jester");
            TestProtocol.Check("PM-17", "Boss-Spawner, Schwelle 8 Abschüsse", "7, 8 und 9 simulierte Abschüsse", "erst bei 8 genau ein Boss",
                $"nach7={!noneAfter7};nach8={oneAfter8};bosse={bosses}", noneAfter7 && oneAfter8 && bosses == 1);
        }

        [UnityTest]
        public IEnumerator PM18_RestartResetsRound()
        {
            MatchSetup.Configured = true;
            MatchSetup.HumanPlaying = false;
            MatchSetup.BotCount = 3;
            MatchSetup.BotVehicleIds.Clear();
            MatchSetup.Seed = 777;
            MatchSetup.RandomizeSeedOnStart = false;
            SceneManager.LoadScene("Proto_Arena");
            yield return null;
            yield return Seconds(4f);
            var mm = MatchManager.Instance;
            var firstLineup = string.Join(",", mm.Participants.Select(p => p.Definition.id));
            var victim = mm.Participants[0];
            victim.ReceiveDamage(DamageEvent.Create(null, victim, DamageType.Missile, 50f, victim.transform.position));
            victim.Status.Ignite(null, 10f);
            mm.Restart();
            yield return null;
            yield return Seconds(0.5f);
            mm = MatchManager.Instance;
            var lineup = string.Join(",", mm.Participants.Select(p => p.Definition.id));
            bool fresh = mm.Participants.All(p => Mathf.Approximately(p.Hp, p.MaxHp) && !p.Status.IsBurning && p.Arsenal.TotalAmmo == 0);
            bool ok = mm.Seed == 777 && lineup == firstLineup && fresh && Projectile.Active.Count == 0;
            TestProtocol.Check("PM-18", "Runde mit Seed 777 läuft, Fahrzeug beschädigt und brennend", "Wiederholen", "gleicher Seed und Kader, alles zurückgesetzt",
                $"seed={mm.Seed};kader_gleich={lineup == firstLineup};frisch={fresh};projektile={Projectile.Active.Count}", ok);
            // Later tests build their own ground; drop the arena geometry.
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects()) Object.Destroy(go);
        }
    }
}
