# ABISMO: El Sueño de R'lyeh

Juego de acción en 2D al estilo **Blasphemous** (combate exigente, paradas, altares, jefes y
una atmósfera opresiva) ambientado en el horror cósmico de **H. P. Lovecraft**.
Está hecho con **Unity 6** y **C#**.

![Personajes](docs/img/personajes.png)

*De izquierda a derecha: el Ahogado (jugador), un Profundo, un Sectario, un Ojo del Vacío y el jefe,
el Arcipreste de las Mareas. Todo el arte es **provisional** y se genera por código: puedes cambiarlo por tus dibujos cuando quieras.*

![Costa de Innsmouth](docs/img/costa_innsmouth.png)

*Vista previa aproximada del comienzo del nivel (en Unity hay además paralaje, luna, niebla, brillos y HUD).*

---

## Qué tiene ya la demo

| Blasphemous | Abismo | Estado |
|---|---|---|
| El Penitente | **El Ahogado**, un pescador de Innsmouth con una escafandra | ✅ |
| Combo, esquiva, parada | Combo de 3 golpes, ataque aéreo, deslizamiento invulnerable y **parada** que aturde al enemigo | ✅ |
| Frascos Biliares | **Láudano**: frascos que curan y se rellenan en los altares | ✅ |
| Fervor y oraciones | **Revelación**: se gana golpeando y se gasta en el conjuro **Signo Arcano** | ✅ |
| Reclinatorios | **Altares del Signo Antiguo**: curan y guardan el punto de reaparición, pero las criaturas reviven | ✅ |
| Culpa | **Fragmento de mente**: al morir queda donde caíste y, hasta que lo recuperes, tu Revelación se reduce | ✅ |
| Lágrimas de Expiación | **Oro de Innsmouth** | ✅ |
| Enemigos | Profundo (cuerpo a cuerpo), Sectario (a distancia), Ojo del Vacío (volador) | ✅ |
| Jefes | **El Arcipreste de las Mareas**: tentáculos, abanico de esferas, embestida imparable y 2 fases | ✅ |
| Ataques rojos imparables | Brillo **naranja** = se puede parar · Brillo **rojo** = hay que esquivar | ✅ |
| Pinchos | Coral espinoso y agua abisal | ✅ |
| Lore en el escenario | Inscripciones con tutoriales y textos lovecraftianos | ✅ |
| Sensación de impacto | Congelación al golpear (hit-stop), temblor de cámara, destellos, partículas | ✅ |
| Sonido | Efectos y ambiente sintetizados por código (no hacen falta archivos de audio) | ✅ |
| Interfaz | Vida, Revelación, frascos, oro, nombres de zona, barra del jefe, pausa, título y muerte | ✅ |

Un nivel completo con 5 zonas: **Costa de Innsmouth → Ruinas Ciclópeas → Santuario de las Mareas
(jefe) → Arrecife del Diablo**.

---

## Cómo abrirlo y jugar (paso a paso)

1. **Instala Unity Hub** desde <https://unity.com/download> e inicia sesión con tu cuenta.
2. En Unity Hub ve a **Installs → Install Editor** e instala **Unity 6 (LTS)**.
   Para jugar en tu ordenador no necesitas módulos extra.
3. **Descarga este proyecto**: en GitHub pulsa el botón verde **Code → Download ZIP** y descomprímelo
   (o usa `git clone` si ya sabes usar git).
4. En Unity Hub: **Projects → Add → Add project from disk** y elige la carpeta del proyecto
   (la que contiene `Assets`, `Packages` y `ProjectSettings`).
5. Ábrelo. Si Unity Hub avisa de que la versión del editor es distinta, elige tu Unity 6 y acepta.
   La primera vez tarda unos minutos.
6. Si aparece un aviso sobre el **nuevo Input System** ("enable the backends?"), pulsa **Yes**:
   Unity se reiniciará. (Si pulsas *No* también funciona, con el sistema de entrada clásico).
7. En la barra de menús de arriba: **Abismo → Construir demo jugable**.
   Esto crea los sprites, los prefabs y la escena `Assets/_Abismo/Scenes/Nivel_01`, y la abre.
8. Pulsa el botón **▶ (Play)** de arriba del todo. ¡A jugar!

### Controles

| Acción | Teclado | Mando |
|---|---|---|
| Moverse | A / D o flechas | Stick izquierdo / cruceta |
| Saltar (mantén para saltar más) | Espacio o K | A |
| Atacar (hasta 3 golpes) | J | X |
| Esquivar deslizándote | L o Shift | B |
| Parar (parry) | I | RB |
| Signo Arcano (gasta Revelación) | U | Y |
| Beber láudano | F | LB |
| Interactuar (rezar, leer) | E o W / ↑ | Cruceta ↑ |
| Bajar de una plataforma | Abajo + Saltar | Abajo + A |
| Pausa (muestra los controles) | Esc | Start |

---

## Cómo está organizado

```
Assets/_Abismo/
├── Scripts/
│   ├── Core/       GameManager (altares, muerte, pausa), cámara, entrada, combate, efectos, sonido
│   ├── Player/     PlayerController (movimiento y combate), PlayerStats (vida, Revelación, láudano, oro)
│   ├── Enemies/    Enemy (base común), Profundo, Sectario, Ojo del Vacío, Arcipreste, proyectiles, tentáculos
│   ├── World/      Altares, inscripciones, peligros, oro, fragmento de mente, arena del jefe, fondos
│   └── UI/         HUD (toda la interfaz se crea por código)
├── Shaders/        SpriteFlash (destellos al recibir golpes) y SpriteAdditive (brillos)
├── Editor/         El menú "Abismo": genera arte, prefabs y la escena
└── Levels/         nivel_01.txt  ← el mapa del nivel, ¡en texto!
```

Al pulsar **Construir** aparecen además `Art/` (PNG y tiles), `Materials/`, `Prefabs/` y `Scenes/`.

---

## Cómo modificar el juego

**Ajustar el tacto del personaje.** Abre `Assets/_Abismo/Prefabs/Jugador` y en el Inspector cambia
los valores de `PlayerController` (velocidad, altura de salto, daño del combo, ventana de parada...).
Truco: puedes cambiarlos durante el Play para probar, pero esos cambios se pierden al parar; apúntalos
y ponlos en el prefab. Igual con los enemigos (`Profundo`, `Sectario`, `OjoDelVacio`, `Arcipreste`).

**Diseñar niveles.** Abre `Assets/_Abismo/Levels/nivel_01.txt` con cualquier editor de texto. Cada
carácter es una casilla (`#` roca, `=` plataforma, `^` coral espinoso, `D` Profundo, `A` altar...;
la leyenda completa está al principio del fichero). Guarda y usa **Abismo → Construir demo jugable**.
Construir rehace la **escena** desde el texto, pero **respeta tus prefabs y tus PNG**.

**Cambiar el arte.** Sustituye los PNG de `Assets/_Abismo/Art/Generated` por tus dibujos con el mismo
nombre. Escala: **16 píxeles = 1 casilla**. Los personajes tienen el punto de apoyo en los pies.
Construir no sobrescribe los PNG que ya existen.

**Volver a empezar.** `Abismo → Restablecer prefabs y construir` recrea los prefabs con los valores
originales. `Abismo → Restablecer sprites provisionales y construir` vuelve al arte provisional.

**Crear un enemigo nuevo.** Crea una clase que herede de `Enemy` y escribe su comportamiento en
`Tick(float dt)`. Mira `DeepOneEnemy.cs` como ejemplo: patrulla, persigue, anuncia el golpe y ataca.

---

## Próximos pasos sugeridos

1. Animaciones de verdad (sprites por fotogramas + Animator) en lugar de la animación por código.
2. Guardar la partida (altar activo, oro, jefes vencidos).
3. Más movimientos: agarrarse a bordes, trepar, golpe hacia abajo.
4. Equipo al estilo de los rosarios y reliquias: **amuletos** que se compran con oro de Innsmouth.
5. Música, más zonas, un mapa y más jefes.

Hay más ideas de historia, enemigos y mecánicas en el [documento de diseño](docs/GDD.md).

---

## Si algo falla

- **No aparece el menú "Abismo"**: hay errores en la ventana **Console** (Window → General → Console).
  Normalmente es porque falta algún paquete: abre **Window → Package Manager** y comprueba que estén
  instalados *Input System*, *Unity UI* y *2D Tilemap Editor*.
- **El personaje atraviesa el suelo o no hay enemigos**: usa **Abismo → Construir demo jugable**, que
  crea las capas `Ground`, `Player` y `Enemy` necesarias.
- **Se ve todo rosa**: el proyecto usa el render pipeline integrado (Built-in). Si lo has convertido a
  URP, vuelve a construir; si sigue rosa, cambia el material de los sprites por `Sprites-Default`.
- **Al pulsar Play no pasa nada**: comprueba que tienes abierta la escena `Assets/_Abismo/Scenes/Nivel_01`.

---

*Los textos y el arte de este proyecto son originales. Los nombres del mito (Cthulhu, Dagón, R'lyeh,
Innsmouth) proceden de la obra de H. P. Lovecraft.*
