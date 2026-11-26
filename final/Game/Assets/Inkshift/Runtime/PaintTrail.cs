using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Inkshift
{
    [DefaultExecutionOrder(110)]
    public sealed class PaintTrail : MonoBehaviour
    {
        [SerializeField] ShapeMotor player;
        [FormerlySerializedAs("sphereInk")]
        [SerializeField] Material inkMaterial;
        [SerializeField] Shader flowShader;

        const float ActiveInterval = 1f / 25f;
        const float FadingInterval = 1f / 7f;
        const float ActiveTime = 1.6f;
        // Paint Flow removes 0.06 thickness per second from a field capped at 1.45.
        const float EmptyAfter = 27f;
        const float WallReach = 0.54f;
        const int TexelsPerMetre = 24;
        const int MaxFieldSize = 320;
        const int MaxSurfaces = 32;
        const int TexelBudget = 1400000;

        static readonly int InkState = Shader.PropertyToID("_InkState");
        static readonly int InkTexel = Shader.PropertyToID("_InkState_TexelSize");
        static readonly int SurfaceParams = Shader.PropertyToID("_Surface");
        static readonly int SegmentParams = Shader.PropertyToID("_Segment");
        static readonly int DepositParams = Shader.PropertyToID("_Deposit");
        static readonly int RippleParams = Shader.PropertyToID("_Ripple");

        readonly Dictionary<FaceKey, Surface> surfaces = new();
        readonly List<FaceKey> expired = new();
        readonly Collider[] wallCandidates = new Collider[48];
        MaterialPropertyBlock properties;
        Material solver;
        Rigidbody body;
        Surface lastGround;
        Surface lastWall;
        BoxCollider lastGroundContact;
        BoxCollider lastWallContact;
        int lastWallFace;
        Vector3 lastGroundPoint;
        Vector3 lastWallPoint;
        Vector2 lastGroundUV;
        Vector2 lastWallUV;
        bool lastBall;
        bool lastWallBall;
        float pendingImpact;
        int allocatedTexels;

        // Face 0 is the top; 1..4 are the +X, -X, +Z and -Z faces.
        readonly struct FaceKey : IEquatable<FaceKey>
        {
            readonly int colliderId;
            readonly int face;

            public FaceKey(BoxCollider collider, int face)
            {
                colliderId = collider.GetInstanceID();
                this.face = face;
            }

            public bool Equals(FaceKey other) => colliderId == other.colliderId && face == other.face;
            public override bool Equals(object obj) => obj is FaceKey other && Equals(other);
            public override int GetHashCode() => unchecked(colliderId * 397 ^ face);
        }

        sealed class Surface
        {
            public BoxCollider Collider;
            public Renderer VisibilitySource;
            public RenderTexture Read;
            public RenderTexture Write;
            public Mesh Mesh;
            public MeshRenderer Renderer;
            public Vector4 Parameters;
            public Vector3 Center;
            public Quaternion Rotation;
            public Vector2 ImpactUV;
            public float LastStep;
            public float LastDeposit;
            public float ImpactTime;
            public float Impulse;
            public float RemainingBound;
            public int Texels;
        }

        void Awake()
        {
            solver = new Material(flowShader);
            body = player.GetComponent<Rigidbody>();
            properties = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            player.Feedback += QueueImpact;
            player.Relocated += BreakTrail;
        }

        void OnDisable()
        {
            player.Feedback -= QueueImpact;
            player.Relocated -= BreakTrail;
        }

        void QueueImpact(SoundCue cue)
        {
            if (cue == SoundCue.Land)
                pendingImpact = Mathf.Lerp(0.66f, 1f, Mathf.InverseLerp(2.6f, 14f, -player.Velocity.y));
            else if (cue == SoundCue.Flip)
                pendingImpact = Mathf.Max(pendingImpact, 0.24f);
        }

        void BreakTrail()
        {
            lastGround = null;
            lastWall = null;
            lastGroundContact = null;
            lastWallContact = null;
            pendingImpact = 0f;
        }

        void FixedUpdate()
        {
            float now = Time.fixedTime;
            DepositGround(now);
            DepositWall(now);

            expired.Clear();
            foreach (KeyValuePair<FaceKey, Surface> entry in surfaces)
            {
                Surface surface = entry.Value;
                if (!surface.Collider || now - surface.LastDeposit >= EmptyAfter ||
                    (now - surface.LastDeposit > 0.2f && surface.RemainingBound < 0.006f))
                {
                    expired.Add(entry.Key);
                    continue;
                }

                float interval = now - surface.LastDeposit < ActiveTime ? ActiveInterval : FadingInterval;
                if (now - surface.LastStep >= interval)
                    Step(surface, now, Vector4.zero, Vector4.zero);
            }

            foreach (FaceKey key in expired)
            {
                Surface surface = surfaces[key];
                if (surface == lastGround)
                    lastGround = null;
                if (surface == lastWall)
                    lastWall = null;
                Release(surface);
                surfaces.Remove(key);
            }
        }

        void LateUpdate()
        {
            foreach (Surface surface in surfaces.Values)
                if (surface.VisibilitySource)
                    surface.Renderer.enabled = surface.VisibilitySource.enabled;
        }

        void DepositGround(float now)
        {
            if (!player.Grounded || player.GroundSurface is not BoxCollider deck ||
                Vector3.Dot(player.GroundNormal, deck.transform.up) < 0.76f)
            {
                lastGround = null;
                lastGroundContact = null;
                pendingImpact = 0f;
                return;
            }

            // Once a stationary patch has evaporated, wait for movement or a new impact.
            if (lastGround == null && lastGroundContact == deck && lastBall == player.IsBall &&
                pendingImpact <= 0f && (player.GroundPoint - lastGroundPoint).sqrMagnitude < 0.024f * 0.024f)
                return;

            Surface surface = GetSurface(deck, 0, now);
            if (surface == null)
            {
                lastGround = null;
                pendingImpact = 0f;
                return;
            }

            Vector2 uv = SurfaceUV(surface, player.GroundPoint);
            bool ball = player.IsBall;
            bool resumed = lastGround == null;
            bool shifted = lastGround != null && ball != lastBall;
            Vector2 start = lastGround == surface && !shifted ? lastGroundUV : uv;
            float distance = Distance(surface, start, uv);
            float impact = Mathf.Max(pendingImpact, resumed ? 0.58f : shifted ? 0.40f : 0f);
            if (distance < 0.024f && impact <= 0f && surface == lastGround)
                return;

            float radius = (ball ? 0.47f : 0.63f) + impact * 0.19f;
            float mass = Mathf.Min(0.65f, distance * 1.13f + 0.11f) + impact * 0.43f;
            Deposit(surface, now, start, uv, radius, mass, ball, impact);
            lastGround = surface;
            lastGroundUV = uv;
            lastGroundContact = deck;
            lastGroundPoint = player.GroundPoint;
            lastBall = ball;
            pendingImpact = 0f;
        }

        void DepositWall(float now)
        {
            Vector3 center = body.position;
            int count = Physics.OverlapSphereNonAlloc(center, WallReach, wallCandidates, ~0,
                QueryTriggerInteraction.Ignore);
            BoxCollider nearest = null;
            Vector3 nearestPoint = default;
            int nearestFace = 0;
            float nearestDistance = WallReach;
            for (int i = 0; i < count; i++)
            {
                if (wallCandidates[i] is not BoxCollider box || !box.enabled)
                    continue;
                Vector3 point = box.ClosestPoint(center);
                Vector3 offset = center - point;
                float distance = offset.magnitude;
                if (distance < 0.001f || distance > nearestDistance)
                    continue;
                Vector3 normal = offset / distance;
                if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.45f)
                    continue;
                Vector3 local = box.transform.InverseTransformDirection(normal);
                if (Mathf.Abs(local.y) > Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)))
                    continue;
                int face = Mathf.Abs(local.x) > Mathf.Abs(local.z) ? (local.x > 0f ? 1 : 2) : (local.z > 0f ? 3 : 4);
                nearest = box;
                nearestPoint = point;
                nearestFace = face;
                nearestDistance = distance;
            }

            if (!nearest)
            {
                lastWall = null;
                lastWallContact = null;
                return;
            }

            if (lastWall == null && lastWallContact == nearest && lastWallFace == nearestFace &&
                lastWallBall == player.IsBall && (nearestPoint - lastWallPoint).sqrMagnitude < 0.025f * 0.025f)
                return;

            Surface surface = GetSurface(nearest, nearestFace, now);
            if (surface == null)
            {
                lastWall = null;
                return;
            }

            Vector2 uv = SurfaceUV(surface, nearestPoint);
            bool ball = player.IsBall;
            bool resumed = lastWall != surface || lastWallBall != ball;
            Vector2 start = resumed ? uv : lastWallUV;
            float distanceAlongWall = Distance(surface, start, uv);
            if (!resumed && distanceAlongWall < 0.025f)
                return;

            float radius = ball ? 0.34f : 0.40f;
            float mass = Mathf.Min(0.5f, distanceAlongWall * 1.05f + (resumed ? 0.42f : 0.09f));
            Deposit(surface, now, start, uv, radius, mass, ball, resumed ? 0.24f : 0f);
            lastWall = surface;
            lastWallUV = uv;
            lastWallContact = nearest;
            lastWallFace = nearestFace;
            lastWallPoint = nearestPoint;
            lastWallBall = ball;
        }

        static Vector2 SurfaceUV(Surface surface, Vector3 point)
        {
            Vector3 local = Quaternion.Inverse(surface.Rotation) * (point - surface.Center);
            return new Vector2(Mathf.Clamp01(local.x / surface.Parameters.x + 0.5f),
                Mathf.Clamp01(local.z / surface.Parameters.y + 0.5f));
        }

        static float Distance(Surface surface, Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            return new Vector2(delta.x * surface.Parameters.x, delta.y * surface.Parameters.y).magnitude;
        }

        void Deposit(Surface surface, float now, Vector2 start, Vector2 end, float radius, float mass,
            bool ball, float impact)
        {
            if (impact >= 0.4f)
            {
                surface.ImpactUV = end;
                surface.ImpactTime = now;
                surface.Impulse = impact * 1.35f;
            }
            surface.LastDeposit = now;
            Step(surface, now, new Vector4(start.x, start.y, end.x, end.y),
                new Vector4(radius, mass, ball ? 0f : 1f, impact));
        }

        void Step(Surface surface, float now, Vector4 segment, Vector4 deposit)
        {
            float dt = Mathf.Clamp(now - surface.LastStep, Time.fixedDeltaTime, 0.25f);
            float push = surface.Impulse * Mathf.Clamp01(1f - (now - surface.ImpactTime) / 0.36f);
            solver.SetVector(SurfaceParams, surface.Parameters);
            solver.SetVector(SegmentParams, segment);
            solver.SetVector(DepositParams, deposit);
            solver.SetVector(RippleParams, new Vector4(surface.ImpactUV.x, surface.ImpactUV.y, push, dt));
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(surface.Read, surface.Write, solver, 0);
            RenderTexture.active = previous;
            (surface.Read, surface.Write) = (surface.Write, surface.Read);
            properties.Clear();
            properties.SetTexture(InkState, surface.Read);
            properties.SetVector(InkTexel, new Vector4(1f / surface.Read.width, 1f / surface.Read.height,
                surface.Read.width, surface.Read.height));
            surface.Renderer.SetPropertyBlock(properties);
            // A conservative upper bound avoids GPU readbacks when an entire field is empty.
            // It falls slower than the shader loss to account for half-float rounding.
            surface.RemainingBound = Mathf.Min(1.45f,
                Mathf.Max(0f, surface.RemainingBound - dt * 0.03f) + deposit.y);
            surface.LastStep = now;
        }

        Surface GetSurface(BoxCollider box, int face, float now)
        {
            var key = new FaceKey(box, face);
            if (surfaces.TryGetValue(key, out Surface existing))
                return existing;
            if (surfaces.Count >= MaxSurfaces || TexelBudget - allocatedTexels < 24 * 24)
                return null;

            Transform support = box.transform;
            Vector3 center = box.center;
            Vector3 normal;
            Vector3 along;
            float width;
            float length;
            float depth = 0f;
            switch (face)
            {
                case 0:
                    center.y += box.size.y * 0.5f;
                    normal = support.up;
                    along = support.forward;
                    width = support.TransformVector(Vector3.right * box.size.x).magnitude;
                    length = support.TransformVector(Vector3.forward * box.size.z).magnitude;
                    depth = support.TransformVector(Vector3.up * box.size.y).magnitude;
                    break;
                case 1:
                case 2:
                    center.x += box.size.x * (face == 1 ? 0.5f : -0.5f);
                    normal = support.right * (face == 1 ? 1f : -1f);
                    along = support.up;
                    width = support.TransformVector(Vector3.forward * box.size.z).magnitude;
                    length = support.TransformVector(Vector3.up * box.size.y).magnitude;
                    break;
                default:
                    center.z += box.size.z * (face == 3 ? 0.5f : -0.5f);
                    normal = support.forward * (face == 3 ? 1f : -1f);
                    along = support.up;
                    width = support.TransformVector(Vector3.right * box.size.x).magnitude;
                    length = support.TransformVector(Vector3.up * box.size.y).magnitude;
                    break;
            }

            if (width < 0.01f || length < 0.01f)
                return null;
            int x = Mathf.Clamp(Mathf.CeilToInt(width * TexelsPerMetre), 24, MaxFieldSize);
            int z = Mathf.Clamp(Mathf.CeilToInt(length * TexelsPerMetre), 24, MaxFieldSize);
            int available = TexelBudget - allocatedTexels;
            while (x * z > available && (x > 24 || z > 24))
            {
                x = Mathf.Max(24, x * 3 / 4);
                z = Mathf.Max(24, z * 3 / 4);
            }
            if (x * z > available)
                return null;

            Quaternion rotation = Quaternion.LookRotation(along, normal);
            Vector3 worldCenter = support.TransformPoint(center);
            var overlay = new GameObject("Wet paint " + support.name + " face " + face);
            overlay.transform.SetParent(transform, false);
            overlay.transform.SetPositionAndRotation(worldCenter + normal * (face == 0 ? 0.018f : 0.008f), rotation);
            overlay.transform.localScale = new Vector3(width, 1f, length);
            Mesh mesh = CreateMesh(width, length, face == 0 ? Mathf.Min(depth, 0.42f) : 0f);
            overlay.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = overlay.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = inkMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            var surface = new Surface
            {
                Collider = box,
                VisibilitySource = face == 0 ? null : box.GetComponent<Renderer>(),
                Read = NewField(x, z),
                Write = NewField(x, z),
                Mesh = mesh,
                Renderer = renderer,
                Center = worldCenter,
                Rotation = rotation,
                Parameters = new Vector4(width, length, Vector3.Dot(Vector3.down, right) * 0.30f / width,
                    Vector3.Dot(Vector3.down, forward) * 0.30f / length),
                LastStep = now - Time.fixedDeltaTime,
                LastDeposit = now,
                Texels = x * z
            };
            allocatedTexels += surface.Texels;
            surfaces.Add(key, surface);
            return surface;
        }

        static RenderTexture NewField(int width, int height)
        {
            var field = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
            {
                name = "Ink pigment field",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            field.Create();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = field;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = previous;
            return field;
        }

        static Mesh CreateMesh(float width, float length, float depth)
        {
            const int divisions = 24;
            const int stride = divisions + 1;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var detail = new List<Vector2>();
            var indices = new List<int>();
            for (int z = 0; z < stride; z++)
            for (int x = 0; x < stride; x++)
            {
                Vector2 coordinate = new Vector2(x, z) / divisions;
                vertices.Add(new Vector3(coordinate.x - 0.5f, 0f, coordinate.y - 0.5f));
                normals.Add(Vector3.up);
                uv.Add(coordinate);
                detail.Add(Vector2.zero);
                if (x == divisions || z == divisions)
                    continue;
                int i = z * stride + x;
                indices.Add(i);
                indices.Add(i + stride);
                indices.Add(i + 1);
                indices.Add(i + 1);
                indices.Add(i + stride);
                indices.Add(i + stride + 1);
            }

            if (depth > 0.015f)
            {
                for (int side = 0; side < 4; side++)
                    AddEdge(side, width, length, depth, vertices, normals, uv, detail, indices);
            }

            var mesh = new Mesh { name = "Paint heightfield and edges" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, detail);
            mesh.SetTriangles(indices, 0);
            mesh.bounds = new Bounds(new Vector3(0f, -depth * 0.5f, 0f),
                new Vector3(1f + 0.08f / width, depth + 0.18f, 1f + 0.08f / length));
            return mesh;
        }

        static void AddEdge(int side, float width, float length, float depth, List<Vector3> vertices,
            List<Vector3> normals, List<Vector2> uv, List<Vector2> detail, List<int> indices)
        {
            const int across = 32;
            const int down = 8;
            int first = vertices.Count;
            Vector3 normal = side == 0 ? Vector3.right : side == 1 ? Vector3.left :
                side == 2 ? Vector3.forward : Vector3.back;
            for (int row = 0; row <= down; row++)
            for (int column = 0; column <= across; column++)
            {
                float u = (float)column / across;
                float v = (float)row / down;
                float x = side == 0 ? 0.5f : side == 1 ? -0.5f : side == 2 ? 0.5f - u : u - 0.5f;
                float z = side == 0 ? u - 0.5f : side == 1 ? 0.5f - u : side == 2 ? 0.5f : -0.5f;
                uv.Add(new Vector2(x + 0.5f, z + 0.5f));
                vertices.Add(new Vector3(x + normal.x * 0.008f * v / width, -v * depth,
                    z + normal.z * 0.008f * v / length));
                normals.Add(normal);
                detail.Add(new Vector2(v, 1f));
                if (row == down || column == across)
                    continue;
                int i = first + row * (across + 1) + column;
                indices.Add(i);
                indices.Add(i + 1);
                indices.Add(i + across + 1);
                indices.Add(i + 1);
                indices.Add(i + across + 2);
                indices.Add(i + across + 1);
            }
        }

        void Release(Surface surface)
        {
            allocatedTexels -= surface.Texels;
            surface.Read.Release();
            surface.Write.Release();
            Destroy(surface.Read);
            Destroy(surface.Write);
            Destroy(surface.Mesh);
            if (surface.Renderer)
                Destroy(surface.Renderer.gameObject);
        }

        void OnDestroy()
        {
            foreach (Surface surface in surfaces.Values)
                Release(surface);
            surfaces.Clear();
            Destroy(solver);
        }
    }
}
