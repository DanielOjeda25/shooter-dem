# Blender MCP — Setup (para conectar el agente a Blender)

> Permite que Claude Code vea y manipule la escena de Blender (importar modelos,
> armar rigs, sacar capturas del viewport) mientras vos animás.
> Ya está funcionando en la PC de escritorio; estos son los pasos para la notebook.

## Qué se necesita
- **Blender** (probado con 5.0; el addon declara 3.0+).
- **uv** (lanza el server MCP): `winget install astral-sh.uv` (o ya está si usaste uv antes).
- El **addon** está en este repo: `BlenderWork/blender-mcp-addon/addon.py`
  (fuente: https://github.com/ahujasid/blender-mcp — MIT).

## Pasos

### 1. Addon en Blender (una vez por PC)
1. Abrir Blender → `Edit → Preferences → Add-ons` → botón **˅** → **Install from Disk...**
2. Elegir `<repo>/BlenderWork/blender-mcp-addon/addon.py` → instalar.
3. **Tildar el checkbox** del addon ("Blender MCP") para activarlo.
4. En la vista 3D: tecla **`N`** → pestaña **BlenderMCP** → **Connect to MCP server**.
   Debe decir `Running on port 9876`.

### 2. Registrar el server MCP en Claude Code (una vez por PC)
**Probar primero lo simple** (en PowerShell o CMD — ¡NO en Git Bash, que rompe el `/c`!):
```
claude mcp add blender -s user -- uvx blender-mcp
claude mcp list   # debe decir blender ✓ Connected (con Blender abierto y addon conectado)
```

**Si falla con error de `pywin32` bloqueado** (nos pasó en la desktop — bug de uv en Windows):
```
uv venv "$env:USERPROFILE\.blender-mcp-venv" --python 3.12
uv pip install blender-mcp --python "$env:USERPROFILE\.blender-mcp-venv\Scripts\python.exe"
claude mcp remove blender -s user
claude mcp add blender -s user -- "$env:USERPROFILE\.blender-mcp-venv\Scripts\blender-mcp.exe"
```

### 3. Usarlo
1. Abrir Blender con el addon **conectado** (paso 1.4 — se conecta en cada sesión de Blender).
2. Abrir/reiniciar la sesión de Claude Code (las herramientas MCP se cargan al arrancar).
3. Listo: pedile al agente que trabaje en Blender (p. ej. "importa los brazos y montale el arma").

## Gotchas conocidos
- `claude mcp list` dice **Failed to connect** si Blender está cerrado o el addon
  no está conectado → es normal, no es error de instalación.
- El registro con `cmd /c uvx ...` desde **Git Bash** se rompe (convierte `/c` en `C:/`).
  Usar PowerShell, o registrar el exe directo (método del venv).
- Blender 5 cambió APIs (`Bone.select`, `action.fcurves` → slots/layers); el agente
  ya conoce los reemplazos.

## Archivos del proyecto
- `BlenderWork/LVAA.blend` — rig **Low-poly Viewmodel Arms V4** (Hozq). Base de las
  animaciones FPS propias (idle/disparo/recarga/correr).
  ⚠️ Licencia: gratis para uso personal/aprendizaje; **uso comercial = $19** (Gumroad).
- `BlenderWork/ASHFALL_FPS_Anims.blend` — experimento con el rig LPSP importado
  (referencia de qué NO hacer: rig de juego sin controles).
- Los `.blend1` son backups de Blender (gitignoreados).
