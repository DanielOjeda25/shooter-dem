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
