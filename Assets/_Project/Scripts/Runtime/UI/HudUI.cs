using System.Collections;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// HUD de exploración: lugar y época, objetivo actual de la escena, indicador de interacción,
    /// aviso de la vista ANTES/AHORA, títulos de escena y avisos breves.
    /// </summary>
    public sealed class HudUI : MonoBehaviour
    {
        private Canvas canvas;
        private Text eraLabel, objectiveLabel, promptLabel, toastLabel, sceneTitle, overlayLabel;
        private RectTransform toastWindow, promptWindow, objectiveWindow, sceneWindow, overlayWindow;
        private Coroutine toastRoutine, sceneRoutine;

        private void Awake()
        {
            canvas = UIKit.CreateCanvas("HudCanvas", 5);
            var era = UIKit.Window(canvas.transform, "Era", new Vector2(0.02f, 0.92f), new Vector2(0.4f, 0.98f));
            eraLabel = UIKit.Stretch(UIKit.Label(era, "", 26), 8f);

            objectiveWindow = UIKit.Window(canvas.transform, "Objective", new Vector2(0.58f, 0.88f), new Vector2(0.98f, 0.98f));
            objectiveLabel = UIKit.Stretch(UIKit.Label(objectiveWindow, "", 24, TextAnchor.MiddleLeft), 10f);

            overlayWindow = UIKit.Window(canvas.transform, "Overlay", new Vector2(0.02f, 0.85f), new Vector2(0.24f, 0.9f));
            overlayLabel = UIKit.Stretch(UIKit.Label(overlayWindow, "", 22, TextAnchor.MiddleCenter, UIKit.AtbColor), 4f);
            overlayWindow.gameObject.SetActive(false);

            promptWindow = UIKit.Window(canvas.transform, "Prompt", new Vector2(0.42f, 0.08f), new Vector2(0.58f, 0.14f));
            promptLabel = UIKit.Stretch(UIKit.Label(promptWindow, "", 26, TextAnchor.MiddleCenter), 6f);
            promptWindow.gameObject.SetActive(false);

            // Avisos y títulos de escena van en su propio lienzo para verse también durante diálogos y menús.
            var top = UIKit.CreateCanvas("ToastCanvas", 50);
            toastWindow = UIKit.Window(top.transform, "Toast", new Vector2(0.2f, 0.74f), new Vector2(0.8f, 0.84f));
            toastLabel = UIKit.Stretch(UIKit.Label(toastWindow, "", 28, TextAnchor.MiddleCenter), 10f);
            toastWindow.gameObject.SetActive(false);
            sceneWindow = UIKit.Rect(top.transform, "SceneTitle", new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.58f));
            sceneTitle = UIKit.Stretch(UIKit.Label(sceneWindow, "", 54, TextAnchor.MiddleCenter, UIKit.ButtonSelected), 0f);
            sceneWindow.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            canvas.gameObject.SetActive(visible);
        }

        public void Toast(string text, float seconds = 2.6f)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(Show(toastWindow, toastLabel, text, seconds));
        }

        /// <summary>Muestra el título de la escena actual (al avanzar de escena).</summary>
        public void ShowSceneTitle()
        {
            var gm = GameManager.Instance;
            var scene = StoryDirector.CurrentScene(gm.Db, gm.State);
            if (scene == null) return;
            if (sceneRoutine != null) StopCoroutine(sceneRoutine);
            sceneRoutine = StartCoroutine(Show(sceneWindow, sceneTitle, GameManager.T(scene.TitleKey), 3f));
        }

        private static IEnumerator Show(RectTransform window, Text label, string text, float seconds)
        {
            label.text = text;
            window.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(seconds);
            window.gameObject.SetActive(false);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State == null || !canvas.gameObject.activeSelf) return;

            var world = WorldBuilder.Instance;
            string place = world != null && world.Current != null ? GameManager.T(world.Current.NameKey) : "";
            eraLabel.text = GameManager.T("era." + gm.State.Location.Era) + (place.Length > 0 ? " · " + place : "");

            var scene = StoryDirector.CurrentScene(gm.Db, gm.State);
            var objective = StoryDirector.CurrentObjective(gm.Db, gm.State);
            objectiveWindow.gameObject.SetActive(objective != null);
            if (objective != null) objectiveLabel.text = GameManager.T("objective.label") + ": " + GameManager.T(scene.ObjectiveKey(objective));

            bool hasOverlay = world != null && world.HasAntesLayer;
            overlayWindow.gameObject.SetActive(hasOverlay);
            if (hasOverlay) overlayLabel.text = world.OverlayActive ? GameManager.T("overlay.active") : GameManager.T("overlay.hint");

            var party = PartyVisuals.Instance;
            var target = party != null && party.Player != null ? party.Player.NearestInteractable() : null;
            promptWindow.gameObject.SetActive(target != null);
            if (target != null) promptLabel.text = GameManager.T(target.PromptKey);
        }
    }
}
