# Simondice

Juego Unity con múltiples mini-juegos de música y memoria.

## Requisitos

- **Unity**: Versión 6000.3.2f1 o superior
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Input System**: Nuevo Input System de Unity

## Mini-Juegos

### Simon (SimonScene)
Juego de memoria visual donde el jugador debe repetir secuencias de colores mostradas en un tablero circular.

**Características:**
- Sistema de vidas (3 corazones)
- Dificultad progresiva (más colores y menos tiempo)
- Estadísticas: nivel, racha actual, mejor racha, tiempo promedio
- Efectos visuales y sonoros
- Tablero adaptativo (4, 5 o 7 piezas según el nivel)

### Rhythm (RhythmScene)
Juego de ritmo con notas que caen en 3 carriles.

**Características:**
- 3 carriles con notas de diferentes colores
- Sistema de puntuación por precisión
- Ventana de golpe personalizable
- Feedback visual inmediato

### TempoTap (TempoTapScene)
Juego de ritmo que combina tapping con un runner 2D.

**Características:**
- Tapping sincronizado con beats de audio
- Sistema de estabilidad (mantén el ritmo para no perder)
- Runner automático con saltos sincronizados
- Asistencia para jugadores casuales
- Sesiones de 30 segundos

## Estructura del Proyecto

```
Assets/
├── Script/              # Scripts C# del juego
│   ├── SimonGameManager.cs
│   ├── RhythmGameManager.cs
│   ├── TempoTapGameManager.cs
│   └── ...
├── Scenes/              # Escenas del juego
│   ├── MainScene.unity
│   ├── SimonScene.unity
│   ├── RhythmScene.unity
│   └── TempoTapScene.unity
├── Audio/               # Archivos de audio
└── Settings/            # Configuración del proyecto
```

## Cómo Ejecutar

1. Abre el proyecto en Unity Hub (versión 6000.3.2f1)
2. Abre la escena `MainScene` en `Assets/Scenes/`
3. Presiona Play en el editor

## Controles

- **Simon**: Click en las piezas del tablero
- **Rhythm**: Click en los botones de los carriles
- **TempoTap**: Click/Tap en cualquier lugar de la pantalla

## Construir para Web

1. Ve a **File > Build Settings**
2. Selecciona **WebGL**
3. Click en **Switch Platform** si es necesario
4. Click en **Build**

## Notas

- El proyecto usa TextMeshPro para UI
- Compatible con dispositivos móviles (Android/iOS)
- Los haptic feedback funcionan en dispositivos móviles
