# Contenido del ACTO I · La cinta que aún no existe (E01–E07).
# Se ejecuta dentro de gen_data.py (comparte item, flag, node, choice, dialogue, es, V, A, L, M).
# Las líneas entre «comillas» provienen del guion; los textos de narrador sin comillas resumen
# las acotaciones del guion o son instrucciones de juego.

S = "char.sura.name"
es["npc.anciana.name"] = "Anciana"
es["npc.custodia.name"] = "La Custodia"
es["npc.guardia_lira.name"] = "Guardia de Lira"
es["npc.mercader.name"] = "Mercader"
es["npc.nina.name"] = "Niña"

def fx(flag, op="Add", value=1):
    return {"flag": flag, "op": op, "value": value}

# ------------------------------------------------------------------ objetos y banderas del acto
item("casco_de_operario", "Casco de operario", "Un casco con el nombre raspado. Guarda el recuerdo de una sirena de turno.", "Clave")
item("foto_de_turno", "Foto de turno", "Una foto de la estación de bombeo. Alguien fue recortado de ella.", "Clave")
item("informe_de_julian", "Informe de Julián", "Hojas mojadas con la letra de Julián Salcedo. El informe incompleto.", "Clave")
item("nomina_mojada", "Nómina mojada", "Nóminas laborales de la estación rescatadas del agua.", "Clave")
item("telefono_de_lia", "Teléfono de Lía", "Un teléfono de 2026. En el pasado, un objeto imposible.", "Clave")

for f, d in [("INSP_ALBUM", "Inspeccionaron el álbum de graduados."), ("INSP_LISTA", "Inspeccionaron la lista del personal."),
             ("INSP_CASETE", "Inspeccionaron el casete de 1986."), ("INSP_MAQUETA", "Inspeccionaron la maqueta del puerto."),
             ("INSP_MURAL", "Inspeccionaron el mural."), ("CINTA_RESUELTA", "La señal de la cinta se aclaró (E02)."),
             ("MARTA_E03", "Hablaron con Marta en la estación (E03)."), ("E04_ARBOL", "Encontraron el árbol que persiste."),
             ("E04_TOMA", "Encontraron la toma de agua que persiste."), ("E04_MURAL", "Encontraron el mural en el museo."),
             ("E04_PERIODICOS", "Leyeron los periódicos incompatibles."), ("E07_SIGNOS", "Aprendieron el alfabeto acústico de Sura.")]:
    flag(f, d)
flag("E01_INSPECCIONES", "Objetos del aula inspeccionados (E01).", 0, 5)
flag("E04_PUNTOS", "Puntos persistentes encontrados en 2026 (E04).", 0, 3)
flag("E05_ESTRATEGIA", "E05: 1 publicar de golpe, 2 contrastar con las familias.", 0, 2)
flag("E06_ENTRADA", "E06: 1 objeto a la guardia, 2 objeto a un mercader, 3 historia falsa.", 0, 3)

absents = ["gabriel"]
es["absent.gabriel.name"] = "Gabriel Mora, 16 años"
es["absent.gabriel.desc"] = "Compañero de clase de Violeta y Ángel en el año 2000. Nadie lo recuerda, pero su voz está en una de las cintas de Violeta."

# ------------------------------------------------------------------ escenas
scenes = []
def scene(id, act, title, objectives, on_complete=None):
    s = {"id": id, "act": act, "objectives": [{"id": oid, "condition": cond} for oid, cond, _ in objectives]}
    if on_complete: s["onObjectivesComplete"] = on_complete
    scenes.append(s)
    es[f"scene.{id}.title"] = title
    for oid, _, text in objectives:
        es[f"scene.{id}.obj.{oid}"] = text

NEVER = "money<0"
scene("E01", 1, "E01 · El aula vacía", [("inspeccionar", "E01_INSPECCIONES>=5", "Inspecciona los cinco objetos del aula de radio.")], "e01_cuaderno")
scene("E02", 1, "E02 · Una voz gastada", [("cinta", "CINTA_RESUELTA", "Reproduce la cinta en el magnetófono y ajusta la señal.")])
scene("E03", 1, "E03 · La madre en la compuerta", [
    ("ir", "MARTA_E03", "Ve a la estación de bombeo N.º 4 (salida del patio)."),
    ("objetos", "item.casco_de_operario && item.foto_de_turno && item.informe_de_julian", "Encuentra en la galería tres objetos que guarden recuerdos."),
    ("decidir", "E03_ELECCION>=1", "El agua sube: decide qué salvar."),
    ("hueco", "resolved.g_hueco", "Estabiliza al Hueco uniendo los objetos con sus testimonios."),
], "e03_circulos")
scene("E04", 1, "E04 · La calle que nos sucede", [("puntos", "E04_PUNTOS>=3", "Encuentra tres lugares que persisten desde el 2000 (mantén Q o LB para ver el ANTES).")], "e04_anochecer")
scene("E05", 1, "E05 · El archivo de los que no fueron", [
    ("archivo", "recruited.lia", "Entra en el Archivo Judicial Municipal."),
    ("erratas", "resolved.a_erratas", "Detén a la Fe de Erratas."),
], "e05_despues")
scene("E06", 1, "E06 · Un tren hacia 330 antes de Cristo", [
    ("paso", "visited.lira_puerto", "Baja al paso subterráneo de la vieja estación."),
    ("entrar", "cierre.cierre_e06", "Encuentra la forma de entrar en Lira."),
])
scene("E07", 1, "E07 · El primer trato", [
    ("tablillas", "E07_SIGNOS", "Encuentra a Sura en la Casa de las Tablillas y aprende su alfabeto."),
    ("decidir", "cierre.cierre_e07", "Decide qué hacer con el censo."),
])
scene("E08", 2, "Acto II · Los nombres enterrados", [("continua", NEVER, "El Acto II llegará en la próxima fase. Viaja entre Nudos con el Diapasón y vincula criaturas.")])
scene("PRUEBAS", 0, "Modo de pruebas", [("libre", NEVER, "Explora libremente: combate, Resonancia, evolución y menús.")])

# ------------------------------------------------------------------ Cierres
cierres = []
def cierre(id, scene_id, echo, flag_id, values, decision, cause, before, variants, on_confirm=None, next_scene=None):
    c = {"id": id, "scene": scene_id, "echoEra": echo, "flag": flag_id, "values": values, "decisionDialogue": decision}
    if on_confirm: c["onConfirmDialogue"] = on_confirm
    if next_scene: c["nextScene"] = next_scene
    cierres.append(c)
    es[f"cierre.{id}.cause"] = cause
    es[f"cierre.{id}.before"] = before
    for v, (now, person) in variants.items():
        es[f"cierre.{id}.now.{v}"] = now
        es[f"cierre.{id}.person.{v}"] = person

cierre("cierre_e06", "E06", "2026", "E06_ENTRADA", [1, 2, 3], "e06_guardias",
       "En Lira, 330 a. C., el grupo eligió cómo entrar en la ciudad.",
       "2026 · Museo del Estuario: la vitrina de «objetos de origen desconocido» está vacía.",
       {1: ("La vitrina exhibe un teléfono fosilizado en caliza, rotulado «ofrenda votiva de Lira».",
            "Guía del museo: «Nadie sabe cómo llegó un aparato así a una ofrenda antigua. Es nuestra pieza más visitada.»"),
        2: ("En un mercado clandestino del puerto se subasta un teléfono imposible, cubierto de sal.",
            "Coleccionista: «Lo heredé de una familia de contrabandistas. Dicen que trae mala suerte… o un secreto.»"),
        3: ("La vitrina sigue vacía. En un registro de Lira figura una familia de viajeros «llegados de una ciudad del oeste».",
            "Archivera del museo: «Encontramos su rastro en tres censos distintos. Ninguno coincide.»")},
       on_confirm="e06_confirmado", next_scene="E07")
cierre("cierre_e07", "E07", "2026", "PACTO_SURA", [1, 2, 3], "e07_bifurcacion",
       "En Lira, el grupo decidió qué hacer con el censo de quienes nunca fueron contados.",
       "2026 · Una niña busca en el registro civil el nombre de su bisabuela. No aparece.",
       {1: ("La niña encuentra el nombre en un archivo comunitario que sobrevivió escondido.",
            "Niña: «Se llamaba igual que yo. Nadie me lo había dicho.»"),
        2: ("En la Plaza de las Voces Libres, el nombre de su bisabuela está grabado entre miles.",
            "Niña: «Mi abuela dice que esta plaza siempre se llamó así. Yo creo que alguien la ganó.»"),
        3: ("La piedra falta en Lira. En 2026, un museo exhibe el rostro de Sura sin reconocer su idioma.",
            "Guía: «Una escriba anónima. No sabemos qué decían sus manos.»")},
       on_confirm="e07_confirmado", next_scene="E08")

# ------------------------------------------------------------------ diálogos
def inspect(did, flag_id, first_text, repeat_text, effects=(), actions=(), speaker=None, extra_counter=None):
    """Inspección: la primera vez fija la bandera (y cuenta); después muestra un texto breve."""
    eff = [fx(flag_id, "Set", 1)] + [fx(*e) for e in effects]
    if extra_counter: eff.append(fx(extra_counter))
    dialogue(did, [
        node(did, "r", None, repeat_text, condition=flag_id, **{"else": "p"}),
        node(did, "p", speaker, first_text, effects=eff, actions=list(actions)),
    ])

def simple(did, lines, actions=(), condition=None):
    nodes = []
    for i, (spk, txt) in enumerate(lines):
        nid = f"n{i+1}"
        nxt = f"n{i+2}" if i + 1 < len(lines) else None
        kw = {}
        if i == len(lines) - 1 and actions: kw["actions"] = list(actions)
        nodes.append(node(did, nid, spk, txt, nxt, **kw))
    if condition: nodes[0]["condition"] = condition
    dialogue(did, nodes)

# E01 · El aula vacía ------------------------------------------------
simple("e01_intro", [
    (None, "Instituto Público del Muelle, Puerto Niebla. Viernes 27 de octubre de 2000, 17:40. Afuera llueve sobre una cancha agrietada."),
    (None, "Violeta y Ángel cumplen una sanción: catalogar las grabaciones de la radio estudiantil por alterar la antena para retransmitir una protesta del comedor."),
    (V, "«Prueba. Prueba. Si alguien encuentra esta cinta dentro de cien años: el olor de esta sala es una mezcla de humedad y tiza.»"),
    (A, "«Y del cable que acabás de quemar.»"),
    (V, "«Se llama recreación histórica.»"),
    (A, "«Tu recreación histórica va a costarme el sábado.»"),
    (V, "«Ya me lo debías. Cancelaste el cine por arreglar la bomba de tu mamá.»"),
    (A, "«No sabía que contaras las veces.»"),
    (V, "«Cuento lo que me importa.»"),
])
inspect("insp_album", "INSP_ALBUM",
        "Un álbum de graduados. En una fotografía alguien recortó con cuidado a una persona: sólo queda el borde de un hombro.",
        "La fotografía recortada sigue ahí. Nadie recuerda quién estaba.", extra_counter="E01_INSPECCIONES")
inspect("insp_lista", "INSP_LISTA",
        "La lista del personal de 1986. Al margen, a lápiz: «cabezal 4 · frecuencia 4 · velocidad 5».",
        "«Cabezal 4 · frecuencia 4 · velocidad 5», anotado a lápiz.", extra_counter="E01_INSPECCIONES")
inspect("insp_casete", "INSP_CASETE",
        "Un casete grabado por estudiantes en 1986. La etiqueta dice: «cabezal 2 · frecuencia 4 · velocidad 3».",
        "La etiqueta del casete de 1986: «cabezal 2 · frecuencia 4 · velocidad 3».", extra_counter="E01_INSPECCIONES")
did = "insp_maqueta"
dialogue(did, [
    node(did, "plano", None, "Ángel levanta la maqueta. Debajo hay un plano doblado de la estación de bombeo N.º 4, con un conducto de servicio que no figura en ningún otro lado.",
         condition="INSP_MAQUETA && !PLANO_OCULTO", effects=[fx("PLANO_OCULTO", "Set", 1)], **{"else": "r"}),
    node(did, "r", None, "La maqueta del puerto. «Cabezal 2 · frecuencia 1 · velocidad 5».", condition="INSP_MAQUETA", **{"else": "p"}),
    node(did, "p", None, "Una maqueta del puerto. En la base, una tarjeta: «cabezal 2 · frecuencia 1 · velocidad 5». La maqueta se mueve un poco, como si algo la calzara.",
         effects=[fx("INSP_MAQUETA", "Set", 1), fx("E01_INSPECCIONES")]),
])
did = "insp_mural"
dialogue(did, [
    node(did, "despues", None, "El mural ahora dice: «NADIE FUE EL ÚLTIMO».", condition="CINTA_RESUELTA", **{"else": "r"}),
    node(did, "r", None, "«SOY EL ÚLTIMO QUE LO RECUERDA».", condition="INSP_MURAL", **{"else": "p"}),
    node(did, "p", None, "En el mural del pasillo alguien escribió: «SOY EL ÚLTIMO QUE LO RECUERDA».", effects=[fx("INSP_MURAL", "Set", 1), fx("E01_INSPECCIONES")]),
])
D = "e01_cuaderno"
dialogue(D, [
    node(D, "n1", None, "Dentro de un cuaderno, Ángel encuentra una anotación íntima de Violeta.", choices=[
        choice(D, "n1", 0, "Pedirle permiso para leerla en voz alta", "n2a", [("CONFIANZA_VA", "Add", 1), ("CONSENTIMIENTO", "Add", 1)]),
        choice(D, "n1", 1, "Leerla en voz alta sin preguntar", "n2b", [("CONFIANZA_VA", "Add", -1)]),
    ]),
    node(D, "n2a", None, "Violeta duda un segundo. Después asiente.", "n3"),
    node(D, "n2b", None, "Violeta sonríe… y luego, en silencio, retira su cuaderno.", "n3"),
    node(D, "n3", None, "Un magnetófono sin cable empieza a girar. La cinta está marcada «28-X-2026 / PARA QUIEN SEA QUE REGRESE».",
         actions=["give:cinta_2026:1", "scene:E02"]),
])

# E02 · Una voz gastada ----------------------------------------------
D = "e02_magnetofono"
dialogue(D, [
    node(D, "hecho", None, "El magnetófono está en silencio. La cinta ya dijo lo que tenía que decir.", condition="CINTA_RESUELTA", **{"else": "visto"}),
    node(D, "visto", None, "La cinta espera. Hay que ajustar cabezal, frecuencia y velocidad.", condition="current.E02", **{"else": "nada"}, next="ajuste_corto"),
    node(D, "ajuste_corto", None, "Violeta vuelve a probar la señal.", actions=["puzzle:cinta"]),
    node(D, "nada", None, "Un magnetófono viejo de la radio estudiantil."),
])
# El primer encuentro con la cinta es un diálogo aparte para no repetir la voz cada vez.
D = "e02_cinta"
dialogue(D, [
    node(D, "n1", None, "La cinta reproduce la voz de una mujer adulta que respira entre frases. Habla de un apagón de recuerdos que comenzará a las 20:26 del 28 de octubre de 2026, y del antiguo depósito de aguas.", "n2"),
    node(D, "n2", "npc.voz_cinta.name", "«Si siguen juntos, no significa que hayan elegido bien. Si se separan, no significa que todo se perdió. No busquen al culpable antes de mirar a las personas que faltan.»", "n3"),
    node(D, "n3", A, "«Esto es una broma horrible.»", "n4"),
    node(D, "n4", V, "«Mi voz no suena así.»", "n5"),
    node(D, "n5", A, "«Nunca te escuchaste con veintiséis años más.»", "n6"),
    node(D, "n6", V, "«¿Por qué dice ‘regresen’? Si todavía no nos fuimos.»", "n7"),
    node(D, "n7", None, "La señal se corta y se mezcla. Hay que ajustar cabezal, frecuencia y velocidad. Quizá las anotaciones del aula sirvan.", actions=["puzzle:cinta"]),
])
D = "e02_resuelto"
dialogue(D, [
    node(D, "n1", None, "La señal se aclara. Bajo las palabras se oye una melodía de tres notas; al reproducirla al revés aparece la voz de Ángel de niño, diciendo un apodo que sólo Marta conoce.", "n2"),
    node(D, "n2", None, "En el pasillo, el mural cambia: ahora dice «NADIE FUE EL ÚLTIMO».", "n3"),
    node(D, "n3", "npc.profesora.name", "Una profesora atraviesa el pasillo y les pregunta por un compañero de clase: Gabriel Mora.", "n4"),
    node(D, "n4", None, "Violeta no recuerda que existiera. Pero en su casete hay una grabación de su voz.", "n5"),
    node(D, "n5", None, "Aparece la primera ficha de ausente: Gabriel Mora, 16 años.", choices=[
        choice(D, "n5", 0, "Conservar la ficha", "n6a", [("FICHA_GABRIEL", "Set", 1), ("TESTIMONIOS", "Add", 1)]),
        choice(D, "n5", 1, "Ignorarla", "n6b"),
    ]),
    node(D, "n6a", None, "Guardan la ficha de Gabriel. Alguien tiene que recordarlo.", "n7", actions=["give:ficha_gabriel:1", "absent:gabriel"]),
    node(D, "n6b", None, "La ficha queda sobre el banco. Al volver a mirar, ya no está.", "n7"),
    node(D, "n7", V, "«La voz dijo ‘el antiguo depósito de aguas’. La estación de bombeo N.º 4.»", "n8"),
    node(D, "n8", A, "«Es donde trabaja mi mamá.»", actions=["scene:E03"]),
])

# E03 · La madre en la compuerta ------------------------------------
D = "e03_marta"
dialogue(D, [
    node(D, "n1", None, "Estación de bombeo N.º 4, 20:10. Marta recibe una llamada de mantenimiento que niega haber hecho. La estación empieza a drenar agua hacia una galería clausurada.", "n2"),
    node(D, "n2", M, "«Lo que van a ver ahí abajo no estaba en los planos. No quiero volver a tapar algo para que ustedes duerman tranquilos.»", "n3"),
    node(D, "n3", A, "«Entonces ¿por qué lo tapaste?»", "n4"),
    node(D, "n4", M, "«Porque en ese momento elegí conservar el empleo antes que una verdad que no sabía defender. No hagan de mí una mártir; háganme preguntas.»", "n5"),
    node(D, "n5", None, "Marta entrega una llave mecánica y admite que Julián dejó un informe incompleto.", "n6", actions=["give:llave_de_marta:1"]),
    node(D, "n6", None, "Un desprendimiento los separa de ella. La única salida es la galería clausurada.", effects=[fx("MARTA_E03", "Set", 1)], actions=["heal"]),
])
# Objetos de la galería: se entregan al inventario (no usan bandera de inspección).
for did, item_id, text in [
    ("insp_casco", "casco_de_operario", "Un casco de operario con el nombre raspado. Al tocarlo, Ángel oye una sirena de cambio de turno que nadie más oye."),
    ("insp_foto", "foto_de_turno", "Una foto de turno de la estación. Alguien fue recortado, igual que en el álbum del instituto."),
    ("insp_informe", "informe_de_julian", "Hojas mojadas con la letra de Julián: el informe incompleto que mencionó Marta."),
]:
    dialogue(did, [
        node(did, "ya", None, "Ya lo llevan consigo.", condition=f"item.{item_id}", **{"else": "p"}),
        node(did, "p", None, text + " Este objeto guarda un recuerdo.", actions=[f"give:{item_id}:1"]),
    ])
simple("insp_inscripciones", [(None, "Inscripciones en varios idiomas, mucho más viejas que la ciudad. En el suelo, cables antiguos dibujan tres círculos.")])
simple("insp_presencia", [(None, "Algo ocupa el centro de la galería: la forma física de una acción que ocurrió y fue retirada del recuerdo colectivo. Golpearla no bastará: hacen falta los tres objetos y sus testimonios.")])
D = "e03_eleccion"
dialogue(D, [
    node(D, "n1", None, "El agua sube. Una caja con nóminas laborales se aleja con la corriente; la compuerta que protege las viviendas sigue abierta.", choices=[
        choice(D, "n1", 0, "Salvar la caja de nóminas", "a", [("E03_ELECCION", "Set", 1), ("TESTIMONIOS", "Add", 1)]),
        choice(D, "n1", 1, "Cerrar de inmediato la compuerta", "b", [("E03_ELECCION", "Set", 2)]),
        choice(D, "n1", 2, "Hacer ambas: el plano oculto muestra un conducto de servicio", "c", [("E03_ELECCION", "Set", 3), ("TESTIMONIOS", "Add", 1)], cond="PLANO_OCULTO"),
    ]),
    node(D, "a", None, "Rescatan las nóminas. La compuerta queda abierta y el agua alcanza las primeras viviendas.", actions=["give:nomina_mojada:1"]),
    node(D, "b", None, "La compuerta se cierra. Las viviendas quedan a salvo; la caja se pierde en la corriente."),
    node(D, "c", None, "Siguiendo el plano, Ángel cierra la compuerta por el conducto de servicio mientras Violeta rescata la caja.", actions=["give:nomina_mojada:1"]),
])
simple("e03_circulos", [
    (None, "El Hueco se disuelve. El lugar queda quieto."),
    (None, "Al tocar los círculos, Violeta escucha olas; Ángel, una multitud."),
    (None, "El túnel se abre hacia una calle que aún no ha sido construida."),
], actions=["scene:E04", "travel:calle_2026:default", "heal"])

# E04 · La calle que nos sucede --------------------------------------
simple("e04_llegada", [
    (None, "Puerto Niebla, 28 de octubre de 2026, 09:12."),
    (None, "Una avenida de pantallas publicitarias ocupa el terreno de la escuela. Violeta busca la cancha y encuentra un estacionamiento."),
    (None, "Mantén pulsado Q (o LB en el mando) para ver el 2000 superpuesto: ANTES / AHORA."),
], condition="current.E04")
simple("insp_placa", [(None, "Ángel ve su apellido en una placa de una obra pública: «Fundación Salcedo por la Memoria».")])
D = "e04_transeunte"
dialogue(D, [
    node(D, "n0", None, "La anciana mira la avenida sin decidirse a cruzar.", condition="!current.E04", **{"else": "n1"}),
    node(D, "n1", None, "Una anciana les ofrece indicaciones y se queda paralizada: no puede recordar el nombre de la persona que la acompaña.", "n2"),
    node(D, "n2", V, "«¿Todavía existe el videoclub de Escobar?»", "n3"),
    node(D, "n3", "npc.anciana.name", "«No sé quién es Escobar.»", "n4"),
    node(D, "n4", A, "«No está preguntando por una persona. Pregunta por el videoclub.»", "n5"),
    node(D, "n5", "npc.anciana.name", "«Sé lo que es un videoclub. Lo que no entiendo es por qué los dos lloran cuando digo que aquí nunca hubo uno.»"),
])
inspect("insp_arbol", "E04_ARBOL", "Un árbol. En el 2000 estaba en la vereda de la escuela. Sigue aquí: un punto que persiste.",
        "El árbol de la vereda de la escuela.", extra_counter="E04_PUNTOS")
inspect("insp_toma", "E04_TOMA", "Una toma de agua de hierro con el mismo escudo municipal del 2000. Un punto que persiste.",
        "La toma de agua de siempre.", extra_counter="E04_PUNTOS")
inspect("insp_mural_museo", "E04_MURAL", "Dentro del museo, detrás de un vidrio: el mural escolar. «NADIE FUE EL ÚLTIMO». Un punto que persiste.",
        "El mural escolar, en su vitrina.", extra_counter="E04_PUNTOS")
inspect("insp_periodicos", "E04_PERIODICOS", "Dos ejemplares de un periódico del mismo día, con titulares incompatibles.", "Dos periódicos del mismo día que no pueden ser ciertos a la vez.")
simple("e04_anochecer", [
    (None, "Al caer la noche, una transmisión pública anuncia una «actualización de identidad civil»."),
    (None, "La cuenta atrás de las 20:26 aparece sobre el reloj del teléfono de un desconocido."),
    (None, "El desastre no llegará en cientos de años: el primer daño irreversible sucederá mañana para los chicos, hoy para el mundo."),
    (V, "«La voz de la cinta habló de los que faltan. Si alguien lleva la cuenta, es el Archivo Judicial.»"),
], actions=["scene:E05"])
D = "vendedor_reloj"
dialogue(D, [
    node(D, "n1", "npc.vendedor.name", "«Un reloj sin agujas, para viajeros. No mide el tiempo: lo acompaña.»", "n2"),
    node(D, "n2", None, "¿Comprar el reloj sin agujas por 600 monedas?", choices=[
        choice(D, "n2", 0, "Comprar", "n3", cond="money>=600"),
        choice(D, "n2", 1, "No, gracias", "n4"),
    ]),
    node(D, "n3", "npc.vendedor.name", "«Llévelo puesto. Los relojes que no se usan se olvidan.»", actions=["money:-600", "give:reloj_sin_agujas:1"]),
    node(D, "n4", "npc.vendedor.name", "«Volverá. Todos vuelven.»"),
])

# E05 · El archivo de los que no fueron ------------------------------
D = "e05_lia"
dialogue(D, [
    node(D, "n1", None, "Archivo Judicial Municipal, 12:30. Lía localiza el casete y los conduce a un sótano cuyas cámaras graban pasillos vacíos mientras sus sensores cuentan personas.", "n2"),
    node(D, "n2", None, "Una pared contiene miles de fichas de nacimiento canceladas antes de registrarse. Algunos nombres pertenecen a personas que Violeta y Ángel conocieron en 2000.", "n3"),
    node(D, "n3", L, "«Un error informático no borra también las lápidas.»", "n4"),
    node(D, "n4", A, "«¿Y vos cómo recordás lo que cambió?»", "n5"),
    node(D, "n5", L, "«Mi hija me enseñó a hacer listas de cosas que me dan vergüenza. Por alguna razón, esas listas no se corrigen.»", "n6"),
    node(D, "n6", V, "«Mi nombre aparece en una ficha.»", "n7"),
    node(D, "n7", L, "«Sí. Y en otra, tu nombre está tachado porque nadie llegó a ponértelo.»", "n8"),
    node(D, "n8", None, "¿Qué hacer con la investigación?", choices=[
        choice(D, "n8", 0, "Publicarla de golpe (más testimonios espontáneos, pánico social)", "n9", [("E05_ESTRATEGIA", "Set", 1), ("TESTIMONIOS", "Add", 1)]),
        choice(D, "n8", 1, "Contrastarla primero con las familias (menos alcance, más consentimiento)", "n9", [("E05_ESTRATEGIA", "Set", 2), ("CONSENTIMIENTO", "Add", 1)]),
    ]),
    node(D, "n9", None, "Las luces del sótano cambian de color: la guardia automatizada intenta destruir el archivo de Lía.", "n10"),
    node(D, "n10", None, "Lía se une al grupo.", actions=["recruit:lia:5", "give:telefono_de_lia:1"]),
])
simple("insp_fichas", [(None, "Fichas de nacimiento canceladas antes de registrarse. Lía conserva copias impresas porque el sistema elimina los archivos digitales en silencio.")])
simple("e05_despues", [
    (None, "La Fe de Erratas se detiene a mitad de una corrección. Por un instante, el sistema no sabe a quién reconocer."),
    (None, "Un mapa de circulación urbana muestra una vía más antigua que las calles: pasa bajo la vieja estación."),
], actions=["scene:E06"])

# E06 · Un tren hacia 330 antes de Cristo -----------------------------
simple("e06_paso", [
    (None, "Paso subterráneo de la vieja estación. Lía identifica la piedra fundacional del muelle, reutilizada durante siglos."),
    (None, "Combinan la vibración de la cinta con la inscripción: cada estación de la ciudad ocupa un lugar donde el suelo repite sonidos antiguos."),
    (A, "«¿Y si saltamos a un año en el que ninguno nacimos?»"),
    (V, "«Entonces, por primera vez, nadie podrá decirnos que ya sabe cómo terminamos.»"),
    (L, "«No confundan no estar escritos con no tener consecuencias.»"),
], actions=["nudo:e06", "travel:lira_puerto:default"], condition="current.E06")
D = "e06_guardias"
dialogue(D, [
    node(D, "n1", None, "La salida llega a un puerto mediterráneo donde se está prohibiendo pronunciar los nombres de las personas ejecutadas. Los guardias no entienden la ropa de los visitantes.", "n2"),
    node(D, "n2", "npc.guardia_lira.name", "Los guardias exigen saber de dónde vienen antes de dejarlos entrar.", "n3"),
    node(D, "n3", None, "Para entrar en la ciudad deben entregar un objeto de 2026 o inventar una historia falsa sobre sus orígenes.", choices=[
        choice(D, "n3", 0, "Entregar el teléfono de Lía al capitán de la guardia", "a", [("E06_ENTRADA", "Set", 1)], cond="item.telefono_de_lia"),
        choice(D, "n3", 1, "Entregárselo a un mercader que ofrece hacerlos pasar", "b", [("E06_ENTRADA", "Set", 2)], cond="item.telefono_de_lia"),
        choice(D, "n3", 2, "Inventar una historia falsa sobre sus orígenes", "c", [("E06_ENTRADA", "Set", 3)]),
    ]),
    node(D, "a", None, "El capitán gira el teléfono entre los dedos como si fuera una ofrenda. Les abre paso.", "fin", actions=["take:telefono_de_lia:1"]),
    node(D, "b", None, "El mercader guarda el teléfono bajo su túnica y los conduce por la puerta de las mercancías.", "fin", actions=["take:telefono_de_lia:1"]),
    node(D, "c", None, "Lía improvisa: son una familia de viajeros de una ciudad del oeste. Los guardias anotan algo en una tablilla.", "fin"),
    node(D, "fin", None, "Primer Cierre parcial: lo que acaban de hacer dejará una huella en otra época.", actions=["cierre:cierre_e06"]),
])
simple("e06_confirmado", [(None, "Las calles de Lira huelen a caliza y tinta. Dicen que una escriba llamada Sura guarda nombres en la Casa de las Tablillas, junto al foro.")])
simple("lira_guardia", [("npc.guardia_lira.name", "Los guardias vigilan la puerta. Sus lanzas no se mueven, pero sus ojos sí.")])
simple("lira_mercader", [("npc.mercader.name", "«¿Pan de cebada? ¿Aceite? Para viajeros con ropa tan rara, precio especial.»")], actions=["give:pan_de_lira:1"])
simple("insp_edicto", [(None, "Un edicto grabado en piedra: se prohíbe pronunciar los nombres de las personas ejecutadas.")])

# E07 · El primer trato ----------------------------------------------
D = "e07_sura"
dialogue(D, [
    node(D, "n1", None, "Casa de las Tablillas. Sura exige que nadie entre armado en su archivo. El grupo deja las armas en la entrada.", "n2"),
    node(D, "n2", None, "Conversan mediante tablillas y señas aprendidas por observación y consentimiento. Mientras tanto, alguien destruye el censo que incluye a mujeres, inmigrantes y personas esclavizadas.", "n3"),
    node(D, "n3", None, "Un magistrado quiere salvar la ciudad de una guerra entregando sólo la lista de insurgentes: en la práctica, borrará a quienes nunca fueron contabilizados.", "n4"),
    node(D, "n4", S, "(escribe) «Una ciudad que decide quién cuenta como persona ya está en guerra.»", "n5"),
    node(D, "n5", V, "«Necesitamos la piedra de su sala.»", "n6"),
    node(D, "n6", S, "«Entonces lean primero los nombres. No se presta el suelo de una casa si no se conoce a sus habitantes.»", "n7"),
    node(D, "n7", A, "«No tenemos días.»", "n8"),
    node(D, "n8", S, "«Ellos tampoco los tuvieron.»", "n9"),
    node(D, "n9", None, "Sura muestra un alfabeto acústico de manos, gestos, huellas en barro y golpes. Hay que repetirlo.", actions=["puzzle:signos"]),
])
D = "e07_sura_npc"
dialogue(D, [
    node(D, "aprender", S, "Sura vuelve a mostrar los signos, con paciencia.", condition="current.E07 && !E07_SIGNOS", actions=["puzzle:signos"], **{"else": "decidir"}),
    node(D, "decidir", S, "Sura señala las tablillas del censo destruido.", condition="current.E07 && E07_SIGNOS && PACTO_SURA==0", actions=["dialogue:e07_bifurcacion"], **{"else": "nada"}),
    node(D, "nada", S, "Sura repasa sus tablillas. Cada nombre, una mano que lo escribió."),
])
D = "e07_bifurcacion"
dialogue(D, [
    node(D, "n1", None, "Ya pueden leer los nombres del censo destruido: mujeres, inmigrantes, personas esclavizadas que ningún registro oficial quiso contar.", "n2", actions=["nudo:e07"]),
    node(D, "n2", None, "¿Qué hacer con el listado?", choices=[
        choice(D, "n2", 0, "Ocultarlo (conserva a las personas, arriesga a Sura)", "a", [("PACTO_SURA", "Set", 1), ("CONSENTIMIENTO", "Add", 1)]),
        choice(D, "n2", 1, "Duplicarlo y publicarlo (empodera a la comunidad, inicia persecución)", "b", [("PACTO_SURA", "Set", 2), ("PLURALIDAD", "Add", 1), ("TESTIMONIOS", "Add", 1)]),
        choice(D, "n2", 2, "Robar la piedra mientras nadie mira", "c", [("PACTO_SURA", "Set", 3), ("DEUDA", "Add", 1)]),
    ]),
    node(D, "a", S, "Sura esconde las tablillas bajo el suelo de su casa. Sabe lo que arriesga.", "fin"),
    node(D, "b", S, "Sura reparte copias entre las familias. Por la tarde, los guardias ya preguntan por ella.", "fin"),
    node(D, "c", None, "Se llevan la piedra. Sura los ve. No dice nada.", "fin"),
    node(D, "fin", None, "El grupo obtiene el Diapasón de Arcilla, que abre Nudos conectados por testimonios, no por coordenadas. También permite sintonizar con criaturas (Resonancia).",
         actions=["give:diapason_de_arcilla:1", "cierre:cierre_e07"]),
])
D = "e07_confirmado"
dialogue(D, [
    node(D, "une", S, "Sura se une al grupo.", "corte", condition="PACTO_SURA<=2", actions=["recruit:sura:6"], **{"else": "no"}),
    node(D, "no", None, "Sura no se unirá hasta que el daño se repare.", "corte"),
    node(D, "corte", None, "En 2026, una niña reconoce el nombre de su bisabuela por primera vez.", "torre", condition="PACTO_SURA<=2", **{"else": "torre"}),
    node(D, "torre", None, "Al mismo tiempo, una torre de Ciudad Pila despierta en el año 2749.", "custodia"),
    node(D, "custodia", "npc.custodia.name", "«Variación detectada. La posibilidad se niega a permanecer cerrada».", actions=["endact:1"]),
])

# ------------------------------------------------------------------ mapas
maps = []
WALL = 3.2

def box(x, z, w, d, h=WALL, color="#888888", y=None, layer=None):
    b = {"pos": [x, h / 2 if y is None else y, z], "size": [w, h, d], "color": color}
    if layer: b["layer"] = layer
    return b

def room(w, d, color, h=WALL):
    hw, hd = w / 2, d / 2
    return [box(0, hd, w, 1, h, color), box(0, -hd, w, 1, h, color), box(hw, 0, 1, d, h, color), box(-hw, 0, 1, d, h, color)]

def npc(id, x, z, dlg, model="person", color="#a08070", cond=None, rot=180, prompt=None):
    n = {"id": id, "pos": [x, z], "dialogue": dlg, "model": model, "color": color, "rotation": rot}
    if cond: n["condition"] = cond
    if prompt: n["promptKey"] = prompt
    return n

def obj(id, x, z, dlg, cond=None, color="#c9a46a"):
    return npc(id, x, z, dlg, "object", color, cond, prompt="interact.inspect")

def enemy(id, x, z, enc, cond=None, respawns=False):
    e = {"id": id, "pos": [x, z], "encounter": enc, "respawns": respawns}
    if cond: e["condition"] = cond
    return e

def exit_(x, z, to_map, to_spawn="default", cond=None, locked=None, w=3, d=3):
    e = {"pos": [x, z], "size": [w, d], "toMap": to_map, "toSpawn": to_spawn}
    if cond: e["condition"] = cond
    if locked: e["lockedTextKey"] = locked
    return e

def trig(id, x, z, dlg, cond=None, w=4, d=4, once=True):
    t = {"id": id, "pos": [x, z], "size": [w, d], "dialogue": dlg, "once": once}
    if cond: t["condition"] = cond
    return t

def wild(id, x, z, species, level):
    return {"id": id, "pos": [x, z], "species": species, "level": level}

def point(x, z, cond=None):
    p = {"pos": [x, z]}
    if cond: p["condition"] = cond
    return p

DIAP = "item.diapason_de_arcilla"

def mapdef(id, era, name, size, floor, sky, spawns, **kw):
    m = {"id": id, "era": era, "size": size, "floorColor": floor, "skyColor": sky, "spawns": spawns,
         "blocks": [], "npcs": [], "chests": [], "enemies": [], "wild": [], "exits": [], "triggers": [], "savePoints": [], "nudoPoints": []}
    m.update(kw)
    maps.append(m)
    es[f"map.{id}.name"] = name
    return m

es["exit.locked.station"] = "Todavía no. La cinta aún no dijo adónde ir."
es["exit.locked.gallery"] = "La galería está clausurada."
es["exit.locked.archive"] = "No tienen motivo para entrar al Archivo… todavía."
es["exit.locked.underpass"] = "El paso subterráneo está cerrado con una reja."
es["exit.locked.tablillas"] = "Los guardias no los dejarán pasar sin entrar antes a la ciudad."

# Instituto Público del Muelle (2000)
m = mapdef("instituto_2000", "2000", "Instituto Público del Muelle", [46, 34], "#8a7a62", "#2a2a3a",
           {"default": [-12, -6, 0], "desde_estacion": [18, -12, 180], "nudo": [-16, -11, 0]},
           isNudo=True, nudoCondition=DIAP)
m["blocks"] += room(46, 34, "#b8a888")
m["blocks"] += [box(2, 0, 1, 20, WALL, "#a89878"),                          # tabique aula / patio
                box(-12, 9, 6, 1.2, 1, "#6b5a45"), box(-12, 3, 6, 1.2, 1, "#6b5a45"),  # mesas
                box(-20, 6, 2, 10, 2.4, "#5a4a3a"),                          # estantería de cintas
                box(14, 4, 12, 0.05, 8, "#5f6670", y=0.03),                  # cancha agrietada
                box(14, 4, 0.2, 0.06, 8, "#d8d8d8", y=0.05)]
m["npcs"] += [obj("album", -18, 0, "insp_album"), obj("lista", -8, 12, "insp_lista"), obj("casete", -16, 12, "insp_casete"),
              obj("maqueta", -4, 4, "insp_maqueta"), npc("mural", -3, 15.3, "insp_mural", "sign", "#d0c8b0", rot=180, prompt="interact.inspect"),
              npc("magnetofono", -8, -2, "e02_magnetofono", "magnetofono", "#555555", prompt="interact.inspect")]
m["triggers"] += [trig("t_e01_intro", -12, -6, "e01_intro", "current.E01", 6, 6),
                  trig("t_e02_cinta", -8, -2, "e02_cinta", "current.E02 && !CINTA_RESUELTA", 5, 5)]
m["exits"] += [exit_(20, -15, "estacion_2000", "default", "scene.E02", "exit.locked.station")]
m["wild"] += [wild("w_tropel_1", 12, 8, "tropel", 3), wild("w_tropel_2", 16, -2, "tropel", 4)]
m["savePoints"] += [point(-19, -14)]
m["nudoPoints"] += [point(-16, -14, DIAP)]

# Estación de bombeo N.º 4 (2000)
m = mapdef("estacion_2000", "2000", "Estación de bombeo N.º 4", [40, 32], "#5d6066", "#1b1e28",
           {"default": [0, -12, 0], "desde_galeria": [13, 9, 180]})
m["blocks"] += room(40, 32, "#6e737a")
m["blocks"] += [box(-12, 6, 6, 8, 4, "#4c5258"), box(-4, 6, 4, 8, 3, "#4c5258"), box(8, -2, 10, 2, 1.5, "#3a6070")]
m["triggers"] += [trig("t_e03_marta", 0, -8, "e03_marta", "current.E03 && !MARTA_E03", 10, 4)]
m["exits"] += [exit_(0, -15, "instituto_2000", "desde_estacion"),
               exit_(14, 13, "galeria_2000", "default", "MARTA_E03", "exit.locked.gallery")]
m["savePoints"] += [point(-16, -12)]

# Galería clausurada (2000) — primer Nudo
m = mapdef("galeria_2000", "2000", "Galería clausurada", [30, 46], "#3e4248", "#0e1016",
           {"default": [0, -20, 0], "nudo": [0, 19, 180]}, isNudo=True, nudoCondition=DIAP)
m["blocks"] += room(30, 46, "#4a4e55")
m["blocks"] += [box(-9, -2, 6, 4, 2, "#3a3e44"), box(9, 8, 6, 4, 2, "#3a3e44"), box(0, -5, 30, 0.05, 6, "#2b4a5a", y=0.03)]
m["npcs"] += [obj("casco", -11, -12, "insp_casco", "!item.casco_de_operario"),
              obj("foto", 11, -1, "insp_foto", "!item.foto_de_turno"),
              obj("informe", -11, 10, "insp_informe", "!item.informe_de_julian"),
              npc("presencia", 0, 9, "insp_presencia", "sign", "#202030", "!resolved.g_hueco", prompt="interact.inspect"),
              npc("inscripciones", 0, 22, "insp_inscripciones", "sign", "#9a9070", prompt="interact.inspect")]
m["triggers"] += [trig("t_e03_eleccion", 0, -5, "e03_eleccion", "current.E03 && E03_ELECCION==0", 28, 3)]
m["enemies"] += [enemy("g_huecos_1", -5, -14, "enc_estacion_huecos"), enemy("g_mixto", 6, -9, "enc_estacion_mixto"),
                 enemy("g_huecos_2", 4, 4, "enc_estacion_huecos"),
                 enemy("g_hueco", 0, 14, "enc_boss_hueco", "item.casco_de_operario && item.foto_de_turno && item.informe_de_julian && E03_ELECCION>=1")]
m["exits"] += [exit_(0, -22, "estacion_2000", "desde_galeria")]
m["nudoPoints"] += [point(0, 20, "scene.E03 && " + DIAP)]

# Puerto Niebla (2026)
m = mapdef("calle_2026", "2026", "Puerto Niebla, 2026", [54, 40], "#3b4250", "#0c1022",
           {"default": [-22, -15, 45], "desde_archivo": [22, 12, 180], "desde_paso": [-22, 14, 90], "nudo": [-23, -11, 0]},
           isNudo=True, nudoCondition=DIAP)
m["blocks"] += room(54, 40, "#2e3440", 6)
m["blocks"] += [box(-6, 10, 10, 2, 5, "#1e7a8a"), box(10, 10, 8, 2, 6, "#8a3a7a"), box(24, -4, 3, 14, 9, "#232a38"),
                box(-14, -2, 6, 6, 7, "#283040"), box(0, -12, 54, 0.04, 6, "#20242c", y=0.03)]
# ANTES (2000): la escuela, la cancha y el videoclub, visibles sólo con la superposición
m["blocks"] += [box(0, 2, 22, 8, 4, "#c8b890", layer="antes"), box(12, -6, 10, 6, 0.1, "#5f6670", layer="antes"),
                box(-14, 6, 5, 4, 3, "#c84a4a", layer="antes")]
m["npcs"] += [obj("arbol", -10, 4, "insp_arbol", color="#3f7a3a"), obj("toma", 6, -8, "insp_toma", color="#7a7a7a"),
              obj("museo", 18, 4, "insp_mural_museo", color="#d0c8b0"), npc("placa", -4, -9, "insp_placa", "sign", "#b0a070", prompt="interact.inspect"),
              obj("periodicos", 8, 14, "insp_periodicos", color="#e0e0d0"),
              npc("anciana", -16, -8, "e04_transeunte", "person", "#8a7a9a"),
              npc("vendedor", -20, 8, "vendedor_reloj", "person", "#8a7a50")]
m["triggers"] += [trig("t_e04_llegada", -22, -15, "e04_llegada", "current.E04", 6, 6)]
m["enemies"] += [enemy("c_drones_1", 4, 0, "enc_2026_drones", respawns=True), enemy("c_drones_2", 14, -12, "enc_2026_drones", respawns=True)]
m["wild"] += [wild("w_pipistrelo", 20, -14, "pipistrelo", 6)]
m["exits"] += [exit_(24, 16, "archivo_2026", "default", "scene.E04", "exit.locked.archive"),
               exit_(-25, 16, "paso_2026", "default", "scene.E05", "exit.locked.underpass")]
m["savePoints"] += [point(-18, -17)]
m["nudoPoints"] += [point(-24, -12, DIAP)]

# Archivo Judicial Municipal (2026)
m = mapdef("archivo_2026", "2026", "Archivo Judicial Municipal", [30, 38], "#454a52", "#0c0e14",
           {"default": [0, -16, 0]})
m["blocks"] += room(30, 38, "#5a606a")
m["blocks"] += [box(-10, 0, 2, 20, 3, "#3a4048"), box(10, 0, 2, 20, 3, "#3a4048"), box(0, 17, 26, 1, 4, "#c0c0b0")]
m["npcs"] += [npc("fichas", 0, 16.3, "insp_fichas", "sign", "#d8d8c8", prompt="interact.inspect")]
m["triggers"] += [trig("t_e05_lia", 0, -11, "e05_lia", "current.E05 && !recruited.lia", 10, 4)]
m["enemies"] += [enemy("a_guardia", 5, 2, "enc_2026_archivo", "recruited.lia"), enemy("a_erratas", 0, 11, "enc_boss_erratas", "recruited.lia")]
m["exits"] += [exit_(0, -18, "calle_2026", "desde_archivo")]
m["savePoints"] += [point(-12, -15)]

# Paso subterráneo (2026)
m = mapdef("paso_2026", "2026", "Paso subterráneo de la vieja estación", [20, 32], "#3a3630", "#08080a",
           {"default": [0, -13, 0], "nudo": [0, 12, 180]}, isNudo=True, nudoCondition=DIAP)
m["blocks"] += room(20, 32, "#4e4a40")
m["blocks"] += [box(0, 4, 3, 2, 1.5, "#8a8270")]  # piedra fundacional
m["triggers"] += [trig("t_e06_paso", 0, 1, "e06_paso", "current.E06", 18, 4)]
m["exits"] += [exit_(0, -15, "calle_2026", "desde_paso")]
m["nudoPoints"] += [point(0, 13, DIAP)]

# Lira, puerto (330 a. C.)
m = mapdef("lira_puerto", "-330", "Lira, puerto mediterráneo", [54, 42], "#c8b894", "#5a7aa8",
           {"default": [-22, -16, 45], "desde_tablillas": [20, 13, 180], "nudo": [-23, -12, 0]},
           isNudo=True, nudoCondition=DIAP)
m["blocks"] += room(54, 42, "#e0d4b4")
m["blocks"] += [box(-10, -4, 2, 10, 4, "#d8ccaa"), box(4, 6, 10, 6, 5, "#e4dcc0"), box(16, -6, 8, 8, 4, "#d4c8a4"),
                box(0, -19, 54, 0.04, 4, "#3a6a9a", y=0.03),               # agua del puerto
                box(-20, 12, 12, 0.04, 10, "#7a8a4a", y=0.03)]             # olivar
m["npcs"] += [npc("guardia", -12, -10, "lira_guardia", "person", "#8a5a3a"), npc("mercader", 0, -12, "lira_mercader", "person", "#9a7a5a"),
              npc("edicto", -6, 0, "insp_edicto", "sign", "#e8e0c8", prompt="interact.inspect")]
m["triggers"] += [trig("t_e06_guardias", -16, -12, "e06_guardias", "current.E06 && E06_ENTRADA==0", 8, 6)]
m["enemies"] += [enemy("l_jabali", -16, 10, "enc_lira_jabali", respawns=True)]
m["wild"] += [wild("w_galapago_1", -22, 14, "galapago_olivar", 6), wild("w_galapago_2", -14, 16, "galapago_olivar", 7)]
m["exits"] += [exit_(22, 16, "casa_tablillas", "default", "scene.E06", "exit.locked.tablillas")]
m["savePoints"] += [point(-20, -18)]
m["nudoPoints"] += [point(-24, -14, DIAP)]

# Casa de las Tablillas (330 a. C.)
m = mapdef("casa_tablillas", "-330", "Casa de las Tablillas", [24, 24], "#b8a47c", "#3a3020",
           {"default": [0, -9, 0]})
m["blocks"] += room(24, 24, "#d4c49c")
m["blocks"] += [box(-9, 6, 2, 8, 2.6, "#8a7656"), box(9, 6, 2, 8, 2.6, "#8a7656"), box(0, 2, 3, 2, 0.6, "#9a8a6a")]
m["npcs"] += [npc("sura", 0, 6, "e07_sura_npc", "personaje", cond="current.E07 || PACTO_SURA==3"),
              npc("tablillas", -9, 10.8, "insp_tablillas", "sign", "#c8b890", prompt="interact.inspect")]
simple("insp_tablillas", [(None, "Tablillas con nombres de mujeres, extranjeros y personas esclavizadas. Cada una lleva la marca de la mano que la escribió.")])
m["triggers"] += [trig("t_e07_sura", 0, -5, "e07_sura", "current.E07 && !E07_SIGNOS", 10, 3)]
m["exits"] += [exit_(0, -11, "lira_puerto", "desde_tablillas")]

# Nudo de pruebas (modo de pruebas desde el título)
m = mapdef("nudo_pruebas", "orillas", "Nudo de pruebas", [70, 70], "#4a3e5a", "#141020",
           {"default": [0, -28, 0]})
m["blocks"] += room(70, 70, "#5a4e6a")
m["npcs"] += [npc("vendedor", -6, -24, "vendedor_reloj", "person", "#8a7a50")]
m["chests"] += [{"id": "p_cofre_1", "pos": [-10, -20], "itemId": "cable_trenzado", "quantity": 1},
                {"id": "p_cofre_2", "pos": [10, -20], "itemId": "espada_de_bronce", "quantity": 1},
                {"id": "p_cofre_3", "pos": [14, -24], "itemId": "malla_ligera", "quantity": 1},
                {"id": "p_cofre_4", "pos": [-14, -24], "itemId": "botiquin", "quantity": 5}]
m["enemies"] += [enemy("p_huecos", -20, -5, "enc_estacion_huecos", respawns=True), enemy("p_mixto", -26, 5, "enc_estacion_mixto", respawns=True),
                 enemy("p_hueco", -24, 18, "enc_boss_hueco", respawns=True), enemy("p_drones", 20, -5, "enc_2026_drones", respawns=True),
                 enemy("p_archivo", 26, 5, "enc_2026_archivo", respawns=True), enemy("p_erratas", 24, 18, "enc_boss_erratas", respawns=True),
                 enemy("p_sellados", 0, 26, "enc_lira_sellados", respawns=True), enemy("p_jabali", 8, 30, "enc_lira_jabali", respawns=True)]
for i, (sp, lv, x, z) in enumerate([("tropel", 4, -8, 0), ("galapago_olivar", 5, 8, 0), ("nutrilla", 5, -6, 8), ("ascuela", 5, 6, 8),
                                    ("grillo_de_laton", 5, -10, 14), ("pipistrelo", 6, 10, 14), ("lumedusa", 6, 0, 12),
                                    ("pichon_de_fragata", 8, -14, 6), ("pinzaro", 8, 14, 6), ("escaralambre", 9, 0, 18),
                                    ("nictela", 9, -4, 20), ("promesin", 10, 4, 20), ("cria_emplumada", 1, 0, 4)]):
    m["wild"].append(wild(f"p_wild_{i}", x, z, sp, lv))
m["savePoints"] += [point(0, -22)]

# ------------------------------------------------------------------ textos de interfaz del Acto I
es.update({
    "interact.inspect": "Examinar",
    "objective.label": "Objetivo",
    "overlay.hint": "Q / LB: ver el ANTES",
    "overlay.active": "ANTES · 2000",
    "cierre.title": "Cierre", "cierre.before": "ANTES", "cierre.now": "AHORA",
    "cierre.confirm": "Confirmar el Cierre", "cierre.reconsider": "Reconsiderar la decisión",
    "cierre.warning": "Tras confirmar, la decisión queda fijada en la Hebra.",
    "puzzle.cinta.title": "La cinta · 28-X-2026",
    "puzzle.cinta.dial0": "Cabezal", "puzzle.cinta.dial1": "Frecuencia", "puzzle.cinta.dial2": "Velocidad",
    "puzzle.cinta.signal": "Señal: {0} / 3",
    "puzzle.cinta.help": "Izquierda/derecha: elegir control · Arriba/abajo: cambiar valor · Confirmar: probar · Cancelar: salir",
    "puzzle.cinta.memories": "Recuerdos anotados en el aula:",
    "puzzle.cinta.memory": "{0}: cabezal {1} · frecuencia {2} · velocidad {3}",
    "puzzle.cinta.mem0": "Casete de 1986", "puzzle.cinta.mem1": "Maqueta del puerto", "puzzle.cinta.mem2": "Lista del personal",
    "puzzle.cinta.unknown": "{0}: (sin examinar)",
    "puzzle.cinta.fail": "La señal sigue mezclada. Tres recuerdos contradictorios… ¿qué tienen en común?",
    "puzzle.signos.title": "Alfabeto acústico de Sura",
    "puzzle.signos.watch": "Observa a Sura…",
    "puzzle.signos.repeat": "Repite la secuencia ({0} / {1})",
    "puzzle.signos.round": "Ronda {0} de {1}",
    "puzzle.signos.wrong": "Sura repite los signos, con paciencia.",
    "puzzle.signos.sign0": "Mano", "puzzle.signos.sign1": "Gesto", "puzzle.signos.sign2": "Huella", "puzzle.signos.sign3": "Golpe",
    "nudo.title": "Viajar entre Nudos", "nudo.prompt": "Nudo", "nudo.need": "Sin el Diapasón de Arcilla, el Nudo sólo vibra.", "nudo.here": "(aquí)",
    "act.end": "Fin del Acto {0}", "act.1.title": "La cinta que aún no existe",
    "notebook.cierres": "Cierres · ANTES / AHORA", "notebook.absents": "Fichas de ausentes", "notebook.objective": "Objetivo: {0}",
    "title.test": "Modo de pruebas",
})
