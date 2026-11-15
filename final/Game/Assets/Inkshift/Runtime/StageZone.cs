using UnityEngine;

namespace Inkshift
{
    public enum ZoneKind { Hint, Checkpoint, Finish }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class StageZone : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] ZoneKind kind;
        [SerializeField] int checkpointOrder;
        [SerializeField, Tooltip("Respawn point. Its forward axis follows the next section of the route.")]
        Transform spawn;
        [SerializeField] LabExitDoor exitDoor;
        [SerializeField] MeshRenderer indicator;
        [SerializeField] Material activeMaterial;
        [SerializeField] string title;
        [SerializeField, TextArea] string message;

        public ZoneKind Kind => kind;
        public int Order => checkpointOrder;
        public Transform Spawn => spawn;
        public LabExitDoor ExitDoor => exitDoor;
        public string Title => title;
        public string Message => message;

        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out ShapeMotor motor))
                flow.EnterZone(this, motor);
        }

        public void Activate()
        {
            if (indicator)
                indicator.sharedMaterial = activeMaterial;
        }
    }
}
