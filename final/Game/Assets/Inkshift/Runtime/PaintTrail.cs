using UnityEngine;
using UnityEngine.Rendering;

namespace Inkshift
{
    public sealed class PaintTrail : MonoBehaviour
    {
        [SerializeField] ShapeMotor player;
        [SerializeField] Material sphereInk;
        [SerializeField] Material cubeInk;

        const int Capacity = 320;
        readonly Transform[] stamps = new Transform[Capacity];
        Mesh quad;
        int next;
        bool wasBall;
        bool touching;
        Vector3 last;

        void Awake()
        {
            quad = new Mesh { name = "Ink stamp" };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            quad.RecalculateBounds();
        }

        void LateUpdate()
        {
            if (!player.Grounded || Time.deltaTime <= 0f)
            {
                touching = false;
                return;
            }

            bool ball = player.IsBall;
            Vector3 point = player.GroundPoint;
            Vector3 along = point - last;
            float spacing = ball ? 0.32f : 0.79f;
            if (!touching || wasBall != ball || along.sqrMagnitude >= spacing * spacing)
            {
                Vector3 normal = player.GroundNormal;
                Vector3 direction = touching && along.sqrMagnitude > 0.001f ? along : Vector3.forward;
                direction = Vector3.ProjectOnPlane(direction, normal).normalized;
                if (direction.sqrMagnitude < 0.01f)
                    direction = Vector3.Cross(normal, Vector3.right);
                Stamp(point, normal, direction, ball);
                last = point;
                touching = true;
                wasBall = ball;
            }
        }

        void Stamp(Vector3 point, Vector3 normal, Vector3 direction, bool ball)
        {
            Transform stamp = stamps[next];
            if (!stamp)
            {
                stamp = new GameObject("Ink").transform;
                stamp.SetParent(transform, false);
                stamp.gameObject.AddComponent<MeshFilter>().sharedMesh = quad;
                MeshRenderer renderer = stamp.gameObject.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                stamps[next] = stamp;
            }

            stamp.GetComponent<MeshRenderer>().sharedMaterial = ball ? sphereInk : cubeInk;
            stamp.SetPositionAndRotation(point + normal * (0.012f + next % 7 * 0.001f), Quaternion.LookRotation(normal, direction));
            stamp.localScale = ball ? new Vector3(0.61f, 0.82f, 1f) : new Vector3(0.79f, 0.74f, 1f);
            next = (next + 1) % Capacity;
        }

        void OnDestroy() => Destroy(quad);
    }
}
