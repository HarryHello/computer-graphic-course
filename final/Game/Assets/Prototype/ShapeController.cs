using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ShapeController : MonoBehaviour
{
    public Transform cube, sphere, cameraRig;
    public float acceleration = 18f, maxSpeed = 10f, jumpSpeed = 7f, rollDuration = 0.22f;
    Rigidbody body;
    bool ball = true, rolling;
    int jumps;
    float rollTime;
    Vector3 rollStart, rollDirection;
    Quaternion rollRotation;

    void Awake() { body = GetComponent<Rigidbody>(); body.freezeRotation = true; SetShape(true); }
    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;
        if (k.tabKey.wasPressedThisFrame && !rolling) SetShape(!ball);
        if (k.spaceKey.wasPressedThisFrame && jumps < 2)
        {
            if (rolling) return;
            var v = body.linearVelocity; v.y = jumpSpeed; body.linearVelocity = v; jumps++;
        }
        if (transform.position.y < -15f) { body.position = new Vector3(0, 1, 0); body.linearVelocity = Vector3.zero; rolling = false; jumps = 0; }
    }
    void FixedUpdate()
    {
        var k = Keyboard.current;
        if (k == null) return;
        bool grounded = Physics.SphereCast(body.position, 0.38f, Vector3.down, out _, 0.17f, ~0, QueryTriggerInteraction.Ignore);
        if (grounded && body.linearVelocity.y <= 0.1f) jumps = 0;
        float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
        float z = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
        Vector3 forward = cameraRig.forward; forward.y = 0; forward.Normalize();
        Vector3 right = cameraRig.right; right.y = 0; right.Normalize();
        Vector3 input = Vector3.ClampMagnitude(right * x + forward * z, 1f);
        if (ball)
        {
            Vector3 v = body.linearVelocity; Vector3 horizontal = new Vector3(v.x, 0, v.z);
            horizontal = Vector3.ClampMagnitude(horizontal + input * acceleration * Time.fixedDeltaTime, maxSpeed);
            horizontal *= 1f / (1f + 0.2f * Time.fixedDeltaTime);
            body.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
            if (horizontal.sqrMagnitude > 0.01f) sphere.Rotate(Vector3.Cross(Vector3.up, horizontal.normalized), horizontal.magnitude * Time.fixedDeltaTime * Mathf.Rad2Deg / 0.5f, Space.World);
        }
        else if (rolling)
        {
            rollTime = Mathf.Min(rollTime + Time.fixedDeltaTime / rollDuration, 1f);
            Vector3 pos = Vector3.Lerp(rollStart, rollStart + rollDirection, rollTime);
            body.MovePosition(new Vector3(pos.x, body.position.y, pos.z));
            cube.rotation = Quaternion.AngleAxis(90f * rollTime, Vector3.Cross(Vector3.up, rollDirection)) * rollRotation;
            if (rollTime >= 1f) rolling = false;
        }
        else if (grounded && input.sqrMagnitude > 0.1f)
        {
            Vector3 direction = Mathf.Abs(x) > Mathf.Abs(z) ? right * Mathf.Sign(x) : forward * Mathf.Sign(z);
            direction = new Vector3(Mathf.Round(direction.x), 0, Mathf.Round(direction.z)).normalized;
            if (direction.sqrMagnitude > 0) { rollDirection = direction; rollStart = body.position; rollRotation = cube.rotation; rollTime = 0; rolling = true; body.linearVelocity = new Vector3(0, body.linearVelocity.y, 0); }
        }
    }
    void SetShape(bool useBall)
    {
        ball = useBall; cube.gameObject.SetActive(!ball); sphere.gameObject.SetActive(ball);
        if (!ball) { var v = body.linearVelocity; body.linearVelocity = new Vector3(0, v.y, 0); }
    }
}
