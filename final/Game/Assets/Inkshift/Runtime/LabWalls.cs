using UnityEngine;

namespace Inkshift
{
    [DefaultExecutionOrder(100)]
    public sealed class LabWalls : MonoBehaviour
    {
        [SerializeField] Transform viewingCamera;
        [SerializeField] Transform[] walls;
        [SerializeField, Min(0f)] float cutawayMargin = 0.65f;

        Renderer[][] renderers;
        bool[][] originallyEnabled;

        void Awake()
        {
            renderers = new Renderer[walls.Length][];
            originallyEnabled = new bool[walls.Length][];
            for (int i = 0; i < walls.Length; i++)
            {
                renderers[i] = walls[i].GetComponentsInChildren<Renderer>(true);
                originallyEnabled[i] = new bool[renderers[i].Length];
                for (int j = 0; j < renderers[i].Length; j++)
                    originallyEnabled[i][j] = renderers[i][j].enabled;
            }
        }

        void LateUpdate()
        {
            Vector3 cameraPosition = viewingCamera.position;
            for (int i = 0; i < walls.Length; i++)
            {
                Transform wall = walls[i];
                float distanceInside = Vector3.Dot(cameraPosition - wall.position, wall.forward);
                bool visible = distanceInside > cutawayMargin;
                for (int j = 0; j < renderers[i].Length; j++)
                    renderers[i][j].enabled = visible && originallyEnabled[i][j];
            }
        }
    }
}
