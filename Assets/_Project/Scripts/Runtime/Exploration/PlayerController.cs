using UnityEngine;

namespace TimeTraveler
{
    /// <summary>
    /// Movimiento del líder en 8 direcciones relativo a la cámara (vista cenital inclinada), con
    /// carrera, gravedad e interacción con el objeto más cercano delante del personaje.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 4.2f;
        public float RunSpeed = 7.5f;
        public float TurnSpeed = 14f;
        public float InteractRadius = 1.6f;

        private CharacterController controller;
        private float verticalVelocity;

        public bool CanAct
        {
            get { return GameManager.Instance != null && GameManager.Instance.IsSettledIn(GameMode.Exploration); }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!controller.enabled) return;
            var input = GameInput.Instance;
            Vector2 move = CanAct ? input.Move.ReadValue<Vector2>() : Vector2.zero;
            if (move.sqrMagnitude > 1f) move.Normalize();

            var cam = Camera.main;
            Vector3 forward = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = cam != null ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 direction = forward * move.y + right * move.x;

            float speed = input.Run.IsPressed() ? RunSpeed : WalkSpeed;
            verticalVelocity = controller.isGrounded ? -1f : verticalVelocity - 20f * Time.deltaTime;
            controller.Move((direction * speed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), TurnSpeed * Time.deltaTime);

            if (CanAct && input.Confirm.WasPressedThisFrame()) TryInteract();
        }

        public Interactable NearestInteractable()
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var hit in Physics.OverlapSphere(transform.position + transform.forward * 0.6f, InteractRadius, ~0, QueryTriggerInteraction.Collide))
            {
                var interactable = hit.GetComponentInParent<Interactable>();
                if (interactable == null || !interactable.isActiveAndEnabled) continue;
                float score = Vector3.Distance(transform.position, interactable.transform.position);
                if (score < bestScore) { bestScore = score; best = interactable; }
            }
            return best;
        }

        private void TryInteract()
        {
            var target = NearestInteractable();
            if (target != null) target.Interact(this);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!CanAct) return;
            var enemy = hit.collider.GetComponentInParent<MapEnemy>();
            if (enemy != null) enemy.Engage(transform);
        }
    }
}
