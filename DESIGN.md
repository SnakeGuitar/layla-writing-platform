---
design_system:
  name: "Cyberminimalism Core"
  version: "1.0.0"
  philosophy: "Hiper-enfoque, reducción cognitiva, estética técnica e industrial de alta precisión."
  tokens:
    colors:
      background:
        primary: "#09090B"      # Deep Obsidian (Fondo base absoluto)
        secondary: "#121216"    # Slate Charcoal (Contenedores secundarios e inputs)
        elevated: "#181820"     # High-Contrast Slate (Paneles, popups o elementos flotantes)
      borders:
        subtle: "#202024"       # Low-Contrast Border (Grilla estructural invisible)
        active: "#3A3A40"       # Active Border (Frontera interactiva enfocada)
      text:
        primary: "#F4F4F5"      # Pure Silver (Lectura primaria de alta fidelidad)
        secondary: "#A1A1AA"    # Muted Zinc (Metadatos, etiquetas y contexto secundario)
        disabled: "#52525B"     # Dead Slate (Elementos inactivos o deshabilitados)
      accent:
        primary: "#00E5FF"      # Luminescent Technical Cyan (Punteros, selección activa, estados críticos de interfaz)
        primary_glow: "rgba(0, 229, 255, 0.12)" # Cyan Glow (Resplandor de fósforo para feedback táctil/hover)
        warning: "#FF9F00"      # Phosphor Amber (Uso exclusivo de alertas de datos o destrucción)
    typography:
      families:
        interface: "'Inter', 'Outfit', system-ui, -apple-system, sans-serif"
        technical: "'JetBrains Mono', 'Fira Code', 'SF Mono', monospace"
      scales:
        base_size: "16px"
        xs: "0.75rem (12px) - Line height: 1.4 - Tracking: 0.05em"
        sm: "0.875rem (14px) - Line height: 1.5 - Tracking: 0.025em"
        md: "1rem (16px) - Line height: 1.5 - Tracking: 0em"
        lg: "1.25rem (20px) - Line height: 1.4 - Tracking: -0.01em"
        xl: "1.5rem (24px) - Line height: 1.3 - Tracking: -0.02em"
        xxl: "2rem (32px) - Line height: 1.2 - Tracking: -0.025em"
      weights:
        regular: 400
        medium: 500
        semibold: 600
        bold: 700
    spacing:
      unit: "4px"
      scale:
        4: "4px"
        8: "8px"
        12: "12px"
        16: "16px"
        24: "24px"
        32: "32px"
        48: "48px"
        64: "64px"
    shapes:
      border_radius: "1px"       # Estructura angular monolítica (prácticamente sin esquinas redondeadas)
      border_width: "1px"
---

# Cyberminimalism Core — Especificación de Sistema de Diseño

Este documento establece la especificación técnica formal y las directrices visuales para el desarrollo del frontend de nuestro ecosistema de software. Toda la interfaz debe ser implementada respetando de manera estricta y quirúrgica las reglas del **Ciber-minimalismo**, una estética nacida de la combinación de la precisión técnica militar, los antiguos terminales de fósforo y la pureza minimalista funcional alemana.

---

## Overview

El **Ciber-minimalismo** (Cyberminimalism) no es un estilo meramente decorativo; es una postura de ingeniería frente a la interfaz de usuario. En un mundo saturado de ruido de información, notificaciones invasivas y degradados distractores, nuestro software se erige como una herramienta de concentración pura. 

### Principios Fundamentales

1. **Diseño Invisible (Invisible UI):** La interfaz de usuario debe retirarse para dejar espacio a la tarea del usuario. Si un elemento no cumple una función utilitaria directa para resolver el problema inmediato del usuario, no se debe renderizar.
2. **Reducción de Fatiga Cognitiva (Cognitive Load Reduction):** La mente del usuario debe enfocar el 95% de su energía en su contenido, datos e interacción lógica. Las pistas visuales deben ser tan sutiles y refinadas que se sientan intuitivas sin demandar atención consciente.
3. **Estética Técnica Monolítica:** Nos inspiramos en los paneles de control de satélites, terminales Unix oscuras y sistemas de hardware industrial. La belleza reside en la precisión de la alineación, el balance del espacio negativo, la uniformidad angular de las cajas y la jerarquía implacable del texto.
4. **Intencionalidad del Color (Zero-Noise Color):** El color es un recurso escaso de alto coste cognitivo. Desterramos la decoración cromática en favor de la pura semántica y la luminiscencia técnica selectiva.

---

## Colors

El color en esta interfaz tiene un propósito puramente funcional y semántico. La paleta de colores emula un sistema de visualización militar u osciloscopio digital de alta fidelidad, donde la oscuridad absoluta provee contraste infinito y descanso visual.

### Paleta Semántica y Reglas de Uso

| Token | Valor CSS (HEX) | Propósito Semántico | Regla de Aplicación |
| :--- | :--- | :--- | :--- |
| `--bg-primary` | `#09090B` | Fondo Base Absoluto | Utilizado exclusivamente para el cuerpo principal y las áreas de edición de código o texto de alta densidad. |
| `--bg-secondary` | `#121216` | Contenedores e Inputs | Utilizado para distinguir paneles laterales de navegación, entradas de texto y áreas de soporte de datos secundarios. |
| `--bg-elevated` | `#181820` | Modales y Popups | Utilizado para interfaces de control efímeras que se posicionan temporalmente sobre la vista principal. |
| `--border-subtle` | `#202024` | Grilla Estructural | Reemplaza cualquier línea divisoria obvia. Debe ser casi invisible, actuando solo como una guía de anclaje visual. |
| `--border-active` | `#3A3A40` | Estado Interactivo Enfocado | Utilizado para enmarcar el elemento activo actual (inputs con foco, botones en hover o paneles seleccionados). |
| `--text-primary` | `#F4F4F5` | Texto Primario | Lectura principal, títulos y datos estructurados de alta importancia. Contraste mínimo exigido de 14.5:1. |
| `--text-secondary`| `#A1A1AA` | Texto Secundario / Metadatos | Etiquetas, marcas de tiempo, datos técnicos no urgentes y descripciones secundarias. |
| `--text-disabled` | `#52525B` | Estado Inactivo | Texto inutilizable, marcadores de posición (placeholders) de campos de entrada sin interactividad. |
| `--accent-primary` | `#00E5FF` | Fósforo Cyan Luminiscente | **El Acento Único**. Se usa exclusivamente para punteros activos, el cursor parpadeante de edición, estados de selección activos y notificaciones críticas de acción completada. Debe ocupar menos del 3% de la pantalla. |
| `--accent-glow` | `rgba(0,229,255,0.12)`| Aura de Fósforo | Filtros de brillo sutil en hover o enfoque en elementos clave de navegación. |
| `--accent-warning`| `#FF9F00` | Fósforo Ámbar Técnico | Exclusivamente para logs de error del compilador, estados destructivos (ej. eliminar) y advertencias del sistema. |

### El Ratio Cromático Implacable (95 - 4.5 - 0.5)

Para mantener la estética limpia y técnica:
*   **95% de la interfaz** debe ser dominada por tonos carbón, pizarra y negros profundos (`--bg-primary`, `--bg-secondary`).
*   **4.5% de la interfaz** debe estructurarse mediante texto plateado (`--text-primary`, `--text-secondary`) y bordes invisibles (`--border-subtle`).
*   **Solo un 0.5% de la superficie visual** puede contener el color de acento luminiscente (`--accent-primary` o `--accent-warning`). Si un desarrollador usa el acento en más de un par de pixeles aislados, ha roto el principio de sobriedad ciber-minimalista.

---

## Typography

La tipografía debe funcionar como un mapa estructurado de datos legibles. Se prioriza la legibilidad a primera vista y la uniformidad monótona que caracteriza a los terminales técnicos de alta resolución.

### Reglas de Configuración

1. **La Interfaz General (Sans-Serif):** Se utiliza `Inter` u `Outfit` con pesos sutiles (Regular y Medium). No se permiten grosores "black" o "extra bold" que agreguen masa visual innecesaria.
2. **Los Datos y Código (Monospace):** Para números, tablas de datos, bloques de terminal, metadatos, contadores y etiquetas de estado se utiliza estrictamente `JetBrains Mono` o `Fira Code`. Esto asegura que los números permanezcan perfectamente tabulados y alineados.

### Escala Tipográfica de Precisión

```css
/* Definiciones de clases de tipografía */

.text-cyber-display {
  font-family: var(--font-interface);
  font-size: 2rem;            /* 32px */
  line-height: 1.2;
  font-weight: 700;
  letter-spacing: -0.025em;
  color: var(--text-primary);
}

.text-cyber-title {
  font-family: var(--font-interface);
  font-size: 1.25rem;         /* 20px */
  line-height: 1.4;
  font-weight: 500;
  letter-spacing: -0.01em;
  color: var(--text-primary);
}

.text-cyber-body {
  font-family: var(--font-interface);
  font-size: 1rem;            /* 16px */
  line-height: 1.5;
  font-weight: 400;
  color: var(--text-secondary);
}

.text-cyber-caption {
  font-family: var(--font-interface);
  font-size: 0.875rem;        /* 14px */
  line-height: 1.5;
  font-weight: 400;
  letter-spacing: 0.025em;
  color: var(--text-secondary);
}

.text-cyber-tech-mono {
  font-family: var(--font-technical);
  font-size: 0.875rem;        /* 14px */
  line-height: 1.4;
  font-weight: 400;
  letter-spacing: 0.05em;
  color: var(--text-primary);
}

.text-cyber-metadata {
  font-family: var(--font-technical);
  font-size: 0.75rem;         /* 12px */
  line-height: 1.4;
  font-weight: 400;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--text-disabled);
}
```

---

## Layout & Grids

El diseño espacial del Ciber-minimalismo debe emular la estructura de los planos técnicos de arquitectura o microprocesadores. Los espacios en blanco (espacio negativo) no son "vacíos"; son canales estructurados que conducen el ojo del usuario de forma inmediata al foco de su actividad.

### Principios de Distribución Espacial

1. **La Estructura Monolítica de Ejes Rígidos:** Los componentes de la interfaz deben estar perfectamente alineados a una cuadrícula base de **4px / 8px**. No se permiten offsets arbitrarios ni márgenes impares.
2. **Ausencia de Sombras Naturales (Flat Panels):** La profundidad visual no se crea con sombras tridimensionales o efectos difuminados. La jerarquía se establece mediante bordes finos de un solo píxel y sutiles variaciones de brillo de fondo entre contenedores.
3. **El Flujo "HUD" (Heads-Up Display):** Al igual que en las pantallas de cazas militares, el usuario debe tener un acceso rápido a la información periférica (como estado del sistema, posición del compilador o progreso de carga) situado discretamente en las esquinas o bordes de la pantalla mediante pequeños bloques monoespaciados que no interfieren con la columna central de trabajo.

### La Grilla de Bordes Invisibles

Para lograr divisiones imperceptibles pero firmes, se establece un patrón de grilla basado en bordes de `1px solid var(--border-subtle)`. Ejemplo de maquetación:

```
+-----------------------------------------------------------+
| [NAV_PANEL] --border-subtle (Right)                       |
+------------------------------------+----------------------+
|                                    | [METADATA_COLUMN]    |
|  [MAIN_WORKSPACE]                  |                      |
|  Sin adornos visuales.             | --border-subtle      |
|  Espacio negativo amplio           | (Left)               |
|  para flujo de concentración.       |                      |
|                                    |                      |
+------------------------------------+----------------------+
| [STATUS_BAR] --border-subtle (Top)                        |
+-----------------------------------------------------------+
```

---

## Components

Todos los componentes del sistema deben seguir una geometría monolítica: **bordes afilados de esquinas angulares (border-radius de 1px o 0px) y un grosor de borde máximo de 1px.** Las micro-interacciones deben responder de forma instantánea al tacto, eliminando transiciones fluidas de carácter "amigable" o lento.

### 1. Botón Técnico (`.cyber-button`)
El botón es un interruptor funcional rígido. No tiene relieves ni gradientes.

```css
.cyber-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-technical);
  font-size: 0.875rem;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 8px 16px;
  background-color: transparent;
  color: var(--text-primary);
  border: 1px solid var(--border-active);
  border-radius: 1px;
  cursor: pointer;
  transition: all 0.1s steps(2); /* Respuesta inmediata de frame */
}

/* Hover: Activación luminiscente instantánea de fósforo */
.cyber-button:hover:not(:disabled) {
  background-color: var(--accent-glow);
  border-color: var(--accent-primary);
  color: var(--accent-primary);
  box-shadow: 0 0 8px var(--accent-glow);
}

/* Active: Contracción visual y solidez visual */
.cyber-button:active:not(:disabled) {
  background-color: var(--accent-primary);
  color: var(--bg-primary);
  box-shadow: none;
}

/* Focus: Borde cyan nítido para navegación por teclado */
.cyber-button:focus-visible {
  outline: none;
  border-color: var(--accent-primary);
  box-shadow: 0 0 0 2px var(--bg-primary), 0 0 0 3px var(--accent-primary);
}

/* Disabled: Tono de desactivación inerte */
.cyber-button:disabled {
  border-color: var(--border-subtle);
  color: var(--text-disabled);
  cursor: not-allowed;
}
```

### 2. Input de Datos Técnico (`.cyber-input`)
Los inputs de texto e inputs de datos deben sentirse como una consola de llenado técnico y digital.

```css
.cyber-input-wrapper {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.cyber-label {
  font-family: var(--font-technical);
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--text-secondary);
}

.cyber-input {
  font-family: var(--font-technical);
  font-size: 0.875rem;
  padding: 10px 12px;
  background-color: var(--bg-secondary);
  border: 1px solid var(--border-subtle);
  border-radius: 1px;
  color: var(--text-primary);
  outline: none;
  transition: border-color 0.12s ease-in-out;
}

/* Hover: Incremento del contraste del contorno */
.cyber-input:hover:not(:disabled) {
  border-color: var(--border-active);
}

/* Focus: Conexión con el acento cyan de sistema */
.cyber-input:focus {
  border-color: var(--accent-primary);
  box-shadow: 0 0 6px var(--accent-glow);
}

/* Placeholder: Tono inerte con espaciado amplio */
.cyber-input::placeholder {
  color: var(--text-disabled);
  letter-spacing: 0.02em;
}

/* Validación incorrecta: Fósforo ámbar en lugar de alertas rojas estridentes */
.cyber-input.is-invalid {
  border-color: var(--accent-warning);
  box-shadow: 0 0 6px rgba(255, 159, 0, 0.12);
}
```

### 3. Contenedor de Datos Monolítico (`.cyber-panel`)
Los paneles dividen el espacio de forma clara y sin ruidos. Reemplazan las sombras con bordes afilados e información técnica en las esquinas.

```css
.cyber-panel {
  position: relative;
  background-color: var(--bg-secondary);
  border: 1px solid var(--border-subtle);
  border-radius: 1px;
  padding: 16px;
}

/* Cabecera del Panel con metadatos técnicos */
.cyber-panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid var(--border-subtle);
  padding-bottom: 8px;
  margin-bottom: 16px;
}

/* Detalle estético opcional: Puntos de retícula técnica (Corner Marks) */
.cyber-panel::before {
  content: "";
  position: absolute;
  top: -1px;
  left: -1px;
  width: 4px;
  height: 4px;
  background-color: var(--accent-primary);
}
```

---

## Do's and Don'ts

Para garantizar la pureza de la estética del diseño Cyberminimalist y evitar desviaciones estilísticas por parte de desarrolladores o agentes de IA, se establecen los siguientes límites inquebrantables de desarrollo:

### Do's (Qué hacer estrictamente)
*   **SÍ** mantén una alineación perfecta de píxeles al píxel. Utiliza `flex` y `grid` con espacios uniformes para mantener la estructura monolítica.
*   **SÍ** limita el uso del color de acento (`#00E5FF`) a elementos que actúen estrictamente como disparadores de interacción final (ej: el botón de enviar activo, la celda en edición, o el cursor).
*   **SÍ** utiliza fuentes monoespaciadas para cualquier representación de números reales, tamaños de archivos, métricas u horas.
*   **SÍ** implementa animaciones discretas y ultra-rápidas (menores a 120ms) que utilicen transiciones de salto técnico (ej: `steps(2)` en CSS o curvas de aceleración `cubic-bezier(0.2, 0.8, 0.2, 1)` muy directas).
*   **SÍ** recurre a espacios en blanco amplios para estructurar la navegación, forzando al usuario a centrar su mirada en un único componente crítico.

### Don'ts (Qué evitar estrictamente)
*   **NO** uses gradientes visuales llamativos, degradados multicolor o patrones de fondo con efectos parallax. La interfaz debe ser plana e industrial.
*   **NO** utilices esquinas redondeadas exageradas (ej: `border-radius: 8px` o `9999px` para botones circulares "amigables"). Todo debe poseer la rigidez de una máquina física.
*   **NO** integres sombras decorativas difuminadas (`box-shadow` suaves y oscuras para simular profundidad natural). Si deseas separar elementos, usa bordes sutiles de `1px` o un fondo con un tono de contraste superior.
*   **NO** agregues iconos con propósitos meramente ornamentales o decorativos que no transmitan información. El texto plano claro y estructurado es siempre superior.
*   **NO** utilices transiciones lentas o animaciones elásticas de "rebote" (spring animations). Estas degradan la percepción de velocidad técnica e instrumental del software.
*   **NO** introduzcas notificaciones "toast" o alertas flotantes con colores primarios estridentes (rojos intensos, verdes vibrantes estándar, azules de redes sociales). Utiliza siempre las variaciones técnicas sutiles del sistema (`--accent-primary` o `--accent-warning`).
