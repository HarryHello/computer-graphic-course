using UnityEngine;

namespace Inkshift
{
    [RequireComponent(typeof(ShapeMotor))]
    public sealed class JellyVisuals : MonoBehaviour
    {
        [SerializeField] Transform elasticPose;
        [SerializeField] Transform airSpin;

        const float SpinDuration = 0.27f;

        ShapeMotor player;
        float stretch;
        float stretchVelocity;
        float spinTime = SpinDuration;
        Vector3 spinAxis;

        void Awake() => player = GetComponent<ShapeMotor>();

        void OnEnable()
        {
            player.Feedback += React;
            player.Relocated += ResetPose;
        }

        void OnDisable()
        {
            player.Feedback -= React;
            player.Relocated -= ResetPose;
        }

        void React(SoundCue cue)
        {
            switch (cue)
            {
                case SoundCue.Flip:
                    stretchVelocity -= 2.8f;
                    break;
                case SoundCue.Shift:
                    stretchVelocity -= 4.2f;
                    break;
                case SoundCue.Jump:
                    stretchVelocity += 5.2f;
                    break;
                case SoundCue.AirJump:
                    stretchVelocity += 7f;
                    spinAxis = Vector3.Cross(Vector3.up, player.MovementDirection);
                    spinTime = 0f;
                    break;
                case SoundCue.Land:
                    float impact = Mathf.InverseLerp(2.6f, 14f, -player.Velocity.y);
                    stretchVelocity -= Mathf.Lerp(6.4f, 10f, impact);
                    break;
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            Vector3 velocity = player.Velocity;
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            float running = Mathf.Clamp01(speed / (player.IsBall ? 10f : 5f));
            float target = player.Grounded
                ? -0.105f * running + Mathf.Sin(Time.time * 4.6f) * 0.012f
                : Mathf.Clamp01(Mathf.Abs(velocity.y) / 9f) * 0.12f;

            float springStep = Mathf.Min(dt, 0.033f);
            stretchVelocity = (stretchVelocity + (target - stretch) * 158f * springStep) * Mathf.Exp(-10f * springStep);
            stretch = Mathf.Clamp(stretch + stretchVelocity * springStep, -0.29f, 0.31f);
            float height = 1f + stretch;
            float width = 1f / Mathf.Sqrt(height);
            elasticPose.localScale = new Vector3(width, height, width);
            // Keep the bottom planted while the body squashes against the ground.
            elasticPose.localPosition = Vector3.up * (stretch * (player.Grounded ? 0.46f : 0.10f));

            if (spinTime < SpinDuration)
            {
                spinTime = Mathf.Min(SpinDuration, spinTime + dt);
                float t = spinTime / SpinDuration;
                float angle = 360f * t * t * (3f - 2f * t);
                airSpin.localRotation = Quaternion.AngleAxis(angle, spinAxis);
            }
        }

        void ResetPose()
        {
            stretch = stretchVelocity = 0f;
            spinTime = SpinDuration;
            elasticPose.localScale = Vector3.one;
            elasticPose.localPosition = Vector3.zero;
            airSpin.localRotation = Quaternion.identity;
        }
    }
}
