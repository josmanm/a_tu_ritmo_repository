# Especificación de Requisitos — *A Tu Ritmo*

**Fase:** Análisis (proceso metodológico de investigación)
**Producto:** *A Tu Ritmo*, aplicación de minijuegos de ritmo y memoria
**Población objetivo:** niños y niñas con síndrome de Down
**Plataformas:** Android (tablet/teléfono, horizontal) y Windows
**Tecnología:** Unity 6 (C#), Firebase Firestore

---

## 1. Introducción

### 1.1 Propósito
Este documento recoge los requisitos funcionales (RF) y no funcionales (RNF) del sistema *A Tu Ritmo*. Sirve de insumo para la fase de análisis del proceso metodológico y de base para el diseño, la validación con expertos y la evaluación posterior.

### 1.2 Alcance
El sistema ofrece cuatro minijuegos que estimulan la **percepción del pulso, la sincronización rítmica y la memoria secuencial**. Además gestiona perfiles de jugador y registra métricas de desempeño en la nube, para que terapeutas, docentes o investigadores puedan analizarlas.

### 1.3 Método de obtención
Los requisitos se obtuvieron por **análisis del prototipo funcional** (ingeniería inversa del código fuente del repositorio) y de las reglas de diseño del proyecto (`AI_RULES.md`). Se recomienda **validarlos con los interesados** (terapeutas, docentes de educación especial, familias) mediante entrevistas o juicio de expertos antes de cerrar la fase de análisis.

### 1.4 Definiciones
| Término | Significado |
|---|---|
| **BPM** | Pulsos por minuto (tempo). |
| **Offset** | Diferencia en milisegundos entre el toque del niño y el pulso esperado (negativo = adelantado, positivo = atrasado). |
| **Intento** | Una partida o nivel jugado dentro de una sesión. |
| **Sesión** | Periodo de uso continuo de un perfil, desde que se selecciona hasta que se cierra o cambia. |
| **Span de memoria** | Longitud de la secuencia de colores que el niño recuerda sin ayuda. |

---

## 2. Actores / Interesados

| Actor | Descripción | Interacción |
|---|---|---|
| **Niño/a jugador** | Usuario principal, con síndrome de Down. | Juega con toques en pantalla. |
| **Acompañante** (padre, madre, docente, terapeuta) | Ayuda a crear o seleccionar el perfil y ajusta la configuración. | Menú de perfiles y configuración. |
| **Investigador / terapeuta** | Analiza el progreso. | Consulta los datos en Firebase (fuera de la app). |
| **Firebase Firestore** | Servicio externo de almacenamiento. | Recibe perfiles, sesiones e intentos. |

---

## 3. Requisitos funcionales

Prioridad según MoSCoW: **A** = Alta (imprescindible), **M** = Media, **B** = Baja.

### 3.1 Gestión de perfiles y sesiones

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-01** | Al abrir la aplicación, el sistema debe mostrar el panel de selección de perfil antes del menú principal. | A |
| **RF-02** | El sistema debe permitir **crear un perfil** con un nombre y un avatar (conejo azul, zorro naranja, oso verde). Los espacios sobrantes del nombre se eliminan. | A |
| **RF-03** | El sistema debe **listar los perfiles existentes** en tarjetas con avatar y nombre, y permitir seleccionar uno. | A |
| **RF-04** | El sistema debe permitir **actualizar (recargar)** la lista de perfiles desde el servidor. | M |
| **RF-05** | El sistema debe recordar el **perfil activo** entre ejecuciones y permitir cambiarlo desde el menú principal. | M |
| **RF-06** | Al seleccionar un perfil, el sistema debe **iniciar una sesión**: identificador único, fecha y hora de inicio. Al cerrar la sesión debe registrar la fecha de fin y el tiempo total en segundos. | A |
| **RF-07** | Si no hay conexión, el sistema debe mostrar los perfiles guardados en caché local y permitir crear perfiles nuevos que se sincronizan más tarde. | A |

### 3.2 Menú principal y navegación

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-08** | El menú principal debe ofrecer las opciones **Jugar**, **Configuración**, **Perfil** y **Salir**. | A |
| **RF-09** | La opción *Jugar* debe mostrar el selector de los cuatro minijuegos: Simón Dice, Tempo Tap, Ritmo Básico y Banda Musical. | A |
| **RF-10** | El sistema debe permitir **volver al menú** desde cualquier minijuego y hacer transiciones animadas entre escenas. | A |
| **RF-11** | Cada minijuego debe tener un **menú de pausa** con opciones para continuar, reiniciar y salir. Al pausar se detienen el audio y la lógica de tiempo; al reanudar, el ritmo sigue sin desfase. | A |
| **RF-12** | Cada minijuego debe mostrar un **HUD superior** con la información relevante (puntaje, vidas, racha o progreso). | M |

### 3.3 Configuración y accesibilidad

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-13** | El sistema debe permitir ajustar el **volumen de la música** y el **volumen de los efectos** por separado (0–100 %). | A |
| **RF-14** | El sistema debe permitir ajustar la **velocidad de juego** entre 0,75× y 1,5×. | A |
| **RF-15** | El sistema debe permitir **activar o desactivar la vibración** (retroalimentación háptica) para aciertos y errores. | M |
| **RF-16** | El sistema debe permitir **restablecer la configuración** a los valores por defecto. | B |
| **RF-17** | La configuración debe **guardarse en el dispositivo** y aplicarse de inmediato en todas las escenas. | A |

### 3.4 Minijuego *Simón Dice* (memoria y ritmo)

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-18** | El sistema debe ofrecer dos modos: **Memoria (clásico)**, en el que el niño reproduce una secuencia de colores, y **Ritmo (compás)**, en el que reproduce la secuencia al pulso de un metrónomo e incluye silencios. | A |
| **RF-19** | Cada modo debe tener una fase de **práctica guiada** (con ayudas y repetición) antes de la fase de **evaluación** sin ayuda. | A |
| **RF-20** | El sistema debe reproducir la secuencia con **iluminación de piezas y sonido** (notas musicales DO, RE, MI, FA…) y luego dar el turno al jugador con una barra de tiempo. | A |
| **RF-21** | El sistema debe **adaptar la dificultad**: tras 3 aciertos seguidos aumenta el span (de 2 a 6) o el número de colores (de 4 a 5). En modo ritmo sube el BPM (de 40 a 90, de 5 en 5) y añade silencios o compases largos. Tras 2 fallos seguidos reduce la dificultad. | A |
| **RF-22** | El sistema debe manejar **vidas** (3), **puntaje** por ronda y **bonificación por racha**. | M |
| **RF-23** | El sistema debe dar **retroalimentación visual y sonora** inmediata (correcto o incorrecto) y una celebración al completar. | A |

### 3.5 Minijuego *Tempo Tap* (pulso con corredor)

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-24** | El sistema debe mostrar un personaje corredor que **salta obstáculos sincronizados con el pulso** cuando el niño toca la pantalla a tiempo. | A |
| **RF-25** | El sistema debe incluir **pulsos de calentamiento** (3–6) y de observación antes de evaluar. | M |
| **RF-26** | El sistema debe clasificar cada toque como **Perfecto** (≤ 60 ms), **Bien** (≤ 120 ms), **Adelantado**, **Atrasado** o **Fallado**, con una ventana de asistencia de 180 ms para que el personaje igual salte. | A |
| **RF-27** | El sistema debe **adaptar el tempo** (30–90 BPM) cada N pulsos según la estabilidad del niño. | A |
| **RF-28** | El sistema debe registrar las **colisiones** con obstáculos y el puntaje acumulado. | M |

### 3.6 Minijuego *Ritmo Básico* (figuras musicales)

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-29** | El sistema debe presentar patrones con las figuras **redonda, blanca, negra, corchea y silencio**, con su ilustración. | A |
| **RF-30** | El sistema debe ofrecer un **tutorial** con audio y guía visual (mano indicadora) para cada figura antes del juego. | A |
| **RF-31** | El niño debe **tocar el tambor** siguiendo el patrón, y el sistema debe evaluar cada nota con tolerancias de *perfecto* (0,12 pulsos) y *bien* (0,25 pulsos). | A |
| **RF-32** | El sistema debe evitar repetir la misma figura más de 2 veces seguidas y dar más peso a las figuras menos practicadas. | M |
| **RF-33** | El sistema debe manejar **vidas** (3) y mostrar mensajes de estado y celebración por ronda. | M |

### 3.7 Minijuego *Banda Musical* (sincronización con canción)

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-34** | El sistema debe presentar una banda de personajes (mono, gato, oso) que tocan con el niño. Cada nivel dura 60 segundos de música. | A |
| **RF-35** | El sistema debe seguir la secuencia pedagógica **Descubrir → Escuchar ("¡Mira cómo lo hago!") → Tocar juntos**, con narración por voz. | A |
| **RF-36** | El sistema debe ofrecer **tres niveles** (80 BPM; 4 y 8 pulsos; con y sin ayuda visual) y **retos de 5 segundos** a los 15, 30 y 45 s. | A |
| **RF-37** | El sistema debe permitir **pedir ayuda / escuchar otra vez** y contar cuántas veces se usó. | M |
| **RF-38** | El sistema debe clasificar los toques en **a tiempo, adelantado, atrasado, extra o fallado** (tolerancia configurable, 250 ms por defecto). | A |

### 3.8 Registro de métricas y reportes

| ID | Requisito | Prioridad |
|---|---|---|
| **RF-39** | Por cada intento, el sistema debe registrar: jugador, sesión, minijuego, nivel, dificultad, BPM, aciertos, errores, repeticiones, completado (sí/no), tiempo, puntaje final, si fue tutorial, motivo de salida y fecha. | A |
| **RF-40** | El sistema debe registrar **métricas específicas por minijuego**: offsets por toque (ms), media del offset absoluto, distribución de toques adelantados y atrasados, desviación estándar de intervalos (Tempo Tap), span de memoria máximo sin ayuda (Simón) y precisión por figura (Ritmo Básico). | A |
| **RF-41** | El sistema debe enviar perfiles, sesiones e intentos a Firebase Firestore (colecciones `players`, `sessions` y `attempts`). | A |
| **RF-42** | Si no hay conexión, el sistema debe guardar los reportes en una **cola local persistente** y reintentar el envío automáticamente cada 15 s hasta lograrlo. | A |
| **RF-43** | El sistema debe registrar los intentos interrumpidos (salida o reinicio) con su motivo de salida. | M |

---

## 4. Requisitos no funcionales

Clasificados según el modelo de calidad **ISO/IEC 25010**.

### 4.1 Usabilidad y accesibilidad (población con síndrome de Down)

| ID | Requisito |
|---|---|
| **RNF-01** | La interfaz debe usar **botones grandes** y fáciles de tocar (área táctil mínima recomendada de 48 × 48 dp). |
| **RNF-02** | La interfaz debe tener **alto contraste** entre elementos y fondo, con colores vivos y diferenciables. |
| **RNF-03** | La retroalimentación debe ser **simple, inmediata y multimodal**: visual, sonora y háptica. Debe evitar la sobrecarga de información. |
| **RNF-04** | Las instrucciones deben apoyarse en **voz, íconos e ilustraciones**, sin depender de la lectura. |
| **RNF-05** | El error no debe penalizar de forma frustrante: debe haber ayudas, ventanas de tolerancia amplias y una dificultad que baja tras fallos consecutivos. |
| **RNF-06** | Los tempos iniciales deben ser lentos (30–40 BPM) y aumentar de forma gradual. |
| **RNF-07** | La aplicación debe funcionar en **orientación horizontal** y adaptarse a teléfonos y tabletas de distinta relación de aspecto. |

### 4.2 Eficiencia de desempeño

| ID | Requisito |
|---|---|
| **RNF-08** | La evaluación del ritmo debe usar el **reloj de audio (DSP)** para alcanzar una precisión del orden de milisegundos entre audio, animación y evaluación del toque. |
| **RNF-09** | La latencia percibida entre el toque y la retroalimentación debe ser imperceptible (< 100 ms). |
| **RNF-10** | La aplicación debe mantener una tasa de cuadros fluida (objetivo: 30–60 FPS) en dispositivos de gama baja. |
| **RNF-11** | El procesamiento de audio debe limitar el uso de memoria. Por ejemplo, solo se procesa el fragmento de canción que se escucha. |

### 4.3 Compatibilidad / portabilidad

| ID | Requisito |
|---|---|
| **RNF-12** | La aplicación debe ejecutarse en **Android 7.1 o superior** (API 25), incluidos dispositivos **ARMv7 de 32 bits**, y en **Windows**. |
| **RNF-13** | La aplicación debe desarrollarse en **Unity 6** con URP 2D, TextMeshPro e Input System. |
| **RNF-14** | La vibración debe funcionar en Android, iOS y mando cuando el hardware lo permita, y fallar sin errores visibles si no está disponible. |

### 4.4 Fiabilidad

| ID | Requisito |
|---|---|
| **RNF-15** | La aplicación debe poder **jugarse sin conexión**. La falta de Internet o un fallo de Firebase no debe impedir jugar. |
| **RNF-16** | Ningún reporte debe perderse: la cola local debe persistir entre cierres de la aplicación. |
| **RNF-17** | Pausar y reanudar no debe desincronizar la música, la evaluación ni las animaciones. |
| **RNF-18** | Si falta un recurso de audio, el nivel debe seguir siendo jugable. |

### 4.5 Seguridad y privacidad

| ID | Requisito |
|---|---|
| **RNF-19** | Solo deben recogerse los datos personales mínimos: nombre (o alias) y avatar. No se almacenan fotos, ubicación ni datos de contacto del menor. |
| **RNF-20** | Los datos deben transmitirse cifrados (HTTPS/TLS de Firebase) y el acceso a Firestore debe restringirse con reglas de seguridad. |
| **RNF-21** | El uso de los datos con fines de investigación requiere el **consentimiento informado** de los padres o tutores legales, conforme a la normativa de protección de datos aplicable (p. ej., Ley 1581 de 2012 en Colombia). |

### 4.6 Mantenibilidad

| ID | Requisito |
|---|---|
| **RNF-22** | El código debe escribirse en **C#**, con los métodos importantes comentados y sin gestores (*managers*) duplicados (patrón *singleton* para perfiles, sesiones y reportes). |
| **RNF-23** | Cada minijuego debe ser una escena independiente, con sus parámetros (BPM, tolerancias, vidas, niveles) configurables desde el Inspector sin cambiar el código. |
| **RNF-24** | El modelo de datos de reportes debe ser **extensible**: cada minijuego tiene su propio bloque de métricas. |
| **RNF-25** | El código fuente debe gestionarse con control de versiones (Git/GitHub). |

---

## 5. Restricciones

- Motor Unity 6 y lenguaje C#.
- Backend en Firebase (Firestore). La consulta de los datos por el investigador se hace fuera de la aplicación.
- Identificador de la aplicación Android: `com.unicauca.aturitmo`.
- La interacción se limita al toque en pantalla (o clic en Windows).

---

## 6. Matriz de trazabilidad (requisito → componente)

| Requisitos | Componente / script |
|---|---|
| RF-01 a RF-07 | `PlayerProfileManager`, `ProfileMenuController`, `SessionManager` |
| RF-08 a RF-12 | `MenuManager`, `SceneTransitionController`, `PausePanelView`, `TopHudPanelView` |
| RF-13 a RF-17 | `GameSettings`, `AccessibleHaptics`, `BackgroundMusicPlayer` |
| RF-18 a RF-23 | `SimonGameManager`, `SimonLearningProgress`, `SimonMetronomeController` |
| RF-24 a RF-28 | `TempoTapGameManager`, `RunnerController2D`, `BeatSyncedObstacleSpawner` |
| RF-29 a RF-33 | `BasicRhythmGameManager` |
| RF-34 a RF-38 | `BandaMusicalGameManager`, `RhythmManager`, `MusicianController`, `BandaSessionRecorder` |
| RF-39 a RF-43 | `GameReportManager`, `ReportOutbox`, `PlayerReportingModels` |
