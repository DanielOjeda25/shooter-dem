# CLAUDE.md — Contexto para el agente (Shooter Demo)

> Léeme al empezar cualquier sesión en este proyecto.

## Qué es esto
Proyecto de **aprendizaje** de desarrollo de videojuegos: un **shooter 3D** en
**Unity 6 (URP)**. Meta del autor: personaje con arma, varios enemigos con IA,
moverse y dispararles.

## Método de trabajo (IMPORTANTE)
El autor **está aprendiendo** y quiere **entender**, no que el agente lo haga todo.
Por cada paso, el agente debe dar:
1. **Qué** vamos a hacer y **por qué**.
2. **Cómo** hacerlo en Unity (los clics/menús concretos).
3. **Qué concepto** se aprende.
El **autor lo hace** en el editor; el agente **verifica por MCP** y enseña. El MCP
se usa para *comprobar* el estado de la escena y para tareas voluminosas — no para
sustituir al autor en lo que debe aprender.

- Responde y documenta **en español**. Sé conciso, paso a paso, sin abrumar.
- Vamos **una cosa a la vez**.

## Idioma técnico Unity (vs Godot, de donde viene el autor)
- **GameObject** = objeto de la escena (en Godot: Nodo).
- **Component** = pieza que le pegas a un GameObject (Transform, MeshRenderer,
  Collider, scripts...). Un objeto es un GameObject + sus components.
- Ventanas: **Hierarchy** (árbol de objetos), **Scene** (vista de edición),
  **Game** (lo que ve el jugador), **Inspector** (propiedades), **Project** (assets).

## MCP (control del editor por IA)
- Conexión: ver **`docs/MCP_SETUP_UNITY.md`**.
- Regla: Unity **abierto** + `Window → MCP for Unity` → **Start Server** (🟢) +
  `UnityMCP` registrado en scope **global** + cliente reiniciado.
- El server vive en Unity: si se cierra Unity, el agente pierde acceso.

## Roadmap (resumen)
Ver `docs/ROADMAP.md`. Orden de construcción (de adentro hacia afuera):
0. Setup (Unity + MCP + repo) · 1. Mundo (suelo, luz) · 2. Jugador (movimiento + cámara)
· 3. Arma (disparo) · 4. Enemigos (IA, NavMesh) · 5. Reglas (daño, HUD, win/lose)
· 6. Pulido (sonido, partículas, animación).

## Estado actual
**Fases 0–5 cerradas.** Decidido: el shooter es **FPS** (cámara en los ojos).
Escena `Assets/Scenes/SampleScene.unity`: suelo (Plane 50×50 con `Materials/Ground.mat`),
Directional Light, Global Volume (URP) y **Player** (cápsula + CharacterController) con la
Main Camera como hija a la altura de los ojos.
**Arma**: `Player/Main Camera/Weapon` (cubo placeholder) con `Weapon.cs` — disparo por
raycast desde la cámara, marca de impacto (prefab `Assets/Prefabs/ImpactMark`, orientada con
`FromToRotation` y pegada al objeto golpeado), munición (cargador + recarga con R vía
corrutina) y daño al enemigo (`Weapon.damage`).
**Enemigos**: prefab `Assets/Prefabs/Enemy` (cápsula roja, `Materials/Enemy.mat`) con
`EnemyHealth.cs`, `NavMeshAgent` y `EnemyAI.cs` (te persigue y te golpea con cooldown; busca
al Player solo con `FindAnyObjectByType`). `EnemySpawner.cs` los genera en círculo al empezar.
NavMesh horneado con `NavMeshSurface` en el Plane; Player y Enemy excluidos con
`NavMeshModifier` (Remove Object). `PlayerHealth.cs` en el Player recibe el daño.
**UI/Reglas**: Canvas con mira (`Crosshair`), HUD (`HUD.cs` → `HealthText`/`AmmoText`), panel
de fin (`GameOverPanel`/`GameOverText`) y menú de pausa (`PausePanel` con botones
Reanudar/Reiniciar/Salir + `PauseTitle`). `GameManager.cs` (singleton) cuenta enemigos, dispara
victoria/derrota con game over real (`Time.timeScale = 0` + cursor libre + panel), y gestiona
pausa con Esc y reinicio (`SceneManager.LoadScene`). `MouseLook` y `Weapon` ignoran input cuando
`Time.timeScale == 0` (no mover cámara ni disparar en pausa/game over).
Scripts en `Assets/Scripts/`: `PlayerMovement`, `MouseLook`, `Weapon`, `EnemyHealth`, `EnemyAI`,
`EnemySpawner`, `PlayerHealth`, `HUD`, `GameManager` — todo con el **Input System nuevo**.
Siguiente: **Fase 5.5 — El escenario** (mapa con ProBuilder + niveles, lo modela el autor),
luego **Fase 6 — Pulido**.
