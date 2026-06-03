# Conectar el MCP de Unity (MCP for Unity) — máquina nueva

Guía para que el agente IA (Claude) controle el editor de Unity vía MCP.
**Sigue el orden exacto** — el paso 3 es el que más se olvida y sin él el agente
no "ve" Unity aunque todo parezca bien.

## Prerequisitos (instalar una vez por máquina)
| Herramienta | Para qué |
|---|---|
| **Unity 6** (`6000.4.x`) + Unity Hub | abrir el proyecto |
| **Claude Code** (CLI/app) | el agente |
| **Python 3.10+** (lo resuelve `uv`) | el servidor MCP usa Python |
| **git** | clonar/sincronizar |

## 1. Clonar y abrir
```bash
git clone https://github.com/DanielOjeda25/shooter-dem.git
```
Abre la carpeta como proyecto en **Unity Hub** (Unity 6). Al abrir, Unity
**descarga solo** el paquete *MCP for Unity* (está en `Packages/manifest.json`).
Espera a que termine de importar.

## 2. Arrancar el servidor dentro de Unity
1. Menú **`Window → MCP for Unity`**.
2. Pestaña **Connect**, sección **Server**:
   - **Transport:** `HTTP Local`
   - **HTTP URL:** `http://127.0.0.1:8080`
3. Pulsa **Start Server**. Debe quedar **🟢 Session Active**
   (el botón pasa a decir *Stop Server*).
4. En **Client Configuration → Per-client setup**, elige **Client: Claude Code**
   y comprueba que esté **🟢 Configured** (si no, pulsa **Configure**).

## 3. ⭐ Registrar el server en Claude a nivel GLOBAL (paso clave)
El "Configure" del plugin deja la config a nivel **proyecto**, y el agente
(que corre en otra carpeta) **no la ve**. Hay que registrarlo en el scope global:

```bash
claude mcp add --scope user --transport http UnityMCP http://127.0.0.1:8080/mcp
```

## 4. Verificar y reconectar
1. Comprueba la conexión:
   ```bash
   claude mcp list
   ```
   Debe aparecer: `UnityMCP: http://127.0.0.1:8080/mcp (HTTP) - ✓ Connected`
2. **Reinicia el cliente de Claude** (la app). Las ~40 herramientas de Unity se
   cargan **al arrancar** la sesión; por eso hay que reiniciar tras conectar.
3. Prueba final: pídele al agente *"lee la jerarquía de mi escena de Unity"*.
   Si la lee → **conectado de verdad**.

## Problemas comunes
- **`✗ Failed to connect`** en `claude mcp list` → el server no está corriendo:
  vuelve al paso 2 y pulsa **Start Server** (Unity debe estar abierto).
- **El agente no ve herramientas** aunque `claude mcp list` diga `✓ Connected` →
  reinicia el cliente (paso 4.2). Las tools solo se cargan al arrancar la sesión.
- **Regla de oro:** Unity tiene que estar **abierto con el server corriendo**
  ANTES de pedirle cosas de Unity al agente. Si cierras Unity, el agente pierde
  las "manos".

## Notas
- Puerto por defecto: **8080** (HTTP Local). Si lo cambias en Unity, ajusta también
  el `claude mcp add` con la nueva URL.
- El paquete del plugin viaja en el repo (`Packages/manifest.json`), así que NO hay
  que instalarlo a mano en cada máquina: Unity lo baja al abrir el proyecto.
