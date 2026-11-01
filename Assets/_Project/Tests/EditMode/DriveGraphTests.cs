using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace IronSyndicate.Tests
{
    /// <summary>Drive graph: shortest route over the bridge while intact, detour after it breaks, depot only after the door.</summary>
    public class DriveGraphTests
    {
        GameObject _root;
        DriveGraph _g;
        Destructible _bridge, _door;

        [SetUp]
        public void Build()
        {
            _root = new GameObject("GraphTest");
            _g = _root.AddComponent<DriveGraph>();
            _bridge = new GameObject("Bridge").AddComponent<Destructible>();
            _door = new GameObject("Door").AddComponent<Destructible>();
            _bridge.transform.SetParent(_root.transform);
            _door.transform.SetParent(_root.transform);
            // 0 north - 1 south across the bridge (short); detour 0-2-3-1 (long); 1-4 depot behind the door.
            Node(0, 0, 20); Node(1, 0, -20); Node(2, 80, 20); Node(3, 80, -20); Node(4, 0, -40);
            Road(0, 1, intact: _bridge);
            Road(0, 2); Road(2, 3); Road(3, 1);
            Road(1, 4, destroyed: _door);
        }

        [TearDown]
        public void Clean() => Object.DestroyImmediate(_root);

        void Node(int i, float x, float z) { _g.nodes.Add(new Vector3(x, 0f, z)); _g.names.Add("n" + i); }

        void Road(int a, int b, Destructible intact = null, Destructible destroyed = null)
        {
            _g.edges.Add(new DriveEdge { from = a, to = b, requiresIntact = intact, requiresDestroyed = destroyed });
            _g.edges.Add(new DriveEdge { from = b, to = a, requiresIntact = intact, requiresDestroyed = destroyed });
        }

        [Test]
        public void UsesBridgeWhileIntactAndDetoursAfterBreak()
        {
            var path = new List<DriveEdge>();
            Assert.IsTrue(_g.FindPath(0, 1, path));
            Assert.AreEqual(1, path.Count, "direct over the bridge");
            int version = Destructible.Version;
            _bridge.Break(null, "test");
            Assert.Greater(Destructible.Version, version, "bots see the arena change");
            Assert.IsTrue(_g.FindPath(0, 1, path));
            Assert.AreEqual(3, path.Count, "detour 0-2-3-1");
        }

        [Test]
        public void DepotOnlyReachableAfterDoorBreaks()
        {
            var path = new List<DriveEdge>();
            Assert.IsFalse(_g.FindPath(0, 4, path));
            _door.Break(null, "test");
            Assert.IsTrue(_g.FindPath(0, 4, path));
        }

        [Test]
        public void NearestIgnoresNodesHighAbove()
        {
            _g.nodes.Add(new Vector3(0f, 8f, 0f));
            _g.names.Add("deck");
            _g.nodes.Add(new Vector3(6f, -8f, 0f));
            _g.names.Add("trench");
            Assert.AreEqual("trench", _g.NameOf(_g.Nearest(new Vector3(0f, -8f, 0f))));
        }
    }
}
