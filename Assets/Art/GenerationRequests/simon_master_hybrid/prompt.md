# Mockup maestro: direccion hibrida B+C para SimonScene

Genera un mockup de pantalla completa para el videojuego educativo infantil de ritmo y memoria musical "A tu Ritmo". Usa como referencia funcional la captura actual adjunta de `Assets/Scenes/SimonScene.unity`. Usa tambien como referencias visuales los mockups B y C adjuntos, pero combina sus mejores decisiones sin copiar literalmente una sola imagen.

## Direccion aprobada

La composicion, claridad y jerarquia deben partir de la Propuesta B. El tablero Simon, los anillos de pulso, la guia visual del compas y el lenguaje de formas redondeadas deben partir de la Propuesta C. Simplifica la propuesta eliminando decoracion no funcional.

## Objetivo pedagogico

La pantalla debe ayudar a comprender rapidamente tres momentos: escuchar/observar, esperar el turno y repetir el patron. Debe ser calmada, accesible y tactil, con un solo foco principal en cada momento. La informacion debe poder entenderse aunque el jugador no lea todo el texto.

## Composicion obligatoria

- Mantener un HUD superior unico y compacto con puntos, nivel o progreso, barra de tiempo con un reloj analogico comun de esfera y manecillas, barra de vida con sus tres corazones funcionales actuales y menu.
- Colocar un tablero Simon central grande como foco principal.
- Usar el diseño organico y redondeado del tablero de la Propuesta C, con cuatro zonas grandes y claramente separadas.
- Mantener dentro de cada zona las letras funcionales que ya utiliza el proyecto, como D, G, E y C, con texto limpio y editable. No sustituirlas por notas musicales, iconos o simbolos decorativos.
- Usar un reloj analogico sencillo y reconocible, con esfera circular y manecillas, como indicador de tiempo. No usar reloj de arena, cronometro complejo ni simbolo abstracto.
- Añadir anillos de pulso alrededor o debajo del tablero para comunicar ritmo y activacion; deben ser sutiles, no flashes intensos.
- Añadir una guia visual de compas encima del tablero, basada en una secuencia corta de pulsos o posiciones, tomada de la Propuesta B/C.
- Mantener botones de jugar y repetir en una franja inferior segura, con boton principal claramente dominante.
- Eliminar por completo el panel o cuadro decorativo situado entre el tablero y los botones. No colocar ningun panel vacio en esa zona.
- Mantener mucho espacio negativo entre HUD, guia, tablero y botones.

## Estilo visual

Ilustracion 2D infantil contemporanea, formas redondeadas, volumen ligero, bordes consistentes y sombras cortas. Fondo muy simple, calmado y de bajo contraste: degradado suave o superficie limpia con una textura minima. No incluir notas musicales, pentagramas, altavoces, flores, personajes, estrellas ni decoracion ambiental que compita con el tablero. No usar una estetica realista, neon, cyberpunk ni excesivamente detallada.

## Paleta

Usar un fondo azul noche o azul petroleo muy uniforme y poco llamativo, superficies crema, turquesa, amarillo sol, coral, verde menta y violeta solo en el tablero y controles. Mantener colores estables para las zonas Simon. Los estados deben comunicarse mediante color, luminosidad, borde y escala, sin corazones decorativos, notas musicales ni exceso de iconos. Los unicos corazones permitidos son los tres de la barra de vida funcional.

## Requisitos para Unity

- Composicion de referencia 16:10, preparada para 1280x800.
- No incluir texto, numeros, letras, logotipos ni marcas de agua dentro de botones, paneles o assets, excepto las letras funcionales D, G, E y C dentro de las zonas Simon.
- Dejar areas limpias para textos editables de TextMesh Pro. Las letras del tablero deben mantenerse como texto editable, no como texto incrustado en una textura.
- El mockup debe sugerir piezas separables: fondo simple, panel HUD, reloj analogico, barra de vida, tablero, zonas con letras, anillos, guia de compas y botones.
- No crear un panel de feedback vacio entre el tablero y los botones; cualquier feedback futuro debe aparecer temporalmente como texto o icono superpuesto y luego desaparecer.
- No crear una pantalla destinada a importarse como una sola imagen en el juego.
- No colocar elementos importantes pegados a los bordes.

Entrega una unica imagen de referencia completa, limpia y accesible que combine la claridad de B con la identidad musical de C.
