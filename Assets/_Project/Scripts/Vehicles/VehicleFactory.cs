using System.Collections.Generic;
using UnityEngine;

namespace IronSyndicate
{
    /// <summary>
    /// The single construction path for vehicles (match, lab, debug spawns). Builds a placeholder body
    /// from primitives unless the definition supplies a body prefab: front marker, muzzle, team stripe
    /// and the collision box stay readable either way.
    /// </summary>
    public static class VehicleFactory
    {
        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        public static Vehicle Spawn(VehicleDefinition def, Vector3 position, Quaternion rotation, int seat, bool human, Color teamColor, int botSeed)
        {
            var root = new GameObject($"{def.displayName} [{seat}]");
            root.layer = LayerMask.NameToLayer("Vehicle");
            root.transform.SetPositionAndRotation(position, rotation);

            var size = def.bodySize;
            float r = def.wheelRadius;
            float rideHeight = r + 0.15f;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = def.mass;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, rideHeight + size.y * 0.5f, 0f);
            col.size = size;
            col.sharedMaterial = LowFriction;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            if (def.bodyPrefab != null)
            {
                var body = Object.Instantiate(def.bodyPrefab, visual);
                body.transform.localPosition = new Vector3(0f, rideHeight, 0f);
            }
            else BuildPlaceholder(def, visual, rideHeight, teamColor);

            // Front marker: bright wedge on the nose so heading reads from any camera.
            Box(visual, "FrontMarker", new Vector3(0f, rideHeight + size.y + 0.05f, size.z * 0.42f), new Vector3(size.x * 0.5f, 0.12f, size.z * 0.12f), new Color(1f, 0.85f, 0.1f));
            // Team stripe.
            Box(visual, "TeamStripe", new Vector3(0f, rideHeight + size.y + 0.02f, -size.z * 0.1f), new Vector3(size.x * 0.25f, 0.06f, size.z * 0.6f), teamColor);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0f, rideHeight + size.y * 0.6f, size.z * 0.5f + 0.7f);
            Box(visual, "MuzzleMark", new Vector3(0f, rideHeight + size.y * 0.6f, size.z * 0.5f + 0.1f), new Vector3(0.25f, 0.25f, 0.4f), Color.black);

            var rear = new GameObject("RearMuzzle").transform;
            rear.SetParent(root.transform, false);
            rear.localPosition = new Vector3(0f, rideHeight + size.y * 0.4f, -size.z * 0.5f - 0.9f);
            rear.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var motor = root.AddComponent<VehicleMotor>();
            BuildWheels(def, root.transform, visual, motor, rideHeight);
            root.AddComponent<VehicleStatus>();
            root.AddComponent<VehicleArsenal>();
            SpecialRegistry.Attach(root, def.specialId);

            var vehicle = root.AddComponent<Vehicle>();
            vehicle.Muzzle = muzzle;
            vehicle.RearMuzzle = rear;
            vehicle.Visual = visual;

            if (human)
            {
                vehicle.Input = root.AddComponent<PlayerSeatInput>();
            }
            else
            {
                var bot = root.AddComponent<BotBrain>();
                vehicle.Input = bot;
                vehicle.Init(def, seat, false, teamColor);
                bot.Init(vehicle, botSeed);
                return vehicle;
            }
            vehicle.Init(def, seat, human, teamColor);
            return vehicle;
        }

        static PhysicsMaterial _lowFriction;
        static PhysicsMaterial LowFriction
        {
            get
            {
                if (_lowFriction == null)
                {
                    _lowFriction = new PhysicsMaterial("VehicleBody")
                    {
                        dynamicFriction = 0.1f,
                        staticFriction = 0.1f,
                        bounciness = 0.1f,
                        frictionCombine = PhysicsMaterialCombine.Minimum
                    };
                }
                return _lowFriction;
            }
        }

        static void BuildWheels(VehicleDefinition def, Transform root, Transform visual, VehicleMotor motor, float rideHeight)
        {
            var size = def.bodySize;
            float r = def.wheelRadius;
            var points = new List<Vector3>();
            float x = size.x * 0.5f - r * 0.35f;
            float z = size.z * 0.5f - r * 1.1f;
            switch (def.bodyStyle)
            {
                case BodyStyle.Wheel:
                    // Two giant side wheels; two hidden stabiliser points keep the chassis level.
                    points.Add(new Vector3(-x, 0f, 0.6f)); points.Add(new Vector3(x, 0f, 0.6f));
                    points.Add(new Vector3(-x, 0f, -0.6f)); points.Add(new Vector3(x, 0f, -0.6f));
                    break;
                case BodyStyle.Bike:
                    points.Add(new Vector3(-0.35f, 0f, z)); points.Add(new Vector3(0.35f, 0f, z));
                    points.Add(new Vector3(-0.35f, 0f, -z)); points.Add(new Vector3(0.35f, 0f, -z));
                    break;
                case BodyStyle.Apc:
                case BodyStyle.BossRig:
                    points.Add(new Vector3(-x, 0f, z)); points.Add(new Vector3(x, 0f, z));
                    points.Add(new Vector3(-x, 0f, 0f)); points.Add(new Vector3(x, 0f, 0f));
                    points.Add(new Vector3(-x, 0f, -z)); points.Add(new Vector3(x, 0f, -z));
                    break;
                default:
                    points.Add(new Vector3(-x, 0f, z)); points.Add(new Vector3(x, 0f, z));
                    points.Add(new Vector3(-x, 0f, -z)); points.Add(new Vector3(x, 0f, -z));
                    break;
            }

            var wheelPoints = new Transform[points.Count];
            var wheelVisuals = new Transform[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                var p = new GameObject("WheelPoint" + i).transform;
                p.SetParent(root, false);
                p.localPosition = points[i] + Vector3.up * (rideHeight + 0.05f);
                wheelPoints[i] = p;

                bool show = def.bodyStyle != BodyStyle.Wheel && !(def.bodyStyle == BodyStyle.Bike && points[i].x > 0f);
                if (!show) continue;
                var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(w.GetComponent<Collider>());
                w.name = "Wheel" + i;
                w.transform.SetParent(visual, false);
                float width = def.bodyStyle == BodyStyle.Bike ? 0.25f : Mathf.Max(0.3f, r * 0.7f);
                w.transform.localScale = new Vector3(r * 2f, width * 0.5f, r * 2f);
                w.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.12f, 0.12f, 0.12f));
                var holder = new GameObject("WheelHolder" + i).transform;
                holder.SetParent(visual, false);
                holder.localPosition = p.localPosition;
                w.transform.SetParent(holder, false);
                w.transform.localPosition = Vector3.zero;
                wheelVisuals[i] = w.transform;
            }

            if (def.bodyStyle == BodyStyle.Wheel)
            {
                // Giant side wheels as static visuals.
                for (int s = -1; s <= 1; s += 2)
                {
                    var w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.Destroy(w.GetComponent<Collider>());
                    w.name = "GiantWheel";
                    w.transform.SetParent(visual, false);
                    w.transform.localPosition = new Vector3(s * (size.x * 0.5f + 0.2f), rideHeight + size.y * 0.5f, 0f);
                    w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    w.transform.localScale = new Vector3(size.y + rideHeight * 1.6f, 0.25f, size.y + rideHeight * 1.6f);
                    w.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.12f, 0.12f, 0.12f));
                }
            }

            motor.wheelPoints = wheelPoints;
            motor.wheelVisuals = wheelVisuals;
            motor.suspensionLength = 0.55f;
        }

        static void BuildPlaceholder(VehicleDefinition def, Transform visual, float rideHeight, Color team)
        {
            var s = def.bodySize;
            var body = def.bodyColor;
            var dark = Color.Lerp(body, Color.black, 0.45f);
            float baseY = rideHeight;

            switch (def.bodyStyle)
            {
                case BodyStyle.Wheel:
                    Box(visual, "Hub", new Vector3(0f, baseY + s.y * 0.5f, 0f), new Vector3(s.x * 0.8f, s.y, s.z * 0.6f), body);
                    Box(visual, "Cockpit", new Vector3(0f, baseY + s.y * 0.85f, 0.4f), new Vector3(s.x * 0.4f, s.y * 0.4f, 0.8f), dark);
                    break;
                case BodyStyle.Bike:
                    Box(visual, "Frame", new Vector3(0f, baseY + s.y * 0.4f, 0f), new Vector3(s.x, s.y * 0.6f, s.z * 0.8f), body);
                    Box(visual, "Rider", new Vector3(0f, baseY + s.y * 0.95f, -0.2f), new Vector3(s.x * 0.8f, s.y * 0.6f, 0.6f), dark);
                    break;
                case BodyStyle.Formula:
                    Box(visual, "Hull", new Vector3(0f, baseY + s.y * 0.35f, 0f), new Vector3(s.x * 0.45f, s.y * 0.6f, s.z), body);
                    Box(visual, "FrontWing", new Vector3(0f, baseY + 0.1f, s.z * 0.45f), new Vector3(s.x, 0.1f, 0.5f), dark);
                    Box(visual, "RearWing", new Vector3(0f, baseY + s.y * 0.9f, -s.z * 0.45f), new Vector3(s.x * 0.9f, 0.1f, 0.5f), dark);
                    break;
                case BodyStyle.Loader:
                    Box(visual, "Body", new Vector3(0f, baseY + s.y * 0.45f, -0.3f), new Vector3(s.x, s.y * 0.9f, s.z * 0.75f), body);
                    Box(visual, "Cab", new Vector3(0f, baseY + s.y * 1.05f, -0.6f), new Vector3(s.x * 0.6f, s.y * 0.5f, s.z * 0.3f), dark);
                    Box(visual, "Shovel", new Vector3(0f, baseY + s.y * 0.3f, s.z * 0.45f), new Vector3(s.x * 1.1f, s.y * 0.6f, 0.4f), new Color(0.75f, 0.6f, 0.15f));
                    break;
                case BodyStyle.MonsterTruck:
                case BodyStyle.Humvee:
                case BodyStyle.Apc:
                case BodyStyle.BossRig:
                case BodyStyle.ShowTruck:
                    Box(visual, "Body", new Vector3(0f, baseY + s.y * 0.5f, 0f), s, body);
                    Box(visual, "Cab", new Vector3(0f, baseY + s.y * 1.1f, s.z * 0.2f), new Vector3(s.x * 0.85f, s.y * 0.35f, s.z * 0.35f), dark);
                    break;
                default:
                    Box(visual, "Body", new Vector3(0f, baseY + s.y * 0.35f, 0f), new Vector3(s.x, s.y * 0.7f, s.z), body);
                    Box(visual, "Cabin", new Vector3(0f, baseY + s.y * 0.85f, -s.z * 0.08f), new Vector3(s.x * 0.85f, s.y * 0.4f, s.z * 0.45f), dark);
                    break;
            }
        }

        static void Box(Transform parent, string name, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
        }

        public static Material Mat(Color c)
        {
            if (Materials.TryGetValue(c, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader);
            m.SetColor("_BaseColor", c);
            m.color = c;
            Materials[c] = m;
            return m;
        }
    }
}
