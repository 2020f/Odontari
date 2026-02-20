# Especificación exacta del periodontograma (maqueta)

Documento de referencia para reproducir o integrar el periodontograma en otro proyecto. Todos los parámetros, estructuras y reglas están definidos sin resumir.

---

## 1. Descripción general

- **Nombre:** Periodontograma (formulario clínico).
- **Tipo:** Maqueta funcional en una sola página (HTML + CSS + JavaScript, sin frameworks).
- **Archivos:** `index.html`, `styles.css`, `app.js`.
- **Persistencia:** localStorage (clave `periodontograma_data`) y exportación/importación en JSON.

---

## 2. Numeración dental (sistema FDI)

### Arcada superior (SUPERIOR)
- **Secuencia exacta (izquierda a derecha):** 18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28.
- **Separación visual:** Entre 11 y 21 hay una línea vertical (hemiarcada derecha 18–11 | hemiarcada izquierda 21–28).
- **Subtítulo en UI:** "18–11 | 21–28".

### Arcada inferior (INFERIOR)
- **Secuencia exacta (izquierda a derecha):** 48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38.
- **Separación visual:** Entre 41 y 31 hay una línea vertical (48–41 | 31–38).
- **Subtítulo en UI:** "48–41 | 31–38".

### Sitios por diente (M, C, D)
- **M:** Mesial.
- **C:** Central.
- **D:** Distal.

---

## 3. Parámetros clínicos (filas de la tabla)

Cada fila tiene: **id** (interno), **label** (texto en UI), **tipo de control**, **key** (nombre en el estado/datos), y cuando aplica: opciones, rango, tooltip, etiqueta de sección, grupo.

| Orden | id | label | type | key | opciones / rango | tooltip | sectionLabel | group | groupEnd |
|-------|----|-------|------|-----|------------------|---------|--------------|-------|----------|
| 1 | ausencia | AUSENCIA | checkbox | ausencia | — | — | Cara vestibular | 1 | — |
| 2 | implante | IMPLANTE | checkbox | implante | — | — | — | 1 | true |
| 3 | movilidad | MOVILIDAD | select | movilidad | 0, 1, 2, 3 | Grado de movilidad: 0–3 | — | 2 | — |
| 4 | pronostico | PRONÓSTICO | select | pronostico | Bueno, Reservado, Malo | — | — | 2 | — |
| 5 | furca | FURCA | select | furca | 0, I, II, III | Clasificación: 0, I, II, III | — | 2 | true |
| 6 | sangrado | SANGRADO | miniMCD | sangrado | — | — | — | 3 | — |
| 7 | supuracion | SUPURACIÓN | miniMCD | supuracion | — | — | — | 3 | — |
| 8 | placa | PLACA | miniMCD | placa | — | — | — | 3 | true |
| 9 | anchuraEncia | ANCHURA ENCÍA | number | anchuraEncia | min: 0, max: 9 | — | — | 4 | — |
| 10 | margenVestibular | MARGEN GINGIVAL (V) | tripleNumber | margenVestibular | min: -9, max: 9 | (-) recesión / (+) agrandamiento. Valores -9 a 9 | — | 4 | — |
| 11 | sondajeVestibular | PROF. SONDAJE (V) | tripleNumber | sondajeVestibular | min: 0, max: 12 | — | — | 4 | — |
| 12 | teethDraw | (vacío) | teethDraw | (null) | — | — | — | 4 | — |
| 13 | margenPalatal | MARGEN GINGIVAL (P/L) | tripleNumber | margenPalatal | min: -9, max: 9 | (-) recesión / (+) agrandamiento. Valores -9 a 9 | Cara palatina / lingual | 4 | — |
| 14 | sondajePalatal | PROF. SONDAJE (P/L) | tripleNumber | sondajePalatal | min: 0, max: 12 | — | — | 4 | true |

- **group:** 1 = Ausencia/Implante, 2 = Movilidad/Pronóstico/Furca, 3 = Sangrado/Supuración/Placa, 4 = Encía/Margen/Sondaje (incluye fila de dibujo y caras vestibular/palatina).
- **sectionLabel:** texto de la fila de separación que se inserta **antes** de esa fila (Cara vestibular antes de AUSENCIA, Cara palatina / lingual antes de MARGEN GINGIVAL (P/L)).
- **groupEnd:** true indica fin de bloque visual (borde inferior más marcado).

---

## 4. Tipos de control (detalle exacto)

- **checkbox:** Un único checkbox por diente. Valor: boolean.
- **select:** Desplegable con opciones fijas. Valor: string (una de las opciones indicadas).
- **miniMCD:** Tres celdas clicables (M, C, D) que alternan on/off. Valor: objeto `{ M: boolean, C: boolean, D: boolean }`.
- **number:** Un input numérico por diente; en la maqueta se usa type="number" con min/max. Valor: string o número en rango (vacío permitido).
- **tripleNumber:** Tres inputs por diente (M, C, D). En la maqueta son `type="text"` con `inputmode="numeric"` y clase `mcd-input`. Valor: objeto `{ M: string|number, C: string|number, D: string|number }` (vacío '' permitido).
- **teethDraw:** Fila de “dibujo” del diente (SVG simple). No tiene key en datos; no se persiste. Visualmente indica implante si `implante === true`.

---

## 5. Estructura de datos (estado en memoria y JSON)

### Estado global (objeto `state`)

```json
{
  "superior": {
    "18": { ...datos del diente 18... },
    "17": { ... },
    "16": { ... },
    "15": { ... },
    "14": { ... },
    "13": { ... },
    "12": { ... },
    "11": { ... },
    "21": { ... },
    "22": { ... },
    "23": { ... },
    "24": { ... },
    "25": { ... },
    "26": { ... },
    "27": { ... },
    "28": { ... }
  },
  "inferior": {
    "48": { ... },
    "47": { ... },
    "46": { ... },
    "45": { ... },
    "44": { ... },
    "43": { ... },
    "42": { ... },
    "41": { ... },
    "31": { ... },
    "32": { ... },
    "33": { ... },
    "34": { ... },
    "35": { ... },
    "36": { ... },
    "37": { ... },
    "38": { ... }
  }
}
```

Las claves de diente son **números** (en JSON aparecen como keys de objeto; en JS se usan como números).

### Objeto por diente (plantilla vacía)

Cada diente tiene exactamente esta estructura. Los tipos indican el valor permitido.

| key | tipo | valor por defecto / forma |
|-----|------|---------------------------|
| ausencia | boolean | false |
| implante | boolean | false |
| movilidad | string | "0" (una de: "0", "1", "2", "3") |
| pronostico | string | "Bueno" (una de: "Bueno", "Reservado", "Malo") |
| furca | string | "0" (una de: "0", "I", "II", "III") |
| sangrado | objeto | { M: false, C: false, D: false } |
| supuracion | objeto | { M: false, C: false, D: false } |
| placa | objeto | { M: false, C: false, D: false } |
| anchuraEncia | string o número | "" (rango 0–9) |
| margenVestibular | objeto | { M: "", C: "", D: "" } (cada valor -9 a 9, vacío permitido) |
| sondajeVestibular | objeto | { M: "", C: "", D: "" } (cada valor 0–12, vacío permitido) |
| margenPalatal | objeto | { M: "", C: "", D: "" } (igual que margenVestibular) |
| sondajePalatal | objeto | { M: "", C: "", D: "" } (igual que sondajeVestibular) |

- No hay más propiedades por diente. Cualquier otra clave debe ignorarse o mapearse a esta estructura al importar.

---

## 6. Reglas de validación y comportamiento (exactas)

1. **Profundidad de sondaje (sondajeVestibular y sondajePalatal):**
   - Valor numérico por sitio (M, C, D) en rango 0–12.
   - Si valor ≥ 4 y &lt; 6: celda con clase CSS de aviso (ej. fondo amarillo).
   - Si valor ≥ 6: celda con clase CSS de peligro (ej. fondo rojo).

2. **Margen gingival (margenVestibular y margenPalatal):**
   - Valor numérico por sitio en rango -9 a 9.
   - Si valor &lt; 0: celda con clase CSS distintiva (ej. borde/color para recesión).

3. **Ausencia (ausencia === true):**
   - Toda la columna de ese diente se considera deshabilitada (inputs deshabilitados, columna con estilo “deshabilitado”).
   - La excepción es el checkbox de AUSENCIA, que sigue editable para poder desmarcar.

4. **Implante (implante === true):**
   - Solo efecto visual en la fila de dibujo (marca/ícono en el diente). No deshabilita controles.

5. **Sanitización de inputs M/C/D (tripleNumber):**
   - Solo se aceptan dígitos y, en margen, un signo menos.
   - Valores se acotan al rango (sondaje 0–12, margen -9–9) y se reescriben en el input.

---

## 7. Formato JSON de exportación (archivo descargado)

El archivo exportado tiene esta estructura exacta:

```json
{
  "version": 1,
  "exportDate": "2025-02-19T12:00:00.000Z",
  "periodontograma": {
    "superior": { "18": { ... }, "17": { ... }, ... "28": { ... } },
    "inferior": { "48": { ... }, ... "38": { ... } }
  }
}
```

- **version:** entero (actualmente 1).
- **exportDate:** cadena en formato ISO 8601 (fecha/hora de exportación).
- **periodontograma:** objeto con exactamente **superior** e **inferior**; cada uno es un objeto cuyas claves son los números de diente en string y los valores son el objeto por diente definido arriba.

Nombre de archivo sugerido: `periodontograma_YYYY-MM-DD.json` (con la fecha de exportDate).

---

## 8. Formato JSON aceptado en importación

Se aceptan dos formas:

1. **Formato completo (exportado):** objeto con propiedad `periodontograma` que contiene `superior` e `inferior`. Las propiedades `version` y `exportDate` se ignoran para el rellenado.
2. **Formato reducido:** objeto con solo `superior` e `inferior` en la raíz.

En ambos casos:
- Solo se actualizan dientes que existan en la maqueta (claves que coincidan con TEETH_SUPERIOR y TEETH_INFERIOR).
- Cada diente importado se fusiona con la plantilla vacía (createEmptyToothData); así no se pierden claves si el JSON viene incompleto.

---

## 9. localStorage

- **Clave:** `periodontograma_data`.
- **Valor:** cadena JSON del objeto `state` (solo `superior` e `inferior`, sin `version` ni `exportDate`).
- Se guarda automáticamente al cambiar datos y al hacer clic en “Guardar”. Al cargar la página se lee y se rellenan las tablas.

---

## 10. Resumen dinámico (métricas)

El panel de resumen muestra estos seis valores, calculados a partir del estado:

| Métrica | data-metric | Cálculo |
|---------|-------------|--------|
| Sitios con sangrado | sangrado | Número de sitios (M/C/D) en todos los dientes donde sangrado[site] === true. |
| Sitios con placa | placa | Número de sitios donde placa[site] === true. |
| Bolsas ≥4 | bolsillos4 | Número de sitios (sondajeVestibular + sondajePalatal) con valor numérico ≥ 4. |
| Bolsas ≥6 | bolsillos6 | Número de sitios con valor numérico ≥ 6. |
| Ausencias | ausentes | Número de dientes con ausencia === true. |
| Implantes | implantes | Número de dientes con implante === true. |

---

## 11. Estructura de la UI (HTML)

- **Cabecera:** título “Periodontograma”, leyenda M/C/D y colores (Prof. ≥4 amarillo, ≥6 rojo), panel de resumen (id `summaryPanel`), botones Guardar (id `btnSave`), Limpiar (id `btnClear`), Exportar JSON (id `btnExport`), Importar JSON (input id `inputImport`).
- **Dos secciones:** SUPERIOR (id `sectionSuperior`) e INFERIOR (id `sectionInferior`). Cada una tiene título (SUPERIOR / INFERIOR), subtítulo de numeración, contenedor de tabla con label lateral VESTIBULAR y PALATINO/LINGUAL, y div de tabla (id `tableSuperior`, `tableInferior`; data-arcade="superior" | "inferior").
- Las tablas se generan en JS (thead con números de diente + separador central; tbody con una fila por parámetro y filas de “Cara vestibular” / “Cara palatina / lingual” según sectionLabel).

---

## 12. Atributos de datos (data-*) usados en la maqueta

- **data-tooth:** número de diente (string), en celdas y controles.
- **data-row:** id de la fila (ej. "sondajeVestibular", "margenPalatal").
- **data-site:** "M", "C" o "D" en controles M/C/D.
- **data-arcade:** "superior" | "inferior" en el contenedor de la tabla.
- **data-metric:** en el resumen (sangrado, placa, bolsillos4, bolsillos6, ausentes, implantes).
- **data-hemi:** "sep" en la celda separadora central.

---

## 13. Clases CSS relevantes (para integración o réplica)

- Contenedor tabla: `.periodontogram-table`, `.periodontogram-wrapper`, `.scroll-container`.
- Columna de parámetros: `.param-cell` (sticky).
- Columna diente: `.tooth-col`; primera columna diente: `.first-tooth-col`.
- Separador central: `.hemi-sep`.
- Celdas M/C/D (Margen/Sondaje): `.td-mcd`; contenedor interno: `.celda-diente`, `.mcd`; inputs: `.mcd-input`.
- Toggles M/C/D (Sangrado/Placa/Supuración): `.mini-cell`, `.mini-cell.active`.
- Validación: `.cell-depth-warning`, `.cell-depth-danger`, `.cell-margin-negative`.
- Columna deshabilitada por ausencia: `.disabled-by-absence`.
- Filas de sección: `.face-label-row`; grupos: `.row-group-end`, `.row-group-1` … `.row-group-4`.

---

## 14. Resumen de constantes (para copiar/pegar)

```javascript
const TEETH_SUPERIOR = [18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28];
const TEETH_INFERIOR = [48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38];
const SITES = ['M', 'C', 'D'];
const STORAGE_KEY = 'periodontograma_data';
```

---

## 15. Orden de filas (array ROWS) en notación compacta

```
ausencia (checkbox), sectionLabel "Cara vestibular", group 1
implante (checkbox), group 1, groupEnd
movilidad (select: 0,1,2,3), group 2
pronostico (select: Bueno, Reservado, Malo), group 2
furca (select: 0,I,II,III), group 2, groupEnd
sangrado (miniMCD), group 3
supuracion (miniMCD), group 3
placa (miniMCD), group 3, groupEnd
anchuraEncia (number 0-9), group 4
margenVestibular (tripleNumber -9..9), group 4
sondajeVestibular (tripleNumber 0-12), group 4
teethDraw (dibujo), group 4
margenPalatal (tripleNumber -9..9), sectionLabel "Cara palatina / lingual", group 4
sondajePalatal (tripleNumber 0-12), group 4, groupEnd
```

---

Este documento define de forma exacta todos los parámetros y comportamientos del periodontograma. Para generar o integrar un periodontograma en otro proyecto, usar este archivo como referencia única.
