using System;
using System.Collections.Generic;

namespace TimeTraveler.Core
{
    /// <summary>
    /// Grupo: hasta 3 humanos activos + 1 mascota de grupo. Violeta y Ángel son fijos salvo en
    /// secuencias de separación (el guion lo exige), así que se marcan como bloqueados.
    /// </summary>
    public sealed class PartyState
    {
        public const int MaxActive = 3;

        public List<string> Active = new List<string>();
        public List<string> Locked = new List<string>();
        public string ActivePetUid;

        public bool IsActive(string id) { return Active.Contains(id); }

        /// <summary>Intercambia un miembro activo por otro reclutado en reserva.</summary>
        public bool Swap(string activeId, string reserveId, Dictionary<string, CharacterState> characters)
        {
            if (!Active.Contains(activeId) || Active.Contains(reserveId)) return false;
            if (Locked.Contains(activeId)) return false;
            CharacterState reserve;
            if (!characters.TryGetValue(reserveId, out reserve) || !reserve.Recruited) return false;
            Active[Active.IndexOf(activeId)] = reserveId;
            return true;
        }

        public bool AddToActive(string id)
        {
            if (Active.Count >= MaxActive || Active.Contains(id)) return false;
            Active.Add(id);
            return true;
        }
    }

    public sealed class Inventory
    {
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public int Money;

        public int Count(string itemId)
        {
            int n;
            return itemId != null && Items.TryGetValue(itemId, out n) ? n : 0;
        }

        public void Add(string itemId, int amount = 1)
        {
            if (amount <= 0) return;
            Items[itemId] = Math.Min(99, Count(itemId) + amount);
        }

        public bool Remove(string itemId, int amount = 1)
        {
            int current = Count(itemId);
            if (amount <= 0 || current < amount) return false;
            if (current == amount) Items.Remove(itemId);
            else Items[itemId] = current - amount;
            return true;
        }
    }

    /// <summary>Uso de consumibles desde el menú (fuera de combate). Los estados alterados no persisten fuera de combate.</summary>
    public static class FieldItemService
    {
        /// <summary>
        /// Usa un consumible sobre un personaje (characterId) o una criatura (creatureUid).
        /// Devuelve false si no tiene efecto (por ejemplo, curar a alguien con la vida llena).
        /// </summary>
        public static bool Use(GameDatabase db, GameState state, string itemId, string characterId, string creatureUid)
        {
            var item = db.Item(itemId);
            if (!item.UsableInField || item.Effect == null || state.Inventory.Count(itemId) <= 0) return false;

            var targets = new List<Func<bool>>();
            if (item.Effect.AllAllies)
            {
                foreach (var id in state.Party.Active) { string c = id; targets.Add(() => ApplyToCharacter(db, state.Characters[c], item.Effect)); }
                if (state.ActivePet != null) targets.Add(() => ApplyToCreature(db, state.ActivePet, item.Effect));
            }
            else if (characterId != null) targets.Add(() => ApplyToCharacter(db, state.Characters[characterId], item.Effect));
            else if (creatureUid != null && state.Sanctuary.Find(creatureUid) != null) targets.Add(() => ApplyToCreature(db, state.Sanctuary.Find(creatureUid), item.Effect));

            bool any = false;
            foreach (var apply in targets) any |= apply();
            if (any) state.Inventory.Remove(itemId);
            return any;
        }

        private static bool ApplyToCharacter(GameDatabase db, CharacterState c, ConsumableEffect effect)
        {
            var stats = StatCalculator.ForCharacter(db, c);
            return Apply(ref c.Hp, ref c.Mp, stats.Get(StatType.MaxHp), stats.Get(StatType.MaxMp), effect);
        }

        private static bool ApplyToCreature(GameDatabase db, CreatureInstance c, ConsumableEffect effect)
        {
            var stats = StatCalculator.ForCreature(db, c);
            return Apply(ref c.Hp, ref c.Mp, stats.Get(StatType.MaxHp), stats.Get(StatType.MaxMp), effect);
        }

        private static bool Apply(ref int hp, ref int mp, int maxHp, int maxMp, ConsumableEffect effect)
        {
            int oldHp = hp, oldMp = mp;
            if (hp <= 0)
            {
                if (effect.Revive <= 0f) return false;
                hp = Math.Max(1, (int)(maxHp * effect.Revive));
            }
            else
            {
                hp = Math.Min(maxHp, hp + effect.HealHp + (int)(maxHp * effect.HealHpPercent));
            }
            mp = Math.Min(maxMp, mp + effect.HealMp);
            return hp != oldHp || mp != oldMp;
        }
    }

    public static class EquipmentRules
    {
        public static readonly EquipSlot[] HumanSlots = { EquipSlot.Arma, EquipSlot.Casco, EquipSlot.Peto, EquipSlot.Guantes, EquipSlot.Botas, EquipSlot.Accesorio };
        public static readonly EquipSlot[] PetSlots = { EquipSlot.Collar, EquipSlot.ArmaduraMascota };

        public static bool CanEquip(ItemDef item, EquipSlot slot, CharacterDef character)
        {
            if (item.Category != ItemCategory.Equipo || item.Slot != slot || item.IsPetEquipment) return false;
            if (item.AllowedActors.Count > 0 && !item.AllowedActors.Contains(character.Id)) return false;
            if (slot == EquipSlot.Arma && item.WeaponType != character.WeaponType) return false;
            return true;
        }

        public static bool CanEquipPet(ItemDef item, EquipSlot slot)
        {
            if (item.Category != ItemCategory.Equipo || item.Slot != slot || !item.IsPetEquipment) return false;
            return item.AllowedActors.Count == 0 || item.AllowedActors.Contains("mascota");
        }

        /// <summary>
        /// Equipa un objeto del inventario. Lo que estaba en el espacio vuelve al inventario.
        /// itemId null = desequipar. El arma no se puede quitar (siempre hay un arma).
        /// </summary>
        public static bool Equip(GameDatabase db, Inventory inventory, Dictionary<EquipSlot, string> equipment, EquipSlot slot, string itemId, Func<ItemDef, bool> canEquip)
        {
            if (itemId == null)
            {
                if (slot == EquipSlot.Arma) return false;
                string old;
                if (equipment.TryGetValue(slot, out old) && old != null) inventory.Add(old);
                equipment.Remove(slot);
                return true;
            }

            var item = db.Item(itemId);
            if (!canEquip(item) || inventory.Count(itemId) <= 0) return false;
            inventory.Remove(itemId);
            string previous;
            if (equipment.TryGetValue(slot, out previous) && previous != null) inventory.Add(previous);
            equipment[slot] = itemId;
            return true;
        }

        public static bool EquipCharacter(GameDatabase db, GameState state, string characterId, EquipSlot slot, string itemId)
        {
            var character = state.Characters[characterId];
            var def = db.Character(characterId);
            bool ok = Equip(db, state.Inventory, character.Equipment, slot, itemId, item => CanEquip(item, slot, def));
            if (ok) ClampVitals(db, character);
            return ok;
        }

        public static bool EquipCreature(GameDatabase db, GameState state, CreatureInstance creature, EquipSlot slot, string itemId)
        {
            return Equip(db, state.Inventory, creature.Equipment, slot, itemId, item => CanEquipPet(item, slot));
        }

        private static void ClampVitals(GameDatabase db, CharacterState character)
        {
            var stats = StatCalculator.ForCharacter(db, character);
            character.Hp = Math.Min(character.Hp, stats.Get(StatType.MaxHp));
            character.Mp = Math.Min(character.Mp, stats.Get(StatType.MaxMp));
        }
    }
}
