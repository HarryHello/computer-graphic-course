using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Inkshift
{
    [DefaultExecutionOrder(20)]
    public sealed class PaintTrail : MonoBehaviour
    {
        [SerializeField] ShapeMotor player;
        [FormerlySerializedAs("sphereInk")]
        [SerializeField] Material inkMaterial;
        [SerializeField] Shader flowShader;

        const float RelaxationTime = 1.55f;
        const float RelaxationInterval = 1f / 30f;
        const int TexelsPerMetre = 27;

        static readonly int InkState = Shader.PropertyToID("_InkState");
        static readonly int InkTexel = Shader.PropertyToID("_InkState_TexelSize");
        static readonly int SurfaceParams = Shader.PropertyToID("_Surface");
        static readonly int SegmentParams = Shader.PropertyToID("_Segment");
        static readonly int DepositParams = Shader.PropertyToID("_Deposit");
        static readonly int RippleParams = Shader.PropertyToID("_Ripple");

        readonly Dictionary<BoxCollider, Surface> surfaces = new();
        MaterialPropertyBlock properties;
        Material solver;
        Mesh grid;
        Surface lastSurface;
        Vector2 lastUV;
        bool lastBall;
        float pendingImpact;

        sealed class Surface
        {
            public RenderTexture Read;
            public RenderTexture Write;
            public MeshRenderer Renderer;
            public Vector4 Parameters;
            public Vector2 ImpactUV;
            public float LastStep;
            public float FlowUntil;
            public float ImpactTime;
            public float Impulse;
        }

        void Awake()
        {
            solver = new Material(flowShader);
            grid = CreateGrid();
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
            lastSurface = null;
            pendingImpact = 0f;
        }

        void FixedUpdate()
        {
            float now = Time.fixedTime;
            Surface fresh = Deposit(now);
            foreach (Surface surface in surfaces.Values)
            {
                if (surface != fresh && now <= surface.FlowUntil && now - surface.LastStep >= RelaxationInterval)
                    Step(surface, now, Vector4.zero, Vector4.zero);
            }
        }

        Surface Deposit(float now)
        {
            if (!player.Grounded || player.GroundSurface is not BoxCollider deck ||
                Vector3.Dot(player.GroundNormal, deck.transform.up) < 0.76f)
            {
                BreakTrail();
                return null;
            }

            if (!surfaces.TryGetValue(deck, out Surface surface))
            {
                surface = CreateSurface(deck, now);
                surfaces.Add(deck, surface);
            }

            Vector3 local = deck.transform.InverseTransformPoint(player.GroundPoint) - deck.center;
            Vector2 uv = new Vector2(Mathf.Clamp01(local.x / deck.size.x + 0.5f),
                Mathf.Clamp01(local.z / deck.size.z + 0.5f));
            bool ball = player.IsBall;
            bool resumed = lastSurface == null;
            bool shifted = lastSurface != null && ball != lastBall;
            Vector2 start = lastSurface == surface && !shifted ? lastUV : uv;
            Vector2 travel = uv - start;
            float distance = new Vector2(travel.x * surface.Parameters.x, travel.y * surface.Parameters.y).magnitude;

            float impact = Mathf.Max(pendingImpact, resumed ? 0.58f : shifted ? 0.40f : 0f);
            if (distance < 0.024f && impact <= 0f && surface == lastSurface)
                return null;

            float radius = (ball ? 0.47f : 0.63f) + impact * 0.19f;
            float mass = Mathf.Min(0.65f, distance * 1.13f + 0.11f) + impact * 0.43f;
            if (impact >= 0.4f)
            {
                surface.ImpactUV = uv;
                surface.ImpactTime = now;
                surface.Impulse = impact * 1.35f;
            }

            var segment = new Vector4(start.x, start.y, uv.x, uv.y);
            var deposit = new Vector4(radius, mass, ball ? 0f : 1f, impact);
            surface.FlowUntil = now + RelaxationTime;
            Step(surface, now, segment, deposit);
            lastSurface = surface;
            lastUV = uv;
            lastBall = ball;
            pendingImpact = 0f;
            return surface;
        }

        void Step(Surface surface, float now, Vector4 segment, Vector4 deposit)
        {
            float dt = Mathf.Clamp(now - surface.LastStep, Time.fixedDeltaTime, 0.05f);
            float push = surface.Impulse * Mathf.Clamp01(1f - (now - surface.ImpactTime) / 0.36f);
            solver.SetVector(SurfaceParams, surface.Parameters);
            solver.SetVector(SegmentParams, segment);
            solver.SetVector(DepositParams, deposit);
            solver.SetVector(RippleParams, new Vector4(surface.ImpactUV.x, surface.ImpactUV.y, push, dt));
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(surface.Read, surface.Write, solver, 0);
            RenderTexture.active = previous;
            (surface.Read, surface.Write) = (surface.Write, surface.Read);
            properties.SetTexture(InkState, surface.Read);
            properties.SetVector(InkTexel, new Vector4(1f / surface.Read.width, 1f / surface.Read.height,
                surface.Read.width, surface.Read.height));
            surface.Renderer.SetPropertyBlock(properties);
            surface.LastStep = now;
        }

        Surface CreateSurface(BoxCollider deck, float now)
        {
            Transform support = deck.transform;
            float width = support.TransformVector(Vector3.right * deck.size.x).magnitude;
            float length = support.TransformVector(Vector3.forward * deck.size.z).magnitude;
            int x = Mathf.Clamp(Mathf.CeilToInt(width * TexelsPerMetre), 32, 448);
            int z = Mathf.Clamp(Mathf.CeilToInt(length * TexelsPerMetre), 32, 448);
            var overlay = new GameObject("Wet paint " + support.name);
            overlay.transform.SetParent(transform, false);
            Vector3 face = support.TransformPoint(deck.center + Vector3.up * (0.5f * deck.size.y));
            overlay.transform.SetPositionAndRotation(face + support.up * 0.007f, support.rotation);
            overlay.transform.localScale = new Vector3(width, 1f, length);
            overlay.AddComponent<MeshFilter>().sharedMesh = grid;
            MeshRenderer renderer = overlay.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = inkMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return new Surface
            {
                Read = NewField(x, z),
                Write = NewField(x, z),
                Renderer = renderer,
                Parameters = new Vector4(width, length, Vector3.Dot(Vector3.down, support.right) * 0.44f / width,
                    Vector3.Dot(Vector3.down, support.forward) * 0.44f / length),
                LastStep = now - Time.fixedDeltaTime
            };
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

        static Mesh CreateGrid()
        {
            const int divisions = 24;
            const int stride = divisions + 1;
            var vertices = new Vector3[stride * stride];
            var uv = new Vector2[vertices.Length];
            var indices = new int[divisions * divisions * 6];
            int written = 0;
            for (int z = 0; z < stride; z++)
            for (int x = 0; x < stride; x++)
            {
                int i = z * stride + x;
                uv[i] = new Vector2(x, z) / divisions;
                vertices[i] = new Vector3(uv[i].x - 0.5f, 0f, uv[i].y - 0.5f);
                if (x == divisions || z == divisions)
                    continue;
                indices[written++] = i;
                indices[written++] = i + stride;
                indices[written++] = i + 1;
                indices[written++] = i + 1;
                indices[written++] = i + stride;
                indices[written++] = i + stride + 1;
            }
            var mesh = new Mesh { name = "Paint heightfield", vertices = vertices, uv = uv, triangles = indices };
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.10f, 1f));
            return mesh;
        }

        void OnDestroy()
        {
            foreach (Surface surface in surfaces.Values)
            {
                surface.Read.Release();
                surface.Write.Release();
                Destroy(surface.Read);
                Destroy(surface.Write);
            }

            Destroy(solver);
            Destroy(grid);
        }
    }
}
