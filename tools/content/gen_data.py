# Uso: python tools/content/gen_data.py <raíz del repositorio>
# Generador inicial de contenido para Las Horas que Faltan.
# Escribe StreamingAssets/Data/*.json y StreamingAssets/Localization/es.json.
import json, os, sys

ROOT = sys.argv[1]
DATA = os.path.join(ROOT, "Assets", "StreamingAssets", "Data")
LOC = os.path.join(ROOT, "Assets", "StreamingAssets", "Localization")
os.makedirs(os.path.join(DATA, "dialogues"), exist_ok=True)
os.makedirs(LOC, exist_ok=True)

es = {}
STATS = ["MaxHp", "MaxMp", "Fuerza", "Magia", "Defensa", "DefensaMagica", "Velocidad", "Precision", "Evasion", "Suerte"]

def stats(*v):
    return {k: x for k, x in zip(STATS, v) if x}

def growth(*v):
    return {k: x for k, x in zip(STATS, v) if x}

def mods(**kw):
    return dict(kw)

# ------------------------------------------------------------------ técnicas
techniques = []
def tech(id, name, desc, kind, element, mp, power, target, **kw):
    t = {"id": id, "kind": kind, "element": element, "mpCost": mp, "power": power, "target": target}
    t.update(kw)
    techniques.append(t)
    es[f"tech.{id}.name"] = name
    es[f"tech.{id}.desc"] = desc

# Violeta (Viento / sonido)
tech("v_latigazo", "Latigazo de cable", "Azota con el cable del micrófono cargado de viento.", "Fisica", "Viento", 2, 15, "EnemigoUno")
tech("v_eco", "Eco", "Devuelve un sonido amplificado contra un enemigo.", "Magica", "Viento", 3, 14, "EnemigoUno")
tech("v_rebobinar", "Rebobinar", "Rebobina un instante del cuerpo de un aliado y le devuelve vida.", "Apoyo", "Neutro", 4, 18, "AliadoUno", healing=True)
tech("v_vendaval", "Vendaval", "Una ráfaga de viento golpea a todos los enemigos.", "Magica", "Viento", 7, 13, "EnemigosTodos")
tech("v_nota_sostenida", "Nota sostenida", "Una nota que acelera el pulso de un aliado (Prisa).", "Apoyo", "Neutro", 5, 0, "AliadoUno", applyStatus="Prisa", statusChance=1.0, statusDuration=4)
tech("v_contrapunto", "Contrapunto", "Dos latigazos en ritmo cruzado contra un grupo cercano.", "Fisica", "Viento", 6, 24, "EnemigoArea", areaRadius=2.5)
tech("v_cancion_inversa", "Canción al revés", "La melodía de tres notas, invertida. Puede confundir.", "Magica", "Viento", 14, 30, "EnemigosTodos", applyStatus="Confusion", statusChance=0.25, statusDuration=2)
# Ángel (Rayo / espada)
tech("a_tajo", "Tajo", "Un corte firme y directo.", "Fisica", "Neutro", 2, 16, "EnemigoUno")
tech("a_pulso", "Pulso", "Descarga eléctrica canalizada desde una radio vieja.", "Magica", "Rayo", 3, 13, "EnemigoUno")
tech("a_corte_cruzado", "Corte cruzado", "Barre a un grupo de enemigos cercanos.", "Fisica", "Neutro", 5, 18, "EnemigoArea", areaRadius=2.5)
tech("a_chispazo", "Chispazo", "Arcos eléctricos saltan entre todos los enemigos.", "Magica", "Rayo", 7, 13, "EnemigosTodos")
tech("a_escudo_chatarra", "Escudo de chatarra", "Arma un escudo improvisado para un aliado (Protección).", "Apoyo", "Neutro", 5, 0, "AliadoUno", applyStatus="Proteccion", statusChance=1.0, statusDuration=4)
tech("a_sobrecarga", "Sobrecarga", "Estocada electrificada. Puede aturdir.", "Fisica", "Rayo", 9, 32, "EnemigoUno", applyStatus="Aturdido", statusChance=0.3, statusDuration=1)
tech("a_tormenta_de_antena", "Tormenta de antena", "Convierte la espada en antena y llama al rayo.", "Magica", "Rayo", 15, 30, "EnemigosTodos")
# Sura (Tierra)
tech("s_tablilla", "Golpe de tablilla", "Golpe seco con su estilo de escriba.", "Fisica", "Neutro", 2, 13, "EnemigoUno")
tech("s_vibracion", "Vibración", "Hace temblar el suelo bajo un enemigo. Puede aturdir.", "Magica", "Tierra", 3, 14, "EnemigoUno", applyStatus="Aturdido", statusChance=0.2, statusDuration=1)
tech("s_escudo_pacto", "Escudo de pacto", "Un pacto protege la mente de todo el grupo (Coraza).", "Apoyo", "Neutro", 8, 0, "AliadosTodos", applyStatus="Coraza", statusChance=1.0, statusDuration=4)
tech("s_sismo", "Sismo", "La tierra se levanta contra todos los enemigos.", "Magica", "Tierra", 8, 16, "EnemigosTodos")
tech("s_nombre_devuelto", "Nombre devuelto", "Pronuncia el nombre de un aliado caído y lo trae de vuelta.", "Apoyo", "Neutro", 10, 0, "AliadoUno", revive=0.3)
tech("s_alfabeto_barro", "Alfabeto de barro", "Escribe en el barro un signo que atrapa. Puede ralentizar.", "Magica", "Tierra", 10, 30, "EnemigoUno", applyStatus="Lentitud", statusChance=0.5, statusDuration=3)
# Teo (Fuego)
tech("t_martillazo", "Martillazo", "Golpe de martillo tipográfico.", "Fisica", "Neutro", 2, 17, "EnemigoUno")
tech("t_tinta_ardiente", "Tinta ardiente", "Tinta inflamada que quema. Puede envenenar.", "Magica", "Fuego", 3, 13, "EnemigoUno", applyStatus="Veneno", statusChance=0.3, statusDuration=3)
tech("t_plancha_caliente", "Plancha caliente", "Una plancha de imprenta al rojo contra un grupo.", "Fisica", "Fuego", 6, 20, "EnemigoArea", areaRadius=2.5)
tech("t_llamarada_prensa", "Llamarada de prensa", "La prensa escupe fuego sobre todos los enemigos.", "Magica", "Fuego", 8, 16, "EnemigosTodos")
tech("t_eco_mecanico", "Eco mecánico", "Un engranaje invocado acelera a un aliado (Prisa).", "Apoyo", "Neutro", 6, 0, "AliadoUno", applyStatus="Prisa", statusChance=1.0, statusDuration=4)
tech("t_edicion_extra", "Edición extra", "Toda la fuerza de una tirada urgente en un golpe.", "Fisica", "Fuego", 11, 34, "EnemigoUno")
# Lía (Luz)
tech("l_carpetazo", "Carpetazo", "Un expediente bien pesado.", "Fisica", "Neutro", 1, 11, "EnemigoUno")
tech("l_destello", "Destello", "Un haz de linterna directo a los ojos. Puede cegar.", "Magica", "Luz", 3, 12, "EnemigoUno", applyStatus="Ceguera", statusChance=0.35, statusDuration=3)
tech("l_auditoria", "Auditoría", "Exige pruebas: puede silenciar a un enemigo.", "Magica", "Luz", 4, 0, "EnemigoUno", applyStatus="Silencio", statusChance=0.6, statusDuration=3)
tech("l_luz_de_archivo", "Luz de archivo", "Ilumina lo que el grupo había olvidado de sí mismo. Cura a todos.", "Apoyo", "Neutro", 8, 14, "AliadosTodos", healing=True)
tech("l_foco", "Foco", "Luz intensa sobre todos los enemigos.", "Magica", "Luz", 7, 15, "EnemigosTodos")
tech("l_sentencia_clara", "Sentencia clara", "Una verdad dicha sin rodeos.", "Magica", "Luz", 12, 32, "EnemigoUno")
# Anouk (Agua)
tech("n_incision", "Incisión", "Corte preciso de bisturí. Puede envenenar.", "Fisica", "Neutro", 2, 14, "EnemigoUno", applyStatus="Veneno", statusChance=0.25, statusDuration=3)
tech("n_marea", "Marea", "Una ola concentrada contra un enemigo.", "Magica", "Agua", 3, 14, "EnemigoUno")
tech("n_sutura", "Sutura", "Cierra heridas de un aliado.", "Apoyo", "Neutro", 4, 24, "AliadoUno", healing=True)
tech("n_triaje", "Triaje", "Atiende a todo el grupo y limpia veneno y ceguera.", "Apoyo", "Neutro", 9, 16, "AliadosTodos", healing=True, cureStatuses=["Veneno", "Ceguera"])
tech("n_sedante", "Sedante", "Puede dormir a un enemigo.", "Magica", "Neutro", 5, 0, "EnemigoUno", applyStatus="Sueno", statusChance=0.5, statusDuration=3)
tech("n_diluvio", "Diluvio", "La bahía entera cae sobre los enemigos.", "Magica", "Agua", 12, 22, "EnemigosTodos")
tech("n_desfibrilar", "Desfibrilar", "Reanima a un aliado caído con la mitad de su vida.", "Apoyo", "Neutro", 12, 0, "AliadoUno", revive=0.5)
# Imen (Metal)
tech("i_puno_servicio", "Puño de servicio", "Golpe con guantelete de mantenimiento.", "Fisica", "Neutro", 2, 16, "EnemigoUno")
tech("i_engranaje", "Engranaje", "Un engranaje proyectado a gran velocidad.", "Magica", "Metal", 3, 14, "EnemigoUno")
tech("i_pasarela", "Pasarela", "Abre una pasarela temporal: todo el grupo gana Prisa.", "Apoyo", "Neutro", 10, 0, "AliadosTodos", applyStatus="Prisa", statusChance=1.0, statusDuration=3)
tech("i_placas", "Placas", "Placas de blindaje para todo el grupo (Protección).", "Apoyo", "Neutro", 8, 0, "AliadosTodos", applyStatus="Proteccion", statusChance=1.0, statusDuration=4)
tech("i_recalibrar", "Recalibrar", "Corrige cualquier alteración de estado de un aliado.", "Apoyo", "Neutro", 4, 0, "AliadoUno", cureStatuses=["Veneno", "Sueno", "Aturdido", "Silencio", "Ceguera", "Confusion", "Olvido", "Lentitud"])
tech("i_prensa_hidraulica", "Prensa hidráulica", "Aplasta a un enemigo con fuerza industrial.", "Fisica", "Metal", 12, 34, "EnemigoUno")
# Anida (Vacío)
tech("d_golpe_de_orbe", "Golpe de orbe", "El orbe de coral golpea con el peso de tres voluntades.", "Fisica", "Neutro", 2, 14, "EnemigoUno")
tech("d_tres_voces", "Tres voces", "Tres conciencias atacan a la vez.", "Magica", "Vacio", 4, 15, "EnemigoUno")
tech("d_desacople", "Desacople", "Separa la intención de la acción. Puede confundir.", "Magica", "Vacio", 5, 0, "EnemigoUno", applyStatus="Confusion", statusChance=0.4, statusDuration=2)
tech("d_coral_sereno", "Coral sereno", "Cura a un aliado y le concede Regeneración.", "Apoyo", "Neutro", 6, 22, "AliadoUno", healing=True, applyStatus="Regeneracion", statusChance=1.0, statusDuration=4)
tech("d_disenso", "Disenso", "Cada voz golpea por su lado a todos los enemigos.", "Magica", "Vacio", 10, 20, "EnemigosTodos")
tech("d_tres_desacuerdos", "Tres desacuerdos", "Rompe una predicción sin daño: puede causar Olvido.", "Magica", "Neutro", 8, 0, "EnemigoUno", applyStatus="Olvido", statusChance=0.6, statusDuration=3)
tech("d_futuro_no_previsto", "Futuro no previsto", "Lo que nadie calculó cae sobre todos los enemigos.", "Magica", "Vacio", 18, 40, "EnemigosTodos")
# Criaturas
tech("c_mordisco", "Mordisco", "Una mordida rápida.", "Fisica", "Neutro", 1, 13, "EnemigoUno")
tech("c_aullido_viento", "Aullido de viento", "Un aullido que corta como ráfaga.", "Magica", "Viento", 4, 14, "EnemigoUno")
tech("c_galerna", "Galerna", "Viento huracanado sobre todos los enemigos.", "Magica", "Viento", 9, 18, "EnemigosTodos")
tech("c_caparazon", "Caparazón", "Se cubre y gana Protección.", "Apoyo", "Neutro", 4, 0, "Propio", applyStatus="Proteccion", statusChance=1.0, statusDuration=4)
tech("c_terremoto_lento", "Terremoto lento", "Un temblor pesado contra todos.", "Magica", "Tierra", 8, 18, "EnemigosTodos")
tech("c_salpicon", "Salpicón", "Chorro de agua a presión.", "Magica", "Agua", 3, 14, "EnemigoUno")
tech("c_marejada", "Marejada", "Una ola que barre a todos los enemigos.", "Magica", "Agua", 9, 18, "EnemigosTodos")
tech("c_picado", "Picado", "Se lanza en picado contra un enemigo.", "Fisica", "Viento", 3, 18, "EnemigoUno")
tech("c_ascua", "Ascua", "Escupe una brasa.", "Magica", "Fuego", 3, 14, "EnemigoUno")
tech("c_fragua", "Fragua", "Calor de horno sobre todos los enemigos.", "Magica", "Fuego", 9, 18, "EnemigosTodos")
tech("c_dar_cuerda", "Dar cuerda", "Le da cuerda a un aliado (Prisa).", "Apoyo", "Neutro", 5, 0, "AliadoUno", applyStatus="Prisa", statusChance=1.0, statusDuration=3)
tech("c_chirrido", "Chirrido", "Un chirrido metálico. Puede aturdir.", "Magica", "Metal", 4, 14, "EnemigoUno", applyStatus="Aturdido", statusChance=0.15, statusDuration=1)
tech("c_estatica", "Estática", "Una descarga pequeña pero precisa.", "Magica", "Rayo", 3, 14, "EnemigoUno")
tech("c_ecolocacion", "Ecolocación", "Desorienta con ultrasonidos. Puede cegar.", "Magica", "Rayo", 5, 0, "EnemigoUno", applyStatus="Ceguera", statusChance=0.5, statusDuration=3)
tech("c_luz_medusa", "Luz de medusa", "Una luz suave que cura a un aliado.", "Apoyo", "Neutro", 5, 18, "AliadoUno", healing=True)
tech("c_aguijon_luz", "Aguijón de luz", "Filamento luminoso. Puede aturdir.", "Magica", "Luz", 4, 14, "EnemigoUno", applyStatus="Aturdido", statusChance=0.2, statusDuration=1)
tech("c_polvo_nocturno", "Polvo nocturno", "Escamas que adormecen a todos los enemigos.", "Magica", "Vacio", 5, 0, "EnemigosTodos", applyStatus="Sueno", statusChance=0.3, statusDuration=2)
tech("c_ala_vacia", "Ala vacía", "Un aleteo que borra un instante.", "Magica", "Vacio", 4, 15, "EnemigoUno")
tech("c_promesa", "Promesa", "Una promesa cumplida cura a todo el grupo.", "Apoyo", "Neutro", 6, 14, "AliadosTodos", healing=True)
tech("c_juramento", "Juramento", "La fuerza de lo prometido contra un enemigo.", "Magica", "Luz", 10, 26, "EnemigoUno")
tech("c_pinza", "Pinza", "Una pinza que no suelta.", "Fisica", "Metal", 2, 16, "EnemigoUno")
tech("c_bastion", "Bastión", "Se planta delante del grupo: todos ganan Protección.", "Apoyo", "Neutro", 8, 0, "AliadosTodos", applyStatus="Proteccion", statusChance=1.0, statusDuration=3)
# Enemigos
tech("e_vaciar", "Vaciar", "Arranca un recuerdo. Puede causar Olvido.", "Magica", "Vacio", 0, 12, "EnemigoUno", applyStatus="Olvido", statusChance=0.15, statusDuration=2)
tech("e_grito_hueco", "Grito hueco", "Un grito sin voz que golpea a todos.", "Magica", "Vacio", 4, 10, "EnemigosTodos")
tech("e_mordida_anguila", "Mordida de anguila", "Muerde desde el agua turbia.", "Fisica", "Agua", 0, 12, "EnemigoUno")
tech("e_descarga_anguila", "Descarga", "Una descarga desde el agua.", "Magica", "Rayo", 3, 12, "EnemigoUno")
tech("e_escaneo", "Escaneo", "Un barrido de luz. Puede cegar.", "Magica", "Luz", 0, 0, "EnemigoUno", applyStatus="Ceguera", statusChance=0.3, statusDuration=2)
tech("e_rayo_dron", "Rayo de dron", "Disparo eléctrico de baja potencia.", "Magica", "Rayo", 2, 12, "EnemigoUno")
tech("e_citacion", "Citación", "Una orden oficial. Puede silenciar.", "Magica", "Luz", 3, 0, "EnemigoUno", applyStatus="Silencio", statusChance=0.4, statusDuration=3)
tech("e_corregir_registro", "Corregir registro", "Reescribe a alguien según su expediente. Puede causar Olvido.", "Magica", "Luz", 4, 14, "EnemigoUno", applyStatus="Olvido", statusChance=0.25, statusDuration=2)
tech("e_sello_legal", "Sello legal", "Un sello que cae sobre todo el grupo.", "Magica", "Metal", 6, 12, "EnemigosTodos")
tech("e_reparar", "Autorreparación", "Repara a una unidad aliada.", "Apoyo", "Neutro", 5, 20, "AliadoUno", healing=True)
tech("e_lanza", "Lanza", "Una estocada de lanza.", "Fisica", "Neutro", 0, 14, "EnemigoUno")
tech("e_censo", "Censo", "Cuenta y clasifica: puede ralentizar a todos.", "Magica", "Tierra", 4, 10, "EnemigosTodos", applyStatus="Lentitud", statusChance=0.3, statusDuration=2)

# ------------------------------------------------------------------ personajes
characters = []
def char(id, name, bio, era, age, element, weapon, base, grow, learn, equip):
    characters.append({
        "id": id, "era": era, "age": age, "element": element, "weaponType": weapon,
        "baseStats": base, "growth": grow,
        "learnset": [{"level": l, "techniqueId": t} for l, t in learn],
        "startingEquipment": equip,
    })
    es[f"char.{id}.name"] = name
    es[f"char.{id}.bio"] = bio

char("violeta", "Violeta", "16 años, año 2000. Graba sonidos para no olvidar la voz de su abuela. Memoria auditiva excelente y poca fe en las versiones oficiales.",
     "2000", 16, "Viento", "cable",
     stats(78, 18, 9, 11, 6, 8, 12, 10, 10, 7), growth(10, 2.6, 1.4, 1.8, 1.0, 1.3, 0.6, 0.4, 0.5, 0.3),
     [(1, "v_latigazo"), (1, "v_eco"), (4, "v_rebobinar"), (8, "v_vendaval"), (12, "v_nota_sostenida"), (18, "v_contrapunto"), (26, "v_cancion_inversa")],
     {"Arma": "cable_microfono", "Peto": "chaqueta_escolar", "Botas": "zapatillas_lona"})
char("angel", "Ángel", "17 años, año 2000. Repara radios y fabrica piezas con chatarra. Cuida a otros para no tener que pedir ayuda.",
     "2000", 17, "Rayo", "espada",
     stats(88, 12, 12, 8, 8, 6, 10, 10, 7, 6), growth(12, 1.8, 1.9, 1.2, 1.3, 0.9, 0.5, 0.5, 0.4, 0.3),
     [(1, "a_tajo"), (1, "a_pulso"), (5, "a_corte_cruzado"), (9, "a_chispazo"), (13, "a_escudo_chatarra"), (20, "a_sobrecarga"), (27, "a_tormenta_de_antena")],
     {"Arma": "espada_de_utileria", "Peto": "campera_de_taller", "Guantes": "guantes_de_taller", "Botas": "zapatillas_lona"})
char("sura", "Sura", "32 años, 330 a. C. Escriba sorda de Lira; fundó un archivo comunitario con los nombres excluidos de los censos.",
     "-330", 32, "Tierra", "estilo",
     stats(80, 20, 9, 12, 7, 10, 9, 10, 8, 6), growth(10, 2.8, 1.2, 1.9, 1.1, 1.5, 0.4, 0.4, 0.4, 0.3),
     [(1, "s_tablilla"), (1, "s_vibracion"), (1, "s_escudo_pacto"), (10, "s_sismo"), (15, "s_nombre_devuelto"), (22, "s_alfabeto_barro")],
     {"Arma": "estilo_de_cana", "Peto": "tunica_de_lino", "Botas": "sandalias_lira"})
char("teo", "Teo", "26 años, 1891. Tipógrafo y organizador de una huelga. Quiere justicia y teme que la verdad destruya el movimiento.",
     "1891", 26, "Fuego", "martillo",
     stats(92, 14, 13, 9, 9, 6, 8, 9, 6, 7), growth(12.5, 2.0, 2.0, 1.3, 1.3, 0.9, 0.4, 0.4, 0.3, 0.3),
     [(1, "t_martillazo"), (1, "t_tinta_ardiente"), (1, "t_plancha_caliente"), (12, "t_llamarada_prensa"), (16, "t_eco_mecanico"), (24, "t_edicion_extra")],
     {"Arma": "martillo_tipografico", "Peto": "delantal_de_imprenta", "Guantes": "guantes_de_cuero"})
char("lia", "Lía", "42 años, 2026. Archivera judicial. Descubre que su firma permitió un protocolo de supresión documental.",
     "2026", 42, "Luz", "linterna",
     stats(74, 22, 7, 12, 6, 10, 10, 11, 8, 8), growth(9.5, 3.0, 1.0, 2.0, 1.0, 1.5, 0.5, 0.5, 0.4, 0.4),
     [(1, "l_carpetazo"), (1, "l_destello"), (1, "l_auditoria"), (7, "l_luz_de_archivo"), (11, "l_foco"), (19, "l_sentencia_clara")],
     {"Arma": "linterna_tactica", "Peto": "saco_de_archivo", "Botas": "zapatos_de_oficina"})
char("anouk", "Anouk", "36 años, 2140. Médica de una ciudad flotante que reparte tratamientos de memoria.",
     "2140", 36, "Agua", "bisturi",
     stats(76, 22, 9, 12, 6, 9, 11, 12, 9, 6), growth(9.5, 3.0, 1.3, 1.9, 1.0, 1.4, 0.5, 0.5, 0.5, 0.3),
     [(1, "n_incision"), (1, "n_marea"), (1, "n_sutura"), (1, "n_triaje"), (14, "n_sedante"), (20, "n_diluvio"), (25, "n_desfibrilar")],
     {"Arma": "bisturi_de_dique", "Peto": "bata_impermeable", "Guantes": "guantes_quirurgicos"})
char("imen", "Imen", "71 años cronológicos, 2749. Restauradora de infraestructuras de Ciudad Pila; participó en el primer prototipo del Archivo.",
     "2749", 71, "Metal", "guantelete",
     stats(90, 16, 13, 10, 10, 8, 9, 10, 6, 5), growth(12, 2.2, 1.9, 1.5, 1.4, 1.1, 0.4, 0.4, 0.3, 0.3),
     [(1, "i_puno_servicio"), (1, "i_engranaje"), (1, "i_pasarela"), (1, "i_placas"), (1, "i_recalibrar"), (20, "i_prensa_hidraulica")],
     {"Arma": "guantelete_pila", "Casco": "visor_de_servicio", "Peto": "traje_de_mantenimiento"})
char("anida", "Anida", "8800. Tres conciencias que comparten un cuerpo. Ninguna de sus voces manda.",
     "8800", 0, "Vacio", "orbe",
     stats(72, 26, 7, 14, 6, 12, 11, 10, 10, 9), growth(9, 3.4, 1.0, 2.2, 1.0, 1.6, 0.5, 0.4, 0.5, 0.4),
     [(1, "d_golpe_de_orbe"), (1, "d_tres_voces"), (1, "d_desacople"), (1, "d_coral_sereno"), (1, "d_disenso"), (1, "d_tres_desacuerdos"), (30, "d_futuro_no_previsto")],
     {"Arma": "orbe_coral", "Peto": "manto_de_baja_luz"})

# ------------------------------------------------------------------ objetos
items = []
def item(id, name, desc, category, **kw):
    it = {"id": id, "category": category}
    it.update(kw)
    items.append(it)
    es[f"item.{id}.name"] = name
    es[f"item.{id}.desc"] = desc

def equip(id, name, desc, slot, st, price, **kw):
    item(id, name, desc, "Equipo", slot=slot, stats=st, price=price, **kw)

# Armas
equip("cable_microfono", "Cable de micrófono", "El cable de la radio estudiantil. Ya quemó uno.", "Arma", mods(Fuerza=4, Magia=2), 0, weaponType="cable", element="Viento", allowedActors=["violeta"])
equip("cable_trenzado", "Cable trenzado", "Tres cables trenzados: más alcance, más eco.", "Arma", mods(Fuerza=9, Magia=4), 320, weaponType="cable", element="Viento", allowedActors=["violeta"])
equip("cable_de_cobre", "Cable de cobre de 1891", "Cobre de los talleres del Barrio Carbón.", "Arma", mods(Fuerza=15, Magia=7), 900, weaponType="cable", element="Viento", allowedActors=["violeta"])
equip("espada_de_utileria", "Espada de utilería", "Del taller de teatro del instituto. Ángel le soldó un alma de hierro.", "Arma", mods(Fuerza=5), 0, weaponType="espada", allowedActors=["angel"])
equip("espada_de_bronce", "Espada de bronce", "Forjada en Lira. Pesada y honesta.", "Arma", mods(Fuerza=10), 380, weaponType="espada", allowedActors=["angel"])
equip("sable_de_talam", "Sable de Talam", "Sable curvo de los navegantes de 1542.", "Arma", mods(Fuerza=16, Velocidad=1), 950, weaponType="espada", allowedActors=["angel"])
equip("estilo_de_cana", "Estilo de caña", "Herramienta de escriba con punta de hueso.", "Arma", mods(Fuerza=4, Magia=4), 0, weaponType="estilo", allowedActors=["sura"])
equip("martillo_tipografico", "Martillo tipográfico", "Para ajustar tipos móviles… y otras cosas.", "Arma", mods(Fuerza=7), 0, weaponType="martillo", allowedActors=["teo"])
equip("linterna_tactica", "Linterna táctica", "Linterna de archivo con modo destello.", "Arma", mods(Fuerza=3, Magia=4), 0, weaponType="linterna", element="Luz", allowedActors=["lia"])
equip("bisturi_de_dique", "Bisturí de dique", "Instrumental de los hospitales flotantes.", "Arma", mods(Fuerza=5, Precision=3), 0, weaponType="bisturi", allowedActors=["anouk"])
equip("guantelete_pila", "Guantelete de Ciudad Pila", "Herramienta de mantenimiento de servidores-catedral.", "Arma", mods(Fuerza=8), 0, weaponType="guantelete", element="Metal", allowedActors=["imen"])
equip("orbe_coral", "Orbe de coral", "Un orbe vivo que canta con tres voces.", "Arma", mods(Magia=8), 0, weaponType="orbe", element="Vacio", allowedActors=["anida"])
# Cascos
equip("gorra_de_lana", "Gorra de lana", "Del año 2000. Abriga y algo protege.", "Casco", mods(Defensa=2), 60)
equip("vincha_de_tela", "Vincha de tela", "Tela de Lira para la frente.", "Casco", mods(Defensa=1, Evasion=2), 70)
equip("casco_de_bronce", "Casco de bronce", "Casco de guardia de Lira.", "Casco", mods(Defensa=5, Velocidad=-1), 260)
equip("visor_de_servicio", "Visor de servicio", "Visor de mantenimiento de 2749.", "Casco", mods(Defensa=3, Precision=2), 0)
# Petos
equip("chaqueta_escolar", "Chaqueta escolar", "Con el escudo del Instituto Público del Muelle.", "Peto", mods(Defensa=3), 50)
equip("campera_de_taller", "Campera de taller", "Manchada de grasa y estaño.", "Peto", mods(Defensa=4), 70)
equip("tunica_de_lino", "Túnica de lino", "Lino de Lira.", "Peto", mods(Defensa=3, DefensaMagica=2), 90)
equip("delantal_de_imprenta", "Delantal de imprenta", "Cuero curtido contra la tinta y el calor.", "Peto", mods(Defensa=4), 0, resistances={"Fuego": 0.5})
equip("saco_de_archivo", "Saco de archivo", "Saco de oficina con bolsillos para todo.", "Peto", mods(Defensa=3, DefensaMagica=3), 0)
equip("bata_impermeable", "Bata impermeable", "Para trabajar con el agua a la cintura.", "Peto", mods(Defensa=4), 0, resistances={"Agua": 0.5})
equip("traje_de_mantenimiento", "Traje de mantenimiento", "Traje de Ciudad Pila con aislamiento.", "Peto", mods(Defensa=6, DefensaMagica=3), 0, resistances={"Rayo": 0.5})
equip("manto_de_baja_luz", "Manto de baja luz", "Tejido orgánico del año 8800.", "Peto", mods(Defensa=4, DefensaMagica=7), 0)
equip("peto_de_cuero", "Peto de cuero", "Cuero endurecido.", "Peto", mods(Defensa=6), 180)
equip("coraza_de_lino_prensado", "Coraza de lino prensado", "Capas de lino encolado, ligeras y resistentes.", "Peto", mods(Defensa=8, DefensaMagica=2), 420)
# Guantes
equip("guantes_de_taller", "Guantes de taller", "Para soldar sin quemarse.", "Guantes", mods(Defensa=1, Fuerza=1), 40)
equip("guantes_de_cuero", "Guantes de cuero", "Cuero de imprenta.", "Guantes", mods(Defensa=2), 80)
equip("guantes_quirurgicos", "Guantes quirúrgicos", "Precisión ante todo.", "Guantes", mods(Defensa=1, Precision=3), 0)
equip("brazales_de_bronce", "Brazales de bronce", "Brazales de Lira.", "Guantes", mods(Defensa=4), 240)
# Botas
equip("zapatillas_lona", "Zapatillas de lona", "Gastadas de tanto caminar el puerto.", "Botas", mods(Defensa=1, Velocidad=1), 40)
equip("sandalias_lira", "Sandalias de Lira", "Ligeras, para la piedra caliza.", "Botas", mods(Defensa=1, Evasion=2), 60)
equip("zapatos_de_oficina", "Zapatos de oficina", "Lustrados. Sorprendentemente cómodos.", "Botas", mods(Defensa=1, DefensaMagica=1), 0)
equip("botas_de_marino", "Botas de marino", "De los navegantes de Talam.", "Botas", mods(Defensa=3), 200)
# Accesorios (objetos mágicos)
equip("pulsera_de_cinta", "Pulsera de cinta", "Cinta magnética trenzada. Aumenta la Magia.", "Accesorio", mods(Magia=4), 150)
equip("amuleto_de_arcilla", "Amuleto de arcilla", "Un signo de pacto de Lira. Aumenta la Defensa mágica.", "Accesorio", mods(DefensaMagica=5), 200)
equip("reloj_sin_agujas", "Reloj sin agujas", "«Para viajeros», dijo el vendedor. Aumenta la Velocidad.", "Accesorio", mods(Velocidad=4), 600)
equip("anillo_de_marea", "Anillo de marea", "Aumenta los PM máximos.", "Accesorio", mods(MaxMp=15), 350)
equip("insignia_de_huelga", "Insignia de huelga", "Aumenta la Fuerza.", "Accesorio", mods(Fuerza=5), 400)
equip("auriculares_aislantes", "Auriculares aislantes", "Protegen de la Confusión y del Sueño.", "Accesorio", mods(DefensaMagica=1), 500, statusImmunities=["Confusion", "Sueno"])
# Equipo de mascota
equip("collar_de_cuero", "Collar de cuero", "Collar sencillo. Aumenta la Fuerza de la mascota.", "Collar", mods(Fuerza=3), 80, allowedActors=["mascota"])
equip("collar_de_resonancia", "Collar de resonancia", "Vibra con el Diapasón. Aumenta Magia y PM.", "Collar", mods(Magia=4, MaxMp=5), 260, allowedActors=["mascota"])
equip("collar_de_bronce", "Collar de bronce", "Aumenta Fuerza y Defensa de la mascota.", "Collar", mods(Fuerza=6, Defensa=2), 420, allowedActors=["mascota"])
equip("manta_acolchada", "Manta acolchada", "Armadura blanda para mascotas.", "ArmaduraMascota", mods(Defensa=3, MaxHp=15), 90, allowedActors=["mascota"])
equip("malla_ligera", "Malla ligera", "Malla metálica a medida.", "ArmaduraMascota", mods(Defensa=6, Velocidad=-1), 300, allowedActors=["mascota"])
# Consumibles
def cons(id, name, desc, price, effect, battle=True, field=True):
    item(id, name, desc, "Consumible", price=price, effect=effect, usableInBattle=battle, usableInField=field)
cons("venda", "Venda", "Recupera 60 de Vida.", 20, {"healHp": 60})
cons("botiquin", "Botiquín", "Recupera 200 de Vida.", 90, {"healHp": 200})
cons("agua_manantial", "Agua de manantial", "Recupera 15 PM.", 40, {"healMp": 15})
cons("te_de_hierbas", "Té de hierbas", "Recupera 40 PM.", 120, {"healMp": 40})
cons("sales_de_olor", "Sales de olor", "Reanima a un aliado con el 25 % de su Vida.", 150, {"revive": 0.25})
cons("antidoto", "Antídoto", "Cura el Veneno.", 15, {"cureStatuses": ["Veneno"]})
cons("colirio", "Colirio", "Cura la Ceguera.", 15, {"cureStatuses": ["Ceguera"]})
cons("campanilla", "Campanilla", "Despierta y devuelve la calma (Sueño, Confusión).", 25, {"cureStatuses": ["Sueno", "Confusion"]})
cons("recuerdo_embotellado", "Recuerdo embotellado", "Cura el Olvido y el Silencio.", 60, {"cureStatuses": ["Olvido", "Silencio"]})
cons("caramelo_de_miel", "Caramelo de miel", "Recupera 30 de Vida a todo el grupo.", 70, {"healHp": 30, "allAllies": True})
# Ofrendas para el Vínculo
def offering(id, name, desc, bonus, price):
    item(id, name, desc, "Ofrenda", bondingBonus=bonus, price=price)
offering("bizcocho", "Bizcocho", "Del almacén de la esquina, año 2000. A los perros callejeros les encanta.", 0.05, 10)
offering("pan_de_lira", "Pan de Lira", "Pan de cebada con aceite.", 0.05, 12)
offering("pescado_seco", "Pescado seco", "Salado al sol de Talam.", 0.05, 12)
offering("carbon_dulce", "Carbón dulce", "Carbón vegetal aromático. Las salamandras lo adoran.", 0.05, 15)
offering("tuerca_brillante", "Tuerca brillante", "Pulida hasta reflejar. Irresistible para los autómatas.", 0.05, 15)
offering("flor_de_estuario", "Flor de estuario", "Néctar dulce de la orilla.", 0.05, 18)
offering("nectar_coral", "Néctar coral", "Gotas del Jardín de Baja Luz.", 0.08, 40)
# Materiales de evolución
def evo_item(id, name, desc, price):
    item(id, name, desc, "Clave", price=price)
evo_item("fragmento_de_eco", "Fragmento de eco", "Un sonido solidificado. Despierta el potencial de una criatura vinculada.", 0)
evo_item("escama_de_marea", "Escama de marea", "Escama que guarda el ritmo de las mareas de Talam.", 0)
evo_item("ascua_eterna", "Ascua eterna", "Una brasa que no se apaga desde 1891.", 0)
evo_item("engranaje_maestro", "Engranaje maestro", "Pieza central de una máquina que ya no existe.", 0)
evo_item("coral_de_baja_luz", "Coral de baja luz", "Coral vivo del año 8800.", 0)
# Objetos clave del guion
item("cinta_2026", "Cinta «28-X-2026»", "«PARA QUIEN SEA QUE REGRESE». Una voz adulta que se parece a la de Violeta.", "Clave")
item("llave_de_marta", "Llave mecánica de Marta", "Marta la entregó en la estación de bombeo N.º 4.", "Clave")
item("ficha_gabriel", "Ficha de Gabriel Mora", "Primera ficha de ausente. 16 años. Nadie lo recuerda.", "Clave")
item("diapason_de_arcilla", "Diapasón de Arcilla", "Abre Nudos conectados por testimonios. También permite sintonizar con criaturas.", "Clave")

# ------------------------------------------------------------------ criaturas
creatures = []
def species(id, name, desc, family, stage, era, element, temperament, base, grow, learn, evolutions=(), bondable=True, difficulty=2, favorite=None, condition=None):
    s = {"id": id, "family": family, "stage": stage, "era": era, "element": element, "temperament": temperament,
         "baseStats": base, "growth": grow,
         "learnset": [{"level": l, "techniqueId": t} for l, t in learn],
         "evolutions": list(evolutions), "bondable": bondable, "bondDifficulty": difficulty}
    if favorite: s["favoriteOfferingId"] = favorite
    if condition: s["bondCondition"] = condition
    creatures.append(s)
    es[f"creature.{id}.name"] = name
    es[f"creature.{id}.desc"] = desc

def evo(to, level, affinity, item=None, era=None, flag=None):
    e = {"toSpeciesId": to, "minLevel": level, "minAffinity": affinity}
    if item: e["requiredItemId"] = item
    if era: e["requiredEra"] = era
    if flag: e["requiredFlag"] = flag
    return e

G_FAST = growth(8, 1.6, 1.4, 1.0, 0.9, 0.8, 0.6, 0.4, 0.5, 0.3)
G_TANK = growth(10, 1.4, 1.2, 0.9, 1.4, 1.1, 0.3, 0.4, 0.2, 0.3)
G_MAGE = growth(7.5, 2.2, 0.9, 1.6, 0.8, 1.2, 0.5, 0.4, 0.4, 0.3)
G_BAL = growth(8.5, 1.8, 1.2, 1.2, 1.0, 1.0, 0.5, 0.4, 0.4, 0.3)

# Callejero (2000, Viento)
species("tropel", "Tropel", "Cachorro callejero del puerto. Desde que oyó la cinta, el viento lo sigue.", "callejero", 1, "2000", "Viento", "Curioso",
        stats(60, 12, 9, 6, 5, 4, 13, 9, 9, 6), G_FAST, [(1, "c_mordisco"), (1, "c_aullido_viento"), (18, "c_galerna")],
        [evo("ventolero", 14, 30)], difficulty=1, favorite="bizcocho")
species("ventolero", "Ventolero", "Perro de pelaje revuelto que corre más rápido que su sombra.", "callejero", 2, "2000", "Viento", "Curioso",
        stats(85, 18, 13, 8, 7, 6, 17, 11, 12, 7), G_FAST, [(1, "c_mordisco"), (1, "c_aullido_viento"), (18, "c_galerna")],
        [evo("galerno", 32, 70, item="fragmento_de_eco")], difficulty=1, favorite="bizcocho")
species("galerno", "Galerno", "Un lebrel enorme hecho de ráfagas. Lleva las voces del puerto en el pelaje.", "callejero", 3, "2000", "Viento", "Curioso",
        stats(120, 26, 20, 12, 10, 9, 22, 13, 15, 9), G_FAST, [(1, "c_mordisco"), (1, "c_aullido_viento"), (1, "c_galerna")], difficulty=1, favorite="bizcocho")
# Galápago (330 a. C., Tierra)
species("galapago_olivar", "Galápago del olivar", "Pequeña tortuga de los olivares de Lira. No tiene prisa.", "galapago", 1, "-330", "Tierra", "Sereno",
        stats(75, 10, 8, 6, 9, 6, 6, 8, 3, 6), G_TANK, [(1, "c_mordisco"), (1, "c_caparazon"), (16, "c_terremoto_lento")],
        [evo("caparazon_anfora", 16, 30)], difficulty=2, favorite="pan_de_lira")
species("caparazon_anfora", "Caparazón de ánfora", "Su caparazón imita las ánforas de los mercaderes.", "galapago", 2, "-330", "Tierra", "Sereno",
        stats(105, 14, 11, 8, 14, 9, 7, 9, 4, 7), G_TANK, [(1, "c_mordisco"), (1, "c_caparazon"), (16, "c_terremoto_lento")],
        [evo("atlas_de_piedra", 34, 70, item="fragmento_de_eco")], difficulty=2, favorite="pan_de_lira")
species("atlas_de_piedra", "Atlas de piedra", "Sobre su caparazón crecen olivos diminutos y nombres grabados.", "galapago", 3, "-330", "Tierra", "Sereno",
        stats(150, 20, 16, 11, 20, 13, 8, 10, 5, 8), G_TANK, [(1, "c_mordisco"), (1, "c_caparazon"), (1, "c_terremoto_lento")], difficulty=2, favorite="pan_de_lira")
# Nutria (1542, Agua)
species("nutrilla", "Nutrilla", "Cría de nutria de los canales de Talam. Roba conchas y las devuelve.", "nutria", 1, "1542", "Agua", "Curioso",
        stats(62, 14, 8, 9, 5, 6, 12, 9, 10, 7), G_BAL, [(1, "c_mordisco"), (1, "c_salpicon"), (17, "c_marejada")],
        [evo("nutria_de_mareas", 15, 30)], difficulty=2, favorite="pescado_seco")
species("nutria_de_mareas", "Nutria de mareas", "Conoce las corrientes mejor que cualquier carta náutica.", "nutria", 2, "1542", "Agua", "Curioso",
        stats(88, 20, 11, 13, 7, 9, 15, 11, 12, 8), G_BAL, [(1, "c_mordisco"), (1, "c_salpicon"), (17, "c_marejada")],
        [evo("marejada", 33, 70, item="escama_de_marea", era="1542")], difficulty=2, favorite="pescado_seco")
species("marejada", "Marejada", "Sólo evoluciona en las aguas de Talam. Lleva la marea consigo.", "nutria", 3, "1542", "Agua", "Curioso",
        stats(125, 28, 15, 19, 10, 13, 19, 13, 14, 9), G_BAL, [(1, "c_mordisco"), (1, "c_salpicon"), (1, "c_marejada")], difficulty=2, favorite="pescado_seco")
# Fragata (1542, Viento)
species("pichon_de_fragata", "Pichón de fragata", "Ave marina de alas enormes para su tamaño. Orgullosa.", "fragata", 1, "1542", "Viento", "Orgulloso",
        stats(58, 12, 11, 7, 5, 5, 15, 11, 12, 6), G_FAST, [(1, "c_picado"), (1, "c_aullido_viento"), (20, "c_galerna")],
        [evo("fragata_tormenta", 20, 40)], difficulty=4, favorite="pescado_seco")
species("fragata_tormenta", "Fragata tormenta", "Planea dentro de los temporales sin batir las alas.", "fragata", 2, "1542", "Viento", "Orgulloso",
        stats(90, 20, 17, 10, 8, 8, 21, 14, 16, 8), G_FAST, [(1, "c_picado"), (1, "c_aullido_viento"), (20, "c_galerna")], difficulty=4, favorite="pescado_seco")
# Salamandra (1891, Fuego)
species("ascuela", "Ascuela", "Salamandra que vive en los hornos apagados del Barrio Carbón. Muy tímida.", "salamandra", 1, "1891", "Fuego", "Timido",
        stats(58, 14, 7, 10, 5, 6, 11, 9, 9, 6), G_MAGE, [(1, "c_mordisco"), (1, "c_ascua"), (17, "c_fragua")],
        [evo("salamandra_de_horno", 15, 30)], difficulty=3, favorite="carbon_dulce")
species("salamandra_de_horno", "Salamandra de horno", "Su piel brilla como una caldera a punto.", "salamandra", 2, "1891", "Fuego", "Timido",
        stats(82, 22, 10, 15, 7, 9, 13, 10, 11, 7), G_MAGE, [(1, "c_mordisco"), (1, "c_ascua"), (17, "c_fragua")],
        [evo("salamandra_caldera", 33, 70, item="ascua_eterna")], difficulty=3, favorite="carbon_dulce")
species("salamandra_caldera", "Salamandra caldera", "Calienta una barriada entera en invierno. Nadie le puso precio.", "salamandra", 3, "1891", "Fuego", "Timido",
        stats(115, 32, 14, 22, 10, 13, 16, 12, 13, 8), G_MAGE, [(1, "c_mordisco"), (1, "c_ascua"), (1, "c_fragua")], difficulty=3, favorite="carbon_dulce")
# Grillo de latón (1891, Metal) — autómata animal de taller
species("grillo_de_laton", "Grillo de latón", "Juguete de cuerda de un taller obrero que empezó a cantar solo.", "grillo", 1, "1891", "Metal", "Curioso",
        stats(60, 12, 9, 8, 8, 5, 10, 10, 7, 6), G_BAL, [(1, "c_chirrido"), (1, "c_dar_cuerda"), (18, "c_pinza")],
        [evo("cigarra_de_vapor", 18, 35)], difficulty=2, favorite="tuerca_brillante")
species("cigarra_de_vapor", "Cigarra de vapor", "Silba como una locomotora en miniatura.", "grillo", 2, "1891", "Metal", "Curioso",
        stats(86, 18, 13, 11, 12, 8, 12, 12, 9, 7), G_BAL, [(1, "c_chirrido"), (1, "c_dar_cuerda"), (18, "c_pinza")],
        [evo("mantis_locomotora", 35, 70, item="engranaje_maestro")], difficulty=2, favorite="tuerca_brillante")
species("mantis_locomotora", "Mantis locomotora", "Una mantis de bielas y pistones. Protege a quien le dio cuerda.", "grillo", 3, "1891", "Metal", "Curioso",
        stats(122, 26, 19, 15, 17, 11, 15, 14, 11, 8), G_BAL, [(1, "c_chirrido"), (1, "c_dar_cuerda"), (1, "c_pinza")], difficulty=2, favorite="tuerca_brillante")
# Murciélago (2026, Rayo)
species("pipistrelo", "Pipistrelo", "Murciélago urbano que duerme entre antenas. Su eco suena a radio mal sintonizada.", "murcielago", 1, "2026", "Rayo", "Timido",
        stats(55, 14, 8, 10, 4, 6, 14, 10, 12, 6), G_MAGE, [(1, "c_mordisco"), (1, "c_estatica"), (10, "c_ecolocacion")],
        [evo("murcielago_estatico", 15, 30)], difficulty=3, favorite="flor_de_estuario")
species("murcielago_estatico", "Murciélago estático", "Sus alas chisporrotean al plegarse.", "murcielago", 2, "2026", "Rayo", "Timido",
        stats(78, 22, 11, 15, 6, 9, 18, 12, 15, 7), G_MAGE, [(1, "c_mordisco"), (1, "c_estatica"), (10, "c_ecolocacion")],
        [evo("tormentala", 33, 70, item="fragmento_de_eco")], difficulty=3, favorite="flor_de_estuario")
species("tormentala", "Tormentala", "Cuando vuela, las pantallas de la ciudad parpadean al compás.", "murcielago", 3, "2026", "Rayo", "Timido",
        stats(108, 32, 15, 22, 8, 13, 23, 14, 18, 8), G_MAGE, [(1, "c_mordisco"), (1, "c_estatica"), (1, "c_ecolocacion")], difficulty=3, favorite="flor_de_estuario")
# Medusa (2140, Luz)
species("lumedusa", "Lumedusa", "Medusa luminosa de la Bahía de los Diques. Guía a los barcos de noche.", "medusa", 1, "2140", "Luz", "Sereno",
        stats(56, 18, 5, 11, 5, 9, 9, 9, 9, 7), G_MAGE, [(1, "c_aguijon_luz"), (1, "c_luz_medusa")],
        [evo("faro_medusa", 22, 40)], difficulty=3, favorite="flor_de_estuario")
species("faro_medusa", "Faro medusa", "Grande como una lámpara de quirófano. Su luz calma.", "medusa", 2, "2140", "Luz", "Sereno",
        stats(90, 30, 8, 18, 8, 14, 11, 11, 11, 8), G_MAGE, [(1, "c_aguijon_luz"), (1, "c_luz_medusa")], difficulty=3, favorite="flor_de_estuario")
# Cangrejo de dique (2140, Metal)
species("pinzaro", "Pinzaro", "Cangrejo que construye su caparazón con chatarra de los diques.", "cangrejo", 1, "2140", "Metal", "Orgulloso",
        stats(70, 10, 10, 5, 11, 6, 7, 9, 4, 6), G_TANK, [(1, "c_pinza"), (1, "c_caparazon"), (24, "c_bastion")],
        [evo("bastion_de_dique", 24, 40)], difficulty=3, favorite="tuerca_brillante")
species("bastion_de_dique", "Bastión de dique", "Un cangrejo del tamaño de una puerta estanca.", "cangrejo", 2, "2140", "Metal", "Orgulloso",
        stats(118, 16, 16, 7, 18, 9, 8, 10, 5, 7), G_TANK, [(1, "c_pinza"), (1, "c_caparazon"), (24, "c_bastion")], difficulty=3, favorite="tuerca_brillante")
# Escarabajo (2749, Rayo)
species("escaralambre", "Escaralambre", "Escarabajo que anida en los servidores-catedral de Ciudad Pila.", "escarabajo", 1, "2749", "Rayo", "Orgulloso",
        stats(62, 14, 10, 9, 9, 7, 10, 10, 7, 6), G_BAL, [(1, "c_estatica"), (1, "c_pinza")],
        [evo("escarabajo_catedral", 26, 50, item="engranaje_maestro")], difficulty=4, favorite="tuerca_brillante")
species("escarabajo_catedral", "Escarabajo catedral", "Sus élitros parecen vitrales que procesan datos.", "escarabajo", 2, "2749", "Rayo", "Orgulloso",
        stats(105, 24, 16, 15, 14, 11, 13, 12, 9, 8), G_BAL, [(1, "c_estatica"), (1, "c_pinza")], difficulty=4, favorite="tuerca_brillante")
# Polilla (8800, Vacío)
species("nictela", "Nictela", "Polilla del Jardín de Baja Luz. Se posa sobre lo que nadie mira.", "polilla", 1, "8800", "Vacio", "Timido",
        stats(54, 16, 6, 11, 5, 8, 13, 9, 13, 8), G_MAGE, [(1, "c_ala_vacia"), (1, "c_polvo_nocturno")],
        [evo("polilla_baja_luz", 20, 35)], difficulty=4, favorite="nectar_coral")
species("polilla_baja_luz", "Polilla de baja luz", "Sus alas muestran paisajes que todavía no existen.", "polilla", 2, "8800", "Vacio", "Timido",
        stats(80, 26, 9, 17, 7, 12, 16, 11, 16, 9), G_MAGE, [(1, "c_ala_vacia"), (1, "c_polvo_nocturno")],
        [evo("mariposa_ultimo_atardecer", 38, 80, item="coral_de_baja_luz")], difficulty=4, favorite="nectar_coral")
species("mariposa_ultimo_atardecer", "Mariposa del último atardecer", "Nadie sabe de qué futuro viene. Ella tampoco quiere decirlo.", "polilla", 3, "8800", "Vacio", "Timido",
        stats(112, 38, 12, 25, 10, 17, 20, 13, 20, 10), G_MAGE, [(1, "c_ala_vacia"), (1, "c_polvo_nocturno")], difficulty=4, favorite="nectar_coral")
# Promesín (Otras Orillas, Luz)
species("promesin", "Promesín", "Criatura hecha de una promesa que nadie llegó a cumplir. Sólo se acerca a un grupo plural.", "promesa", 1, "orillas", "Luz", "Sereno",
        stats(64, 20, 7, 12, 7, 10, 10, 10, 10, 10), G_MAGE, [(1, "c_promesa"), (30, "c_juramento")],
        [evo("juramento", 30, 80, flag="FUTURO_LIBRE")], difficulty=5, condition="PLURALIDAD>=3")
species("juramento", "Juramento", "La promesa cumplida. Brilla sin quemar.", "promesa", 2, "orillas", "Luz", "Sereno",
        stats(110, 34, 12, 22, 12, 17, 13, 13, 13, 12), G_MAGE, [(1, "c_promesa"), (1, "c_juramento")], difficulty=5)
# Cretácico: nunca vinculable (regla del guion: señal de campo, no mascota)
species("cria_emplumada", "Cría emplumada", "La cría de la orilla de Vidriamar. Si se la salvó, aparece como señal de campo en futuras incursiones. Nunca es mascota.", "cretacico", 1, "-72400000", "Viento", "Timido",
        stats(40, 0, 5, 0, 3, 3, 12, 8, 10, 5), G_FAST, [(1, "c_mordisco")], bondable=False, difficulty=5)

# ------------------------------------------------------------------ enemigos
enemies = []
def enemy(id, name, level, element, st, actions, xp, money, drops=(), boss=False, immun=(), resist=None, hint="blob"):
    e = {"id": id, "level": level, "element": element, "stats": st, "actions": actions, "xp": xp, "money": money,
         "drops": [{"itemId": i, "chance": c} for i, c in drops], "isBoss": boss, "statusImmunities": list(immun), "modelHint": hint}
    if resist: e["resistances"] = resist
    enemies.append(e)
    es[f"enemy.{id}.name"] = name

def act(tech=None, w=1, minhp=0.0, maxhp=1.0):
    a = {"weight": w}
    if tech: a["techniqueId"] = tech
    if minhp: a["minHpPercent"] = minhp
    if maxhp != 1.0: a["maxHpPercent"] = maxhp
    return a

enemy("hueco_menor", "Hueco menor", 2, "Vacio", stats(38, 10, 8, 6, 4, 4, 9, 8, 5, 2), [act(None, 3), act("e_vaciar", 1)], 14, 8, [("venda", 0.2)], hint="hueco")
enemy("hueco_ecoico", "Hueco ecoico", 3, "Viento", stats(46, 16, 7, 9, 4, 6, 11, 8, 7, 2), [act(None, 2), act("e_grito_hueco", 1)], 18, 10, [("agua_manantial", 0.2)], hint="hueco")
enemy("anguila_de_galeria", "Anguila de galería", 3, "Agua", stats(52, 12, 10, 7, 5, 5, 10, 9, 6, 3), [act("e_mordida_anguila", 2), act("e_descarga_anguila", 1)], 20, 12, [("pescado_seco", 0.3)], hint="eel")
enemy("boss_hueco", "El Hueco", 5, "Vacio", stats(620, 60, 11, 11, 6, 6, 8, 10, 2, 4),
      [act(None, 2), act("e_vaciar", 2), act("e_grito_hueco", 1, maxhp=0.6)], 150, 120, [("sales_de_olor", 1.0)], boss=True, immun=["Sueno", "Confusion", "Olvido"], hint="boss_hueco")
enemy("dron_de_vigilancia", "Dron de vigilancia", 5, "Rayo", stats(60, 20, 9, 11, 6, 8, 14, 10, 12, 3), [act("e_escaneo", 1), act("e_rayo_dron", 2)], 26, 15, [("tuerca_brillante", 0.35)], immun=["Veneno"], hint="drone")
enemy("recolector_de_identidad", "Recolector de identidad", 6, "Luz", stats(80, 15, 11, 9, 8, 7, 10, 10, 7, 4), [act(None, 2), act("e_citacion", 1)], 30, 25, [("venda", 0.3)], hint="humanoid")
enemy("guardia_automatizada", "Guardia automatizada", 7, "Metal", stats(110, 20, 14, 6, 12, 5, 7, 10, 3, 2), [act(None, 3), act("e_reparar", 1)], 38, 30, [("tuerca_brillante", 0.4)], immun=["Veneno", "Sueno"], hint="robot")
enemy("boss_fe_de_erratas", "La Fe de Erratas", 9, "Metal", stats(900, 80, 14, 15, 10, 12, 9, 12, 4, 5),
      [act(None, 2), act("e_corregir_registro", 2), act("e_sello_legal", 1, maxhp=0.6)], 300, 250, [("anillo_de_marea", 1.0)], boss=True, immun=["Sueno", "Confusion", "Olvido", "Veneno"], hint="boss_erratas")
enemy("nombre_sellado", "Nombre Sellado", 9, "Tierra", stats(120, 14, 16, 8, 13, 7, 9, 11, 5, 3), [act("e_lanza", 2), act("e_censo", 1)], 45, 30, [("pan_de_lira", 0.4)], hint="soldier")
enemy("escriba_del_magistrado", "Escriba del magistrado", 8, "Tierra", stats(85, 24, 8, 14, 7, 11, 10, 10, 6, 4), [act(None, 1), act("e_censo", 2)], 40, 35, [("amuleto_de_arcilla", 0.05)], hint="humanoid")
enemy("jabali_del_olivar", "Jabalí del olivar", 8, "Tierra", stats(130, 0, 17, 0, 10, 4, 9, 9, 4, 3), [act(None, 1)], 42, 10, [("pan_de_lira", 0.2)], hint="boar")

# ------------------------------------------------------------------ encuentros
encounters = []
def enc(id, slots, flee=True, rule=None):
    e = {"id": id, "enemies": [{"enemyId": i, "x": x, "y": y} for i, x, y in slots], "canFlee": flee}
    if rule: e["dramaticRule"] = rule
    encounters.append(e)

enc("enc_estacion_huecos", [("hueco_menor", -1.8, 3.2), ("hueco_menor", 1.8, 3.2)])
enc("enc_estacion_mixto", [("anguila_de_galeria", -1.5, 3.0), ("hueco_ecoico", 1.5, 3.6)])
enc("enc_boss_hueco", [("boss_hueco", 0, 3.8)], flee=False, rule="hueco")
enc("enc_2026_drones", [("dron_de_vigilancia", -2.0, 3.0), ("dron_de_vigilancia", 2.0, 3.0), ("recolector_de_identidad", 0, 4.0)])
enc("enc_2026_archivo", [("guardia_automatizada", -1.5, 3.4), ("dron_de_vigilancia", 1.8, 3.0)])
enc("enc_boss_erratas", [("boss_fe_de_erratas", 0, 4.2), ("guardia_automatizada", -3.0, 3.0)], flee=False, rule="fe_de_erratas")
enc("enc_lira_sellados", [("nombre_sellado", -1.8, 3.2), ("nombre_sellado", 1.8, 3.2), ("escriba_del_magistrado", 0, 4.4)])
enc("enc_lira_jabali", [("jabali_del_olivar", 0, 3.4)])

# ------------------------------------------------------------------ combos
combos = []
def combo(id, name, desc, parts, kind, element, power, target, **kw):
    c = {"id": id, "participants": [], "kind": kind, "element": element, "power": power, "target": target}
    for actor, technique, *rest in parts:
        p = {"actor": actor}
        if technique: p["techniqueId"] = technique
        if rest and rest[0]: p["petElement"] = rest[0]
        c["participants"].append(p)
    c.update(kw)
    combos.append(c)
    es[f"combo.{id}.name"] = name
    es[f"combo.{id}.desc"] = desc

combo("estatica_del_aula", "Estática del aula", "El eco de Violeta lleva el pulso de Ángel a todos los enemigos.",
      [("violeta", "v_eco"), ("angel", "a_pulso")], "Magica", "Rayo", 30, "EnemigosTodos")
combo("tajo_vendaval", "Tajo vendaval", "El cable de Violeta arrastra el corte de Ángel en un remolino.",
      [("violeta", "v_latigazo"), ("angel", "a_tajo")], "Fisica", "Viento", 34, "EnemigoArea", areaRadius=3.0)
combo("rebobinado_doble", "Rebobinado doble", "Ángel sostiene el escudo mientras Violeta rebobina a todo el grupo.",
      [("violeta", "v_rebobinar"), ("angel", "a_escudo_chatarra")], "Apoyo", "Neutro", 26, "AliadosTodos", healing=True)
combo("cancion_de_la_tierra", "Canción de la tierra", "La vibración de Sura y el eco de Violeta: la tierra canta. Puede aturdir.",
      [("violeta", "v_eco"), ("sura", "s_vibracion")], "Magica", "Tierra", 34, "EnemigosTodos", applyStatus="Aturdido", statusChance=0.25, statusDuration=1)
combo("imprenta_de_chispas", "Imprenta de chispas", "El pulso de Ángel enciende la tinta de Teo.",
      [("angel", "a_pulso"), ("teo", "t_tinta_ardiente")], "Magica", "Fuego", 36, "EnemigosTodos")
combo("cortocircuito_de_archivo", "Cortocircuito de archivo", "El chispazo de Ángel recorre la luz de Lía. Puede cegar.",
      [("angel", "a_chispazo"), ("lia", "l_destello")], "Magica", "Luz", 40, "EnemigosTodos", applyStatus="Ceguera", statusChance=0.3, statusDuration=2)
combo("cinta_luminosa", "Cinta luminosa", "Violeta rebobina mientras Lía ilumina: cura a todos.",
      [("violeta", "v_rebobinar"), ("lia", "l_luz_de_archivo")], "Apoyo", "Neutro", 34, "AliadosTodos", healing=True)
combo("marea_viva", "Marea viva", "El vendaval de Violeta empuja la marea de Anouk.",
      [("violeta", "v_vendaval"), ("anouk", "n_marea")], "Magica", "Agua", 40, "EnemigosTodos")
combo("carga_conjunta", "Carga conjunta", "Ángel y la mascota atacan juntos al mismo enemigo.",
      [("angel", "a_tajo"), ("mascota", None)], "Fisica", "Neutro", 30, "EnemigoUno", petMpCost=2)
combo("ventisca_companera", "Ventisca compañera", "El eco de Violeta y una criatura de viento se vuelven tormenta.",
      [("violeta", "v_eco"), ("mascota", None, "Viento")], "Magica", "Viento", 34, "EnemigosTodos", petMpCost=3)
combo("rayo_guia", "Rayo guía", "Una criatura de rayo guía el pulso de Ángel.",
      [("angel", "a_pulso"), ("mascota", None, "Rayo")], "Magica", "Rayo", 36, "EnemigoArea", areaRadius=3.0, petMpCost=3)
combo("testimonio_compartido", "Testimonio compartido", "Tres voces cuentan lo mismo de tres maneras. Puede silenciar.",
      [("violeta", "v_eco"), ("angel", "a_pulso"), ("lia", "l_destello")], "Magica", "Luz", 60, "EnemigosTodos", applyStatus="Silencio", statusChance=0.4, statusDuration=3)
combo("primer_trato", "Primer trato", "Viento, rayo y tierra: el pacto de Lira hecho fuerza.",
      [("violeta", "v_vendaval"), ("angel", "a_chispazo"), ("sura", "s_sismo")], "Magica", "Tierra", 70, "EnemigosTodos")
combo("las_horas_que_faltan", "Las horas que faltan", "Violeta, Ángel y su compañero, en un solo compás.",
      [("violeta", "v_contrapunto"), ("angel", "a_sobrecarga"), ("mascota", None)], "Fisica", "Vacio", 90, "EnemigosTodos", petMpCost=6)

# ------------------------------------------------------------------ banderas narrativas
flags = []
def flag(id, desc, lo=0, hi=1, init=0):
    flags.append({"id": id, "min": lo, "max": hi, "initial": init})
    es[f"flag.{id}.desc"] = desc

flag("TESTIMONIOS", "Testimonios conservados.", 0, 8)
flag("CONSENTIMIENTO", "Veces que se pidió permiso antes de actuar.", 0, 8)
flag("PLURALIDAD", "Historias distintas que se dejaron coexistir.", 0, 8)
flag("DEUDA", "Testimonios perdidos por forzar conexiones inestables.", 0, 9)
flag("CONFIANZA_VA", "Confianza entre Violeta y Ángel.", -4, 6)
flag("ANCLA_2000", "Se fijó un ancla en el año 2000.")
flag("PACTO_SURA", "Resultado del pacto con Sura (0 ninguno, 1 ocultar listado, 2 publicar, 3 reparación).", 0, 3)
flag("HUELGA_1891", "Desenlace de la huelga de 1891.", 0, 3)
flag("MAPA_1542", "Destino del mapa de Talam (1 abierto, 2 custodiado, 3 falsificado).", 0, 3)
flag("NINO_2140", "Decisión sobre el niño de 2140.", 0, 3)
flag("ARCHIVO_ABIERTO", "El Archivo de Continuidad se abrió al público.")
flag("TRIBUNAL_2749", "Resultado del tribunal de 2749.", 0, 3)
flag("FUTURO_LIBRE", "Se sostuvo un futuro sin consolidación.")
flag("OTRA_ORILLA", "Relación con las Otras Orillas.", 0, 3)
flag("MEMORIA_MARTA", "Marta conserva su memoria.")
flag("VERDAD_JULIAN", "Ángel conoce la verdad de Julián.")
flag("ECO_VIOLETA", "Se escuchó a la Violeta adulta.")
flag("FICHA_GABRIEL", "Se conservó la ficha de Gabriel Mora (E02).")
flag("PLANO_OCULTO", "Se encontró el plano oculto del aula (E01).")
flag("E03_ELECCION", "E03: 1 salvar nóminas, 2 cerrar compuerta, 3 ambas.", 0, 3)
flag("PROMESA_E14", "Promesa de la azotea (1 honestidad, 2 cuidado, 3 ninguna).", 0, 3)

# ------------------------------------------------------------------ diálogos
dialogues = []
def dialogue(id, nodes, start=None):
    d = {"id": id, "start": start or nodes[0]["id"], "nodes": nodes}
    dialogues.append(d)

def node(dlg, nid, speaker, text, nxt=None, **kw):
    key = f"dlg.{dlg}.{nid}"
    es[key] = text
    n = {"id": nid, "textKey": key}
    if speaker: n["speakerKey"] = speaker
    if nxt: n["next"] = nxt
    n.update(kw)
    return n

def choice(dlg, nid, idx, text, nxt, effects=(), cond=None):
    key = f"dlg.{dlg}.{nid}.c{idx}"
    es[key] = text
    c = {"textKey": key, "next": nxt, "effects": [{"flag": f, "op": op, "value": v} for f, op, v in effects]}
    if cond: c["condition"] = cond
    return c

V, A, L, M = "char.violeta.name", "char.angel.name", "char.lia.name", "npc.marta.name"
es["npc.marta.name"] = "Marta"
es["npc.profesora.name"] = "Profesora"
es["npc.voz_cinta.name"] = "Voz de la cinta"
es["npc.vendedor.name"] = "Vendedor sin reloj"

exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gen_act1.py"), encoding="utf-8").read())

# ------------------------------------------------------------------ textos de interfaz
ui = {
    "game.title": "Las Horas que Faltan",
    "game.subtitle": "Una aventura a través de lo que olvidamos",
    "title.new": "Nueva partida", "title.continue": "Continuar", "title.quit": "Salir",
    "menu.party": "Grupo", "menu.equip": "Equipo", "menu.techniques": "Técnicas", "menu.items": "Objetos",
    "menu.sanctuary": "Santuario", "menu.notebook": "Cuaderno de Costuras", "menu.save": "Guardar", "menu.load": "Cargar",
    "menu.options": "Opciones", "menu.close": "Cerrar", "menu.back": "Volver", "menu.money": "Monedas: {0}",
    "menu.playtime": "Tiempo: {0}", "menu.level": "Nv {0}", "menu.xp_next": "Exp. para subir: {0}",
    "menu.hp": "Vida {0}/{1}", "menu.mp": "PM {0}/{1}", "menu.empty_slot": "— vacío —", "menu.unequip": "Quitar",
    "menu.use": "Usar", "menu.on_whom": "¿En quién?", "menu.saved": "Partida guardada en la ranura {0}.", "menu.loaded": "Partida cargada.",
    "menu.slot": "Ranura {0}", "menu.slot_empty": "Ranura {0} — vacía", "menu.slot_info": "Ranura {0} — {1} · Nv {2} · {3}",
    "menu.swap_member": "Cambiar integrante", "menu.locked": "(fijo)", "menu.reserve": "Reserva",
    "menu.nothing": "No hay nada aquí todavía.", "menu.learned": "Aprendida", "menu.learns_at": "Nv {0}",
    "options.battle_mode": "Modo de combate: {0}", "options.battle_speed": "Velocidad de combate: {0}", "options.language": "Idioma: {0}",
    "battlemode.Activa": "Activo", "battlemode.Espera": "Espera",
    "sanctuary.active": "Compañero actual", "sanctuary.set_active": "Llevar como compañero", "sanctuary.evolve": "Evolucionar",
    "sanctuary.offering": "Dar ofrenda", "sanctuary.stance": "Postura: {0}", "sanctuary.release": "Liberar",
    "sanctuary.equip": "Equipar", "sanctuary.affinity": "Afinidad {0}/100", "sanctuary.capacity": "Criaturas: {0}/{1}",
    "sanctuary.can_evolve": "¡Puede evolucionar!", "sanctuary.evolved": "{0} evolucionó en {1}.", "sanctuary.no_creatures": "Aún no hay criaturas vinculadas. Usa el Diapasón cerca de una criatura salvaje.",
    "sanctuary.release_confirm": "¿Devolver a {0} a su hábitat? No se puede deshacer.",
    "stance.Ofensiva": "Ofensiva", "stance.Defensiva": "Defensiva", "stance.Apoyo": "Apoyo",
    "temperament.Timido": "Tímido", "temperament.Curioso": "Curioso", "temperament.Orgulloso": "Orgulloso", "temperament.Sereno": "Sereno",
    "battle.attack": "Atacar", "battle.technique": "Técnica", "battle.combo": "Combo", "battle.item": "Objeto",
    "battle.defend": "Defender", "battle.flee": "Huir", "battle.rule": "Especial", "battle.target": "Elige objetivo",
    "battle.no_combos": "No hay combos listos", "battle.miss": "Falla", "battle.immune": "Inmune", "battle.critical": "¡Crítico!",
    "battle.weak": "¡Débil!", "battle.resist": "Resiste", "battle.victory": "¡Victoria!", "battle.defeat": "El grupo cayó…",
    "battle.fled": "Escaparon.", "battle.flee.failed": "¡No pudieron escapar!", "battle.flee.impossible": "¡No hay escapatoria!",
    "battle.xp": "Experiencia: {0}", "battle.money_gained": "Monedas: {0}", "battle.item_gained": "Obtienen: {0}",
    "battle.level_up": "{0} sube al nivel {1}.", "battle.learned": "{0} aprende {1}.", "battle.affinity": "La afinidad de {0} sube {1}.",
    "battle.skip": "{0} no puede moverse.", "battle.defends": "{0} se defiende.", "battle.ready": "Turno de {0}",
    "battle.gameover.retry": "Cargar última partida", "battle.gameover.title": "Volver al título", "battle.continue": "Continuar",
    "rule.hueco.intro": "El Hueco es una acción retirada del recuerdo colectivo. Golpearlo lo vuelve más violento.",
    "rule.hueco.fury": "La furia del Hueco crece ({0}).",
    "rule.hueco.testimony": "Un objeto y su testimonio se unen. El Hueco se estabiliza ({0}).",
    "rule.hueco.cmd.name": "Testimonio", "rule.hueco.cmd.desc": "Une un objeto con su recuerdo: calma al Hueco y lo daña.",
    "rule.erratas.intro": "La Fe de Erratas corrige personas según expedientes. Quien está registrado no puede ser dañado.",
    "rule.erratas.immune": "«Expediente válido.» El daño se anula.",
    "rule.erratas.exposed": "La Fe de Erratas no reconoce el testimonio. ¡Expediente anulado durante {0} turnos!",
    "rule.erratas.restored": "El sistema restaura el Expediente.",
    "rule.erratas.cmd.name": "Testimonio no registrado", "rule.erratas.cmd.desc": "Obliga al sistema a leer a alguien que no reconoce como ciudadano.",
    "bond.prompt": "Sintonizar con {0}", "bond.title": "Resonancia", "bond.instructions": "Pulsa cada nota cuando llegue al círculo.",
    "bond.offering": "¿Ofrecer algo antes de sintonizar?", "bond.no_offering": "Sin ofrenda", "bond.score": "Sintonía: {0} %",
    "bond.chance": "Probabilidad de que acepte: {0} %",
    "bond.reason.accepted": "{0} decidió acompañarlos.", "bond.reason.refused": "{0} los escuchó… pero eligió quedarse.",
    "bond.reason.no_tune": "No hubo sintonía. {0} se aleja.", "bond.reason.not_bondable": "Esta criatura pertenece a su mundo. No se la puede llevar.",
    "bond.reason.condition": "{0} no se acerca todavía. Quizá el grupo deba cambiar antes.", "bond.reason.full": "El Santuario está lleno.",
    "bond.reason.no_offering": "No tienen esa ofrenda.", "bond.need_diapason": "Necesitan el Diapasón de Arcilla para sintonizar.",
    "bond.cooldown": "{0} todavía no quiere acercarse.",
    "interact.talk": "Hablar", "interact.open": "Abrir", "interact.chest_got": "Obtienen: {0} ×{1}", "interact.chest_empty": "Está vacío.",
    "explore.encounter": "¡Enemigos!", "explore.saved_point": "Punto de guardado",
    "notebook.title": "Cuaderno de Costuras", "notebook.scenes": "Escenas completadas: {0}", "notebook.flags": "Huellas de tus decisiones",
    "era.2000": "Puerto Niebla, 2000", "era.2026": "Puerto Niebla, 2026", "era.-330": "Lira, 330 a. C.", "era.1542": "Talam, 1542",
    "era.1891": "Barrio Carbón, 1891", "era.2140": "Bahía de los Diques, 2140", "era.2749": "Ciudad Pila, 2749", "era.8800": "Jardín de Baja Luz, 8800",
    "era.-72400000": "Cuenca de Vidriamar, 72.400.000 a. C.", "era.orillas": "Las Otras Orillas",
    "element.Neutro": "Neutro", "element.Viento": "Viento", "element.Rayo": "Rayo", "element.Tierra": "Tierra", "element.Fuego": "Fuego",
    "element.Luz": "Luz", "element.Agua": "Agua", "element.Metal": "Metal", "element.Vacio": "Vacío",
    "slot.Arma": "Arma", "slot.Casco": "Casco", "slot.Peto": "Peto", "slot.Guantes": "Guantes", "slot.Botas": "Botas", "slot.Accesorio": "Objeto mágico",
    "slot.Collar": "Collar", "slot.ArmaduraMascota": "Armadura",
    "stat.MaxHp": "Vida", "stat.MaxMp": "PM", "stat.Fuerza": "Fuerza", "stat.Magia": "Magia", "stat.Defensa": "Defensa",
    "stat.DefensaMagica": "Def. mágica", "stat.Velocidad": "Velocidad", "stat.Precision": "Precisión", "stat.Evasion": "Evasión", "stat.Suerte": "Suerte",
    "status.Veneno": "Veneno", "status.Sueno": "Sueño", "status.Aturdido": "Aturdido", "status.Silencio": "Silencio", "status.Ceguera": "Ceguera",
    "status.Confusion": "Confusión", "status.Olvido": "Olvido", "status.Prisa": "Prisa", "status.Lentitud": "Lentitud",
    "status.Proteccion": "Protección", "status.Coraza": "Coraza", "status.Regeneracion": "Regeneración",
    "kind.Fisica": "Física", "kind.Magica": "Mágica", "kind.Apoyo": "Apoyo", "tech.cost": "{0} PM",
    "input.confirm": "Confirmar", "input.cancel": "Cancelar", "input.menu": "Menú", "input.run": "Correr",
    "proto.welcome": "Muévete con el stick izquierdo o WASD · Interactúa con A / Enter · Menú con Start / Tab",
    "error.input_system": "Activa el Input System: Edit › Project Settings › Player › Active Input Handling = Input System Package (New) o Both.",
}
es.update(ui)

# ------------------------------------------------------------------ escritura
def write(path, obj):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2)
        f.write("\n")

write(os.path.join(DATA, "techniques.json"), techniques)
write(os.path.join(DATA, "characters.json"), characters)
write(os.path.join(DATA, "items.json"), items)
write(os.path.join(DATA, "creatures.json"), creatures)
write(os.path.join(DATA, "enemies.json"), enemies)
write(os.path.join(DATA, "encounters.json"), encounters)
write(os.path.join(DATA, "combos.json"), combos)
write(os.path.join(DATA, "flags.json"), flags)
write(os.path.join(DATA, "scenes.json"), scenes)
write(os.path.join(DATA, "cierres.json"), cierres)
write(os.path.join(DATA, "absents.json"), absents)
os.makedirs(os.path.join(DATA, "maps"), exist_ok=True)
for f in os.listdir(os.path.join(DATA, "maps")):
    os.remove(os.path.join(DATA, "maps", f))
for m in maps:
    write(os.path.join(DATA, "maps", m["id"] + ".json"), m)
for f in os.listdir(os.path.join(DATA, "dialogues")):
    os.remove(os.path.join(DATA, "dialogues", f))
for d in dialogues:
    write(os.path.join(DATA, "dialogues", d["id"] + ".json"), d)
write(os.path.join(LOC, "es.json"), dict(sorted(es.items())))
print(f"técnicas={len(techniques)} personajes={len(characters)} objetos={len(items)} criaturas={len(creatures)} "
      f"enemigos={len(enemies)} encuentros={len(encounters)} combos={len(combos)} banderas={len(flags)} "
      f"diálogos={len(dialogues)} textos={len(es)}")
