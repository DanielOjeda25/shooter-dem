# LA FUNDICIÓN — Documento de diseño del nivel

> 🗺️ **Diagrama visual:** [`MAPA_FUNDICION_diagrama.svg`](MAPA_FUNDICION_diagrama.svg)
> (corte vertical: 3 estratos + Atrio + recorrido A→F). Abrir en navegador.

> Diseño congelado (jun 2026) con las decisiones del autor. Referencias: cutaway del
> "refugio de ladrones" (estratos con profundidad) + automap de DOOM (zonas en loop).
> Pilares: recorrido con principio y final · mapa interconectado (regla DOOM: ninguna
> zona con una sola salida) · exploración con secretos estilo Serious Sam.

## Concepto
Planta de extracción abandonada — el origen de la ceniza que cubre el mundo (lore).
**Duración objetivo: 15–20 min** sin secretos. Tamaño total ≈ 120×120 m + dunas.

## Estructura vertical (3 estratos + atrio)
| Nivel | Zonas | Rol |
|---|---|---|
| **N1 Superficie** | La Travesía (dunas) · Entrada Colapsada · Patio de Carga · Plataforma de Carga | Inicio, primera arena |
| **N0 Planta** | Sala de Turbinas · Galería de Cintas · Sala de Control · balcones del Atrio | Descenso, mecanismos |
| **N−1 Subsuelo** | Túneles de Extracción · Depósito Inundado · Almacén · Cámara del Núcleo | Tensión, clímax |
| **El Atrio del Horno** | Pozo central que atraviesa los 3 niveles | Hub, landmark, mega-arena |

**Conexiones:** rampas de escombros (la IA SÍ las usa — cada nivel alcanzable por horda),
2 montacargas (SOLO jugador: atajo táctico), pozos de caída (solo ida), conducto de
ventilación (atajo subsuelo→superficie), compuerta de presión (la abre el botón de Control).

## El recorrido (principio → final)
| Etapa | Lugar | Beat |
|---|---|---|
| A. Inicio | **La Travesía** — dunas, 2-3 min de caminata solitaria (viento, chimenea lejana, goteo de enemigos) | Soledad SS |
| B. Arena 1 | Patio de Carga — oleada abre el portón | Primer combate real |
| C. Descenso | Planta N0: Turbinas (arena) → botón en Control | Mecanismos |
| D. Tensión | Subsuelo: túneles A OSCURAS + depósito | Terror barato |
| E. Clímax | Cámara del Núcleo — horda final en el fondo del pozo | Pico de intensidad |
| F. Final | **HUIDA CON COLAPSO**: el núcleo estalla → countdown → correr el mapa EN REVERSA derrumbándose, salir a las dunas | Final 6B |

**Avance:** portones que abre la oleada ("limpiá la arena → se abre la puerta").
Las zonas visitadas quedan abiertas → loops para explorar/escapar.
**Regla del ritmo:** los secretos viven en los VALLES (calma post-arena), nunca en el caos.

## Secretos (12) — contador "Secretos: X/12" estilo SS
| # | Secreto | Tipo |
|---|---|---|
| 1 | **El corazón en la duna** — botiquín visible a ~200 m, 90 s de caminata, emboscada kamikaze a mitad de camino (homenaje del autor) | Visible de lejos |
| 2 | Cráneo de metal semienterrado (lore + munición en el ojo) | Landmark curioso |
| 3 | Azotea de contenedores del Patio (salto + dash) | Parkour |
| 4 | Cuarto tras la Sala de Control (segunda pulsación larga del botón) | Botón doble |
| 5 | Pared agrietada en Túneles — se rompe a tiros | Muro falso |
| 6 | Alcoba sumergida del Depósito (buceo) | Riesgo de ahogo |
| 7 | Cadena de barriles en el Núcleo — abre rejilla del piso | Física |
| 8 | **Cuarto retro** — mini-sala pixelada con ítem absurdo | Homenaje bizarro |
| 9 | **Secreto sonoro** — goteo que guía al muro falso (se busca con los oídos) | Audio |
| 10 | **Francotirador paciente** — brillo a 200 m desde la azotea; 1 tiro imposible = cofre | Puntería |
| 11 | **Mapa roto** — hueco "bug" intencional → mirador fuera del nivel | Meta |
| 12 | **NPC muerto** — cadáver con notas de la Fundición + arma única | Lore |

## Mecanismos (8)
| # | Mecanismo | Aprendizaje | Dificultad |
|---|---|---|---|
| 1 | Puerta con botón (compuerta del Depósito) | Interacción + eventos | 🟢 |
| 2 | Montacargas ×2 | Plataformas móviles + player encima | 🟡 |
| 3 | Compuerta que abre un loop nuevo | Mapa dinámico | 🟢 |
| 4 | Puente extensible sobre el pozo del Atrio | Animación + colliders | 🟡 |
| 5 | Ventiladores gigantes (pasar en el hueco del ritmo) | Timing + daño | 🟡 |
| 6 | Cinta transportadora activa (empuja a todos) | Físicas en zona | 🟡 |
| 7 | Trampa: palanca electrifica el piso del Atrio 10 s (cooldown largo) | Área de daño activable | 🟡 |
| 8 | **Generador apagado** — subsuelo a oscuras + linterna; encenderlo ilumina todo | Iluminación dinámica | 🔴 |

## Agua (Depósito Inundado) — decisión 3A: AGUA REAL
- Buceo con **barra de aire** + daño por ahogo al agotarse.
- ⚠️ Honesto: requiere **sistema de nado** (el Movement del pack no lo trae) — movimiento
  3D bajo el agua + flotación + UI de aire. Es una feature en sí (🔴). Se construye
  cuando lleguemos a la fase del subsuelo.

## Eventos de ambiente (7A-D, todos)
- **Tormenta de ceniza** — periódica en superficie: niebla + viento (baja visibilidad).
- **Temblores** — sacudida + polvo/escombros al avanzar etapas.
- **La sirena** — alarma vieja antes de cada horda grande (aviso + miedo).
- **Luces que fallan** — parpadeo en el subsuelo.

## El final (6B) — Huida con colapso
1. Matar la horda del Núcleo → cinemática corta: el núcleo se desestabiliza.
2. **Countdown visible** (~2:30) + sirena continua + temblores fuertes.
3. Correr el mapa EN REVERSA: derrumbes que CAMBIAN la ruta (pasos que se cierran,
   atajos que se abren), enemigos sueltos, sin oleadas formales.
4. Salir a las dunas → explosión a tus espaldas → pantalla de victoria con stats
   (tiempo, kills, **secretos X/12**).
- ⚠️ Honesto: es el final más ambicioso (countdown + eventos de destrucción + ruta
  alterada). Vale la pena: reutiliza TODO el mapa. Se construye al final.

## Fases de construcción (orden de aprendizaje)
1. **Atrio** (pozo 3 alturas + rampas + balcones) — si el combate funciona acá, funciona todo.
2. **Anillo N0** (Turbinas + Cintas + Control) → primer loop completo.
3. **Superficie** (Travesía + Patio + portones de oleada) → ya hay "principio".
4. **Subsuelo** (túneles + oscuridad + generador) — sin agua aún.
5. **Mecanismos** 1→8 (en orden de dificultad).
6. **Secretos** (de a tandas; primero 1, 3, 5 — los estructurales).
7. **Agua + buceo** (Depósito completo).
8. **Final con colapso** + contador de secretos + stats.

## Reglas técnicas (recordatorios)
- La IA navega por **rampas** (NavMesh no usa ascensores) — verificar navmesh por nivel.
- Arenas con frentes anchos (hordas), nunca pasillos < 4 m.
- Not Walkable en muros/coberturas (lección aprendida del blockout v1).
- Materiales Gridbox URP para todo el blockout; arte después.
- Cada zona ≥ 2 salidas. Landmark (chimenea del Horno) visible desde superficie y atrio.
