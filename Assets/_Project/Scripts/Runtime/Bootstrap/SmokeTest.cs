using System;
using System.Collections;
using System.Linq;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// Prueba de humo automática. Sólo se activa si el juego se lanza con el argumento -smoketest:
    ///   LasHorasQueFaltan.exe -batchmode -nographics -smoketest -logFile smoke.log
    /// Recorre todos los mapas, pelea combates (incluido un jefe), abre los menús, el viaje entre
    /// Nudos, una ficha de Cierre y los puzzles, y termina con código 0 si no hubo errores.
    /// </summary>
    public sealed class SmokeTest : MonoBehaviour
    {
        private int errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-smoketest") < 0) return;
            var go = new GameObject("SmokeTest");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTest>();
        }

        private void Awake()
        {
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
            };
        }

        private static void Log(string text) { Debug.Log("[SMOKE] " + text); }

        private static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        private static IEnumerator Until(Func<bool> condition, float seconds)
        {
            float t = 0f;
            while (!condition() && t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private IEnumerator Start()
        {
            yield return Frames(5);
            var gm = GameManager.Instance;
            Log("Arranque: modo " + gm.Mode + ", " + gm.Db.Maps.Count + " mapas");
            gm.Dialogue.AutoPlay = true;
            gm.Battle.AutoPlay = true;
            Time.timeScale = 4f;

            // Historia real: la partida nueva dispara la escena E01 al pisar la zona de inicio.
            gm.Title.Hide();
            gm.NewGame();
            yield return Until(() => gm.State.Flags.Get("E01_INSPECCIONES") >= 0 && gm.Mode == GameMode.Exploration, 20f);
            Log("Partida nueva → escena " + gm.State.Story.CurrentScene + ", mapa " + gm.State.Location.MapId);

            // Modo de pruebas: grupo de nivel 8 con todo.
            gm.NewTestGame();
            yield return Frames(10);

            foreach (var mapId in gm.Db.Maps.Keys.ToList())
            {
                bool arrived = false;
                gm.TravelTo(mapId, "default", () => arrived = true);
                yield return Until(() => arrived, 10f);
                gm.ReturnToExploration();
                yield return Until(() => gm.Mode == GameMode.Exploration, 10f);
                yield return Frames(5);
                Log("Mapa " + mapId + (arrived ? " OK" : " NO LLEGÓ") + " · objetos: " + WorldBuilder.Instance.transform.GetComponentsInChildren<Transform>().Length);
            }

            bool back = false;
            gm.TravelTo("nudo_pruebas", "default", () => back = true);
            yield return Until(() => back, 10f);
            gm.ReturnToExploration();
            yield return Frames(5);

            foreach (var encounter in new[] { "enc_estacion_huecos", "enc_2026_drones", "enc_boss_hueco" })
            {
                StoryActions.HealAll(gm.Db, gm.State);
                var leader = PartyVisuals.Instance.Leader;
                gm.StartBattle(encounter, leader.position + leader.forward * 4f, null, null);
                yield return Until(() => gm.Mode != GameMode.Battle, 240f);
                Log("Combate " + encounter + " terminado → modo " + gm.Mode);
                yield return Until(() => gm.Mode == GameMode.Exploration, 20f);
            }

            // Menú de pausa: abrir cada pestaña (sin pulsar Guardar/Cargar dentro).
            gm.Pause.Open();
            yield return Frames(3);
            var tabs = GameObject.Find("PauseCanvas").transform.Find("Tabs").GetComponentsInChildren<Button>(true);
            for (int i = 0; i < tabs.Length - 1; i++)
            {
                tabs[i].onClick.Invoke();
                yield return Frames(3);
            }
            gm.Pause.Close();
            yield return Frames(3);
            Log("Menú de pausa: " + (tabs.Length - 1) + " pestañas abiertas");

            gm.Story.ShowNudoTravel();
            yield return Frames(3);
            gm.ReturnToExploration();
            GameObject.Find("StoryCanvas").SetActive(false);
            Log("Viaje entre Nudos OK");

            // Cierre: el diálogo de decisión elige la primera opción y la ficha se confirma.
            gm.State.Story.CurrentScene = "E06";
            gm.StartDialogue("e06_guardias");
            yield return Until(() => gm.Mode == GameMode.Story, 20f);
            yield return Frames(3);
            var confirm = GameObject.Find("StoryCanvas").GetComponentsInChildren<Button>().First();
            confirm.onClick.Invoke();
            yield return Until(() => gm.Mode == GameMode.Exploration, 20f);
            Log("Cierre E06 confirmado → escena " + gm.State.Story.CurrentScene + ", cierres " + gm.State.Story.Cierres.Count);

            gm.TapePuzzle.Open(_ => { });
            yield return Frames(5);
            gm.TapePuzzle.Close();
            gm.SignPuzzle.Open(_ => { });
            yield return Frames(30);
            gm.SignPuzzle.Close();
            gm.ReturnToExploration();
            Log("Puzzles abiertos y cerrados");

            gm.SaveGame(3);
            Log("Guardado y carga: " + gm.LoadGame(3));

            Time.timeScale = 1f;
            Log("RESULTADO: " + (errors == 0 ? "OK" : errors + " ERRORES"));
            Application.Quit(errors == 0 ? 0 : 1);
        }
    }
}
