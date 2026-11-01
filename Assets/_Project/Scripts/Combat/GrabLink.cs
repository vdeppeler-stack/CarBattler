using System.Collections.Generic;

namespace IronSyndicate
{
    /// <summary>
    /// Registry of active grabs/tethers. A death on either end releases the link in the same step,
    /// so no sequence can keep holding a vehicle.
    /// </summary>
    public static class GrabLink
    {
        public interface IHolder
        {
            Vehicle Holder { get; }
            Vehicle Held { get; }
            void Release(string reason);
        }

        static readonly List<IHolder> Links = new List<IHolder>();

        public static void Register(IHolder link) => Links.Add(link);
        public static void Unregister(IHolder link) => Links.Remove(link);

        public static bool IsHeld(Vehicle v)
        {
            foreach (var l in Links)
                if (l.Held == v) return true;
            return false;
        }

        public static void ReleaseAll(Vehicle v)
        {
            for (int i = Links.Count - 1; i >= 0; i--)
            {
                if (i >= Links.Count) continue;
                var l = Links[i];
                if (l.Holder == v || l.Held == v) l.Release("death");
            }
        }

        public static void Clear() => Links.Clear();
    }
}
