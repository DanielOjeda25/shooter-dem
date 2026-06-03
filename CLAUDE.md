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
**Fase 1 iniciada.** Escena `Assets/Scenes/SampleScene.unity`: cámara, luz
direccional, Global Volume (URP) y un **Plane** (suelo) recién creado.
Pendiente inmediato: centrar el Plane en (0,0,0), agrandarlo y trabajar la luz.
Falta definir: **FPS o TPS** (el autor dijo "personaje con arma" → probablemente TPS).
