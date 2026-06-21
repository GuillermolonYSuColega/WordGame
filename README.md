# Palabrario · Dataset léxico (cimiento del producto)

Convierte la lista plana de palabras lematizadas en el **catálogo de cartas** del
juego: cada palabra con su rareza (color), categoría (icono) y flags. Es la pieza
que el estudio marca como recomendación nº3 ("construir el dataset limpio y con
derechos claros como cimiento del producto").

## Archivos

| Archivo | Qué es |
|---|---|
| `enriquecer_dataset.py` | Pipeline: lee el `.txt` y genera el JSON enriquecido. |
| `palabrario_dataset.json` | Catálogo completo: 107.920 registros. |
| `ejemplo_cartas.json` | Muestra legible (3 cartas por rareza) para inspección rápida. |
| `PalabraData.cs` | Clases C#/Unity que deserializan el catálogo (Newtonsoft.Json). |

## Esquema de cada carta

| Campo | Tipo | Para qué sirve | Estado |
|---|---|---|---|
| `id` | int | Clave estable. | ✅ |
| `palabra` | string | Texto del anverso. | ✅ |
| `lema` | string | Clave normalizada (minúsculas). | ✅ |
| `categoria` | enum | **Icono** (sustantivo/verbo/adjetivo…). | ⚠️ heurística |
| `categoria_fuente` | string | `"heuristica"` → pendiente de validar. | ✅ |
| `rareza` | enum \| null | **Color** de la carta. `null` si reservada. | ✅ |
| `frecuencia_zipf` | float | Señal de frecuencia (balance/depuración). | ✅ |
| `incluida_en_pool` | bool | ¿Entra en la colección de lanzamiento? | ✅ |
| `es_locucion` | bool | Locución / término compuesto. | ✅ |
| `regimen` | string[] | Régimen verbal: `transitivo`/`intransitivo`/`pronominal` (combinables). Vacío si no es verbo. → reverso. | ⚠️ semilla heurística (solo pronominal) |
| `regimen_fuente` | string \| null | `"heuristica"` → pendiente de validar. | ✅ |
| `region` | string \| null | Regionalismo → marco especial. | ⬜ pendiente |
| `es_poetica` | bool \| null | Marca poética RAE → legendarias/métrica. | ⬜ pendiente |
| `acepcion_corta` | string \| null | Significado del reverso. | ⬜ IA offline |
| `acepcion_completa` | string \| null | Entrada ampliada. | ⬜ pendiente |

El estado por copia del jugador (maestría, shiny) **no** va en el catálogo:
ver `CartaEnPropiedad` en el `.cs`.

## Dos decisiones de diseño que toma este pipeline

1. **El diccionario completo NO es la colección de lanzamiento.** El 49 % de las
   palabras no tienen frecuencia medible (son términos oscuros de diccionario).
   Se marcan `incluida_en_pool = false` y se reservan como posibles "deep cuts"
   de live-ops. El pool jugable son las ~54.810 con señal real de frecuencia.

2. **El recuento por rareza es naturalmente "invertido"** (pocas comunes, muchas
   legendarias) porque el español tiene pocas palabras muy frecuentes y una cola
   larguísima de poco frecuentes. Esto **no se arregla bajando umbrales** (eso
   etiquetaría palabras oscuras como "comunes", rompiendo la semántica del color).
   Se arregla en el **sistema de drop del sobre**: ponderar por tier de rareza con
   independencia de cuántas cartas distintas haya en cada tier, y/o curar más el
   pool. Los umbrales viven en `UMBRALES_RAREZA` (editables) al inicio del script.

## Lo que falta (fases siguientes, todas con fuentes de licencia limpia)

- **Validar categoría gramatical** contra Wikcionario (CC-BY-SA) — corrige fallos
  de la heurística (p. ej. `abril` sale como adjetivo).
- **Régimen verbal** (transitivo/intransitivo/pronominal): rellenar desde
  Wikcionario o la pasada de IA. No es deducible de la palabra; la transitividad
  hay que importarla. Idealmente, por acepción (casa con la acepción del reverso).
- **Acepción corta** del reverso: pasada **offline por lotes** con el árbitro de
  IA (coste fijo y único; ver 8.3 del estudio). Es el primer uso de IA del
  proyecto y es de coste controlado.
- **Marca poética y regionalismos**: cruce con datos RAE / listas regionales.
- **Rendimiento en Unity**: convertir el JSON a SQLite o shards, o cargar solo
  `incluida_en_pool == true`.

## Reproducir

```bash
pip install wordfreq
python enriquecer_dataset.py 0_palabras_todas_no_conjugaciones.txt palabrario_dataset.json
```
