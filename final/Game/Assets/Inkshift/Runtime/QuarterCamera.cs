using UnityEngine;

namespace Inkshift
{
    [RequireComponent(typeof(Camera))]
    public sealed class QuarterCamera : MonoBehaviour
    {
        [SerializeField] ShapeMotor target;
        [SerializeField] float distance = 8f;
        [SerializeField] float height = 8.8f;
        [SerializeField] float lookAhead = 1.4f;
        [SerializeField, Range(20f, 90f)] float fieldOfView = 51f;
        [SerializeField] float turnDuration = 0.34f;

        Camera lens;
        Vector3 focus;
        Vector3 focusVelocity;
        float yaw;
        float turnStart;
        float turnTarget;
        float turnElapsed;
        public int Heading { get; private set; }
        public Quaternion ControlRotation => Quaternion.Euler(0f, 90f * Heading, 0f);

        void Awake() => lens = GetComponent<Camera>();

        public void Snap(int heading, Vector3 position)
        {
            Heading = (heading + 4) % 4;
            yaw = turnStart = turnTarget = Heading * 90f;
            turnElapsed = turnDuration;
            focus = position;
            focusVelocity = Vector3.zero;
            lens.fieldOfView = fieldOfView;
            PositionCamera();
        }

        public void Turn(int steps)
        {
            Heading = (Heading + steps + 4) % 4;
            turnStart = yaw;
            turnTarget = yaw + Mathf.DeltaAngle(yaw, Heading * 90f);
            turnElapsed = 0f;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            turnElapsed = Mathf.Min(turnElapsed + dt, turnDuration);
            yaw = Mathf.SmoothStep(turnStart, turnTarget, turnElapsed / turnDuration);
            Vector3 lead = target.Velocity;
            lead.y = 0f;
            float speed = lead.magnitude;
            focus = Vector3.SmoothDamp(focus, target.transform.position + Vector3.ClampMagnitude(lead * 0.12f, 1.45f),
                ref focusVelocity, 0.13f, Mathf.Infinity, dt);
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, fieldOfView + Mathf.Clamp01(speed / 12f) * 5f, 1f - Mathf.Exp(-5f * dt));
            PositionCamera();
        }

        void PositionCamera()
        {
            Quaternion orbit = Quaternion.Euler(0f, yaw, 0f);
            Vector3 look = focus + orbit * new Vector3(0f, 0.1f, lookAhead);
            Vector3 position = focus + orbit * new Vector3(0f, height, -distance);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position, Vector3.up));
        }
    }
}
