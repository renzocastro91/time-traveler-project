using UnityEngine;

namespace TimeTraveler
{
    /// <summary>
    /// Cámara cenital inclinada estilo JRPG clásico: ángulo fijo, campo de visión estrecho
    /// (aplana la perspectiva, recuerda al 2D) y seguimiento suave. En combate encuadra el área.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        public float Pitch = 52f;
        public float Yaw = 0f;
        public float Distance = 24f;
        public float BattleDistance = 27f;
        public float FieldOfView = 28f;
        public float Smooth = 6f;

        private Transform target;
        private Vector3? battleFocus;
        private Camera cam;

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            cam.fieldOfView = FieldOfView;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 200f;
        }

        public void Follow(Transform t) { target = t; }
        public void FocusBattle(Vector3 center) { battleFocus = center; }
        public void ReleaseBattle() { battleFocus = null; }

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.rotation = Quaternion.Euler(Pitch, Yaw, 0);
            transform.position = target.position - transform.forward * Distance;
        }

        private void LateUpdate()
        {
            Vector3 focus;
            float distance;
            if (battleFocus.HasValue) { focus = battleFocus.Value; distance = BattleDistance; }
            else if (target != null) { focus = target.position + Vector3.up * 0.8f; distance = Distance; }
            else return;

            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            Vector3 desired = focus - rotation * Vector3.forward * distance;
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Smooth * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, Smooth * Time.deltaTime);
        }
    }
}
