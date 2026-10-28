using System.Collections.Generic;
using TimeTraveler.Core;
using UnityEngine;

namespace TimeTraveler
{
    /// <summary>
    /// Modelos del grupo en el mapa. El líder lleva el control; el resto (y la mascota) lo siguen
    /// en fila pisando sus huellas. En combate, BattleController toma estos mismos modelos y los
    /// coloca en formación sobre el propio mapa, sin cambiar de escena.
    /// </summary>
    public sealed class PartyVisuals : MonoBehaviour
    {
        public static PartyVisuals Instance { get; private set; }

        private const float TrailSpacing = 0.25f;
        private const int StepsPerFollower = 5;

        private readonly Dictionary<string, Transform> views = new Dictionary<string, Transform>();
        private readonly List<Transform> followers = new List<Transform>();
        private readonly List<Vector3> trail = new List<Vector3>();

        public Transform Leader { get; private set; }
        public PlayerController Player { get; private set; }

        /// <summary>Mientras es false (en combate), los seguidores no se mueven solos.</summary>
        public bool FollowEnabled = true;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PartyChanged += Rebuild;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.PartyChanged -= Rebuild;
        }

        public Transform ViewFor(string sourceId)
        {
            Transform t;
            return views.TryGetValue(sourceId, out t) ? t : null;
        }

        public void Rebuild()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State == null) return;
            Vector3 leaderPos = Leader != null ? Leader.position : Vector3.zero;
            Quaternion leaderRot = Leader != null ? Leader.rotation : Quaternion.identity;

            foreach (var v in views.Values) if (v != null) Destroy(v.gameObject);
            views.Clear();
            followers.Clear();

            var state = gm.State;
            for (int i = 0; i < state.Party.Active.Count; i++)
            {
                string id = state.Party.Active[i];
                var model = Placeholders.Character(id).transform;
                model.SetParent(transform, false);
                views[id] = model;
                if (i == 0)
                {
                    Leader = model;
                    var cc = model.gameObject.AddComponent<CharacterController>();
                    cc.center = new Vector3(0, 0.9f, 0);
                    cc.height = 1.8f;
                    cc.radius = 0.35f;
                    Player = model.gameObject.AddComponent<PlayerController>();
                }
                else followers.Add(model);
            }

            var pet = state.ActivePet;
            if (pet != null)
            {
                var model = Placeholders.Creature(gm.Db.Species(pet.SpeciesId)).transform;
                model.SetParent(transform, false);
                views[pet.Uid] = model;
                followers.Add(model);
            }

            PlaceLeader(leaderPos, leaderRot);
            var rig = CameraRig.Instance;
            if (rig != null) rig.Follow(Leader);
        }

        public void PlaceLeader(Vector3 position, Quaternion? rotation = null)
        {
            if (Leader == null) return;
            var cc = Leader.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            Leader.position = position;
            if (rotation.HasValue) Leader.rotation = rotation.Value;
            if (cc != null) cc.enabled = true;

            trail.Clear();
            Vector3 back = -Leader.forward;
            for (int i = 0; i < followers.Count * StepsPerFollower + 1; i++) trail.Add(position + back * TrailSpacing * i);
            for (int i = 0; i < followers.Count; i++)
            {
                followers[i].position = trail[(i + 1) * StepsPerFollower];
                followers[i].rotation = Leader.rotation;
            }
        }

        private void LateUpdate()
        {
            if (Leader == null || !FollowEnabled) return;

            if (trail.Count == 0 || Vector3.Distance(trail[0], Leader.position) >= TrailSpacing)
            {
                trail.Insert(0, Leader.position);
                int max = followers.Count * StepsPerFollower + 2;
                if (trail.Count > max) trail.RemoveRange(max, trail.Count - max);
            }

            for (int i = 0; i < followers.Count; i++)
            {
                int index = Mathf.Min((i + 1) * StepsPerFollower, trail.Count - 1);
                var f = followers[i];
                Vector3 target = trail[index];
                Vector3 delta = target - f.position;
                f.position = Vector3.Lerp(f.position, target, 12f * Time.deltaTime);
                delta.y = 0;
                if (delta.sqrMagnitude > 0.0004f) f.rotation = Quaternion.Slerp(f.rotation, Quaternion.LookRotation(delta), 10f * Time.deltaTime);
            }
        }

        /// <summary>Tras un combate, los seguidores vuelven a su fila detrás del líder.</summary>
        public void RegroupAfterBattle()
        {
            FollowEnabled = true;
            PlaceLeader(Leader.position, Leader.rotation);
        }
    }
}
