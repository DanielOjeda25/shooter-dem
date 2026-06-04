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

## Fase 5.5 — El escenario ⬜
> Objetivo del autor: un mapa **bueno**, con niveles o subniveles. El autor modelará
> dentro de Unity (tiene **ProBuilder**). El agente guía y verifica por MCP.
- [ ] Greybox del nivel con ProBuilder (paredes, rampas, cobertura, salas)
- [ ] Marcar geometría como obstáculo + **re-hornear el NavMesh** (los enemigos rodean)
- [ ] Niveles / subniveles: estructura de escenas o zonas + transiciones (puertas/triggers)
- [ ] Iluminación y ambiente básico del escenario

## Fase 6 — Pulido ⬜
- [ ] Sonidos (disparo, impactos)
- [ ] Partículas (muzzle flash, sangre/chispas)
- [ ] Animaciones básicas

---

### Hito actual
**Fases 0–5 cerradas** ✅. Juego jugable de principio a fin: mundo + jugador FPS + arma
(raycast/impacto/munición) + enemigos que se generan (`EnemySpawner`), te persiguen por
NavMesh y te atacan; HUD de vida/munición, mira, y **victoria/derrota** con game over real
(`GameManager` congela el juego y muestra panel GANASTE/PERDISTE).
Siguiente: **Fase 5.5 — El escenario** (mapa con ProBuilder + niveles, lo modela el autor),
y luego **Fase 6 — Pulido** (sonido, partículas, animación).
