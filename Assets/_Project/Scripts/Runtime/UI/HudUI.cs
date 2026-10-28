using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>HUD de exploración: época actual, indicador de interacción y avisos breves.</summary>
    public sealed class HudUI : MonoBehaviour
    {
        private Canvas canvas;
        private Text eraLabel, promptLabel, toastLabel;
        private RectTransform toastWindow, promptWindow;
        private Coroutine toastRoutine;

        private void Awake()
        {
            canvas = UIKit.CreateCanvas("HudCanvas", 5);
            var era = UIKit.Window(canvas.transform, "Era", new Vector2(0.02f, 0.92f), new Vector2(0.32f, 0.98f));
            eraLabel = UIKit.Stretch(UIKit.Label(era, "", 26), 8f);

            promptWindow = UIKit.Window(canvas.transform, "Prompt", new Vector2(0.42f, 0.08f), new Vector2(0.58f, 0.14f));
            promptLabel = UIKit.Stretch(UIKit.Label(promptWindow, "", 26, TextAnchor.MiddleCenter), 6f);
            promptWindow.gameObject.SetActive(false);

            // Los avisos van en su propio lienzo para verse también durante diálogos y menús.
            var toastCanvas = UIKit.CreateCanvas("ToastCanvas", 50);
            toastWindow = UIKit.Window(toastCanvas.transform, "Toast", new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.88f));
            toastLabel = UIKit.Stretch(UIKit.Label(toastWindow, "", 28, TextAnchor.MiddleCenter), 10f);
            toastWindow.gameObject.SetActive(false);
        }

        public void SetVisible(bool visible)
        {
            canvas.gameObject.SetActive(visible);
        }

        public void Toast(string text, float seconds = 2.6f)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ShowToast(text, seconds));
        }

        private IEnumerator ShowToast(string text, float seconds)
        {
            toastLabel.text = text;
            toastWindow.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(seconds);
            toastWindow.gameObject.SetActive(false);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State == null || !canvas.gameObject.activeSelf) return;
            eraLabel.text = GameManager.T("era." + gm.State.Location.Era);

            var party = PartyVisuals.Instance;
            var target = party != null && party.Player != null ? party.Player.NearestInteractable() : null;
            promptWindow.gameObject.SetActive(target != null);
            if (target != null) promptLabel.text = GameManager.T(target.PromptKey);
        }
    }
}
