# Las Horas que Faltan

JRPG de viajes en el tiempo para PC (Unity 6), con combate por turnos de barra activa, combos
entre personajes y con la mascota, vínculo con criaturas mediante Resonancia, evolución, equipo
completo y decisiones narrativas persistentes. Basado en el guion original *Las horas que faltan*.

- **Diseño técnico:** [`docs/DISENO_TECNICO.md`](docs/DISENO_TECNICO.md)
- **Estado:** fase 2 — Acto I jugable completo (E01–E07) con modelos provisionales.

## Abrir el proyecto

1. Instala **Unity Hub** e inicia sesión con una cuenta de Unity (licencia Personal gratuita).
2. Instala el editor **Unity 6.3 LTS — 6000.3.27f1** (la versión del proyecto).
3. Unity Hub → **Add › Add project from disk** → elige esta carpeta.
4. Abre la escena `Assets/Scenes/Main.unity` y pulsa **Play**: el juego se construye solo.

El Input System ya está activado en la configuración del proyecto (teclado y mando).

### Menú «Time Traveler» del editor

- **Validar datos y textos**: revisa referencias rotas y textos faltantes.
- **Preparar proyecto**: crea la escena principal e incluye los shaders necesarios.
- **Compilar para Windows**: genera `Builds/Windows/LasHorasQueFaltan.exe`.

### Prueba de humo (el juego se juega solo)

```powershell
Builds\Windows\LasHorasQueFaltan.exe -batchmode -nographics -smoketest -logFile smoke.log
```

Recorre todos los mapas, pelea combates (incluido un jefe), abre los menús, un Cierre y los
puzzles, guarda y carga. Termina con código 0 si no hubo errores (buscar `[SMOKE]` en el log).

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
