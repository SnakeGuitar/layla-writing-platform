---
design_system:
  name: "Layla Design System"
  version: "2.0.0"
  theme: "Neumorphism"
  platforms:
    desktop:
      client: "client-desktop (WPF .NET 9)"
      tokens: "Nm* XAML resource keys"
      primary_bg: "#1E1E2E"
      accent: "#1A47C8"
    web:
      client: "client-web (Blazor Server .NET 9)"
      tokens: "CSS custom properties --nm-*"
      primary_bg: "#1E1E2E"
      accent: "#1A47C8"
  colors:
    bg_base:        "#1E1E2E"
    shadow_light:   "#2A2A3E"
    shadow_dark:    "#141420"
    text_primary:   "#E8E8F0"
    text_secondary: "#9090A8"
    accent:         "#1A47C8"
    accent_hover:   "#2E5CE6"
    error:          "#FF7675"
    success:        "#55EFC4"
  font: "Inter"
---

# Layla Design System — Neumorfismo

Referencia visual autoritativa para ambos clientes. El lenguaje visual es **Neumorfismo oscuro** en las dos plataformas: misma paleta, misma tipografía, mismo sistema de sombras. La implementación técnica difiere por plataforma (XAML vs. CSS).

| Plataforma | Cliente | Tokens | Stack |
| :--- | :--- | :--- | :--- |
| Desktop | `src/client-desktop` | `Nm*` resource keys en XAML | WPF .NET 9 |
| Web | `src/client-web` | `--nm-*` custom properties en CSS | Blazor Server + Tailwind v4 |

---

## Identidad Visual Compartida

### Paleta de Colores

| Nombre | Hex | Uso |
| :--- | :--- | :--- |
| Bg Base | `#1E1E2E` | Fondo principal de toda la aplicación |
| Surface | `#1E1E2E` | Paneles, sidebars, tarjetas |
| Shadow Light | `#2A2A3E` | Cara iluminada del relieve neumórfico |
| Shadow Dark | `#141420` | Cara sombreada del relieve neumórfico |
| Text Primary | `#E8E8F0` | Texto de lectura principal |
| Text Secondary | `#9090A8` | Etiquetas, metadatos, texto de ayuda |
| Accent | `#1A47C8` | Acción primaria, foco, selección activa |
| Accent Hover | `#2E5CE6` | Acento en hover (tono más brillante) |
| Error | `#FF7675` | Error, acciones destructivas |
| Success | `#55EFC4` | Confirmación, éxito |

**Variante clara:** mismos nombres, valores invertidos (`Bg Base #E0E5EC`, `Shadow Light #FFFFFF`, `Shadow Dark #A3B1C6`, textos oscuros). El acento `#1A47C8` es invariante.

### Tipografía

**Familia única: Inter** — Regular 400, SemiBold 600, Bold 700.

| Nivel | Tamaño | Peso | Color | Uso |
| :--- | :--- | :--- | :--- | :--- |
| Display | 30px | Light | Text Primary | Encabezados de pantalla completa |
| Title | 17px | SemiBold | Text Primary | Títulos de sección, cabeceras de panel |
| Body | 13.5px | Regular | Text Secondary | Cuerpo de texto, descripciones |
| Label | 11px | Regular | Text Secondary | Etiquetas de formulario, captions |

### Sistema de Sombras

El neumorfismo se basa en **sombras duales** que simulan relieve físico.

#### Estado elevado (reposo)

```
Sombra CLARA:  desplazada arriba-izquierda  Color: Shadow Light (#2A2A3E)
Sombra OSCURA: desplazada abajo-derecha     Color: Shadow Dark  (#141420)
```

#### Estado hundido (pressed, inputs)

Bordes inset o `inset box-shadow`:
```
TOP-LEFT:   Shadow Dark  (la luz no llega desde arriba)
BOT-RIGHT:  Shadow Light (reflejo desde abajo)
```

### Geometría

| Radio | Contexto |
| :--- | :--- |
| 22px | Tarjetas principales, ventana raíz |
| 16px | Botones primarios, barra de título |
| 14px | Inputs de texto y contraseña |
| 10px | ComboBox / Select, botones ghost |
| 8px | Botones de sidebar |
| 6px | Items de lista, dropdowns |
| 4px | Controles pequeños (checkbox) |

**Espaciado base:** 4px. Escala: `4, 6, 8, 10, 12, 15, 16, 24, 32, 36`.

---

## client-desktop (WPF .NET 9)

Fuente de verdad: `src/client-desktop/Themes/NeumorphismTheme.xaml`.  
Tema activo: `NeumorphismTheme` (alias `DarkTheme`). Variante clara: `LightTheme`.

### Tokens XAML

Cada color tiene un par `{Token}Color` (WPF Color) + `{Token}Brush` (SolidColorBrush).

| Token | Hex |
| :--- | :--- |
| `NmBgBaseColor` / `NmBgBaseBrush` | `#1E1E2E` |
| `NmSurfaceColor` / `NmSurfaceBrush` | `#1E1E2E` |
| `NmShadowLightColor` / `NmShadowLightBrush` | `#2A2A3E` |
| `NmShadowDarkColor` / `NmShadowDarkBrush` | `#141420` |
| `NmTextPrimaryColor` / `NmTextPrimaryBrush` | `#E8E8F0` |
| `NmTextSecondaryColor` / `NmTextSecondaryBrush` | `#9090A8` |
| `NmAccentColor` / `NmAccentBrush` | `#1A47C8` |
| `NmAccentHoverColor` / `NmAccentHoverBrush` | `#2E5CE6` |
| `NmErrorColor` / `NmErrorBrush` | `#FF7675` |
| `NmSuccessColor` / `NmSuccessBrush` | `#55EFC4` |

Aliases deprecated (no usar en código nuevo): `WindowBackground`, `SidebarBackground`, `PrimaryText`, `SecondaryText`, `AccentColor`.

### Estilos de texto (`x:Key`)

| Clave | Tamaño | Peso |
| :--- | :--- | :--- |
| `NmDisplayText` | 30px | Light |
| `NmTitleText` | 17px | SemiBold |
| `NmBodyText` | 13.5px | Regular |
| `NmLabelText` | 11px | Regular |

### Sombras — valores exactos de `DropShadowEffect`

**Raised (botones, tarjetas):**
```xml
<!-- Clara -->
<DropShadowEffect Color="{StaticResource NmShadowLightColor}"
                  ShadowDepth="7" Direction="135" BlurRadius="14" Opacity="1.0"/>
<!-- Oscura -->
<DropShadowEffect Color="{StaticResource NmShadowDarkColor}"
                  ShadowDepth="7" Direction="315" BlurRadius="14" Opacity="0.8"/>
```

**Tarjeta (`NmCardBorder`):**
```xml
<DropShadowEffect Color="{StaticResource NmShadowDarkColor}"
                  ShadowDepth="8" Direction="315" BlurRadius="18" Opacity="0.85"/>
```

**Ventana:**
```xml
<DropShadowEffect Color="#000000" ShadowDepth="0" BlurRadius="26" Opacity="0.32"/>
```

**Sunken (inputs en reposo):** bordes inset de 1.5px en `ControlTemplate` — `NmShadowDarkColor` arriba-izquierda, `NmShadowLightColor` abajo-derecha.

### Componentes

#### `NmButtonPrimary` — implicit Button style

Background `NmAccentBrush` · Foreground `#FFFFFF` · 12.5px SemiBold · Padding `16,10` · CornerRadius 16 · Raised en reposo · Sunken en pressed.

> `ContentPresenter.Resources` fuerza `Foreground="White"` en todos los TextBlock internos. Para texto de otro color usar `NmButtonGhost` o `Border + MouseLeftButtonDown`.

#### `NmButtonGhost` — acciones secundarias

Background Transparent · Foreground `NmTextSecondaryBrush` · 12px · Padding `12,8` · CornerRadius 10 · Sin sombra · Hover: `#10FFFFFF` bg + text → Primary.

#### `NmButtonDanger` — BasedOn NmButtonPrimary

Background `NmErrorBrush` (#FF7675). Para acciones destructivas.

#### `NmInputStyle` / `NmPasswordStyle` — implicit TextBox/PasswordBox

Background `NmBgBaseBrush` · Padding `12,8` · FontSize 13px · CornerRadius 14 · Sunken en reposo · Focus: borde accent 1px.

#### `NmCardBorder`

CornerRadius 22 · Background `NmSurfaceBrush` · BorderThickness 0 · sombra tipo tarjeta.

### Layouts canónicos

| Vista | Estructura |
| :--- | :--- |
| `LoginView` | 42% branding (accent bg) \| 58% formulario |
| `WorkspaceView` | 52–260px sidebar colapsable \| `*` TabControl |
| `ManuscriptEditorView` | 230px capítulos \| splitter \| `*` editor \| splitter \| 250px contexto |

**Animación de `GridLength`:** no existe `GridLengthAnimation` nativa. Usar `DispatcherTimer` + smoothstep en code-behind.

### Do's and Don'ts (Desktop)

- **SÍ** usar `NmButtonGhost` o `Border + MouseLeftButtonDown` en lugar de `Button` cuando no se quiere el estilo implícito primario.
- **SÍ** sobreescribir `CornerRadius` inline cuando el radio semántico del contexto difiere del estilo implícito de `Border` (22px).
- **NO** usar `Style="{x:Null}"` para escapar `NmButtonPrimary` — el estilo implícito sigue aplicando.
- **NO** insertar imágenes en `RichTextBox` vía `InlineUIContainer` — WPF no las serializa en RTF.
- **NO** crear estilos nuevos fuera de `Themes/*.xaml`.

---

## client-web (Blazor Server + Tailwind v4)

Fuente de verdad: `src/client-web/UI/Styles/Styles.css`.  
Salida compilada: `src/client-web/wwwroot/styles/styles.css` — **no editar directamente**.

### Tokens CSS (`--nm-*`)

Definidos en `:root` de `UI/Styles/Styles.css`:

```css
:root {
  /* Superficies */
  --nm-bg-base:        #1E1E2E;
  --nm-surface:        #1E1E2E;
  --nm-shadow-light:   #2A2A3E;
  --nm-shadow-dark:    #141420;

  /* Texto */
  --nm-text-primary:   #E8E8F0;
  --nm-text-secondary: #9090A8;

  /* Acento */
  --nm-accent:         #1A47C8;
  --nm-accent-hover:   #2E5CE6;

  /* Semánticos */
  --nm-error:          #FF7675;
  --nm-success:        #55EFC4;

  /* Tipografía */
  --nm-font: 'Inter', system-ui, sans-serif;
}
```

### Sombras CSS

```css
/* Raised — elemento en reposo */
box-shadow:
  7px 7px 14px var(--nm-shadow-dark),
  -7px -7px 14px var(--nm-shadow-light);

/* Sunken — inputs, pressed */
box-shadow:
  inset 4px 4px 8px var(--nm-shadow-dark),
  inset -4px -4px 8px var(--nm-shadow-light);

/* Tarjeta */
box-shadow:
  8px 8px 18px var(--nm-shadow-dark),
  -8px -8px 18px var(--nm-shadow-light);
```

### Clases de texto

| Clase | Tamaño | Peso | Color |
| :--- | :--- | :--- | :--- |
| `.nm-text-display` | 2rem | 300 | `--nm-text-primary` |
| `.nm-text-title` | 1.25rem | 600 | `--nm-text-primary` |
| `.nm-text-body` | 0.875rem | 400 | `--nm-text-secondary` |
| `.nm-text-label` | 0.75rem | 400 | `--nm-text-secondary` |

### Componentes CSS

#### `.nm-button` — acción primaria

```css
.nm-button {
  background: var(--nm-accent);
  color: #fff;
  font-family: var(--nm-font);
  font-size: 0.8rem;
  font-weight: 600;
  padding: 10px 16px;
  border: none;
  border-radius: 16px;
  box-shadow: 7px 7px 14px var(--nm-shadow-dark),
              -7px -7px 14px var(--nm-shadow-light);
  cursor: pointer;
  transition: background 120ms ease, box-shadow 120ms ease;
}
.nm-button:hover {
  background: var(--nm-accent-hover);
}
.nm-button:active {
  box-shadow: inset 4px 4px 8px var(--nm-shadow-dark),
              inset -4px -4px 8px var(--nm-shadow-light);
}
.nm-button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
```

#### `.nm-button-ghost` — acción secundaria

```css
.nm-button-ghost {
  background: transparent;
  color: var(--nm-text-secondary);
  font-size: 0.8rem;
  padding: 8px 12px;
  border: none;
  border-radius: 10px;
  box-shadow: none;
  cursor: pointer;
  transition: background 120ms ease, color 120ms ease;
}
.nm-button-ghost:hover {
  background: rgba(255, 255, 255, 0.06);
  color: var(--nm-text-primary);
}
```

#### `.nm-button-danger` — acción destructiva

Igual que `.nm-button` con `background: var(--nm-error)`.

#### `.nm-input` — campo de texto

```css
.nm-input {
  background: var(--nm-bg-base);
  color: var(--nm-text-primary);
  font-family: var(--nm-font);
  font-size: 0.875rem;
  padding: 10px 12px;
  border: none;
  border-radius: 14px;
  box-shadow: inset 4px 4px 8px var(--nm-shadow-dark),
              inset -4px -4px 8px var(--nm-shadow-light);
  outline: none;
  caret-color: var(--nm-accent);
}
.nm-input:focus {
  box-shadow: inset 4px 4px 8px var(--nm-shadow-dark),
              inset -4px -4px 8px var(--nm-shadow-light),
              0 0 0 1px var(--nm-accent);
}
.nm-input::placeholder {
  color: var(--nm-text-secondary);
  opacity: 0.6;
}
```

#### `.nm-card` — tarjeta / panel

```css
.nm-card {
  background: var(--nm-surface);
  border-radius: 22px;
  padding: 24px;
  box-shadow: 8px 8px 18px var(--nm-shadow-dark),
              -8px -8px 18px var(--nm-shadow-light);
}
```

### Do's and Don'ts (Web)

- **SÍ** editar `UI/Styles/Styles.css` y recompilar. Nunca tocar `wwwroot/styles/styles.css` directamente.
- **SÍ** usar clases `bg-[var(--nm-bg-base)]` con Tailwind v4 para integrar los tokens con las utilidades.
- **SÍ** mantener `border-radius` en los rangos semánticos definidos arriba.
- **NO** mezclar tokens `--nm-*` con valores de color hardcodeados. Toda referencia de color pasa por una variable.
- **NO** usar `box-shadow: none` en un elemento que visualmente debería tener relieve — el neumorfismo depende de las sombras para dar profundidad.
- **NO** aplicar sombras raised a inputs en reposo — los inputs son siempre sunken (hundidos).
