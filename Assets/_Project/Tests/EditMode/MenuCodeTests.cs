using NUnit.Framework;

namespace IronSyndicate.Tests
{
    /// <summary>Each code once correct, once shortened, once with swapped direction.</summary>
    public class MenuCodeTests
    {
        static string Enter(params MenuKey[] keys)
        {
            var codes = new MenuCodes(MenuCodes.VehicleSelect);
            string result = null;
            float t = 0f;
            foreach (var k in keys) result = codes.Push(k, t += 0.2f) ?? result;
            return result;
        }

        [Test]
        public void CorrectCodesUnlock()
        {
            foreach (var c in MenuCodes.VehicleSelect)
                Assert.AreEqual(c.Id, Enter(c.Keys), c.Id);
        }

        [Test]
        public void ShortenedCodesDoNotUnlock()
        {
            foreach (var c in MenuCodes.VehicleSelect)
            {
                var shortened = new MenuKey[c.Keys.Length - 1];
                System.Array.Copy(c.Keys, shortened, shortened.Length);
                Assert.IsNull(Enter(shortened), c.Id);
            }
        }

        [Test]
        public void SwappedDirectionDoesNotUnlock()
        {
            foreach (var c in MenuCodes.VehicleSelect)
            {
                var swapped = (MenuKey[])c.Keys.Clone();
                for (int i = 0; i < swapped.Length; i++)
                {
                    if (swapped[i] == MenuKey.Left) { swapped[i] = MenuKey.Right; break; }
                    if (swapped[i] == MenuKey.Right) { swapped[i] = MenuKey.Left; break; }
                    if (swapped[i] == MenuKey.Up) { swapped[i] = MenuKey.Down; break; }
                    if (swapped[i] == MenuKey.Down) { swapped[i] = MenuKey.Up; break; }
                }
                Assert.IsNull(Enter(swapped), c.Id);
            }
        }

        [Test]
        public void SlowEntryDoesNotUnlock()
        {
            var c = MenuCodes.VehicleSelect[0];
            var codes = new MenuCodes(MenuCodes.VehicleSelect);
            string result = null;
            float t = 0f;
            foreach (var k in c.Keys) result = codes.Push(k, t += 1.5f) ?? result;
            Assert.IsNull(result);
        }
    }
}
