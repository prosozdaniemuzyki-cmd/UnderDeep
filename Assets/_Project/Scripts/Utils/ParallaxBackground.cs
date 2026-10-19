using UnityEngine;

namespace UnderDeep.Utils
{
    public class ParallaxBackground : MonoBehaviour
    {
        public Transform cam;
        public float factor = 0.2f;
        Vector3 _start;
        void Start(){ _start=transform.position; if(!cam) cam=Camera.main.transform; }
        void Update(){ if(cam) transform.position = _start + Vector3.up * cam.position.y * factor; }
    }
}
