using System;
using System.Collections;
using System.Collections.Generic;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// E07 · Alfabeto acústico de Sura. Sura muestra una secuencia de signos (mano, gesto, huella,
    /// golpe) y el grupo la repite con cuatro botones. Equivocarse no castiga: Sura vuelve a mostrar.
    /// Cancelar sale; se puede retomar hablando con Sura.
    /// </summary>
    public sealed class SignPuzzleUI : MonoBehaviour
    {
        private static readonly Color[] SignColors =
        {
            new Color(0.95f, 0.8f, 0.6f), new Color(0.6f, 0.85f, 1f), new Color(0.7f, 0.55f, 0.35f), new Color(1f, 0.5f, 0.4f)
        };

        private Canvas canvas;
        private Text status, bigSign;
        private Image bigSignBack;
        private RectTransform buttonList;
        private SignSequencePuzzle puzzle;
        private readonly List<int> answer = new List<int>();
        private Action<bool> onDone;
        private bool watching;

        private void Awake()
        {
            canvas = UIKit.CreateCanvas("SignCanvas", 46);
            var window = UIKit.Window(canvas.transform, "Signs", new Vector2(0.22f, 0.12f), new Vector2(0.78f, 0.88f));
            var title = UIKit.Label(window, "", 40, TextAnchor.UpperCenter, UIKit.ButtonSelected);
            title.name = "Title";
            Stretch(title.rectTransform, 0.02f, 0.88f, 0.98f, 0.98f);
            status = UIKit.Label(window, "", 30, TextAnchor.MiddleCenter);
            Stretch(status.rectTransform, 0.05f, 0.78f, 0.95f, 0.88f);

            var sign = UIKit.Rect(window, "Sign", new Vector2(0.3f, 0.45f), new Vector2(0.7f, 0.76f));
            bigSignBack = sign.gameObject.AddComponent<Image>();
            bigSign = UIKit.Stretch(UIKit.Label(sign, "", 48, TextAnchor.MiddleCenter, Color.black), 8f);

            var holder = UIKit.Rect(window, "Buttons", new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.43f));
            buttonList = UIKit.VerticalList(holder, false, 6f);
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
            puzzle = new SignSequencePuzzle(Environment.TickCount);
            canvas.gameObject.SetActive(true);
            canvas.transform.Find("Signs/Title").GetComponent<Text>().text = GameManager.T("puzzle.signos.title");
            StartCoroutine(ShowSequence());
        }

        private IEnumerator ShowSequence()
        {
            watching = true;
            answer.Clear();
            UIKit.Clear(buttonList);
            status.text = GameManager.T("puzzle.signos.round", puzzle.Round + 1, SignSequencePuzzle.RoundLengths.Length) + "\n" + GameManager.T("puzzle.signos.watch");
            yield return new WaitForSecondsRealtime(0.8f);
            foreach (int s in puzzle.Sequence)
            {
                SetSign(s);
                yield return new WaitForSecondsRealtime(0.75f);
                SetSign(-1);
                yield return new WaitForSecondsRealtime(0.2f);
            }
            watching = false;
            BuildButtons();
        }

        private void SetSign(int s)
        {
            bigSignBack.color = s < 0 ? new Color(0, 0, 0, 0.3f) : SignColors[s];
            bigSign.text = s < 0 ? "" : GameManager.T("puzzle.signos.sign" + s);
        }

        private void BuildButtons()
        {
            UIKit.Clear(buttonList);
            status.text = GameManager.T("puzzle.signos.repeat", answer.Count, puzzle.Sequence.Count);
            Button first = null;
            for (int i = 0; i < SignSequencePuzzle.SignCount; i++)
            {
                int s = i;
                var b = UIKit.Button(buttonList, GameManager.T("puzzle.signos.sign" + s), () => Press(s));
                if (first == null) first = b;
            }
            UIKit.Focus(first);
        }

        private void Press(int sign)
        {
            if (watching) return;
            answer.Add(sign);
            SetSign(sign);
            status.text = GameManager.T("puzzle.signos.repeat", answer.Count, puzzle.Sequence.Count);
            if (answer.Count < puzzle.Sequence.Count) return;

            bool ok = puzzle.Submit(answer);
            if (puzzle.IsComplete) { Finish(true); return; }
            if (!ok) status.text = GameManager.T("puzzle.signos.wrong");
            StartCoroutine(ShowSequence());
        }

        private void Update()
        {
            if (!canvas.gameObject.activeSelf) return;
            if (GameInput.Instance.Cancel.WasPressedThisFrame()) Finish(false);
        }

        public void Close() { if (canvas.gameObject.activeSelf) Finish(false); }

        private void Finish(bool solved)
        {
            StopAllCoroutines();
            canvas.gameObject.SetActive(false);
            var callback = onDone;
            onDone = null;
            callback?.Invoke(solved);
        }
    }
}
