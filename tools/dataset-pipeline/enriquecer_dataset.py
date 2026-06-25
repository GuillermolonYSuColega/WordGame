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

    # Salida de depuración (legible, todas las palabras):
    python enriquecer_dataset.py entrada.txt salida.json

    # Salida de producción (minificada, solo pool, sin nulls, comprimida):
    python enriquecer_dataset.py entrada.txt salida.json \\
        --minificar --solo-pool --sin-nulls --gzip
"""

import argparse
import gzip
import json
import os
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
    # Mapeo de los valores internos (español) a los NOMBRES de enum en inglés
    # que espera WordData (CATEGORY/RARITY) en Unity. Los valores de 'regimen'
    # se quedan en español porque IsTransitive/IsPronominal los comparan así.
    CAT_EN = {
        "SUSTANTIVO": "Noun", "VERBO": "Verb", "ADJETIVO": "Adjective",
        "ADVERBIO": "Adverb", "LOCUCION": "Locution",
    }
    RAR_EN = {
        "COMUN": "Common", "POCO_COMUN": "Uncommon", "RARA": "Rare",
        "EPICA": "Epic", "LEGENDARIA": "Legendary",
    }

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

    # Claves EN INGLÉS para casar 1:1 con WordData ([JsonProperty(...)]).
    return {
        "id": idx,
        "word": p,                                   # forma de pantalla (anverso)
        "normalizedWord": pl,                         # clave normalizada (minúsculas)
        "category": CAT_EN[categoria],                # -> icono; nombre de enum CATEGORY
        "categorySource": "heuristica",               # honesto: validar con Wikcionario
        "rarity": RAR_EN[rareza] if rareza else None, # -> color; nombre de enum RARITY (o null)
        "zipfFrequency": round(zipf, 2),              # señal cruda (depuración/balance)
        "includedInPool": en_pool,                    # ¿entra en la colección de lanzamiento?
        "isLocution": (" " in p or "-" in p),
        # Régimen verbal (vacío si no es verbo). Valores en español a propósito.
        "regimen": regimen,
        "regimenSource": "heuristica" if es_verbo else None,
        "region": None,                               # regionalismo (marco especial); luego
        "isPoetic": None,                             # marca poética RAE; luego
        "shortDefinition": None,                      # reverso; IA offline por lotes
        "fullDefinition": None,                       # reverso ampliado; fuente abierta
        # 'isShiny' y 'masteryLevel' son estado por copia del jugador
        # (CardInProperty), NO atributos del catálogo. No van aquí.
    }


def _quitar_vacios(registro: dict) -> dict:
    """Elimina campos None, [] o '' para reducir el peso del JSON.
    Conserva False y 0.0 (son datos válidos). Unity/Newtonsoft tolera campos
    ausentes: quedan con su valor por defecto en C#."""
    return {k: v for k, v in registro.items() if v not in (None, [], "")}


def _mb(ruta: str) -> str:
    return f"{os.path.getsize(ruta) / 1024 / 1024:.2f} MB"


def main():
    ap = argparse.ArgumentParser(
        description="Enriquece la lista de palabras y genera el catálogo JSON."
    )
    ap.add_argument("entrada", help="Lista de palabras (una por línea).")
    ap.add_argument("salida", help="Ruta del JSON de salida.")
    ap.add_argument("--minificar", action="store_true",
                    help="JSON compacto sin indentado (producción).")
    ap.add_argument("--solo-pool", action="store_true",
                    help="Incluir solo las palabras con incluida_en_pool=True.")
    ap.add_argument("--sin-nulls", action="store_true",
                    help="Omitir campos vacíos (None/[]/'').")
    ap.add_argument("--gzip", action="store_true",
                    help="Escribir también una versión .gz comprimida.")
    args = ap.parse_args()

    with open(args.entrada, encoding="utf-8") as f:
        palabras = [l.strip() for l in f if l.strip()]

    registros = [construir_registro(i, w) for i, w in enumerate(palabras)]
    total_bruto = len(registros)

    # ---- Filtrado y limpieza (no alteran el informe de reparto) ----
    salida_registros = registros
    if args.solo_pool:
        salida_registros = [r for r in salida_registros if r["includedInPool"]]
    if args.sin_nulls:
        salida_registros = [_quitar_vacios(r) for r in salida_registros]

    # ---- Serialización ----
    dump_kw = dict(ensure_ascii=False)
    dump_kw["separators"] = (",", ":") if args.minificar else None
    if not args.minificar:
        dump_kw["indent"] = 1
    dump_kw = {k: v for k, v in dump_kw.items() if v is not None}

    with open(args.salida, "w", encoding="utf-8") as f:
        json.dump(salida_registros, f, **dump_kw)

    if args.gzip:
        ruta_gz = args.salida + ".gz"
        with open(args.salida, "rb") as f_in, \
             gzip.open(ruta_gz, "wb", compresslevel=9) as f_out:
            f_out.writelines(f_in)

    # ---- Informe ----
    from collections import Counter
    rar = Counter(r["rarity"] for r in registros)
    cat = Counter(r["category"] for r in registros)
    en_pool = sum(1 for r in registros if r["includedInPool"])

    print(f"Procesadas: {total_bruto:,} palabras")
    print(f"En pool jugable: {en_pool:,}  |  Reservadas: {total_bruto-en_pool:,}")
    print(f"Escritas al archivo: {len(salida_registros):,}")
    print("\nReparto por RAREZA (orden de colección):")
    for r in ["Common", "Uncommon", "Rare", "Epic", "Legendary", None]:
        etq = r if r else "(sin frecuencia · reservadas)"
        print(f"  {etq:32} {rar.get(r,0):7,}")
    print("\nReparto por CATEGORÍA (heurística, a validar):")
    for c, n in cat.most_common():
        print(f"  {c:32} {n:7,}")

    print(f"\nArchivo: {args.salida}  ->  {_mb(args.salida)}")
    if args.gzip:
        print(f"Comprimido: {args.salida}.gz  ->  {_mb(args.salida + '.gz')}")


if __name__ == "__main__":
    main()
