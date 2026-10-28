using TimeTraveler.Core;
using UnityEngine;

namespace TimeTraveler
{
    /// <summary>Pantalla de título y pantalla de derrota.</summary>
    public sealed class TitleScreen : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform list;
        private UnityEngine.UI.Text heading, subheading;

        private void Awake()
        {
            canvas = UIKit.CreateCanvas("TitleCanvas", 60);
            var bg = UIKit.Rect(canvas.transform, "Background", Vector2.zero, Vector2.one);
            bg.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.02f, 0.03f, 0.1f, 0.92f);
            heading = UIKit.Label(canvas.transform, "", 84, TextAnchor.MiddleCenter, UIKit.ButtonSelected);
            heading.rectTransform.anchorMin = new Vector2(0, 0.62f);
            heading.rectTransform.anchorMax = new Vector2(1, 0.82f);
            heading.rectTransform.offsetMin = heading.rectTransform.offsetMax = Vector2.zero;
            subheading = UIKit.Label(canvas.transform, "", 32, TextAnchor.MiddleCenter, UIKit.DimText);
            subheading.rectTransform.anchorMin = new Vector2(0, 0.55f);
            subheading.rectTransform.anchorMax = new Vector2(1, 0.62f);
            subheading.rectTransform.offsetMin = subheading.rectTransform.offsetMax = Vector2.zero;
            var window = UIKit.Window(canvas.transform, "Menu", new Vector2(0.38f, 0.18f), new Vector2(0.62f, 0.48f));
            list = UIKit.VerticalList(window, false, 8f);
            canvas.gameObject.SetActive(false);
        }

        public void Show()
        {
            var gm = GameManager.Instance;
            heading.text = GameManager.T("game.title");
            subheading.text = GameManager.T("game.subtitle");
            UIKit.Clear(list);
            var newGame = UIKit.Button(list, GameManager.T("title.new"), () => { Hide(); gm.NewGame(); });
            bool hasSave = false;
            for (int slot = 1; slot <= SaveService.SlotCount; slot++)
                if (System.IO.File.Exists(SaveService.SlotPath(gm.SaveDirectory, slot))) hasSave = true;
            var cont = UIKit.Button(list, GameManager.T("title.continue"), () => gm.Pause.OpenLoadScreen(fromTitle: true), null, hasSave);
            UIKit.Button(list, GameManager.T("title.quit"), Application.Quit);
            canvas.gameObject.SetActive(true);
            UIKit.Focus(hasSave ? cont : newGame);
        }

        public void ShowGameOver()
        {
            var gm = GameManager.Instance;
            heading.text = GameManager.T("battle.defeat");
            subheading.text = "";
            UIKit.Clear(list);
            var retry = UIKit.Button(list, GameManager.T("battle.gameover.retry"), () =>
            {
                Hide();
                if (!gm.LoadGame(gm.LastSaveSlot)) gm.NewGame();
            });
            UIKit.Button(list, GameManager.T("battle.gameover.title"), Show);
            canvas.gameObject.SetActive(true);
            UIKit.Focus(retry);
        }

        public void Hide()
        {
            canvas.gameObject.SetActive(false);
        }
    }
}
