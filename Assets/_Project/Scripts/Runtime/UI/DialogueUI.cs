using System;
using System.Collections;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// Caja de diálogo con texto que aparece letra a letra, nombre del hablante y opciones.
    /// Confirmar completa la línea o avanza; las opciones se eligen con el mando como botones.
    /// </summary>
    public sealed class DialogueUI : MonoBehaviour
    {
        public float CharactersPerSecond = 55f;
        /// <summary>Pruebas automáticas: avanza solo y elige siempre la primera opción visible.</summary>
        public bool AutoPlay;

        private Canvas canvas;
        private Text speaker, body;
        private RectTransform speakerWindow, choices;
        private DialogueRunner runner;
        private Action onFinished;
        private Coroutine typing;
        private bool lineComplete;
        private int lastChangeFrame;

        private void Awake()
        {
            canvas = UIKit.CreateCanvas("DialogueCanvas", 30);
            var window = UIKit.Window(canvas.transform, "Dialogue", new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.3f));
            body = UIKit.Stretch(UIKit.Label(window, "", 34, TextAnchor.UpperLeft), 26f);
            speakerWindow = UIKit.Window(canvas.transform, "Speaker", new Vector2(0.1f, 0.31f), new Vector2(0.3f, 0.37f));
            speaker = UIKit.Stretch(UIKit.Label(speakerWindow, "", 30, TextAnchor.MiddleCenter, UIKit.ButtonSelected), 6f);
            var choiceWindow = UIKit.Window(canvas.transform, "Choices", new Vector2(0.5f, 0.32f), new Vector2(0.9f, 0.62f));
            choices = UIKit.VerticalList(choiceWindow, false, 6f);
            canvas.gameObject.SetActive(false);
        }

        public void Play(string dialogueId, Action<string> onAction, Action finished)
        {
            var gm = GameManager.Instance;
            onFinished = finished;
            runner = new DialogueRunner(gm.Db, gm.State, onAction);
            canvas.gameObject.SetActive(true);
            runner.Start(dialogueId);
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            lastChangeFrame = Time.frameCount;
            UIKit.Clear(choices);
            choices.parent.gameObject.SetActive(false);

            if (runner.IsFinished)
            {
                canvas.gameObject.SetActive(false);
                var callback = onFinished;
                onFinished = null;
                callback?.Invoke();
                return;
            }

            var node = runner.Current;
            bool hasSpeaker = !string.IsNullOrEmpty(node.SpeakerKey);
            speakerWindow.gameObject.SetActive(hasSpeaker);
            speaker.text = hasSpeaker ? GameManager.T(node.SpeakerKey) : "";
            body.fontStyle = hasSpeaker ? FontStyle.Normal : FontStyle.Italic;
            if (typing != null) StopCoroutine(typing);
            typing = StartCoroutine(Type(GameManager.T(node.TextKey)));
        }

        private IEnumerator Type(string text)
        {
            lineComplete = false;
            body.text = "";
            for (int i = 1; i <= text.Length; i++)
            {
                body.text = text.Substring(0, i);
                yield return new WaitForSecondsRealtime(1f / CharactersPerSecond);
            }
            CompleteLine(text);
        }

        private void CompleteLine(string text)
        {
            if (typing != null) StopCoroutine(typing);
            typing = null;
            body.text = text;
            lineComplete = true;
            var visible = runner.VisibleChoices();
            if (visible.Count == 0) return;

            choices.parent.gameObject.SetActive(true);
            for (int i = 0; i < visible.Count; i++)
            {
                int index = i;
                UIKit.Button(choices, GameManager.T(visible[i].TextKey), () =>
                {
                    runner.Choose(index);
                    ShowCurrent();
                });
            }
            UIKit.FocusFirst(choices);
        }

        private void Update()
        {
            if (runner == null || !canvas.gameObject.activeSelf || Time.frameCount == lastChangeFrame) return;
            if (AutoPlay)
            {
                if (!lineComplete) CompleteLine(GameManager.T(runner.Current.TextKey));
                if (runner.VisibleChoices().Count > 0) runner.Choose(0);
                else runner.Advance();
                ShowCurrent();
                return;
            }
            if (!GameInput.Instance.Confirm.WasPressedThisFrame()) return;

            if (!lineComplete)
            {
                CompleteLine(GameManager.T(runner.Current.TextKey));
                lastChangeFrame = Time.frameCount;
                return;
            }
            if (runner.VisibleChoices().Count > 0) return; // las opciones se eligen con los botones
            runner.Advance();
            ShowCurrent();
        }
    }
}
