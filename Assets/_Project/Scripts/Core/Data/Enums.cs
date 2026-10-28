namespace TimeTraveler.Core
{
    public enum EquipSlot
    {
        // Humanos
        Arma,
        Casco,
        Peto,
        Guantes,
        Botas,
        Accesorio,
        // Mascotas
        Collar,
        ArmaduraMascota
    }

    public enum ItemCategory
    {
        Consumible,
        Equipo,
        Ofrenda,
        Clave
    }

    public enum TechniqueKind
    {
        Fisica,
        Magica,
        Apoyo
    }

    public enum TargetType
    {
        EnemigoUno,
        EnemigoArea,
        EnemigosTodos,
        AliadoUno,
        AliadosTodos,
        Propio
    }

    public enum StatusType
    {
        Veneno,
        Sueno,
        Aturdido,
        Silencio,
        Ceguera,
        Confusion,
        /// <summary>Estado temático: impide usar técnicas y combos.</summary>
        Olvido,
        Prisa,
        Lentitud,
        Proteccion,
        Coraza,
        Regeneracion
    }

    /// <summary>Carácter de una criatura: cambia el ritmo de la Resonancia y su conducta.</summary>
    public enum Temperament
    {
        Timido,
        Curioso,
        Orgulloso,
        Sereno
    }

    /// <summary>Postura de la mascota en combate (actúa por su cuenta según esta postura).</summary>
    public enum PetStance
    {
        Ofensiva,
        Defensiva,
        Apoyo
    }

    public enum BattleMode
    {
        /// <summary>El tiempo corre aunque el menú esté abierto.</summary>
        Activa,
        /// <summary>El tiempo se detiene mientras se elige una acción.</summary>
        Espera
    }

    public enum Team
    {
        Jugador,
        Enemigo
    }

    public enum UnitKind
    {
        Humano,
        Mascota,
        Enemigo
    }
}
