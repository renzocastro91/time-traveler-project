using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// Vínculo por Resonancia. El Diapasón reproduce el motivo de tres notas y el jugador lo
    /// acompaña pulsando cada nota cuando llega a la marca. El temperamento de la criatura cambia
    /// el tempo (Tímido: irregular; Orgulloso: rápido; Sereno: lento y constante; Curioso: medio).
    /// La sintonía obtenida alimenta BondingSystem, y la criatura decide si se une.
    /// </summary>
    public sealed class ResonanceMinigame : MonoBehaviour
    {
        private sealed class Note
        {
            public int Lane;
            public float HitTime;
            public bool Judged;
            public RectTransform View;
        }

        private const float TravelTime = 1.6f;
        private const float PerfectWindow = 0.08f;
        private const float GoodWindow = 0.18f;
        private const float HitZoneX = 0.12f;
        private static readonly Color[] LaneColors = { new Color(0.4f, 0.7f, 1f), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.45f) };

        private GameManager gm;
        private Canvas canvas;
        private RectTransform window, lanesArea, list;
        private Text title, status;
        private readonly List<Note> notes = new List<Note>();
        private readonly Text[] laneLabels = new Text[3];

        private void Awake()
        {
            gm = GetComponent<GameManager>();
            canvas = UIKit.CreateCanvas("ResonanceCanvas", 35);
            window = UIKit.Window(canvas.transform, "Resonance", new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.8f));
            title = UIKit.Label(window, "", 38, TextAnchor.UpperCenter, UIKit.ButtonSelected);
            title.rectTransform.anchorMin = new Vector2(0, 0.86f);
            title.rectTransform.anchorMax = new Vector2(1, 0.98f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            status = UIKit.Label(window, "", 28, TextAnchor.MiddleCenter);
            status.rectTransform.anchorMin = new Vector2(0.05f, 0.74f);
            status.rectTransform.anchorMax = new Vector2(0.95f, 0.86f);
            status.rectTransform.offsetMin = status.rectTransform.offsetMax = Vector2.zero;

            lanesArea = UIKit.Rect(window, "Lanes", new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.7f));
            for (int lane = 0; lane < 3; lane++)
            {
                float y0 = 1f - (lane + 1) / 3f, y1 = 1f - lane / 3f;
                var row = UIKit.Rect(lanesArea, "Lane" + lane, new Vector2(0, y0), new Vector2(1, y1), new Vector2(0, 4), new Vector2(0, -4));
                row.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);
                var zone = UIKit.Rect(row, "HitZone", new Vector2(HitZoneX - 0.015f, 0), new Vector2(HitZoneX + 0.015f, 1));
                zone.gameObject.AddComponent<Image>().color = LaneColors[lane];
                laneLabels[lane] = UIKit.Label(row, "", 26, TextAnchor.MiddleLeft, LaneColors[lane]);
                laneLabels[lane].rectTransform.anchorMin = new Vector2(0, 0);
                laneLabels[lane].rectTransform.anchorMax = new Vector2(HitZoneX - 0.02f, 1);
                laneLabels[lane].rectTransform.offsetMin = new Vector2(10, 0);
                laneLabels[lane].rectTransform.offsetMax = Vector2.zero;
            }

            var listHolder = UIKit.Rect(window, "Choices", new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.7f));
            list = UIKit.VerticalList(listHolder, true, 6f);
            canvas.gameObject.SetActive(false);
        }

        public void Begin(WildCreature wild, CreatureSpeciesDef species, int level, Action<bool> onDone)
        {
            gm.SetMode(GameMode.Resonance);
            canvas.gameObject.SetActive(true);
            title.text = GameManager.T("bond.prompt", GameManager.T(species.NameKey));
            ShowOfferings(species, level, onDone);
        }

        private void ShowOfferings(CreatureSpeciesDef species, int level, Action<bool> onDone)
        {
            lanesArea.gameObject.SetActive(false);
            list.parent.gameObject.SetActive(true);
            status.text = GameManager.T("bond.offering");
            UIKit.Clear(list);
            UIKit.Button(list, GameManager.T("bond.no_offering"), () => StartCoroutine(Play(species, level, null, onDone)));
            foreach (var pair in gm.State.Inventory.Items.OrderBy(p => p.Key))
            {
                ItemDef item;
                if (!gm.Db.Items.TryGetValue(pair.Key, out item) || item.Category != ItemCategory.Ofrenda) continue;
                var it = item;
                string star = it.Id == species.FavoriteOfferingId ? "  ★" : "";
                UIKit.Button(list, GameManager.T(it.NameKey) + "  ×" + pair.Value + star, () => StartCoroutine(Play(species, level, it.Id, onDone)));
            }
            UIKit.FocusFirst(list);
        }

        private IEnumerator Play(CreatureSpeciesDef species, int level, string offeringId, Action<bool> onDone)
        {
            list.parent.gameObject.SetActive(false);
            lanesArea.gameObject.SetActive(true);
            status.text = GameManager.T("bond.instructions");
            for (int i = 0; i < 3; i++) laneLabels[i].text = GameInput.Instance.NoteLabel(i);

            BuildPattern(species);
            float start = Time.unscaledTime + 1f;
            float total = 0f;
            int count = notes.Count;

            while (notes.Any(n => !n.Judged))
            {
                float now = Time.unscaledTime - start;
                foreach (var n in notes)
                {
                    float x = HitZoneX + (n.HitTime - now) / TravelTime * (1f - HitZoneX);
                    n.View.gameObject.SetActive(!n.Judged && x <= 1.02f);
                    n.View.anchorMin = new Vector2(x - 0.02f, 0.15f);
                    n.View.anchorMax = new Vector2(x + 0.02f, 0.85f);
                    if (!n.Judged && now - n.HitTime > GoodWindow) { n.Judged = true; Flash(n.Lane, false); }
                }

                for (int lane = 0; lane < 3; lane++)
                {
                    if (!GameInput.Instance.Notes[lane].WasPressedThisFrame()) continue;
                    var target = notes.Where(n => !n.Judged && n.Lane == lane && Mathf.Abs(n.HitTime - now) <= GoodWindow + 0.07f)
                                      .OrderBy(n => Mathf.Abs(n.HitTime - now)).FirstOrDefault();
                    if (target == null) { total = Mathf.Max(0f, total - 0.25f); Flash(lane, false); continue; }
                    float error = Mathf.Abs(target.HitTime - now);
                    total += error <= PerfectWindow ? 1f : error <= GoodWindow ? 0.6f : 0.2f;
                    target.Judged = true;
                    Flash(lane, true);
                }
                yield return null;
            }

            foreach (var n in notes) if (n.View != null) Destroy(n.View.gameObject);
            notes.Clear();

            float score = Mathf.Clamp01(total / count);
            var result = BondingSystem.Attempt(gm.Db, gm.State, species.Id, level, score, offeringId, new SystemRandom());
            lanesArea.gameObject.SetActive(false);
            status.text = GameManager.T("bond.score", Mathf.RoundToInt(score * 100)) + "\n" +
                          (result.Chance > 0f ? GameManager.T("bond.chance", Mathf.RoundToInt(result.Chance * 100)) + "\n" : "") +
                          gm.Loc.Format(result.ReasonKey, GameManager.T(species.NameKey));

            yield return new WaitForSecondsRealtime(0.4f);
            list.parent.gameObject.SetActive(true);
            UIKit.Clear(list);
            var button = UIKit.Button(list, GameManager.T("battle.continue"), () =>
            {
                canvas.gameObject.SetActive(false);
                gm.SetMode(GameMode.Exploration);
                onDone(result.Accepted);
            });
            UIKit.Focus(button);
        }

        /// <summary>Patrón basado en el motivo de tres notas, variado según dificultad y temperamento.</summary>
        private void BuildPattern(CreatureSpeciesDef species)
        {
            notes.Clear();
            int count = 6 + species.BondDifficulty * 2;
            float interval, jitter;
            switch (species.Temperament)
            {
                case Temperament.Timido: interval = 0.9f; jitter = 0.3f; break;
                case Temperament.Orgulloso: interval = 0.5f; jitter = 0.05f; break;
                case Temperament.Sereno: interval = 0.95f; jitter = 0f; break;
                default: interval = 0.7f; jitter = 0.12f; break;
            }
            int[] motif = { 0, 1, 2 };
            float t = 0f;
            for (int i = 0; i < count; i++)
            {
                // Las primeras tres notas son siempre el motivo; luego se varía.
                int lane = i < 3 ? motif[i] : UnityEngine.Random.Range(0, 3);
                t += interval + UnityEngine.Random.Range(-jitter, jitter);
                var view = UIKit.Rect(lanesArea.GetChild(lane), "Note", Vector2.zero, Vector2.zero);
                view.gameObject.AddComponent<Image>().color = LaneColors[lane];
                view.gameObject.SetActive(false);
                notes.Add(new Note { Lane = lane, HitTime = t, View = view });
            }
        }

        private void Flash(int lane, bool good)
        {
            var row = lanesArea.GetChild(lane).GetComponent<Image>();
            StartCoroutine(FlashRoutine(row, good ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 0f, 0f, 0.3f)));
        }

        private static IEnumerator FlashRoutine(Image image, Color color)
        {
            var original = new Color(0, 0, 0, 0.35f);
            image.color = color;
            yield return new WaitForSecondsRealtime(0.1f);
            image.color = original;
        }
    }
}
