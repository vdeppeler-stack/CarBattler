using NUnit.Framework;

namespace IronSyndicate.Tests
{
    /// <summary>Each combo: complete, interrupted and too slow; prefix precedence for the rear variants.</summary>
    public class ComboBufferTests
    {
        const float Gap = ComboBuffer.DefaultMaxGap;
        const ComboDir U = ComboDir.Up, D = ComboDir.Down, L = ComboDir.Left, R = ComboDir.Right;

        static Maneuver Feed(ComboBuffer b, float step, bool primary, params ComboDir[] dirs)
        {
            var result = Maneuver.None;
            float t = 10f;
            foreach (var d in dirs)
            {
                var m = b.Push(d, t, primary);
                if (m != Maneuver.None) result = m;
                t += step;
            }
            var late = b.Tick(t + Gap * 2f);
            return late != Maneuver.None ? late : result;
        }

        [Test]
        public void EveryDefaultComboTriggersWhenEnteredInTime()
        {
            foreach (var c in ComboBuffer.Default)
            {
                var b = new ComboBuffer();
                Assert.AreEqual(c.Maneuver, Feed(b, 0.1f, c.RequiresPrimaryHeld, c.Sequence), c.Maneuver.ToString());
            }
        }

        [Test]
        public void TooSlowInputDoesNotTrigger()
        {
            foreach (var c in ComboBuffer.Default)
            {
                var b = new ComboBuffer();
                var m = Feed(b, Gap + 0.05f, c.RequiresPrimaryHeld, c.Sequence);
                Assert.AreNotEqual(c.Maneuver, m, c.Maneuver.ToString());
            }
        }

        [Test]
        public void InterruptedInputDoesNotTrigger()
        {
            var b = new ComboBuffer();
            // Aegis (L D R) broken by an extra Up in the middle.
            Assert.AreNotEqual(Maneuver.AegisBarrier, Feed(b, 0.1f, false, L, U, D, R));
        }

        [Test]
        public void RearCryoDoesNotAlsoFireRearSalvo()
        {
            var b = new ComboBuffer();
            float t = 0f;
            var fired = new System.Collections.Generic.List<Maneuver>();
            foreach (var d in new[] { U, D, D, D, R, U })
            {
                var m = b.Push(d, t += 0.1f, false);
                if (m != Maneuver.None) fired.Add(m);
            }
            var late = b.Tick(t + 1f);
            if (late != Maneuver.None) fired.Add(late);
            CollectionAssert.AreEqual(new[] { Maneuver.RearCryoPulse }, fired);
        }

        [Test]
        public void RearSalvoFiresOnTimeoutWhenNotExtended()
        {
            var b = new ComboBuffer();
            Assert.AreEqual(Maneuver.None, b.Push(U, 0.0f, false));
            Assert.AreEqual(Maneuver.None, b.Push(D, 0.1f, false));
            Assert.AreEqual(Maneuver.None, b.Push(D, 0.2f, false), "prefix must wait for a possible longer sequence");
            Assert.AreEqual(Maneuver.None, b.Tick(0.4f));
            Assert.AreEqual(Maneuver.RearSalvo, b.Tick(0.6f));
        }

        [Test]
        public void RearSalvoFiresWhenNextPressRulesOutLongerSequence()
        {
            var b = new ComboBuffer();
            b.Push(U, 0.0f, false);
            b.Push(D, 0.1f, false);
            b.Push(D, 0.2f, false);
            Assert.AreEqual(Maneuver.RearSalvo, b.Push(L, 0.3f, false));
        }

        [Test]
        public void CerberusNeedsPrimaryHeld()
        {
            Assert.AreNotEqual(Maneuver.CerberusBarrage, Feed(new ComboBuffer(), 0.1f, false, R, R, L, L));
            Assert.AreEqual(Maneuver.CerberusBarrage, Feed(new ComboBuffer(), 0.1f, true, R, R, L, L));
        }

        [Test]
        public void NoTwoCombosShareTheSameSequence()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var c in ComboBuffer.Default)
                Assert.IsTrue(seen.Add(string.Join(",", c.Sequence) + c.RequiresPrimaryHeld), c.Maneuver.ToString());
        }
    }
}
