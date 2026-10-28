using TimeTraveler.Core;
using UnityEngine;

namespace TimeTraveler
{
    /// <summary>Algo con lo que el líder puede interactuar pulsando Confirmar cerca.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public string PromptKey = "interact.talk";
        public abstract void Interact(PlayerController player);

        protected static GameManager Gm { get { return GameManager.Instance; } }

        protected void FacePlayer(PlayerController player)
        {
            Vector3 d = player.transform.position - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }
    }

    public sealed class NpcInteractable : Interactable
    {
        public string DialogueId;

        public override void Interact(PlayerController player)
        {
            FacePlayer(player);
            Gm.StartDialogue(DialogueId);
        }
    }

    /// <summary>Cofre de un solo uso; queda registrado en la partida por su UniqueId.</summary>
    public sealed class ChestInteractable : Interactable
    {
        public string UniqueId;
        public string ItemId;
        public int Quantity = 1;

        private bool Opened { get { return Gm.State.Story.ClearedEncounters.Contains(UniqueId); } }

        public override void Interact(PlayerController player)
        {
            if (Opened)
            {
                Gm.Hud.Toast(GameManager.T("interact.chest_empty"));
                return;
            }
            Gm.State.Inventory.Add(ItemId, Quantity);
            Gm.State.Story.ClearedEncounters.Add(UniqueId);
            Gm.Hud.Toast(GameManager.T("interact.chest_got", GameManager.T(Gm.Db.Item(ItemId).NameKey), Quantity));
            Refresh();
        }

        public void Refresh()
        {
            var lid = transform.Find("Lid");
            if (lid != null) lid.localRotation = Opened ? Quaternion.Euler(-70, 0, 0) : Quaternion.identity;
        }
    }

    /// <summary>Punto de guardado: restaura al grupo y abre el menú de guardar.</summary>
    public sealed class SavePoint : Interactable
    {
        public override void Interact(PlayerController player)
        {
            var gm = Gm;
            foreach (var c in gm.State.Characters.Values)
            {
                var stats = StatCalculator.ForCharacter(gm.Db, c);
                c.Hp = stats.Get(StatType.MaxHp);
                c.Mp = stats.Get(StatType.MaxMp);
            }
            foreach (var cr in gm.State.Sanctuary.Creatures)
            {
                var stats = StatCalculator.ForCreature(gm.Db, cr);
                cr.Hp = stats.Get(StatType.MaxHp);
                cr.Mp = stats.Get(StatType.MaxMp);
            }
            gm.Pause.OpenSaveScreen();
        }
    }

    /// <summary>
    /// Criatura salvaje visible en el mapa. No se combate: se interactúa con ella y, si el grupo
    /// tiene el Diapasón de Arcilla, se inicia la Resonancia. Deambula cerca de su punto de origen.
    /// </summary>
    public sealed class WildCreature : Interactable
    {
        public string UniqueId;
        public string SpeciesId;
        public int Level = 3;
        public const double RefuseCooldownSeconds = 90;

        private Vector3 home;
        private Vector3 wanderTarget;
        private float wanderTimer;

        private void Start()
        {
            home = transform.position;
            wanderTarget = home;
        }

        private void Update()
        {
            if (Gm == null || Gm.Mode != GameMode.Exploration) return;
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                wanderTimer = Random.Range(2f, 5f);
                var offset = Random.insideUnitCircle * 2.5f;
                wanderTarget = home + new Vector3(offset.x, 0, offset.y);
            }
            Vector3 d = wanderTarget - transform.position;
            d.y = 0;
            if (d.magnitude > 0.1f)
            {
                transform.position += d.normalized * 1.2f * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 5f * Time.deltaTime);
            }
        }

        public override void Interact(PlayerController player)
        {
            var gm = Gm;
            var species = gm.Db.Species(SpeciesId);
            string name = GameManager.T(species.NameKey);

            if (!species.Bondable)
            {
                gm.Hud.Toast(GameManager.T("bond.reason.not_bondable"));
                return;
            }
            if (gm.State.Inventory.Count("diapason_de_arcilla") <= 0)
            {
                gm.Hud.Toast(GameManager.T("bond.need_diapason"));
                return;
            }
            double until;
            if (gm.State.Story.BondCooldowns.TryGetValue(UniqueId, out until) && gm.State.PlayTimeSeconds < until)
            {
                gm.Hud.Toast(GameManager.T("bond.cooldown", name));
                return;
            }

            FacePlayer(player);
            gm.Resonance.Begin(this, species, Level, accepted =>
            {
                if (accepted)
                {
                    gm.State.Story.ClearedEncounters.Add(UniqueId);
                    gm.NotifyPartyChanged();
                    Destroy(gameObject);
                }
                else
                {
                    gm.State.Story.BondCooldowns[UniqueId] = gm.State.PlayTimeSeconds + RefuseCooldownSeconds;
                }
            });
        }
    }

    /// <summary>
    /// Grupo de enemigos visible en el mapa (no hay encuentros aleatorios). Al tocarlo, el combate
    /// empieza ahí mismo. Si el grupo huye, el enemigo queda inactivo unos segundos.
    /// </summary>
    public sealed class MapEnemy : MonoBehaviour
    {
        public string UniqueId;
        public string EncounterId;
        public float ChaseRadius = 5f;
        public float ChaseSpeed = 2.4f;

        private float cooldown;
        private Vector3 home;

        private void Start()
        {
            home = transform.position;
        }

        public void StartCooldown()
        {
            cooldown = 4f;
            transform.position = home;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Mode != GameMode.Exploration) return;
            if (cooldown > 0f) { cooldown -= Time.deltaTime; return; }

            var leader = PartyVisuals.Instance != null ? PartyVisuals.Instance.Leader : null;
            if (leader == null) return;
            Vector3 d = leader.position - transform.position;
            d.y = 0;
            if (d.magnitude < ChaseRadius && Vector3.Distance(home, transform.position) < ChaseRadius * 1.5f)
            {
                transform.position += d.normalized * ChaseSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 6f * Time.deltaTime);
                if (d.magnitude < 1.3f) Engage(leader);
            }
        }

        public void Engage(Transform leader)
        {
            if (cooldown > 0f) return;
            var gm = GameManager.Instance;
            if (gm.Mode != GameMode.Exploration) return;
            gm.StartBattle(EncounterId, transform.position, this);
        }
    }
}
