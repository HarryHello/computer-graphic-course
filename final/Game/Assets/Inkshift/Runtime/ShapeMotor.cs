using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Inkshift
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class ShapeMotor : MonoBehaviour
    {
        [SerializeField] Transform cube;
        [SerializeField] Transform sphere;
        [SerializeField] QuarterCamera view;
        [SerializeField] LayerMask surfaces = 1;
        [SerializeField, Min(1f)] float ballSpeed = 12f;
        [SerializeField, Min(1f)] float ballAcceleration = 21f;
        [SerializeField, Min(0.08f)] float flipDuration = 0.22f;
        [SerializeField, Min(1f)] float jumpSpeed = 8f;

        const float Radius = 0.46f;
        const float ProbeRadius = 0.38f;
        const float CoyoteTime = 0.11f;
        const float BufferTime = 0.14f;
        const float GroundStick = 1.4f;
        const float CubeCornerRadius = 0.14f;

        Rigidbody body;
        Vector2 move;
        bool jumpHeld;
        bool controlsEnabled;
        bool switchQueued;
        bool cutJump;
        float jumpDeadline = float.NegativeInfinity;
        float lastGrounded = float.NegativeInfinity;
        float groundLockUntil;
        float inputLockUntil;
        int jumpsUsed;

        bool flipping;
        bool flipReturning;
        Vector2 flipInput;
        float flipElapsed;
        Vector3 flipStart;
        Vector3 flipDirection;
        Quaternion cubeRest;
        Vector3 previousPosition;

        public bool IsBall { get; private set; }
        public bool Grounded { get; private set; }
        public int JumpsRemaining => Grounded ? 2 : Mathf.Min(2 - jumpsUsed, Time.time <= lastGrounded + CoyoteTime ? 2 : 1);
        public Vector3 Velocity => body.linearVelocity;
        public Vector3 GroundPoint { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public Collider GroundSurface { get; private set; }
        public Vector3 MovementDirection { get; private set; } = Vector3.forward;
        public event Action<SoundCue> Feedback;
        public event Action Relocated;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            GetComponent<SphereCollider>().radius = Radius;
            SetShape(false);
        }

        public void AllowInput(bool allowed)
        {
            controlsEnabled = allowed;
            move = Vector2.zero;
            jumpDeadline = float.NegativeInfinity;
            jumpHeld = false;
            switchQueued = false;
        }

        public void Place(Vector3 feet, int heading, bool ball, float inputDelay)
        {
            body.position = feet + Vector3.up * (Radius + 0.045f);
            body.linearVelocity = Vector3.zero;
            transform.position = body.position;
            previousPosition = body.position;
            flipping = false;
            cubeRest = Quaternion.identity;
            cube.localRotation = Quaternion.identity;
            cube.localPosition = Vector3.zero;
            sphere.localRotation = Quaternion.identity;
            SetShape(ball);
            Grounded = false;
            GroundSurface = null;
            jumpsUsed = 0;
            cutJump = false;
            lastGrounded = float.NegativeInfinity;
            groundLockUntil = 0f;
            inputLockUntil = Time.time + inputDelay;
            AllowInput(controlsEnabled);
            view.Snap(heading, body.position);
            MovementDirection = view.ControlRotation * Vector3.forward;
            Relocated?.Invoke();
        }

        void Update()
        {
            if (!controlsEnabled || Time.time < inputLockUntil)
                return;

            Keyboard keys = Keyboard.current;
            if (keys == null)
                return;

            move.x = ReadAxis(keys.dKey, keys.rightArrowKey, keys.aKey, keys.leftArrowKey, move.x);
            move.y = ReadAxis(keys.wKey, keys.upArrowKey, keys.sKey, keys.downArrowKey, move.y);
            jumpHeld = keys.spaceKey.isPressed;
            if (keys.spaceKey.wasPressedThisFrame)
                jumpDeadline = Time.time + BufferTime;
            if (keys.tabKey.wasPressedThisFrame)
                switchQueued = !switchQueued;

            int turn = Axis(keys.eKey.wasPressedThisFrame, keys.qKey.wasPressedThisFrame);
            if (turn != 0)
                view.Turn(turn);
        }

        static int Axis(bool positive, bool negative) => (positive ? 1 : 0) - (negative ? 1 : 0);

        static float ReadAxis(KeyControl positive, KeyControl positiveAlt, KeyControl negative, KeyControl negativeAlt, float previous)
        {
            bool positiveHeld = positive.isPressed || positiveAlt.isPressed;
            bool negativeHeld = negative.isPressed || negativeAlt.isPressed;
            if (!(positiveHeld && negativeHeld))
                return Axis(positiveHeld, negativeHeld);

            // When both directions are held, the newer press takes control of the axis.
            int pressed = Axis(positive.wasPressedThisFrame || positiveAlt.wasPressedThisFrame,
                negative.wasPressedThisFrame || negativeAlt.wasPressedThisFrame);
            return pressed == 0 ? previous : pressed;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector3 velocity = body.linearVelocity;
            Vector3 travel = body.position - previousPosition;
            previousPosition = body.position;

            bool wasGrounded = Grounded;
            RaycastHit ground = default;
            Grounded = Time.time >= groundLockUntil && TryGround(velocity, out ground);
            if (Grounded)
            {
                GroundPoint = ground.point;
                GroundNormal = ground.normal;
                GroundSurface = ground.collider;
                lastGrounded = Time.time;
                jumpsUsed = 0;
                cutJump = false;
                if (!wasGrounded && velocity.y < -2.6f)
                    Feedback?.Invoke(SoundCue.Land);
            }

            if (flipping && (!Grounded || flipElapsed >= flipDuration || (flipReturning && flipElapsed <= 0f)))
                FinishFlip(Grounded && !flipReturning && FlipDistance() >= 0.86f);

            if (switchQueued && !flipping)
            {
                switchQueued = false;
                SetShape(!IsBall);
                if (!IsBall && Grounded)
                    velocity = Vector3.zero;
                Feedback?.Invoke(SoundCue.Shift);
            }

            Quaternion heading = view.ControlRotation;
            Vector3 input = heading * Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f);
            Vector3 planarVelocity = Planar(velocity);
            if (planarVelocity.sqrMagnitude > 0.10f)
                MovementDirection = planarVelocity.normalized;
            else if (input.sqrMagnitude > 0f)
                MovementDirection = input.normalized;

            if (jumpDeadline >= Time.time && jumpsUsed < 2)
            {
                bool first = jumpsUsed == 0 && (Grounded || Time.time <= lastGrounded + CoyoteTime);
                if (flipping)
                    FinishFlip(FlipDistance() >= 0.5f);
                if (switchQueued)
                {
                    switchQueued = false;
                    SetShape(!IsBall);
                    Feedback?.Invoke(SoundCue.Shift);
                }
                jumpsUsed = first ? 1 : 2;
                Grounded = false;
                groundLockUntil = Time.time + 0.13f;
                jumpDeadline = float.NegativeInfinity;
                cutJump = true;
                if (!IsBall && input.sqrMagnitude > 0f && Planar(velocity).sqrMagnitude < 4f)
                    velocity = input * 4.3f;
                velocity.y = first ? jumpSpeed : jumpSpeed * 0.91f;
                Feedback?.Invoke(first ? SoundCue.Jump : SoundCue.AirJump);
            }

            if (IsBall)
            {
                velocity = Grounded ? DriveBall(velocity, input, dt) : DriveAir(velocity, input, dt, 9f, ballSpeed);
                RollSphere(travel);
            }
            else if (Grounded)
            {
                velocity = DriveCube(heading, input, dt);
            }
            else
            {
                velocity = DriveAir(velocity, input, dt, 15f, 4.6f);
            }

            if (!Grounded)
                velocity.y = UpdateGravity(velocity.y, dt);
            body.linearVelocity = velocity;
        }

        bool TryGround(Vector3 velocity, out RaycastHit hit)
        {
            // The smaller probe clears the player's sides and leaves room for steps across seams.
            return Physics.SphereCast(body.position + Vector3.up * 0.10f, ProbeRadius, Vector3.down,
                       out hit, 0.25f, surfaces, QueryTriggerInteraction.Ignore)
                && hit.normal.y >= 0.65f
                && Vector3.Dot(velocity, hit.normal) < 1.1f;
        }

        Vector3 DriveBall(Vector3 velocity, Vector3 input, float dt)
        {
            Vector3 normal = GroundNormal;
            if (input.sqrMagnitude > 0f &&
                Physics.Raycast(body.position + input * 0.33f + Vector3.up * 0.14f, Vector3.down,
                    out RaycastHit ahead, 0.76f, surfaces, QueryTriggerInteraction.Ignore) &&
                ahead.normal.y >= 0.65f && Vector3.Dot(input, ahead.normal) < -0.08f &&
                ahead.point.y >= GroundPoint.y)
            {
                // Anticipate an uphill seam before the sphere pushes into it horizontally.
                normal = ahead.normal;
            }

            Vector3 tangent = Vector3.ProjectOnPlane(velocity, normal);
            if (input.sqrMagnitude > 0f)
            {
                Vector3 desired = Vector3.ProjectOnPlane(input, normal).normalized * ballSpeed;
                float acceleration = Vector3.Dot(tangent, desired) < 0f ? ballAcceleration * 1.45f : ballAcceleration;
                tangent = Vector3.MoveTowards(tangent, desired, acceleration * dt);
            }
            else
            {
                tangent = Vector3.MoveTowards(tangent, Vector3.zero, 2.2f * dt);
            }

            tangent += Vector3.ProjectOnPlane(Vector3.down * 16f, normal) * dt;
            return Vector3.ClampMagnitude(tangent, ballSpeed + 0.9f) - normal * GroundStick;
        }

        Vector3 DriveCube(Quaternion heading, Vector3 input, float dt)
        {
            if (!flipping && move != Vector2.zero)
            {
                Vector3 step = Mathf.Abs(move.x) > Mathf.Abs(move.y)
                    ? Vector3.right * Mathf.Sign(move.x)
                    : Vector3.forward * Mathf.Sign(move.y);
                Vector3 direction = heading * step;
                if (!Physics.SphereCast(body.position + Vector3.up * 0.12f, ProbeRadius, direction,
                        out RaycastHit wall, 1.06f, surfaces, QueryTriggerInteraction.Ignore)
                    || wall.normal.y >= 0.55f)
                {
                    flipping = true;
                    flipReturning = false;
                    flipInput = move;
                    flipElapsed = 0f;
                    flipStart = body.position;
                    flipDirection = direction;
                }
            }

            if (!flipping)
                return Vector3.down * GroundStick;

            float traveled = FlipDistance();
            float angle = FlipAngle(traveled);
            if (move != flipInput)
            {
                // Turning the camera remaps the next step, while a changed key can reverse this one.
                flipInput = move;
                float along = Vector3.Dot(input, flipDirection);
                if ((!flipReturning && along < -0.45f) || (flipReturning && along > 0.45f))
                {
                    flipReturning = !flipReturning;
                    flipElapsed = angle / (Mathf.PI * 0.5f) * flipDuration;
                }
            }

            flipElapsed = Mathf.Clamp(flipElapsed + (flipReturning ? -dt : dt), 0f, flipDuration);
            float radians = Mathf.PI * 0.5f * (flipElapsed / flipDuration);
            // Returning uses the same pivot path, so it lands on the original position.
            float distance = 0.5f * (1f + Mathf.Sin(radians) - Mathf.Cos(radians));
            float speed = Mathf.Clamp((distance - traveled) / dt, -7f, 7f);
            cube.localRotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, flipDirection)) * cubeRest;
            float lift = (0.5f - CubeCornerRadius) * (Mathf.Sin(angle) + Mathf.Cos(angle) - 1f);
            cube.localPosition = Vector3.up * lift;

            Vector3 velocity = flipDirection * speed;
            velocity.y = -Vector3.Dot(velocity, GroundNormal) / GroundNormal.y - GroundStick;
            return velocity;
        }

        float FlipDistance() => Vector3.Dot(body.position - flipStart, flipDirection);

        static float FlipAngle(float distance) =>
            Mathf.Asin((2f * Mathf.Clamp01(distance) - 1f) / Mathf.Sqrt(2f)) + Mathf.PI * 0.25f;

        void FinishFlip(bool completed)
        {
            flipping = false;
            if (completed)
                cubeRest = Quaternion.AngleAxis(90f, Vector3.Cross(Vector3.up, flipDirection)) * cubeRest;
            cube.localRotation = cubeRest;
            cube.localPosition = Vector3.zero;
            if (completed && Grounded)
                Feedback?.Invoke(SoundCue.Flip);
        }

        static Vector3 DriveAir(Vector3 velocity, Vector3 input, float dt, float acceleration, float maxSpeed)
        {
            Vector3 horizontal = Planar(velocity);
            // An airborne morph preserves takeoff speed until steering or landing changes it.
            if (input.sqrMagnitude > 0f)
            {
                float speed = horizontal.magnitude;
                float limit = Mathf.Max(maxSpeed, speed - 1.0f * dt);
                horizontal = Vector3.MoveTowards(horizontal, input * limit, acceleration * dt);
            }

            horizontal.y = velocity.y;
            return horizontal;
        }

        float UpdateGravity(float vertical, float dt)
        {
            if (cutJump && !jumpHeld && vertical > 4.6f)
            {
                cutJump = false;
                vertical = 4.6f;
            }

            float gravity = vertical < 0f ? 26f : 22f;
            if (cutJump && jumpHeld && Mathf.Abs(vertical) < 1.2f)
                gravity *= 0.7f;
            return Mathf.Max(vertical - gravity * dt, -28f);
        }

        void RollSphere(Vector3 travel)
        {
            Vector3 alongSurface = Vector3.ProjectOnPlane(travel, Grounded ? GroundNormal : Vector3.up);
            if (alongSurface.sqrMagnitude < 0.00001f)
                return;
            Vector3 axis = Vector3.Cross(Grounded ? GroundNormal : Vector3.up, alongSurface).normalized;
            sphere.Rotate(axis, alongSurface.magnitude / Radius * Mathf.Rad2Deg, Space.World);
        }

        void SetShape(bool ball)
        {
            IsBall = ball;
            cube.gameObject.SetActive(!ball);
            sphere.gameObject.SetActive(ball);
        }

        static Vector3 Planar(Vector3 value) => new Vector3(value.x, 0f, value.z);
    }
}
