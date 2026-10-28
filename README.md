# Las Horas que Faltan

JRPG de viajes en el tiempo para PC (Unity 6), con combate por turnos de barra activa, combos
entre personajes y con la mascota, vínculo con criaturas mediante Resonancia, evolución, equipo
completo y decisiones narrativas persistentes. Basado en el guion original *Las horas que faltan*.

- **Diseño técnico:** [`docs/DISENO_TECNICO.md`](docs/DISENO_TECNICO.md)
- **Estado:** fase 2 — Acto I jugable completo (E01–E07) con modelos provisionales.

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
| Ver el ANTES (2000) sobre el 2026 | Q (mantener) | LB / L1 (mantener) |
| Notas de Resonancia | J · K · L | X · Y · B (□ · △ · ○) |

## Qué se puede jugar

- **Nueva partida:** el **Acto I completo** (E01–E07): el aula de radio, el puzzle de la cinta, la
  estación de bombeo y el Hueco, el Puerto Niebla de 2026 con la vista ANTES/AHORA, el Archivo y la
  Fe de Erratas, el viaje a Lira, el primer Cierre, Sura y el Diapasón. El objetivo actual se ve
  siempre arriba a la derecha.
- **Modo de pruebas** (en el título): el *Nudo de pruebas*, con el grupo a nivel 8, todos los
  combates y jefes, criaturas de varias épocas para la Resonancia y cofres con equipo.
- **Start:** Grupo, Equipo (6 espacios), Técnicas y combos, Objetos, **Santuario** (compañero,
  evolución, ofrendas, postura, collar y armadura), **Cuaderno de Costuras** (objetivo, Cierres
  ANTES/AHORA, fichas de ausentes, huellas de decisiones), Guardar, Cargar y Opciones.

## Pruebas automáticas (sin Unity)

La lógica del juego (`Assets/_Project/Scripts/Core`) no depende de Unity y tiene pruebas:

```bash
dotnet test tests/TimeTraveler.Core.Tests
```

Requiere el SDK de .NET 8. Las pruebas recorren el Acto I completo y validan que los datos JSON
se referencian bien entre sí, que todos los mapas son alcanzables y que no falta ningún texto.
