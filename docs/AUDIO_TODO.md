# AUDIO — Inventario y sonidos que faltan (ASHFALL)

> Documento vivo. Lista lo que **ya tenemos** y lo que **falta** para el sabor de
> *horde shooter* con atmósfera. Generado revisando `Assets/Audio/` + los scripts.

**Leyenda de prioridad**
🔴 Alta (falta feedback que se nota al jugar) · 🟡 Media · ⚪ Baja / pulido · 🎵 Ambiente/música

**Leyenda de "Falta"**
- `clip` → solo falta el archivo de sonido (el código ya tiene dónde meterlo).
- `clip + código` → falta el archivo **y** el enganche en el script/SO (hay que programarlo).

---

## ✅ Lo que YA tenemos

| Categoría | Clips | Cableado en |
|---|---|---|
| **Pasos** | `Footsteps/concrete1..4`, `sprint` | `PlayerFootsteps.cs` |
| **Impactos** | `Impacts/concrete1..4` (pared), `flesh1..5` (enemigo) | `WeaponEffects` |
| **Pistola** | `fire1..2`, `reload`, `empty` | `WeaponData` (fire/reload/empty) |
| **Escopeta** | `sg_fire1..4`, `sg_reload1..3`, `sg_cock`, `sg_empty` | `WeaponData` |
| **Bazooka** | `rocketfire1` (solo el disparo) | `WeaponData` |
| **Compartidos** | `weapon_switch` (cambio de arma), `pl_shell1..3` (casquillos) | `WeaponManager`, `WeaponEffects` |
| **Dash** | `Player/dash.wav` (whoosh de esquiva) | `PlayerMovement.cs` (`PlayOneShot` al iniciar el dash) |
| **Melee (ataque)** | `monsterAttack-mele.mp3` | `EnemyAudio` (prefab Enemy) — `attackClips`, lo dispara `EnemyAI` tras `Execute` |
| **Kamikaze** | `kamikazeIdle` (loop), `kamikazeAlert`+`kamikazeScream_1` (alerta), `kamikazeExplosion` (muerte) | `EnemyAudio` (prefab Kamikaze) + prefab `ExplosionKamikaze` |
| **Enemigos (común)** | `enemyHurt`+`enemyHurt2` (daño), `enemyDeath` (muerte), `enemyShoot` (ranged) | `EnemyAudio` en los 4 prefabs; muerte vía `PooledSfx`/`SfxOneShot` |

`WeaponData` (SO) ya expone: `fireClips[]`, `reloadClips[]`, `emptyClip`. Cualquier arma nueva
solo necesita arrastrar clips ahí (sin tocar código).

---

## ❌ Sonidos que FALTAN

### 1) Movimiento del jugador  (`PlayerMovement.cs` — hoy NO tiene audio)
| Sonido | Acción / tecla | Falta | Prioridad |
|---|---|---|---|
| ~~**Dash / esquiva**~~ | Alt (whoosh corto) | ✅ **HECHO** (`Player/dash.wav`) | ✅ |
| **Salto** | Espacio (esfuerzo/tela) | clip + código | 🟡 |
| **Aterrizaje** | al tocar suelo tras caer | clip + código | 🟡 |
| **Agacharse / levantarse** | Ctrl (roce de tela) | clip + código | ⚪ |
| **Sin stamina** | al intentar sprint/dash sin barra | clip + código | ⚪ |
| Pasos por superficie (ceniza/metal) | hoy solo "concrete" | clip + código | ⚪ |

### 2) Daño y muerte del jugador  (`PlayerHealth.cs` — hoy NO tiene audio)
| Sonido | Cuándo | Falta | Prioridad |
|---|---|---|---|
| **Recibir daño / gruñido** | `TakeDamage` | clip + código | 🔴 |
| **Muerte del jugador** | `OnDeath` / `PlayerDied` | clip + código | 🔴 |
| Latido / alarma a vida baja | HP por debajo de X | clip + código | ⚪ |

### 3) Enemigos  (`EnemyData.cs` NO tiene campos de audio — hay que añadirlos)
| Sonido | Cuándo | Falta | Prioridad |
|---|---|---|---|
| ~~**Muerte del enemigo** (vocal)~~ | `Health.Died` | ✅ **HECHO** (`enemyDeath.mp3` vía `PooledSfx`) | ✅ |
| ~~**Ataque melee** (zarpazo/golpe)~~ | `MeleeAttack` | ✅ **HECHO** (`monsterAttack-mele.mp3`) | ✅ |
| ~~**Detección / aggro / gruñido**~~ | al detectar al jugador (`EnemyAI.Aggroed`) | ✅ **HECHO** (kamikaze: alert+scream) | ✅ |
| ~~**Disparo enemigo ranged**~~ | `RangedAttack` (vía `EnemyAI.PlayAttack`) | ✅ **HECHO** (`enemyShoot.wav`) | ✅ |
| **Proyectil enemigo: vuelo + impacto** | `EnemyProjectile.cs` (sin audio) | clip + código | 🟡 |
| ~~**Kamikaze: gruñido / idle al acercarse**~~ | mientras corre a explotar | ✅ **HECHO** (`kamikazeIdle` loop 3D) | ✅ |
| **Tanque: pisada pesada / golpe fuerte** | enemigo tanque | clip + código (falta clip propio) | ⚪ |
| ~~Quejido al ser herido (vocal)~~ | `Health.Damaged` (no letal) | ✅ **HECHO** (`enemyHurt`+`enemyHurt2`) | ✅ |

> ✅ **Arquitectura hecha:** componente **`EnemyAudio`** en el prefab del enemigo (no en `EnemyData`,
> que es solo spawn). `AudioSource` **3D** (oyes de dónde viene cada enemigo) + campos `idleLoop`,
> `alertClips[]`, `attackClips[]`, `hurtClips[]`, `deathClips[]`. Reacciona a `EnemyAI.Aggroed`
> (detección), `EnemyAI.PlayAttack()`, `Health.Damaged` (hurt no letal) y `Health.Died` (muerte).
> **Cableado en los 4 tipos** (melee/kamikaze/ranged/tank). Enemigo nuevo = arrastrar clips, sin código.
>
> ✅ **Muerte del enemigo (resuelto):** el enemigo vuelve al pool al morir (su `AudioSource` se
> cortaría), así que el clip suena en un objeto **independiente que sobrevive**: **`PooledSfx`**
> (prefab `SfxOneShot`) — se saca del pool, suena en su posición y vuelve solo al terminar. El
> kamikaze no usa `deathClips` (su muerte ES la explosión `ExplosionKamikaze`).

### 4) Explosiones  (`Projectile.cs` y `KamikazeAttack.cs`)
| Sonido | Cuándo | Falta | Prioridad |
|---|---|---|---|
| ~~**Explosión** (bazooka + kamikaze)~~ | al impactar / al morir kamikaze | ✅ **HECHO** (`Explosions/bazookaExplosion.wav`) | ✅ |

> ✅ El clip vive en el **`AudioSource` del prefab `Explosion`** (`PlayOnAwake`), reutilizado por
> bazooka y kamikaze sin código. `PoolManager.RestartEffects` reinicia el `AudioSource` al sacar
> el efecto del pool, así suena en **cada** explosión (no solo la primera).

### 5) Armas — huecos sueltos
| Sonido | Arma | Falta | Prioridad |
|---|---|---|---|
| **Recarga / cargar cohete** | Bazooka (solo tiene `rocketfire1`) | clip | 🟡 |
| **Clic sin munición** | Bazooka (`emptyClip` vacío) | clip | ⚪ |
| Cola/silbido del cohete en vuelo | Bazooka | clip + código | ⚪ |
| ~~**Hitmarker** (confirmación de impacto)~~ | todas (`Weapon.Hit`→`HudController`) | ✅ **HECHO** (`UI/hitmarker.mp3` + **X** visual en el crosshair) | ✅ |

### 6) Oleadas y fin de partida  (`WaveSystem.cs`, `GameManager.cs` — sin audio)
| Sonido | Cuándo | Falta | Prioridad |
|---|---|---|---|
| **Inicio de oleada / "horda entrante"** | nueva oleada | clip + código | 🟡 |
| **Oleada superada** | se limpia la oleada | clip + código | 🟡 |
| **Victoria** ("GANASTE") | `TriggerVictory` | clip + código | 🟡 |
| **Derrota** ("PERDISTE") | `HandlePlayerDied` | clip + código | 🟡 |

### 7) UI / menús  (`PauseMenuController.cs`, `GameOverController.cs` — sin audio)
| Sonido | Cuándo | Falta | Prioridad |
|---|---|---|---|
| **Clic de botón** | menús (pausa, game over) | clip + código | ⚪ |
| Hover de botón | menús | clip + código | ⚪ |
| **Abrir pausa / reanudar** | Esc | clip + código | ⚪ |

### 8) Ambiente y música  (no existe nada aún) 🎵
| Sonido | Uso | Falta | Prioridad |
|---|---|---|---|
| **Ambiente decadente** (viento, ceniza, drones) | loop de fondo | clip + código | 🎵 |
| **Música de combate** | sube en oleadas intensas | clip + código | 🎵 |
| Stinger | inicio de oleada / hito | clip + código | 🎵 |

### 9) Futuro (cuando existan estos sistemas)
- **Pickups** (munición / vida): aún no hay sistema de recogidas → pendiente de diseño.
- **Props destructibles**: sonido de rotura cuando se implementen.

---

## Resumen rápido — lo más urgente (🔴)
1. ~~**Dash** (whoosh).~~ ✅ HECHO
2. **Daño y muerte del jugador**. ← *único 🔴 que queda (lo grabás vos)*
3. ~~**Muerte del enemigo** + **ataque melee**.~~ ✅ HECHO (+ hurt + disparo ranged)
4. ~~**Explosión** (bazooka + kamikaze).~~ ✅ HECHO

## Notas
- **Formato**: `.wav` (como el resto del proyecto). Mono para SFX 3D posicional.
- **Convención de carpetas**: replicar el estilo actual, p. ej.
  `Assets/Audio/Player/`, `Assets/Audio/Enemies/<tipo>/`, `Assets/Audio/Explosions/`,
  `Assets/Audio/UI/`, `Assets/Audio/Ambience/`.
- **Data-driven**: meter los clips en los SO (`WeaponData`, y un futuro audio en `EnemyData`)
  evita tocar código por cada variante.
- **Fuentes gratis** sugeridas (verificar licencia CC0/atribución): Freesound, Sonniss GDC,
  Kenney (UI), Mixkit. Igual que se hizo con VFX por packs gratis.
