using System;
using System.Linq;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// Pantallas de historia: ficha de Cierre (ANTES / AHORA), cartel de fin de acto y viaje entre Nudos.
    /// La ficha sigue la sección 7.3 del guion: causa, objeto/lugar antes y ahora, una frase humana,
    /// y la opción de reconsiderar mientras el Cierre no esté confirmado.
    /// </summary>
    public sealed class StoryUI : MonoBehaviour
    {
        private GameManager gm;
        private Canvas canvas;
        private RectTransform panel;
        private Action onCancel;

        private void Awake()
        {
            gm = GetComponent<GameManager>();
            canvas = UIKit.CreateCanvas("StoryCanvas", 45);
            canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!canvas.gameObject.activeSelf || onCancel == null || !gm.IsSettledIn(GameMode.Story)) return;
            if (GameInput.Instance.Cancel.WasPressedThisFrame()) { var c = onCancel; Close(); c(); }
        }

        private RectTransform Open(Vector2 min, Vector2 max)
        {
            UIKit.Clear(canvas.transform);
            canvas.gameObject.SetActive(true);
            onCancel = null;
            panel = UIKit.Window(canvas.transform, "Panel", min, max);
            return panel;
        }

        private void Close()
        {
            canvas.gameObject.SetActive(false);
            onCancel = null;
        }

        // ------------------------------------------------------------------ Cierre

        public void ShowCierre(string cierreId)
        {
            var def = gm.Db.Cierres[cierreId];
            var preview = CierreService.Preview(gm.Db, gm.State, cierreId);
            var window = Open(new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.9f));

            var title = UIKit.Label(window, GameManager.T("cierre.title") + " · " + GameManager.T("era." + def.EchoEra), 44, TextAnchor.UpperCenter, UIKit.ButtonSelected);
            Place(title.rectTransform, 0.02f, 0.88f, 0.98f, 0.98f);
            var cause = UIKit.Label(window, GameManager.T(def.CauseKey), 28, TextAnchor.UpperCenter, UIKit.DimText);
            Place(cause.rectTransform, 0.05f, 0.79f, 0.95f, 0.88f);

            Column(window, GameManager.T("cierre.before"), GameManager.T(def.BeforeKey), null, 0.03f, 0.49f);
            Column(window, GameManager.T("cierre.now"), GameManager.T(def.NowKey(preview.Value)), GameManager.T(def.PersonKey(preview.Value)), 0.51f, 0.97f);

            var warning = UIKit.Label(window, GameManager.T("cierre.warning"), 24, TextAnchor.MiddleCenter, UIKit.DimText);
            Place(warning.rectTransform, 0.05f, 0.2f, 0.95f, 0.26f);
            var buttons = UIKit.Rect(window, "Buttons", new Vector2(0.25f, 0.03f), new Vector2(0.75f, 0.2f));
            var list = UIKit.VerticalList(buttons, false, 6f);
            var confirm = UIKit.Button(list, GameManager.T("cierre.confirm"), () => { Close(); gm.ConfirmCierre(cierreId); });
            UIKit.Button(list, GameManager.T("cierre.reconsider"), () => { Close(); gm.ReconsiderCierre(cierreId); });
            UIKit.Focus(confirm);
        }

        private static void Column(Transform parent, string heading, string body, string quote, float x0, float x1)
        {
            var box = UIKit.Window(parent, heading, new Vector2(x0, 0.28f), new Vector2(x1, 0.77f));
            box.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.18f, 0.95f);
            var list = UIKit.VerticalList(box, false, 10f);
            UIKit.Label(list, heading, 34, TextAnchor.MiddleCenter, UIKit.ButtonSelected);
            UIKit.Label(list, body, 28);
            if (quote != null) UIKit.Label(list, quote, 26, TextAnchor.MiddleLeft, UIKit.MpColor).fontStyle = FontStyle.Italic;
        }

        private static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ fin de acto

        public void ShowActEnd(int act, Action onDone)
        {
            UIKit.Clear(canvas.transform);
            canvas.gameObject.SetActive(true);
            onCancel = null;
            var bg = UIKit.Rect(canvas.transform, "Background", Vector2.zero, Vector2.one);
            bg.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.01f, 0.04f, 0.96f);
            var title = UIKit.Label(bg, GameManager.T("act.end", act), 72, TextAnchor.MiddleCenter, UIKit.ButtonSelected);
            Place(title.rectTransform, 0f, 0.55f, 1f, 0.75f);
            var sub = UIKit.Label(bg, GameManager.T("act." + act + ".title"), 40, TextAnchor.MiddleCenter, UIKit.DimText);
            Place(sub.rectTransform, 0f, 0.45f, 1f, 0.55f);
            var holder = UIKit.Rect(bg, "Holder", new Vector2(0.4f, 0.18f), new Vector2(0.6f, 0.28f));
            var list = UIKit.VerticalList(holder, false, 6f);
            UIKit.Focus(UIKit.Button(list, GameManager.T("battle.continue"), () => { Close(); onDone(); }));
        }

        // ------------------------------------------------------------------ viaje entre Nudos

        public void ShowNudoTravel()
        {
            gm.SetMode(GameMode.Story);
            var window = Open(new Vector2(0.3f, 0.15f), new Vector2(0.7f, 0.85f));
            onCancel = gm.ReturnToExploration;
            var title = UIKit.Label(window, GameManager.T("nudo.title"), 38, TextAnchor.UpperCenter, UIKit.ButtonSelected);
            Place(title.rectTransform, 0.02f, 0.88f, 0.98f, 0.98f);
            var holder = UIKit.Rect(window, "Holder", new Vector2(0, 0), new Vector2(1, 0.87f));
            var list = UIKit.VerticalList(holder, true, 6f);

            var state = gm.State;
            var destinations = gm.Db.Maps.Values
                .Where(m => m.IsNudo && state.Story.VisitedMaps.Contains(m.Id) && ConditionEvaluator.Evaluate(m.NudoCondition, state.ResolveVariable))
                .OrderBy(m => m.Era).ThenBy(m => m.Id);
            foreach (var map in destinations)
            {
                var m = map;
                bool here = m.Id == state.Location.MapId;
                string label = GameManager.T("era." + m.Era) + " · " + GameManager.T(m.NameKey) + (here ? "  " + GameManager.T("nudo.here") : "");
                string spawn = m.Spawns.ContainsKey("nudo") ? "nudo" : "default";
                UIKit.Button(list, label, () => { Close(); gm.TravelTo(m.Id, spawn, null); }, null, !here);
            }
            UIKit.Button(list, GameManager.T("menu.close"), () => { Close(); gm.ReturnToExploration(); });
            UIKit.FocusFirst(list);
        }
    }
}
