# ROADMAP — Shooter Demo

Fases del proyecto. Un juego se construye **de adentro hacia afuera**: primero el
suelo, luego moverte, luego el arma, luego a quién dispararle, luego las reglas.

Leyenda: ✅ hecho · 🟡 en progreso · ⬜ pendiente

---

## Fase 0 — Setup ✅
- [x] Unity 6 (URP) + proyecto creado
- [x] Paquete MCP for Unity instalado (en `Packages/manifest.json`)
- [x] MCP conectado (server HTTP 8080 + registro global) — ver `docs/MCP_SETUP_UNITY.md`
- [x] Repo Git + remoto (`shooter-dem`)
- [x] Documentación base

## Fase 1 — El mundo ✅
- [x] Suelo (Plane) creado
- [x] Centrar el suelo en (0,0,0) y agrandarlo (scale 5,1,5 → 50×50)
- [x] Material/color al suelo (`Assets/Materials/Ground.mat`)
- [x] Entender y ajustar la luz (Directional Light); skybox por defecto (aporta luz ambiente)

## Fase 2 — El jugador ✅
- [x] Decidir **FPS o TPS** → **FPS** (cámara en los ojos)
- [x] GameObject del jugador (cápsula) + movimiento (WASD) — `PlayerMovement.cs` + CharacterController
- [x] Cámara en primera persona (Main Camera hija del Player) — `MouseLook.cs`
- [x] Input System (el nuevo: `Keyboard.current` / `Mouse.current`)

## Fase 3 — El arma ✅
- [x] Arma (placeholder) en el jugador — cubo alargado, hija de la Main Camera
- [x] Disparo por raycast + efecto de impacto — `Weapon.cs` + prefab `ImpactMark`
- [x] Munición — cargador (`magazineSize`) + recarga con R (corrutina `reloadTime`)

## Fase 4 — Los enemigos ✅
- [x] Enemigo (cápsula roja) + vida — `EnemyHealth.cs`
- [x] IA con NavMesh — `EnemyAI.cs` (persecución + ataque). NavMesh horneado con
  `NavMeshSurface` en el Plane; Player/Enemy excluidos con `NavMeshModifier`
  (Remove Object). _Patrulla pendiente (opcional)._
- [x] Daño mutuo — disparo baja vida del enemigo (`Weapon.damage`); el enemigo
  golpea al jugador al acercarse (`PlayerHealth.cs`, ataque con cooldown)

## Fase 5 — Las reglas ✅
- [x] Mira / crosshair (Canvas + Image circular, `Knob`)
- [x] HUD (vida, munición) — `HUD.cs` lee `PlayerHealth` y `Weapon`, textos TMP anclados a esquinas
- [x] Spawns de enemigos — `EnemySpawner.cs` instancia N copias del prefab `Enemy` en círculo
- [x] Victoria / derrota — `GameManager.cs` (singleton) cuenta enemigos; game over real
  (`Time.timeScale = 0` + cursor libre + panel `GameOverPanel`/`GameOverText`)
- [x] Menú de pausa (Esc) — `PausePanel` con botones Reanudar/Reiniciar/Salir; reinicio
  recarga la escena (`SceneManager.LoadScene`). `MouseLook` y `Weapon` ignoran input con
  `Time.timeScale == 0` (no mover cámara ni disparar en pausa/game over)

## Fase 6 — Pulido ✅
Orden acordado: **1) partículas → 2) sonidos → 3) animaciones** (las partículas no
dependen de assets externos; los sonidos necesitan clips que aporta el autor).
- [x] Partículas — sistema de efectos del arma `WeaponEffects` (con **pooling**): fogonazo,
  humo, impacto y sangre. _Actualizado en v2.0 a **VFX de packs URP** (Vefects + Gabriel Aguiar);
  ver abajo._
- [x] Sonidos — `AudioSource` en `Weapon` (2D). Disparo (`fire1`), sin munición (`empty`),
  recarga (`reload`) vía `PlayOneShot`; impacto en pared (`concrete1..4`) vs enemigo (`flesh1..5`)
  elegido al azar. Clips en `Assets/Audio/`. Disparo semiautomático (1 tiro por clic, sin cadencia tope).
- [x] Recoil del arma — efecto procedural por código en `Weapon.cs`: al disparar la pose
  retrocede (`recoilKickback`, eje Z local) y vuelve suave en `LateUpdate` (offsets que decaen
  con `Lerp`). `recoilPitch` (cabeceo) disponible pero a 0 por decisión del autor (solo retroceso).

---

## Visión v2.0 — Arena horde shooter (estilo Serious Sam) 🎯
> **El Norte del proyecto.** Referencia explícita del autor: **Serious Sam** (First/Second
> Encounter). Objetivo: **mapas enormes** + **hordas masivas** de enemigos que rodean al
> jugador; combate de moverse sin parar (*backpedaling*) disparando a docenas a la vez.
> A partir de aquí, **toda decisión de diseño/arquitectura se evalúa por**: ¿escala a mapa
> grande + hordas? Se documenta como v2.0 pero marca el rumbo de cada paso de v1.

**Pilares para llegar ahí (fuera del alcance del v1 actual):**
- **Mapa-arena grande**: escenario amplio con cobertura, alturas y espacios abiertos para
  hordas. Modelado con **ProBuilder** (o malla externa) + **NavMesh horneado sobre área
  extensa** para que los enemigos rodeen; posibles transiciones entre zonas (triggers/puertas).
- **Hordas y oleadas**: ✅ *hecho* — `WaveSystem` híbrido (finito/infinito) con **pacing**
  (tope de vivos + spawn por tandas), **escalado de dificultad** (vida/velocidad por oleada) y
  **contador de oleada en HUD**. Falta: spawners por **zonas** del mapa grande.
- **Rendimiento para hordas** (será el tema central): ✅ *muy avanzado* — `EnemyAI` cachea al
  Player (`PlayerHealth.Current`) y hace *throttle* de repath; **object pooling** hecho para
  **enemigos** (`EnemyPool`) y **efectos de impacto** (`PrefabPool` genérico: chispas y marcas).
  Falta: límites de balas/sonidos simultáneos; pooling de proyectiles si hace falta.
- **Variedad de enemigos tipo SS**: 🟡 *iniciado* — arquitectura de **estrategia de ataque**
  (`EnemyAttack` abstracto; `MeleeAttack` y `KamikazeAttack`) + **`EnemyData` (SO)** y spawner
  multi-tipo con **sorteo ponderado** (pool por prefab). Hechos: **melee** (rojo) y **kamikaze**
  (naranja: corre y explota en área). El kamikaze ahora explota **al morir por cualquier causa**
  (`Health.Died`) → **explosión en cadena** entre kamikazes cercanos. Faltan: **ranged** (dispara
  a distancia) y **tanque** (lento, mucha vida); enemigos animados (Blender).
- **Explosiones de área y props destructibles**: ✅ *base hecha* — `Projectile.cs` explota al
  impactar y aplica daño en área con `Physics.OverlapSphere` → `TakeDamage` a todo `IDamageable`
  del radio (con caída por distancia) + **knockback radial**. Usado por el arma en modo
  `Projectile` (bazooka) y por el **kamikaze**, con **VFX de explosión** (prefab reutilizable,
  pack Gabriel Aguiar). Falta: **clip real** de sonido (ahora placeholder) y props destructibles
  (objetos `IDamageable` que al morir se cambian por escombros). Descartado: deformación real del
  terreno (caro + choca NavMesh).
- **Personajes y animaciones (modelos 3D)**: 🟡 *plan definido — **SIGUIENTE TAREA***. Reemplazar
  las **cápsulas placeholder** por un **maniquí humanoide animado**. Decisión tomada:
  - **Modelo + locomoción**: Unity **"Starter Assets – ThirdPerson (URP)"** (gratis) — trae el
    **mannequin gris** (`Armature_Mesh`, estilo maniquí de Unreal) + Animator con **idle/walk/run/jump**.
    Su `ThirdPersonController` es para el JUGADOR; para los **enemigos** usamos solo su **modelo + animaciones**.
  - **Combate**: **Mixamo** (Adobe, gratis) para **Attack** y **Death** (+ hit reaction) — el pack NO
    los trae. Bajar en *FBX for Unity*, *Without Skin*. Compatibles porque todo es **rig Humanoid**.
    (Alternativa de modelo: Mixamo `Y Bot`/`X Bot`, también maniquí gris.)
  - **Pasos de integración (pendientes)**: (1) importar Starter Assets + bajar Attack/Death de Mixamo;
    (2) Rig = **Humanoid** en los FBX; (3) **Animator Controller para enemigos** (idle ↔ walk ↔ attack ↔
    death con parámetros); (4) script puente Animator↔`EnemyAI` (velocidad→walk, ataque en rango→attack,
    `Health.Died`→death) que **reemplaza la cápsula** del prefab; (5) empezar por el **melee**, luego
    replicar a kamikaze/ranged/tank. A futuro: **viewmodel de brazos+arma** para el FPS.
  - El recoil procedural por código puede convivir encima de las animaciones de disparo.
- **Arsenal**: ✅ *hecho (base)* — armas **dirigidas por datos** (`WeaponData`: daño, falloff,
  cargador, audio por arma, `ejectsShell`, etc.) con **3 formas de disparo** (`Single` raycast,
  `Shotgun` N perdigones, `Projectile` con AoE). **`WeaponManager`** (inventario + cambio con
  1/2/3 o rueda, munición por arma + sonido de cambio) y assets **Pistola / Escopeta / Bazooka**
  funcionando. Falta: más armas, y **modelos/prefabs reales** (siguen placeholders).
- **Movimiento y feel**: ✅ *hecho* — sprint, salto y crouch sobre `CharacterController`;
  `MovementFeel` (cámara que se hunde al agacharse/esprintar, arma que se repliega).
- **Audio**: ✅ *hecho* — sonido **por arma** (en `WeaponData`), pasos/sprint (`PlayerFootsteps`),
  casquillos, cambio de arma, whoosh+explosión de bazooka. Audio organizado en carpetas.

---

### Hito actual
**v1 cerrado (Fases 0–6)** ✅ + **v2.0 en marcha**. Sobre el v1 (mundo, jugador FPS, arma,
enemigos por NavMesh, reglas/HUD/game over) ya hay buena parte de la Visión v2.0:
- **Movimiento**: sprint/salto/crouch + feel de cámara/arma.
- **Oleadas**: `WaveSystem` híbrido con pacing, escalado de dificultad y contador en HUD.
- **Rendimiento**: pooling de enemigos e impactos; `EnemyAI` cacheado + throttle.
- **Arsenal**: `WeaponManager` + Pistola/Escopeta/Bazooka data-driven; knockback; explosión AoE.
- **Audio**: por arma, pasos, casquillos, cambio de arma, bazooka.

- **Enemigos**: ✅ **4 tipos** — melee, **kamikaze** (explosión en cadena), **ranged** (proyectil
  esquivable, mantiene distancia) y **tanque** (lento, mucha vida, golpe fuerte). Sistema data-driven
  (`EnemyData` SO) + spawner multi-tipo ponderado.
- **Detección / aggro** (`EnemyAI`): ✅ los enemigos están **DORMIDOS** hasta que el jugador entra en
  `detectionRange` → despiertan (evento `Aggroed`, sticky) y persiguen. **Clave para mapas grandes +
  rendimiento**: un dormido solo hace un check de distancia/frame (no calcula ruta). Base para
  **activación por zonas**. Pendiente futuro: line-of-sight, aggro al recibir daño, "leash".
- **Audio de enemigos** (`EnemyAudio`): ✅ componente por prefab con `AudioSource` **3D** — idle loop,
  alerta al detectar, golpe (`attackClips`), daño (`Health.Damaged`) y muerte (`Health.Died`).
  **Cableado en los 4 tipos** (melee/kamikaze/ranged/tank). El **disparo del ranged** y la muerte de
  todos funcionan. La muerte usa **`PooledSfx`** (prefab `SfxOneShot`): un audio que se saca del pool
  y **sobrevive** al reciclaje del enemigo (el kamikaze no lo usa: su muerte es `ExplosionKamikaze`).
- **VFX (realistas, URP)**: ✅ migrados a **packs gratis** (Vefects Free Blood/Free Fire,
  Gabriel Aguiar Free Quick Effects); `WeaponEffects` con pooling — fogonazo/humo/impacto/sangre
  y explosión por prefab. Tamaño/color en el prefab, no en código.
- **Gore**: ✅ decals **persistentes** (`SurfaceDecalPool` + `Blood`/`BulletDecalManager`): **agujeros
  de bala** en el mundo y **charcos de sangre** en el suelo (tope para hordas), + **marcas de sangre en
  el cuerpo** del enemigo (`BodyDecals`, se limpian al reciclar del pool).

**Siguiente (orden sugerido):** 🎯 **MODELOS 3D ANIMADOS** — reemplazar las cápsulas por el maniquí
de **Starter Assets – ThirdPerson (URP)** (locomoción incluida) + **Mixamo** (attack/death). Ver el
detalle y los **pasos pendientes** en el pilar *"Personajes y animaciones (modelos 3D)"* de arriba.
Después: **spawners por zonas** → **mapa-arena grande** (el gran salto restante).
Pendientes menores: clip real de sonido de explosión, props destructibles, más armas, **decals de
bala por superficie** (sistema propio del autor, a futuro).

---

## 🔀 DECISIÓN (2026-06-08) — Camino A: adoptar el framework del *Low Poly Shooter Pack*

> **Contexto:** importamos el **Low Poly Shooter Pack – Free Sample** (InfimaGames) para el
> **viewmodel FPS** (brazos + armas + animaciones). Intentamos el *Camino C* (solo su arte +
> puente a nuestro `Weapon`), pero el cableado del viewmodel resultó **muy tedioso** (el origen
> del rig, la pose Idle, el controller dependiente de sus scripts, etc.). Tras evaluar opciones,
> se decide el **Camino A**.

**Camino A = ASHFALL (URP) sigue siendo la base + adoptamos la LÓGICA del pack para
jugador/arma/viewmodel.** Archivamos nuestros scripts de jugador/arma (no se borran) y usamos su
sistema, que ya trae el viewmodel y las armas funcionando.

**Por qué A y no las otras:**
- **No el "Camino B"** (abrir el proyecto del pack y traer lo nuestro): su proyecto es **Built-in**,
  y todo nuestro **VFX/gore/decals es URP** → saldría roto y habría que rehacerlo. Pérdida enorme.
- Lo más caro de ASHFALL (**enemigos/hordas/gore/oleadas en URP**) **se conserva intacto**.

**Qué CONSERVAMOS:** enemigos (4 tipos + IA/aggro + `EnemyAudio`), oleadas, gore/decals, VFX URP,
GameManager, proyecto + git. **Qué ARCHIVAMOS** (a `Assets/_Archive/`): `PlayerMovement`,
`WeaponManager`, `Weapon`, `MouseLook`, recoil, footsteps nuestros. El **dash/stamina** se re-pega
sobre el movimiento del pack más adelante.

**Pasos para MAÑANA (orden):**
1. **Commit de seguridad** de ASHFALL como está (antes del cambio grande).
2. **Archivar** nuestros scripts de jugador/arma a `Assets/_Archive/` (fuera de compilación).
3. **Importar el `Code/`** del pack + su **player prefab** `P_LPSP_FP_CH`; **convertir sus
   materiales a URP** (ya sabemos cómo: Standard/Autodesk Interactive → URP/Lit).
4. **Puentes** (lo único nuevo a escribir):
   - su arma (raycast) → daña a **nuestros enemigos** vía `IDamageable`/`Health`.
   - nuestros enemigos (`EnemyAI`/ataques) → dañan a **su jugador** (su Health).
   - HUD: usar el suyo al inicio; re-enganchar **oleadas** después.
5. Probar: **jugador del pack** (viewmodel + armas que funcionan) **vs nuestras hordas**.

**Bonus a aprovechar del pack:** **barriles explosivos + props destructibles** (cumple el pendiente
"props destructibles") y sus **VFX** (muzzle flash, impactos, casquillos).

**✅ EJECUTADO (jun 2026).** El Camino A se completó: player del pack (`Player_LPSP`, viewmodel
con brazos+armas reales) integrado con **salto/dash/stamina re-pegados** sobre su `Movement`
(+ jump buffer), puente de daño `LpspBulletDamage` (balas del pack → nuestros enemigos, con
gore), barriles explosivos funcionando, HUD híbrido (mira/munición del pack + caja SALUD +
overlay propio de daño direccional y hitmarker). FOV corregido a 70 (el pack traía 90 vertical
≈ 121 horizontal). Scripts propios archivados según lo planeado.

---

## 🔊 Sección AUDIO — ✅ CERRADA (jun 2026)

Sistema de audio completo; el detalle vive en `docs/AUDIO_TODO.md`. Resumen:
- **Jugador** (`PlayerAudio`, voces grabadas por el autor): daño (anti-eco), muerte, salto,
  aterrizaje (por fuerza), dash, sin-stamina, latido a vida baja. Fuente 2D propia (lección:
  compartir fuente con los pasos del pack silenciaba la muerte al congelar el juego).
- **Enemigos** (`EnemyAudio` 3D en los 4 tipos): idle/alerta (aggro), ataque, daño, muerte
  vía **`PooledSfx`** (audio que sobrevive al reciclaje del pool). Kamikaze con explosión propia.
- **Detección/aggro** (`EnemyAI.detectionRange` + evento `Aggroed`): enemigos dormidos hasta
  que el jugador se acerca — base de la activación por zonas del mapa grande.
- **Hitmarker** (X + tic, bus `HitConfirmed`), **UI** (`UiAudio`: click/hover/esc) y
  **pausa real de audio** (`AudioListener.pause` como interruptor maestro).
- **🎵 Música adaptativa estilo Serious Sam** (`MusicManager`): 2 capas en loop permanente
  (peace/fight generadas con IA, frase firma compartida, E frigia dominante) + crossfade por
  enemigos vivos + histéresis. `firstWaveDelay = 10s` de calma inicial.

**Siguiente (orden sugerido):** 🎯 **modelos 3D animados para ENEMIGOS** (las cápsulas siguen;
el lado jugador lo resolvió el pack) → **spawners por zonas** → **mapa-arena grande**.
Audio: decisión del autor — **sin** jingles de oleada/victoria/derrota (la música adaptativa
ya comunica el combate); ranged cubierto. Solo queda pulido opcional (stinger, crouch, ambiente).

---

## 🎬 Sección ANIMACIONES PROPIAS + PARKOUR (jun 2026) — pipeline cerrado, debug pendiente

**Pivote de viewmodel:** se descartó el viewmodel del pack para el largo plazo; el juego corre
en **modo desarmado** (`UnarmedMode`: bloquea Fire/Aim/Reload por input; reversible) con
**brazos propios** — rig **Hozq LVA4** (`BlenderWork/LVAA.blend`) + animaciones del autor.
Setup de Blender MCP en `docs/BLENDER_MCP_SETUP.md`; quirks del pipeline en la memoria del agente.

- **Animaciones del autor** (FBX `Assets/Characters/FPSArms/SK_FPSArms_LVA4.fbx`, 3 takes vía NLA):
  - `Idle_Unarmed` (240f/10s): respiración + puños ocasionales (pose, cascada de dedos y
    timing del autor; espejado L→R por script con flip de cuaterniones).
  - `Run_Unarmed` (16f, ×1.45): brazos en contrafase que se cruzan, manos abiertas relajadas.
  - `Climb_Unarmed` (14f): estirada → agarre (75% del puño del autor, palmas posadas) → tracción.
- **Animator:** Blend Tree `Locomotion` por `Speed` real (0/4.3=idle, 6.8=run) + estado `Climb`
  por trigger. `ViewmodelLocomotion` alimenta Speed; `LedgeClimb.ClimbStarted` dispara clim+sonido.
- **Parkour (`LedgeClimb`):** Espacio cerca de un muro → mantle fluido (pegado→subir→entrar,
  perpendicular al muro por su normal). **Auto-agarre en el aire** (saltar hacia muros altos).
  Lenguaje de alturas: ≤2.4m trepable directo · 2.4–3.5 salto+agarre · >3.5 solo rampas.
- **Game feel:** bob de cámara procedural espejo del Blend Tree (frecuencia constante = pisadas
  de la anim 4.35/s; amplitud = peso del blend) + golpe de aterrizaje con recuperación
  (`LandingBob`). **Dash DESACTIVADO** por diseño (`dashEnabled=false`; el código queda).

**✅ BUG RESUELTO (con instrumentacion en vivo):** el "piso fantasma" tenia DOS raices,
halladas con un logger temporal (147 muestras): (1) el grounded por colisiones del pack
contaba RAMPAS rozadas en pleno salto como piso; (2) la stamina REGENERABA EN EL AIRE
(saltar+correr = sprint infinito, nunca llegaba a 0). Fix: grounded deterministico por
FixedUpdate (spherecast angosto bajo los pies + normal horizontal + no estar subiendo)
y stamina congelada en el aire. Leccion de metodo: medir > teorizar.

**Siguiente:** anim de **disparo** (AR_X montado en el rig de Hozq) →
modelos de enemigos (concepto "El Calcinado" listo para Gemini) → **La Fundición Fase 1: el Atrio**
(diseño completo en `docs/MAPA_FUNDICION.md`).
