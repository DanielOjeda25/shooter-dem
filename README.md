# Shooter Demo (shooter-dem)

Proyecto de **aprendizaje**: un pequeño **shooter 3D** en **Unity 6 (URP)**.
Meta: un personaje con un arma, varios enemigos con IA, moverte y dispararles.

> Desarrollo asistido por un agente IA (Claude) conectado al editor de Unity vía
> **MCP**. El enfoque es didáctico: el agente **guía y explica**, y el autor
> **hace y entiende** cada paso.

## Cómo abrir
1. Instala **Unity Hub** y **Unity 6** (versión `6000.4.x`).
2. Clona este repo y ábrelo como proyecto desde Unity Hub.
3. La primera vez, Unity importa todo y **descarga el paquete MCP for Unity**
   automáticamente (está en `Packages/manifest.json`). Espera a que termine.

## Conectar el agente IA (MCP) — IMPORTANTE
La guía completa, paso a paso, está en:
👉 **[`docs/MCP_SETUP_UNITY.md`](docs/MCP_SETUP_UNITY.md)**

Resumen: abrir Unity → `Window → MCP for Unity` → **Start Server** → registrar el
server en Claude a nivel global → reiniciar el cliente.

## Documentación
- 🔧 [`docs/MCP_SETUP_UNITY.md`](docs/MCP_SETUP_UNITY.md) — instalar/conectar el MCP
- 📋 [`docs/ROADMAP.md`](docs/ROADMAP.md) — fases del juego y progreso
- 🤖 [`CLAUDE.md`](CLAUDE.md) — contexto y método de trabajo para el agente
