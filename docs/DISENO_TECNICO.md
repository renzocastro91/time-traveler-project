# Las Horas que Faltan — Documento de diseño técnico

Versión 0.2 · 28/10/2026 · Fase 2: Acto I jugable (E01–E07)

Este documento describe **cómo está construido el juego**, no la historia: la historia vive en
el guion (`Las_horas_que_faltan_Guion_Videojuego.pdf`, fuera del repositorio) y se respeta tal cual.

---

## 1. Decisiones de plataforma

| Tema | Decisión | Motivo |
|---|---|---|
| Motor | **Unity 6 (LTS)**, C# | 3D con buen rendimiento en PC, soporte de mando maduro, gran ecosistema de assets |
| Plataforma | Windows (PC) | Pedido del proyecto; Unity permite portar más tarde |
| Render | Built‑in en la fase 1 → **URP** cuando llegue el arte | URP permite el aspecto "HD‑2D" (profundidad de campo, bloom, iluminación cálida) |
| Entrada | **Input System** (teclado + cualquier mando XInput/DualShock/DualSense) | Mapeo unificado, cambio automático de iconos |
| Interfaz | uGUI construida en código (fase 1) | Navegable con mando sin configurar escenas; se puede migrar a prefabs con arte |
| Datos | **JSON** en `StreamingAssets/Data` | Editables sin recompilar, versionables, validados automáticamente |
| Textos | `StreamingAssets/Localization/{idioma}.json` | Español por defecto, listo para traducir |

### Estética objetivo
Cámara **cenital inclinada (~52°) con campo de visión estrecho (28°)**: aplana la perspectiva y
recuerda a los JRPG de 16 bits, pero con escenarios 3D reales. Personajes en 3D estilizado de
proporciones *chibi* (cabeza grande, ~3 cabezas de alto), materiales con sombreado suave, contornos
finos y un posprocesado con profundidad de campo inclinada (*tilt‑shift*) y bloom suave.

---

## 2. Arquitectura

```
Assets/_Project/Scripts/
├── Core/        (TimeTraveler.Core — C# puro, SIN dependencias de Unity)
│   ├── Common/      Elementos, estadísticas, JSON, azar, localización
│   ├── Data/        Definiciones (personajes, técnicas, combos, objetos, criaturas, enemigos…)
│   ├── Progression/ Niveles, experiencia, equipo, inventario, grupo, uso de objetos
│   ├── Creatures/   Criaturas, Santuario, evolución, Vínculo por Resonancia
│   ├── Combat/      Motor ATB, fórmulas, IA, combos, reglas dramáticas de jefes
│   ├── Narrative/   Banderas, condiciones, diálogos, escenas y objetivos, Cierres, puzzles, acciones
│   └── Save/        Estado de partida y guardado
├── Runtime/     (TimeTraveler.Runtime — capa Unity)
│   ├── Bootstrap/   GameManager: arranque, modos de juego, acciones de diálogo
│   ├── Input/       GameInput: teclado + mando
│   ├── Exploration/ Jugador, grupo en fila, cámara, interactuables, enemigos visibles
│   ├── Battle/      BattleController: combate sobre el mapa, menús, animación de eventos
│   ├── Resonance/   Minijuego de vínculo
│   ├── Puzzles/     Cinta (E02) y alfabeto acústico (E07)
│   ├── UI/          Kit de interfaz, HUD, diálogos, menú de pausa, título
│   └── World/       WorldBuilder (construye mapas desde datos) y modelos provisionales
└── Editor/      Menú «Time Traveler › Validar datos y textos»
tests/TimeTraveler.Core.Tests/   Pruebas xUnit del Core (se ejecutan sin Unity)
tools/content/                   Generadores de contenido (JSON de datos y textos)
```

**Regla de oro:** toda decisión de reglas (daño, quién actúa, si una criatura acepta, qué cambia
una elección) vive en `Core`. Unity sólo **muestra** y **pide órdenes**. Así:
- el combate, la progresión y **el Acto I completo** se prueban automáticamente (`dotnet test`, 40 pruebas);
- cambiar el arte o la interfaz nunca rompe las reglas;
- el equilibrio se ajusta en datos y en `DamageCalculator`, en un solo lugar.

### Flujo de modos (`GameManager.Mode`)
`Title → Exploration ⇄ Dialogue / Menu / Battle / Resonance / Story`, y `GameOver` tras una derrota.
`Story` cubre puzzles, la ficha de Cierre, los viajes con fundido y el cartel de fin de acto.
Cada modo bloquea la entrada de los demás; el frame en que cambia el modo se ignora para que una
misma pulsación no haga dos cosas.

---

## 3. Combate

### 3.1 Barra de tiempo activa (ATB)
- Cada unidad llena su barra a `(10 + Velocidad) × 1,6` puntos/s (Prisa ×1,5, Lentitud ×0,6).
- Al llenarse: los **humanos** esperan una orden; los **enemigos** actúan solos; la **mascota**
  espera 2 s (por si el jugador quiere usarla en un combo) y después actúa por su cuenta.
- **Modo Espera** (por defecto): el tiempo se detiene mientras hay un menú abierto.
  **Modo Activo**: el tiempo sigue corriendo. Se cambia en *Opciones*.
- El combate ocurre **sobre el propio mapa**: el grupo se coloca en formación frente al enemigo,
  sin pantalla de carga. Los enemigos se ven en el mapa (no hay encuentros aleatorios).
- Grupo en combate: **3 humanos + 1 mascota de grupo**. Violeta y Ángel son fijos, como pide
  el guion, salvo en las secuencias de separación.

### 3.2 Órdenes
Atacar · Técnica · Combo · Objeto · Especial (sólo contra jefes con regla dramática) · Defender ·
Huir. Con varios personajes listos, **Cancelar en el menú raíz pasa al siguiente**.

### 3.3 Fórmulas (`Core/Combat/DamageCalculator.cs`)
```
daño = (Potencia/10) · Ataque · 2 · 100 / (100 + Defensa · 2)
       × elemento × crítico(1,75) × variación(0,9–1,1)
       × Defender(0,5) × Protección/Coraza(0,67)
acierto físico = 0,95 + (Precisión − Evasión)·0,01   [30 %–100 %], −40 % con Ceguera
crítico físico = 4 % + Suerte·0,2 %
curación = (Potencia/10) · Magia · 1,5 × variación
combo: estadística de ataque = Σ(estadística de cada participante × 0,6)
```
Potencia 10 = ataque básico. Física usa Fuerza/Defensa; mágica, Magia/Defensa mágica.

### 3.4 Elementos
Cada humano tiene un elemento y técnicas mágicas de ese elemento.

| Personaje | Elemento | Arma | Rol |
|---|---|---|---|
| Violeta | Viento (sonido) | Cable de micrófono | Apoyo, resonancia, daño en área |
| Ángel | Rayo | Espada | Daño físico, protección |
| Sura | Tierra | Estilo de escriba | Escudos de grupo, control |
| Teo | Fuego | Martillo tipográfico | Daño físico fuerte, venenos |
| Lía | Luz | Linterna táctica | Estados (ceguera, silencio), curación de grupo |
| Anouk | Agua | Bisturí | Curación, reanimación |
| Imen | Metal | Guantelete | Tanque, buffs de grupo, limpia estados |
| Anida | Vacío | Orbe de coral | Magia pesada, confusión, «Olvido» |

Ciclo de afinidades: **Fuego › Metal › Viento › Tierra › Rayo › Agua › Fuego**; Luz y Vacío son
fuertes entre sí. Fuerte ×1,5, débil ×0,5, mismo elemento ×0,75. El equipo puede añadir
resistencias (p. ej. el delantal de Teo: Fuego ×0,5); un valor negativo significa absorción.

### 3.5 Técnicas y combos
- 86 técnicas: 6–7 por humano (**físicas y mágicas para todos**), de criaturas y de enemigos.
- Se aprenden **por nivel** (tabla `learnset` de cada personaje).
- **Combos** (14 en la fase 1): dobles, triples y **con la mascota**. Requisitos: todos los
  participantes con la barra llena, vivos, sin Olvido, conociendo la técnica requerida y con PM.
  Cada uno paga el PM de su técnica. Ejemplos:
  - *Estática del aula* (Violeta Eco + Ángel Pulso) — rayo a todos.
  - *Carga conjunta* (Ángel Tajo + cualquier mascota).
  - *Ventisca compañera* (Violeta + mascota de **Viento**).
  - *Primer trato* (Violeta + Ángel + Sura).
  - *Las horas que faltan* (Violeta + Ángel + mascota) — el combo emblemático.

### 3.6 Estados
Veneno, Sueño (despierta al recibir daño), Aturdido, Silencio (sin magia), Ceguera, Confusión
(golpea a cualquiera), **Olvido** (temático: sin técnicas ni combos), Prisa, Lentitud, Protección,
Coraza y Regeneración. Duran turnos del afectado.

### 3.7 Reglas dramáticas de jefes (sección 7.4 del guion)
Los jefes se combaten **de forma convencional**, pero cada uno añade una regla basada en su
función dramática y una orden **Especial**. Implementado en `Core/Combat/Rules/`:

| Jefe | Regla | Orden especial |
|---|---|---|
| **El Hueco** (E03) | Cada golpe aumenta su Furia (+15 % de daño, máx. 5). **No cae hasta unir los tres objetos de la galería** («no se lo derrota golpeando») | *Testimonio* (con Furia ≥ 2 y un objeto aún sin unir): calma la Furia y le quita el 25 % de la vida ignorando defensas |
| **La Fe de Erratas** (E05) | Inmune mientras su Expediente esté activo | *Testimonio no registrado* (Lía o quien lleve la ficha de Gabriel Mora): lo anula durante 3 de sus turnos |

Para agregar un jefe: crear una clase que herede de `DramaticRuleBase`, registrarla en
`DramaticRuleRegistry` y poner su id en `encounters.json`. Pendientes del guion: Nombres Sellados,
Crecida Ciega, Cartógrafo Ciego, El Contador, La Compensación, Versión Mejorada, Pronósticos
Ineludibles y La Custodia.

---

## 4. Criaturas: Vínculo, Santuario y evolución

### 4.1 Vínculo por Resonancia (no hay captura)
- Las criaturas salvajes se ven en el mapa y **no se combaten para obtenerlas**.
- Con el **Diapasón de Arcilla** (objeto del guion), el jugador inicia la *Resonancia*: un
  minijuego rítmico con el **motivo de tres notas** de la obra. Se puede ofrecer algo antes.
- El temperamento cambia el tempo: Tímido (irregular), Curioso (medio), Orgulloso (rápido),
  Sereno (lento y constante). La dificultad de la especie (1–5) define cuántas notas hay.
- `probabilidad = sintonía^1,5 × (1,25 − 0,15·dificultad) + ofrenda (+15 % si es la favorita)`,
  con un tope del 95 %. Con sintonía inferior al 40 % no hay vínculo. **La criatura decide**: si
  rechaza, se aleja 90 s.
- Algunas especies exigen condiciones narrativas (p. ej. *Promesín*: PLURALIDAD ≥ 3).
- **Fauna del Cretácico: nunca vinculable** (el guion la define como señal de campo).

### 4.2 Santuario
Guarda hasta 12 criaturas (ampliable) y vive en el Museo, la base móvil del guion. Desde
**Start › Santuario**: elegir compañero, evolucionar, dar ofrendas (+afinidad), cambiar la postura
(Ofensiva/Defensiva/Apoyo), equipar **Collar** y **Armadura**, y liberar.

### 4.3 Evolución
Requisitos combinables: nivel, **afinidad** (sube combatiendo juntos: +1 por combate, +2 por combo),
objeto, época o bandera narrativa. Ejemplo: *Nutria de mareas → Marejada* sólo evoluciona en las
aguas de Talam (1542) con una Escama de marea. Evolucionar conserva nivel, afinidad y equipo, y
enseña las técnicas nuevas. Es siempre una decisión del jugador (nunca automática).

### 4.4 Familias (32 especies)
| Familia | Época | Elemento | Etapas |
|---|---|---|---|
| Callejero | 2000 | Viento | Tropel → Ventolero → Galerno |
| Galápago | 330 a. C. | Tierra | Galápago del olivar → Caparazón de ánfora → Atlas de piedra |
| Nutria | 1542 | Agua | Nutrilla → Nutria de mareas → Marejada |
| Fragata | 1542 | Viento | Pichón de fragata → Fragata tormenta |
| Salamandra | 1891 | Fuego | Ascuela → Salamandra de horno → Salamandra caldera |
| Grillo de latón | 1891 | Metal | Grillo de latón → Cigarra de vapor → Mantis locomotora |
| Murciélago | 2026 | Rayo | Pipistrelo → Murciélago estático → Tormentala |
| Medusa | 2140 | Luz | Lumedusa → Faro medusa |
| Cangrejo | 2140 | Metal | Pinzaro → Bastión de dique |
| Escarabajo | 2749 | Rayo | Escaralambre → Escarabajo catedral |
| Polilla | 8800 | Vacío | Nictela → Polilla de baja luz → Mariposa del último atardecer |
| Promesa | Otras Orillas | Luz | Promesín → Juramento |
| (Cría emplumada) | Cretácico | — | No vinculable |

---

## 5. Equipo y progresión
- **Humanos — 6 espacios:** Arma (según su tipo de arma), Casco, Peto, Guantes, Botas y
  **Objeto mágico** (accesorio que sube una estadística o da inmunidades).
- **Mascotas — 2 espacios:** Collar (golpes/magia) y Armadura.
- Niveles 1–99: experiencia total para el nivel N = `12·(N−1)^2,35`. Estadísticas =
  base + crecimiento·(nivel−1) + equipo.
- Los reclutados en reserva reciben el 50 % de la experiencia (evita tener que repetir combates).
- Tras una victoria, quien cayó vuelve con 1 de vida (toque moderno).

---

## 6. Narrativa y decisiones
- Las 21 banderas del guion (TESTIMONIOS, CONSENTIMIENTO, PLURALIDAD, DEUDA, CONFIANZA_VA,
  HUELGA_1891, MAPA_1542…) están en `flags.json` con sus rangos y se respetan siempre.
- **Diálogos** en `Data/dialogues/*.json`: nodos con hablante, texto, condición, opciones con
  efectos y acciones (`give:`, `recruit:`, `battle:`, `scene:`, `money:`, `dialogue:`).
- **Condiciones**: `CONFIANZA_VA>=2 && !PACTO_SURA || item.ficha_gabriel` (se admiten `money`,
  `recruited.x` y `scene.E05`).
- El **Cuaderno de Costuras** del menú muestra las huellas de las decisiones.
- **Escenas y objetivos** (`scenes.json`): cada escena del guion tiene objetivos con condición;
  el HUD muestra el primero sin cumplir. Al cumplirlos todos se reproduce su diálogo automático.
- **Acciones** (`Core/Narrative/StoryActions.cs`): `give`, `take`, `money`, `recruit`, `scene`,
  `absent`, `heal`, `nudo`, `resolve` se resuelven en Core; `travel`, `battle`, `puzzle`, `cierre`,
  `dialogue` y `endact` se encolan y Unity las ejecuta en orden al terminar el diálogo.
- **Cierres** (`cierres.json`, guion 1.1 y 7.3): al llegar a la decisión de un Nudo se toma una
  instantánea (`nudo:id`). La ficha **ANTES / AHORA** muestra la causa, el lugar antes y ahora y
  una frase humana; **Reconsiderar** deshace banderas y objetos y repite la decisión; **Confirmar**
  la fija en la Hebra y avanza la escena. El Cuaderno conserva todas las fichas.
- **Fichas de ausentes** (`absents.json`): Gabriel Mora es la primera (E02).
- **Viaje entre Nudos**: con el Diapasón, los puntos de Nudo listan los Nudos ya visitados.

### 6.1 Recorrido del Acto I (E01–E07)

| Escena | Mapa | Jugabilidad | Decisión / consecuencia |
|---|---|---|---|
| E01 El aula vacía | Instituto (2000) | Examinar 5 objetos; la maqueta esconde el **plano oculto** | Leer el cuaderno con o sin permiso (CONFIANZA_VA) |
| E02 Una voz gastada | Instituto | **Puzzle de la cinta**: 3 recuerdos contradictorios → la señal que comparten | Conservar la ficha de Gabriel (ausentes) |
| E03 La madre en la compuerta | Estación + Galería (2000) | Marta, 3 objetos‑testimonio, Huecos y **jefe El Hueco** | Nóminas / compuerta / **ambas sólo con el plano** |
| E04 La calle que nos sucede | Puerto Niebla (2026) | Vista **ANTES/AHORA** (Q / LB): 3 puntos persistentes | — |
| E05 El archivo de los que no fueron | Archivo Judicial (2026) | Reclutar a Lía; **jefe Fe de Erratas** | Publicar de golpe / contrastar con las familias |
| E06 Un tren hacia 330 a. C. | Paso subterráneo → Lira | Primer viaje; los guardias de Lira | **Cierre**: teléfono a la guardia (museo) / a un mercader (mercado clandestino) / historia falsa |
| E07 El primer trato | Casa de las Tablillas (330 a. C.) | **Alfabeto acústico** de Sura | **Cierre**: ocultar / publicar el censo / robar la piedra (Sura no se une); Diapasón; fin del acto |

El modo **Pruebas** (desde el título) abre el *Nudo de pruebas* con el grupo a nivel 8, todos los
encuentros, 13 criaturas de distintas épocas y equipo para probar sin jugar la historia.

---

## 7. Diseño visual de personajes (propuesta para arte)

Principio común: siluetas legibles desde la cámara alta, un **color identitario** por personaje
(el mismo que usan los modelos provisionales) y un detalle que lea su elemento.

- **Violeta (16, año 2000)** — Morado. Pelo castaño oscuro con flequillo y una pinza; campera de
  jean sobre buzo del colegio, falda tableada sobre calzas, zapatillas de lona gastadas. Walkman y
  auriculares de vincha al cuello; el cable del micrófono enrollado en el antebrazo (su arma).
  Magia: ondas de sonido visibles como anillos verdes translúcidos.
- **Ángel (17, año 2000)** — Azul. Pelo corto despeinado, campera de taller con parches, remera de
  una radio FM, jean con rodilleras, guantes de soldador. Espada de utilería del teatro escolar con
  alma de hierro y cinta aislante en la empuñadura; chispas al canalizar el Rayo.
- **Sura (32, 330 a. C.)** — Ocre. Túnica de lino teñida con bordes de tablillas grabadas, cinturón
  con estilos y tablillas de cera, sandalias. Manos siempre visibles (habla en señas): la
  animación de manos es prioritaria. La Tierra se expresa con vibraciones y signos de barro.
- **Teo (26, 1891)** — Rojo óxido. Camisa remangada, chaleco, delantal de cuero manchado de tinta,
  gorra de obrero de paño (de época), martillo tipográfico. Fuego como tinta que arde.
- **Lía (42, 2026)** — Amarillo pálido. Saco de oficina sobre remera, credencial colgada, lentes,
  carpetas bajo el brazo, linterna táctica. Luz como haces y destellos de escáner.
- **Anouk (36, 2140)** — Turquesa. Bata impermeable de hospital flotante con franjas reflectantes,
  botas altas, instrumental en la cintura, pelo recogido práctico. Agua en arcos limpios y
  burbujas clínicas.
- **Imen (71 cronológicos, 2749)** — Gris acero. Parece joven: piel con finas costuras de
  reconstrucción, traje de mantenimiento con aislamiento, visor de servicio, guantelete mecánico.
  Metal como placas que se ensamblan en el aire.
- **Anida (8800)** — Violeta oscuro. Un cuerpo con tres "voces" visibles: tres colores en el pelo
  y en las marcas de la piel, que se iluminan según quién habla. Manto orgánico de baja luz y orbe
  de coral. Vacío como recortes de oscuridad con borde brillante.

**Coherencia de época:** ningún objeto anacrónico en los habitantes de cada época (en el Cretácico
no hay humanos; en Lira, ni metal moderno ni tela sintética; en 1891, gorra de paño sí y visera
de plástico no). Los viajeros sí llevan su ropa de origen: el contraste es intencional y el guion
lo usa (E06: «los guardias no entienden la ropa de los visitantes»).

---

## 8. Cómo agregar contenido

| Quiero… | Edito | Y luego |
|---|---|---|
| Un personaje, técnica, objeto, criatura o enemigo | `StreamingAssets/Data/*.json` | textos en `Localization/es.json` |
| Un combate | `encounters.json` (+ `dramaticRule` si es jefe) | colocar un `MapEnemy` con ese id |
| Una escena con diálogo | `Data/dialogues/<id>.json` | textos `dlg.<id>.<nodo>` en `es.json` |
| Un mapa | `Data/maps/<id>.json` (bloques, PNJ, cofres, enemigos, criaturas, salidas, zonas, Nudos) | condiciones con la sintaxis de los diálogos |
| Una escena del guion con objetivos | `scenes.json` | textos `scene.<id>.title` y `scene.<id>.obj.<objetivo>` |
| Un Cierre | `cierres.json` + acción `cierre:<id>` en el diálogo de la decisión | textos `cierre.<id>.*` |
| Un jefe con regla nueva | clase en `Core/Combat/Rules` | registrarla en `DramaticRuleRegistry` |
| Traducir | copiar `es.json` como `en.json` y traducir | se elige en *Opciones › Idioma* |

Siempre: **Time Traveler › Validar datos y textos** en Unity, o `dotnet test`. Las pruebas fallan
si hay referencias rotas, condiciones con variables inexistentes, acciones desconocidas, mapas
inalcanzables o textos faltantes.

**Generadores** (`tools/content/`): el contenido actual se escribió con `gen_data.py` (sistemas) y
`gen_act1.py` (Acto I). Para cambios grandes conviene editar los generadores y ejecutar
`python tools/content/gen_data.py .`, que reescribe los JSON. Si se retoca un JSON a mano, hay que
llevar el cambio también al generador (o dejar de usarlo) para no perderlo en la próxima generación.

---

## 9. Hoja de ruta

1. **Fase 1 (esta entrega):** núcleo de sistemas, mapa de prueba con los dos jefes del Acto I,
   menús completos, mando, guardado.
2. **Fase 2 — Acto I jugable (E01–E07):** ✔ mapas por datos, escenas y objetivos, puzzles de la
   cinta y del alfabeto, Hueco con testimonios, vista ANTES/AHORA, Cierres con reconsideración,
   fichas de ausentes, Sura y Diapasón, viaje entre Nudos, modo de pruebas.
3. **Fase 3 — arte y sonido:** URP, modelos 3D, animaciones, efectos de técnicas, música
   (motivo de tres notas original), retratos para diálogos, migración de la interfaz a prefabs.
4. **Fases 4–7:** Actos II–V, misiones M01–M14, los 16 finales, Nueva Hebra+.
