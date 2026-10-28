# Las Horas que Faltan

JRPG de viajes en el tiempo para PC (Unity 6), con combate por turnos de barra activa, combos
entre personajes y con la mascota, vínculo con criaturas mediante Resonancia, evolución, equipo
completo y decisiones narrativas persistentes. Basado en el guion original *Las horas que faltan*.

- **Diseño técnico:** [`docs/DISENO_TECNICO.md`](docs/DISENO_TECNICO.md)
- **Estado:** fase 1 — núcleo de sistemas + mapa de prueba del Acto I con modelos provisionales.

## Abrir el proyecto

1. Instala **Unity Hub** y un editor **Unity 6 LTS** (6000.x) con el módulo *Windows Build Support*.
2. Unity Hub → **Add › Add project from disk** → elige esta carpeta.
   La primera apertura tarda: Unity descarga los paquetes y genera `Library/` y `ProjectSettings/`.
3. Si Unity pregunta por el **Input System** y ofrece reiniciar, acepta. Si no pregunta, ve a
   *Edit › Project Settings › Player › Other Settings › Active Input Handling* y elige
   **Input System Package (New)** o **Both**.
4. Abre cualquier escena (la vacía que crea Unity sirve) y pulsa **Play**: el juego se construye
   solo (cámara, luz, mapa de prueba, interfaz).
5. Haz un commit con los archivos `.meta` y `ProjectSettings/` que Unity generó.

## Controles

| Acción | Teclado | Mando |
|---|---|---|
| Moverse / navegar | WASD o flechas | Stick izquierdo o cruceta |
| Confirmar / hablar | Enter, Espacio o Z | A (Xbox) · ✕ (PlayStation) |
| Cancelar / volver | Escape, X o Retroceso | B · ○ |
| Menú (Start) | Tab o M | Start / Options |
| Correr | Shift | X · □ |
| Notas de Resonancia | J · K · L | X · Y · B (□ · △ · ○) |

## Qué se puede probar en el mapa de prueba

- **Sur — Instituto (2000):** escena E01 al empezar, la profesora (E02, ficha de Gabriel Mora),
  cofres y dos cachorros *Tropel*.
- **Oeste — Estación de bombeo (2000):** Marta (E03), Huecos, punto de guardado y el jefe
  **El Hueco** (usa *Especial › Testimonio* cuando su furia crece).
- **Este — Puerto Niebla (2026):** Lía se une al grupo, vendedor del reloj sin agujas, drones y el
  jefe **La Fe de Erratas** (Lía o la ficha de Gabriel anulan su Expediente).
- **Norte — Nudo de pruebas:** el **Diapasón de Arcilla** (habla con el pedestal) y criaturas de
  varias épocas para practicar la Resonancia; la cría del Cretácico demuestra que esa fauna no se
  puede vincular.
- **Start:** Grupo, Equipo (6 espacios), Técnicas y combos, Objetos, **Santuario** (cambiar de
  compañero, evolucionar, ofrendas, postura, collar y armadura), Cuaderno de Costuras, Guardar,
  Cargar y Opciones (modo Activo/Espera, velocidad, idioma).

## Pruebas automáticas (sin Unity)

La lógica del juego (`Assets/_Project/Scripts/Core`) no depende de Unity y tiene pruebas:

```bash
dotnet test tests/TimeTraveler.Core.Tests
```

Requiere el SDK de .NET 8. Las pruebas también validan que todos los datos JSON se referencian
bien entre sí y que no falta ningún texto en español.
