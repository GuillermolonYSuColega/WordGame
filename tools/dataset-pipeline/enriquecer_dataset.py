#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Palabrario · Pipeline de enriquecimiento del dataset léxico
===========================================================

Toma la lista plana de palabras lematizadas (una por línea) y produce un
dataset enriquecido con los atributos que el juego necesita:

    frecuencia  ->  rareza        (color de la carta, pilar nº1 del diseño)
    categoría   ->  icono         (sustantivo / verbo / adjetivo / ...)
    flags       ->  locución, pronominal, región, poética, shiny

FUENTES Y DERECHOS
------------------
- Frecuencias: paquete `wordfreq` (licencia abierta, corpus públicos). NO se
  hace scraping de RAE ni de fuentes con derechos, tal como exige el estudio.
- Categoría gramatical: heurística morfológica de primer paso. Se marca con
  `categoria_fuente = "heuristica"` para que conste que debe VALIDARSE contra
  una fuente léxica abierta (Wikcionario, CC-BY-SA) en una fase posterior.
- Acepción y marca poética: se dejan vacías (null). La acepción corta se
  rellenará OFFLINE con el árbitro de IA por lotes (coste fijo y único, ver
  sección 8.3 del estudio); la marca poética requiere cruce con datos RAE.
- Régimen verbal (transitivo/intransitivo/pronominal): solo aplica a verbos y
  NO se puede deducir de la palabra. Se rellena en enriquecimiento desde
  Wikcionario (CC-BY-SA) o la pasada de IA. Aquí solo se siembra "pronominal"
  por heurística (-se) como valor provisional; 'regimen_fuente' lo marca.

Uso:
    pip install wordfreq
    python enriquecer_dataset.py entrada.txt salida.json
"""

import json
import sys
import unicodedata
from wordfreq import zipf_frequency

# ---------------------------------------------------------------------------
# CONFIGURACIÓN  ·  toca solo esto para reequilibrar el juego
# ---------------------------------------------------------------------------

# Umbrales de rareza por frecuencia Zipf (escala log 0–8; 8 = "de", "la").
# Semántica alineada con la paleta del estudio (8.1):
#   COMÚN        = el "pegamento" del idioma (preposiciones, ultrafrecuentes)
#   POCO_COMUN   = vocabulario frecuente con valor descriptivo
#   RARA         = menos habitual, buen rendimiento en minijuegos
#   EPICA        = términos ricos y específicos
#   LEGENDARIA   = vocabulario preciso/poético (p. ej. "inefable", zipf ~2.8)
# Se evalúan de mayor a menor; el primero que cumple gana.
UMBRALES_RAREZA = [
    (5.0, "COMUN"),
    (4.0, "POCO_COMUN"),
    (3.0, "RARA"),
    (2.0, "EPICA"),
    (0.01, "LEGENDARIA"),
]

# Política para las palabras SIN frecuencia medible (≈49% del diccionario).
# El diccionario completo NO es la colección de lanzamiento: la cola ultra-
# oscura se RESERVA fuera del pool jugable inicial (candidatas a "deep cuts"
# de live-ops más adelante). Esto evita la pirámide invertida.
#   "RESERVAR" -> rareza=None, incluida_en_pool=False
#   "LEGENDARIA" -> todas legendarias e incluidas (no recomendado: 53k cartas)
POLITICA_SIN_FRECUENCIA = "RESERVAR"

# Heurística de categoría gramatical (primer paso, requiere validación).
SUFIJOS_ADVERBIO = ("mente",)
SUFIJOS_VERBO = ("ar", "er", "ir", "arse", "erse", "irse")
SUFIJOS_ADJETIVO = (
    "oso", "osa", "able", "ible", "ivo", "iva", "ico", "ica",
    "ante", "iente", "udo", "uda", "izo", "iza", "il",
)
# Excepciones frecuentes: sustantivos que terminan como un infinitivo.
NO_VERBOS = {
    "azucar", "lugar", "collar", "pesar", "andar", "deber", "poder",
    "haber", "placer", "amanecer", "atardecer", "anochecer", "sabor",
    "color", "amor", "calor", "valor", "honor", "dolor", "temor",
}

# ---------------------------------------------------------------------------


def _sin_tildes(s: str) -> str:
    return "".join(
        c for c in unicodedata.normalize("NFD", s) if unicodedata.category(c) != "Mn"
    )


def detectar_categoria(palabra: str) -> str:
    """Heurística morfológica. Devuelve la categoría más probable."""
    p = palabra.lower().strip()
    base = _sin_tildes(p)

    if " " in p or "-" in p:
        return "LOCUCION"
    if base.endswith(SUFIJOS_ADVERBIO) and len(base) > 6:
        return "ADVERBIO"
    if base.endswith(SUFIJOS_VERBO) and len(base) > 3 and base not in NO_VERBOS:
        return "VERBO"
    if base.endswith(SUFIJOS_ADJETIVO) and len(base) > 4:
        return "ADJETIVO"
    return "SUSTANTIVO"  # opción por defecto: la mayoría de entradas son nombres


def asignar_rareza(zipf: float):
    """Devuelve (rareza, incluida_en_pool) a partir de la frecuencia."""
    if zipf <= 0.0:
        if POLITICA_SIN_FRECUENCIA == "RESERVAR":
            return None, False
        return "LEGENDARIA", True
    for umbral, rareza in UMBRALES_RAREZA:
        if zipf >= umbral:
            return rareza, True
    return "LEGENDARIA", True


def construir_registro(idx: int, palabra: str) -> dict:
    p = palabra.strip()
    pl = p.lower()
    zipf = zipf_frequency(pl, "es")
    rareza, en_pool = asignar_rareza(zipf)
    categoria = detectar_categoria(p)
    es_verbo = categoria == "VERBO"

    # Régimen verbal. Solo se siembra "pronominal" por heurística (-se); la
    # transitividad NO es deducible y se rellena luego desde Wikcionario / IA.
    regimen = []
    if es_verbo and pl.endswith("se"):
        regimen.append("pronominal")

    return {
        "id": idx,
        "palabra": p,                       # forma de pantalla (anverso)
        "lema": pl,                          # clave normalizada
        "categoria": categoria,              # -> icono de la carta
        "categoria_fuente": "heuristica",    # honesto: validar con Wikcionario
        "rareza": rareza,                    # -> color de la carta (o None)
        "frecuencia_zipf": round(zipf, 2),   # señal cruda (depuración/balance)
        "incluida_en_pool": en_pool,         # ¿entra en la colección de lanzamiento?
        "es_locucion": (" " in p or "-" in p),
        # Régimen verbal (vacío si no es verbo). Se muestra en el reverso:
        # "transitivo" / "intransitivo" / "pronominal", combinables.
        "regimen": regimen,
        "regimen_fuente": "heuristica" if es_verbo else None,
        "region": None,                      # regionalismo (marco especial); rellenar luego
        "es_poetica": None,                  # marca poética RAE; rellenar luego
        "acepcion_corta": None,              # anverso/reverso; IA offline por lotes
        "acepcion_completa": None,           # reverso ampliado; fuente abierta
        # 'shiny' y 'nivel_maestria' son estado de cada copia en propiedad
        # del jugador (runtime), NO atributos del catálogo. No van aquí.
    }


def main():
    if len(sys.argv) != 3:
        print("Uso: python enriquecer_dataset.py <entrada.txt> <salida.json>")
        sys.exit(1)
    entrada, salida = sys.argv[1], sys.argv[2]

    with open(entrada, encoding="utf-8") as f:
        palabras = [l.strip() for l in f if l.strip()]

    registros = [construir_registro(i, w) for i, w in enumerate(palabras)]

    with open(salida, "w", encoding="utf-8") as f:
        json.dump(registros, f, ensure_ascii=False, indent=1)

    # ---- Informe ----
    from collections import Counter
    rar = Counter(r["rareza"] for r in registros)
    cat = Counter(r["categoria"] for r in registros)
    en_pool = sum(1 for r in registros if r["incluida_en_pool"])

    print(f"Procesadas: {len(registros):,} palabras")
    print(f"En pool jugable: {en_pool:,}  |  Reservadas: {len(registros)-en_pool:,}")
    print("\nReparto por RAREZA (orden de colección):")
    for r in ["COMUN", "POCO_COMUN", "RARA", "EPICA", "LEGENDARIA", None]:
        etq = r if r else "(sin frecuencia · reservadas)"
        print(f"  {etq:32} {rar.get(r,0):7,}")
    print("\nReparto por CATEGORÍA (heurística, a validar):")
    for c, n in cat.most_common():
        print(f"  {c:32} {n:7,}")


if __name__ == "__main__":
    main()
