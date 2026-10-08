# ABISMO: El Sueño de R'lyeh · Documento de diseño

> Documento vivo: es el punto de partida, cámbialo todo lo que quieras.

## 1. Concepto

Un metroidvania de acción en 2D con pixel art, combate exigente y una estética religiosa y decadente,
como **Blasphemous**, pero cambiando el catolicismo barroco por el **horror cósmico de Lovecraft**:
cultos marinos, ciudades ciclópeas con geometrías imposibles y dioses que sueñan bajo el mar.

**Frase de venta:** *"Un penitente ahogado se abre paso por una costa maldita mientras el sueño de un
dios dormido se filtra en el mundo de los vivos."*

### Pilares

1. **Combate de lectura y castigo.** Cada enemigo anuncia sus golpes; leer y parar es más eficaz que pulsar botones.
2. **Atmósfera opresiva.** Paleta verdosa y enfermiza, niebla, siluetas colosales al fondo, sonido grave.
3. **Conocimiento prohibido.** Aprender del mundo (inscripciones, Revelación) te hace más fuerte... y más frágil.
4. **Mundo interconectado.** Atajos, altares y zonas que se abren con nuevas habilidades.

## 2. Historia

Hace un siglo, los pescadores de Innsmouth pactaron con los **Profundos**: oro y peces a cambio de
sangre. Cuando el pacto se rompió, el mar se tragó el pueblo y en su lugar emergió un fragmento de
**R'lyeh**, la ciudad donde el gran Cthulhu duerme. Su sueño, **el Sueño**, deforma la carne y la mente
de todo lo que toca.

El jugador es **el Ahogado**, un pescador que se hundió en el Arrecife del Diablo y volvió a la orilla
con la escafandra llena de agua negra. No recuerda su nombre, solo una orden susurrada en un idioma
que ningún humano debería entender. Su objetivo: llegar al corazón de R'lyeh y decidir si despierta
al dios... o si lo sella para siempre.

## 3. Equivalencias con Blasphemous

| Blasphemous | Abismo | Notas |
|---|---|---|
| El Penitente | El Ahogado | Escafandra de latón con un ojo de buey que brilla |
| Mea Culpa (espada) | El Garfio de Devil Reef | Hoja con forma de anzuelo |
| Milagro | El Sueño | Lo que corrompe el mundo |
| Fervor | Revelación | Se gana golpeando y parando |
| Oraciones | Signos | Conjuros que gastan Revelación (el primero: Signo Arcano) |
| Frascos Biliares | Láudano | Se rellenan en los altares; se pueden conseguir más |
| Reclinatorio | Altar del Signo Antiguo | Punto de guardado; reaparecen los enemigos |
| Culpa | Fragmento de mente | Queda donde mueres; reduce la Revelación hasta recuperarlo |
| Lágrimas de Expiación | Oro de Innsmouth | Joyería extraña de los Profundos |
| Cuentas de rosario | Amuletos | Ranuras de equipo con efectos pasivos |
| Reliquias | Artefactos | Habilidades de exploración (ver sección 6) |

## 4. Mecánicas implementadas

- **Movimiento:** carrera, salto variable, *coyote time* y *buffer* de salto, bajar de plataformas.
- **Combate:** combo de 3 golpes (el tercero es más fuerte), ataque aéreo, hit-stop y temblor de cámara.
- **Esquiva:** deslizamiento con invulnerabilidad (solo en el suelo, como en Blasphemous).
- **Parada:** ventana de 0,28 s; aturde al atacante 1,4 s y los golpes contra un aturdido hacen el doble.
  Las esferas enemigas paradas se devuelven con el doble de daño.
- **Código de colores:** brillo naranja = golpe parable · brillo rojo = golpe imparable.
- **Láudano:** curación del 45 % en 0,75 s (te pueden interrumpir y no gastas el frasco).
- **Signo Arcano:** proyectil perforante que cuesta 30 de Revelación.
- **Altares:** curan, rellenan el láudano, fijan la reaparición y reviven a los enemigos.
- **Muerte:** reapareces en el último altar; queda un fragmento de mente donde caíste.
- **Peligros:** coral espinoso y agua abisal (daño y vuelta al último suelo firme).

## 5. Bestiario

| Criatura | Comportamiento | Cómo vencerla |
|---|---|---|
| **Profundo** | Patrulla, persigue y lanza un zarpazo con estocada | Para el zarpazo y castiga |
| **Sectario** | Mantiene la distancia y lanza esferas de energía | Devuelve las esferas o acércate deslizándote |
| **Ojo del Vacío** | Vuela, atraviesa paredes y se lanza en picado | Para el picado o golpéalo al subir |
| **Arcipreste de las Mareas** (jefe) | Tentáculos del suelo, abanico de esferas, embestida roja; 2.ª fase más rápida | Lee el suelo, devuelve esferas, esquiva la embestida |

**Ideas para más adelante:**
- *Shoggoth menor:* masa que se divide en dos al recibir golpes fuertes.
- *Byakhee:* volador que te agarra y te suelta desde lo alto.
- *Sacerdotisa de Hydra:* invoca charcos que ralentizan.
- *Mi-Go:* insecto que roba Revelación con su aguijón.
- *Jefes:* La Madre Hydra, El Coloso de Basalto, El Que No Debe Ser Nombrado.

## 6. Progresión (por implementar)

- **Artefactos de exploración:**
  - *Branquias de Dagón:* respirar bajo el agua abisal (abre zonas inundadas).
  - *Lámpara de Alhazred:* revela plataformas invisibles "no euclidianas".
  - *Ancla del Ahogado:* caída en picado que rompe suelos agrietados.
- **Amuletos** (equipables con ranuras): más Revelación por golpe, parada más amplia, curación más rápida...
- **Signos** (conjuros): Signo Arcano (hecho), Muro de Coral, Grito del Abismo (golpe en área).
- **Mejoras:** más frascos de láudano y más vida, comprados o encontrados.

## 7. Mundo

| Zona | Ambiente | Estado |
|---|---|---|
| Costa de Innsmouth | Playa de piedra, niebla, la luna enferma | Demo |
| Ruinas Ciclópeas | Interiores de piedra con ángulos imposibles | Demo |
| Santuario de las Mareas | Templo del Arcipreste | Demo (jefe) |
| Arrecife del Diablo | Rocas negras en mar abierto | Demo (final) |
| Archivos de Miskatonic | Biblioteca hundida, libros que susurran | Idea |
| Jardines de la Madre Hydra | Corales que respiran | Idea |
| R'lyeh | La ciudad del dios dormido | Idea (final) |

## 8. Dirección de arte y sonido

- **Pixel art** a 16 píxeles por casilla; personajes de unos 32 píxeles de alto.
- **Paleta:** verdes y turquesas enfermizos, piedra gris azulada, púrpuras de la carne corrompida,
  latón y oro como únicos colores cálidos.
- **Fondos:** varias capas con paralaje; siluetas colosales que se mueven muy despacio.
- **Sonido:** zumbidos graves, mar lejano, coros disonantes en los altares y en los jefes.

## 9. Hoja de ruta

1. **Prototipo jugable** ✅ (este repositorio).
2. **Vertical slice:** arte y animaciones definitivas de la Costa y el primer jefe, música, guardado.
3. **Primer bioma completo:** 3 zonas interconectadas, 6 enemigos, 2 jefes, amuletos y tienda.
4. **Contenido:** resto de biomas, artefactos de exploración, finales.
5. **Pulido y publicación** (itch.io / Steam).
