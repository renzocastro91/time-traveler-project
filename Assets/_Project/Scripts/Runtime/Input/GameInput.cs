using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeTraveler
{
    /// <summary>
    /// Entrada unificada teclado + mando (XInput/DualShock/genéricos vía Input System).
    /// Las acciones se definen en código para que funcionen sin configurar nada en el editor.
    ///
    ///  Mover:     WASD / flechas / stick izquierdo / cruceta
    ///  Confirmar: Enter / Espacio / Z / botón Sur (A en Xbox, X en PlayStation)
    ///  Cancelar:  Escape / X / Retroceso / botón Este (B, Círculo)
    ///  Menú:      Tab / M / Start (Options)
    ///  Correr:    Shift / botón Oeste (X, Cuadrado)
    ///  Notas de Resonancia: J K L / botones Oeste, Norte, Este
    /// </summary>
    public sealed class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; private set; }

        public InputAction Move { get; private set; }
        public InputAction Confirm { get; private set; }
        public InputAction Cancel { get; private set; }
        public InputAction Menu { get; private set; }
        public InputAction Run { get; private set; }
        public InputAction[] Notes { get; private set; }

        /// <summary>True si el último dispositivo usado fue un mando (para mostrar los iconos correctos).</summary>
        public bool UsingGamepad { get; private set; }

        private void Awake()
        {
            Instance = this;

            Move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow").With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");
            Move.AddBinding("<Gamepad>/dpad");

            Confirm = Button("Confirm", "<Keyboard>/enter", "<Keyboard>/space", "<Keyboard>/z", "<Gamepad>/buttonSouth");
            Cancel = Button("Cancel", "<Keyboard>/escape", "<Keyboard>/x", "<Keyboard>/backspace", "<Gamepad>/buttonEast");
            Menu = Button("Menu", "<Keyboard>/tab", "<Keyboard>/m", "<Gamepad>/start");
            Run = Button("Run", "<Keyboard>/leftShift", "<Gamepad>/buttonWest");
            Notes = new[]
            {
                Button("Note1", "<Keyboard>/j", "<Gamepad>/buttonWest"),
                Button("Note2", "<Keyboard>/k", "<Gamepad>/buttonNorth"),
                Button("Note3", "<Keyboard>/l", "<Gamepad>/buttonEast"),
            };

            foreach (var action in new[] { Move, Confirm, Cancel, Menu, Run }) action.Enable();
            foreach (var note in Notes) note.Enable();
        }

        private static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) action.AddBinding(b);
            return action;
        }

        private void Update()
        {
            if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame && Gamepad.current.CheckStateIsAtDefaultIgnoringNoise() == false)
                UsingGamepad = true;
            else if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                UsingGamepad = false;
        }

        private void OnDestroy()
        {
            foreach (var action in new[] { Move, Confirm, Cancel, Menu, Run }) action?.Dispose();
            if (Notes != null) foreach (var note in Notes) note.Dispose();
        }

        public string NoteLabel(int index)
        {
            if (UsingGamepad) return index == 0 ? "X / □" : index == 1 ? "Y / △" : "B / ○";
            return index == 0 ? "J" : index == 1 ? "K" : "L";
        }
    }
}
