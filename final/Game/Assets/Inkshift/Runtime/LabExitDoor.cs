using UnityEngine;

namespace Inkshift
{
    public sealed class LabExitDoor : MonoBehaviour
    {
        [SerializeField] Transform shutter;
        [SerializeField] Transform stagingPoint;
        [SerializeField] Transform interiorPoint;
        [SerializeField, Min(0f)] float liftHeight = 3.5f;

        Vector3 closedPosition;

        public Vector3 StagingPosition => stagingPoint ? stagingPoint.position : transform.TransformPoint(0f, 0f, -1.55f);
        public Vector3 InteriorPosition => interiorPoint ? interiorPoint.position : transform.TransformPoint(0f, 0f, 2.4f);
        public Vector3 Forward => transform.forward;

        void Awake()
        {
            if (shutter)
                closedPosition = shutter.localPosition;
        }

        public void Configure(Transform shutterPanel, Transform staging, Transform interior, float lift)
        {
            shutter = shutterPanel;
            stagingPoint = staging;
            interiorPoint = interior;
            liftHeight = lift;
            if (shutter)
                closedPosition = shutter.localPosition;
        }

        public void SetOpenAmount(float open)
        {
            if (shutter)
            {
                Vector3 lift = shutter.parent.InverseTransformVector(transform.up * liftHeight);
                shutter.localPosition = closedPosition + lift * Mathf.Clamp01(open);
            }
        }
    }
}
