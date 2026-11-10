using UnityEngine;
using UnityEngine.InputSystem;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 9f, height = 8f, turnSpeed = 7f;
    float angle, currentAngle;
    void LateUpdate()
    {
        if (!target) return;
        var k = Keyboard.current;
        if (k != null) { if (k.qKey.wasPressedThisFrame) angle -= 90f; if (k.eKey.wasPressedThisFrame) angle += 90f; }
        currentAngle = Mathf.LerpAngle(currentAngle, angle, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        Quaternion yaw = Quaternion.Euler(0, currentAngle, 0);
        transform.position = target.position + yaw * new Vector3(0, height, -distance);
        transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * 0.3f - transform.position);
    }
}
