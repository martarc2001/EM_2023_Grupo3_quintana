using UnityEngine;

namespace Scenery
{
    public class ParallaxLayer : MonoBehaviour
    {
        public Vector3 movementScale = Vector3.one;
        private Transform _camera;
        
        private void Awake()
        {
            _camera = Camera.main?.transform;
        }

        private void LateUpdate()
        {
            transform.position = Vector3.Scale(_camera.position, movementScale);
        }
    }
}