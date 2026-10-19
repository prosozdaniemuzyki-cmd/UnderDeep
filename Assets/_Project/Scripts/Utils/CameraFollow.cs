using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Utils
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 5f;
        public Vector2 offset = new Vector2(0,2);
        public float minY = 0, maxY = 100;

        void LateUpdate()
        {
            if (!target) { if(ServiceLocator.TryGet<Player.DroneController>(out var d)) target=d.transform; else return; }
            var pos = target.position + (Vector3)offset;
            pos.x = 0; // center horizontally
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            pos.z = -10;
            transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime*smooth);
        }
    }
}
