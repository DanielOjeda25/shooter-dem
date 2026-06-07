# Estilo de UI — Shooter Demo

> **A partir de ahora, TODA la UI del juego usa esta paleta y sistema.** (HUD, menú de
> pausa, game over, menús futuros, ventanas…). Estética: **Half-Life** (sobrio, plano,
> texto a un lado, animaciones sutiles al hover).

## Paleta (duotono)
| Rol | Color | Valor |
|---|---|---|
| Fondo / oscuro | azul-carbón | `rgb(22, 22, 30)` (#16161E) |
| Texto / acento | blanco-hueso | `rgb(215, 232, 208)` (#D7E8D0) |
| Aviso / valor bajo | rojo | `rgb(230, 51, 38)` |
| Backdrop (overlays) | negro translúcido | `rgba(8, 9, 12, 0.7)` |

- Texto inactivo: el blanco-hueso a **~55% alpha**; al **hover/activo**, al 100%.

## Tipografía
- **Rajdhani** (`Assets/UI/Fonts/Rajdhani-SemiBold.ttf`), condensada, aire sci-fi/HL.
- Se referencia en USS con `-unity-font-definition` en el contenedor raíz (se hereda).

## Sistema
- **UI Toolkit** (UXML + USS), no uGUI. USS soporta `:hover`, `transition-*`, `translate`,
  variables `--var` → animaciones y theming como en web.
- Layout estilo HL: listas **alineadas a la izquierda**, texto pequeño, hover que **desliza**
  el ítem a la derecha + sube el brillo (transición suave).
- Resolución de referencia 1920×1080 (PanelSettings `ScaleWithScreenSize`).

## Convención
- Centralizar colores (idealmente como variables USS) para cambiar el tema en un sitio.
- uGUI solo para lo que UITK no cubre bien (p. ej. UI en el mundo 3D / barras sobre enemigos).
