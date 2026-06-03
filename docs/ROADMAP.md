# ROADMAP — Shooter Demo

Fases del proyecto. Un juego se construye **de adentro hacia afuera**: primero el
suelo, luego moverte, luego el arma, luego a quién dispararle, luego las reglas.

Leyenda: ✅ hecho · 🟡 en progreso · ⬜ pendiente

---

## Fase 0 — Setup 🟡
- [x] Unity 6 (URP) + proyecto creado
- [x] Paquete MCP for Unity instalado (en `Packages/manifest.json`)
- [x] MCP conectado (server HTTP 8080 + registro global) — ver `docs/MCP_SETUP_UNITY.md`
- [x] Repo Git + remoto (`shooter-dem`)
- [ ] Documentación base (este commit)

## Fase 1 — El mundo 🟡
- [x] Suelo (Plane) creado
- [ ] Centrar el suelo en (0,0,0) y agrandarlo
- [ ] Material/color al suelo
- [ ] Entender y ajustar la luz (Directional Light) + skybox

## Fase 2 — El jugador ⬜
- [ ] Decidir **FPS o TPS**
- [ ] GameObject del jugador (cápsula) + movimiento (WASD)
- [ ] Cámara (en primera o tercera persona)
- [ ] Input System

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
Cerrar **Fase 1**: dejar el mundo base (suelo centrado y con tamaño, luz a punto)
listo para meter al jugador en la Fase 2.
