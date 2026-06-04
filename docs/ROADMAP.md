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

## Fase 3 — El arma ⬜
- [ ] Arma (placeholder) en el jugador
- [ ] Disparo por raycast + efecto de impacto
- [ ] Munición

## Fase 4 — Los enemigos ⬜
- [ ] Enemigo (cápsula) + vida
- [ ] IA con NavMesh (patrulla / persecución / ataque)
- [ ] Daño mutuo (disparo baja vida del enemigo; el enemigo daña al jugador)

## Fase 5 — Las reglas ⬜
- [ ] HUD (vida, munición)
- [ ] Spawns de enemigos
- [ ] Victoria / derrota

## Fase 6 — Pulido ⬜
- [ ] Sonidos (disparo, impactos)
- [ ] Partículas (muzzle flash, sangre/chispas)
- [ ] Animaciones básicas

---

### Hito actual
**Fases 0, 1 y 2 cerradas** ✅. Mundo base (suelo 50×50 con material + luz + skybox)
y jugador FPS funcional (moverse con WASD + mirar con ratón). Siguiente: **Fase 3 — El arma**
(arma placeholder en el jugador + disparo por raycast).
