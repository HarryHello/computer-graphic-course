using UnityEngine;

namespace Inkshift
{
    [DefaultExecutionOrder(100)]
    public sealed class LabWalls : MonoBehaviour
    {
        [SerializeField] Transform viewingCamera;
        [SerializeField] Transform[] walls;
        [SerializeField, Min(0f)] float cutawayMargin = 0.65f;

        void LateUpdate()
        {
            Vector3 cameraPosition = viewingCamera.position;
            foreach (Transform wall in walls)
            {
                float distanceInside = Vector3.Dot(cameraPosition - wall.position, wall.forward);
                bool visible = distanceInside > cutawayMargin;
                if (wall.gameObject.activeSelf != visible)
                    wall.gameObject.SetActive(visible);
            }
        }
    }
}
