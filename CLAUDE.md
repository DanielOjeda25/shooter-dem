# CLAUDE.md — Contexto para el agente (Shooter Demo)

> Léeme al empezar cualquier sesión en este proyecto.

## ⚠️ NOTA IMPORTANTE — Git LFS (leer al sincronizar en otra PC)
Este repo usa **Git LFS** para binarios: imágenes, fuentes, audio y **los DLLs de Roslyn**
en `Assets/Packages/` (instalados con NuGetForUnity para que el `execute_code` del MCP use
el backend Roslyn y no el CodeDom, que petaba con un BOM → `Line 1: ﻿`).

**Si algo va mal tras `git pull`** (DLLs/imágenes que aparecen como archivos de texto de ~1 KB,
o Unity da errores raros al importar `Assets/Packages/`):
1. `git lfs install`  (una sola vez por PC)
2. `git lfs pull`     (descarga los binarios reales)

Luego abre el proyecto en Unity (la 1ª vez tarda: reconstruye `Library/` y baja los paquetes
de git — unity-mcp y NuGetForUnity — desde GitHub, necesita internet).
Nota: el **juego compila aunque falte LFS** (los scripts no dependen de Roslyn); solo se
rompería el tooling de tests por MCP.

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
**v1 cerrado (Fases 0–6)** y **v2.0 en marcha** (arena horde shooter estilo **Serious Sam**).
Es **FPS** (cámara en los ojos). El detalle vivo está en `docs/ROADMAP.md`; resumen:

- **Base v1 (FPS):** suelo 50×50, Player (cápsula + CharacterController), movimiento WASD +
  sprint/salto/crouch (`PlayerMovement`/`MouseLook`/`MovementFeel`), arma por raycast,
  munición/recarga, daño mutuo, NavMesh, **HUD/menús en UI Toolkit**, game over y pausa
  (`GameManager`). Todo con el **Input System nuevo**.
- **Arsenal data-driven:** `WeaponData` (SO) con 3 disparos (Single/Shotgun/Projectile),
  `WeaponManager` (cambio 1/2/3 + rueda), Pistola/Escopeta/Bazooka. Audio por arma.
- **Enemigos data-driven:** `EnemyData` (SO) + spawner ponderado + `EnemyPool`. Tipos:
  **melee** (rojo) y **kamikaze** (naranja) con patrón **Strategy** (`EnemyAttack`). El
  kamikaze explota **al morir por cualquier causa** (`Health.Died`) → **explosión en cadena**.
  Oleadas con `WaveSystem` (pacing + escalado de dificultad).
- **VFX (realistas, URP):** efectos por **packs gratis** — Vefects (Free Blood, Free Fire) y
  Gabriel Aguiar (Free Quick Effects). `WeaponEffects` (con **pooling**) cablea fogonazo, humo,
  impacto y sangre; el **tamaño/color vive en cada prefab/variant** (`Assets/Prefabs/VFX/`), no
  en código. **Explosión** = prefab reutilizable (kamikaze + bazooka).
- **Gore:** **sangre persistente** en el suelo — `BloodDecalManager` (pool circular de decals
  con tope para hordas) + `Materials/BloodDecal.mat`.

**Norte del proyecto:** mapas enormes + hordas (Serious Sam). Cada decisión se evalúa por:
¿escala a mapa grande + hordas?
**Siguiente:** 3er/4º enemigo (**ranged** + **tanque**) → spawners por zonas → **mapa grande**.
