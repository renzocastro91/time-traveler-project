using System;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// E02 · Puzzle de la cinta: tres controles (cabezal, frecuencia, velocidad) de 1 a 5.
    /// Muestra los recuerdos anotados que el jugador examinó en E01. La señal sólo se mide al
    /// probar (Confirmar), para que sea un puzzle de deducción y no de tanteo.
    /// </summary>
    public sealed class TapePuzzleUI : MonoBehaviour
    {
        private Canvas canvas;
        private Text[] dialLabels;
        private Text feedback, memories;
        private RectTransform[] dialBoxes;
        private readonly int[] values = { 3, 3, 3 };
        private int selected;
        private Vector2 lastMove;
        private int openedFrame;
        private Action<bool> onDone;

        private void Awake()
        {
            canvas = UIKit.CreateCanvas("TapeCanvas", 46);
            var window = UIKit.Window(canvas.transform, "Tape", new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f));
            var title = UIKit.Label(window, "", 40, TextAnchor.UpperCenter, UIKit.ButtonSelected);
            title.name = "Title";
            Stretch(title.rectTransform, 0.02f, 0.88f, 0.98f, 0.98f);

            dialLabels = new Text[TapePuzzle.Dials];
            dialBoxes = new RectTransform[TapePuzzle.Dials];
            for (int i = 0; i < TapePuzzle.Dials; i++)
            {
                float x0 = 0.08f + i * 0.29f;
                dialBoxes[i] = UIKit.Window(window, "Dial" + i, new Vector2(x0, 0.5f), new Vector2(x0 + 0.26f, 0.84f));
                dialLabels[i] = UIKit.Stretch(UIKit.Label(dialBoxes[i], "", 36, TextAnchor.MiddleCenter), 8f);
            }
            memories = UIKit.Label(window, "", 26, TextAnchor.UpperLeft);
            Stretch(memories.rectTransform, 0.06f, 0.2f, 0.94f, 0.48f);
            feedback = UIKit.Label(window, "", 28, TextAnchor.MiddleCenter, UIKit.AtbColor);
            Stretch(feedback.rectTransform, 0.04f, 0.1f, 0.96f, 0.2f);
            var help = UIKit.Label(window, "", 22, TextAnchor.MiddleCenter, UIKit.DimText);
            help.name = "Help";
            Stretch(help.rectTransform, 0.02f, 0.01f, 0.98f, 0.1f);
            canvas.gameObject.SetActive(false);
        }

        private static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public void Open(Action<bool> done)
        {
            onDone = done;
            openedFrame = Time.frameCount;
            selected = 0;
            canvas.gameObject.SetActive(true);
            canvas.transform.Find("Tape/Title").GetComponent<Text>().text = GameManager.T("puzzle.cinta.title");
            canvas.transform.Find("Tape/Help").GetComponent<Text>().text = GameManager.T("puzzle.cinta.help");
            feedback.text = "";

            var state = GameManager.Instance.State;
            var sb = new System.Text.StringBuilder(GameManager.T("puzzle.cinta.memories")).Append('\n');
            for (int i = 0; i < TapePuzzle.Memories.Length; i++)
            {
                string name = GameManager.T("puzzle.cinta.mem" + i);
                var m = TapePuzzle.Memories[i];
                sb.Append("• ").Append(state.Flags.Get(TapePuzzle.MemoryFlags[i]) != 0
                    ? GameManager.T("puzzle.cinta.memory", name, m[0], m[1], m[2])
                    : GameManager.T("puzzle.cinta.unknown", name)).Append('\n');
            }
            memories.text = sb.ToString();
            Redraw();
        }

        private void Redraw()
        {
            for (int i = 0; i < TapePuzzle.Dials; i++)
            {
                dialLabels[i].text = GameManager.T("puzzle.cinta.dial" + i) + "\n\n◄  " + values[i] + "  ►";
                dialBoxes[i].GetComponent<Image>().color = i == selected ? new Color(0.25f, 0.2f, 0.05f, 0.95f) : UIKit.WindowColor;
            }
        }

        private void Update()
        {
            if (!canvas.gameObject.activeSelf || Time.frameCount == openedFrame) return;
            var input = GameInput.Instance;

            // Movimiento por pulsaciones (no continuo): se detecta el cambio de dirección.
            Vector2 move = input.Move.ReadValue<Vector2>();
            Vector2 dir = new Vector2(Mathf.Abs(move.x) > 0.5f ? Mathf.Sign(move.x) : 0f, Mathf.Abs(move.y) > 0.5f ? Mathf.Sign(move.y) : 0f);
            if (dir != lastMove)
            {
                if (dir.x != 0) selected = (selected + (int)dir.x + TapePuzzle.Dials) % TapePuzzle.Dials;
                else if (dir.y != 0) values[selected] = Mathf.Clamp(values[selected] + (int)dir.y, TapePuzzle.MinValue, TapePuzzle.MaxValue);
                lastMove = dir;
                Redraw();
            }

            if (input.Confirm.WasPressedThisFrame())
            {
                if (TapePuzzle.IsSolved(values)) { Finish(true); return; }
                feedback.text = GameManager.T("puzzle.cinta.signal", TapePuzzle.Signal(values)) + "\n" + GameManager.T("puzzle.cinta.fail");
            }
            else if (input.Cancel.WasPressedThisFrame())
            {
                Finish(false);
            }
        }

        public void Close() { if (canvas.gameObject.activeSelf) Finish(false); }

        private void Finish(bool solved)
        {
            canvas.gameObject.SetActive(false);
            var callback = onDone;
            onDone = null;
            callback?.Invoke(solved);
        }
    }
}
