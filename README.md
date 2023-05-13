# EM_2023_Grupo3_quintana


## Funcionalidad actual del juego RONIN RUMBLE REVOLUTION
El actual juego presenta bastantes limitaciones, uno de los objetivos de la práctica consiste en mejorar estas limitaciones:
- [X] El juego actualmente no dispone de una forma de que una partida comience de
manera sincronizada o termine cuando alguien gane.
- [ ] El juego puede tener errores, bugs o defectos que deben ser corregidos.
- [X] No hay HUD en el juego, que muestre la vida, el tiempo, el nombre del jugador, la
imagen de los personajes, etc.
- [X] Los personajes aparecen siempre en el mismo sitio.
- [X] Los jugadores no tienen nombre ni pueden elegir personaje.
- [X] Los ataques de los personajes no causan daño. Hay que implementar un sistema de
daño.
- [X] El juego no funciona con un servidor autoritativo.
- [X] No hay límite de jugadores.
- [X] No tiene un contador de tiempo para acabar el combate.
- [ ] Se debe poder jugar partidas adicionales sin reiniciar el servidor.
- [ ] El servidor no está optimizado.
- [ ] El juego no controla si los rivales abandonan la partida.

<br>

## Nuevas funcionalidades en el juego RONIN RUMBLE REVOLUTION

Se desea ampliar el juego y ofrecer nuevas funcionalidades. Para ello, se proponen cumplir
los siguientes requisitos:

**Requisitos Funcionales:**

- [ ] **RF01: El juego debe tener un sistema de inicio y finalización de partida, que se inicie
de manera sincronizada y finalice cuando un jugador gane o se acabe el tiempo.**
- [X] **RF02: Debe implementarse un HUD en el juego, que muestre información como la vida,
tiempo restante, nombre del jugador, imagen del personaje, entre otros.**
- [X] **RF03: Los personajes deben tener una barra de vida, y cuando llegue a cero, mueren.
Entornos Multijugador**
- [X] **RF04: El último jugador con vida debe ganar la partida, o en caso de acabarse el tiempo,
ganará el que disponga de más vida.**
- [ ] **RF05: Si todos los demás jugadores se desconectan, el último jugador conectado debe ganar
automáticamente.**
- [ ] RF06: El juego debe permitir distintas rondas para una misma partida.
- [X] **RF07: El juego debe mostrar al ganador de la partida al finalizarla.**
- [ ] RF08: Después de finalizar una partida, el juego debe volver a la sala con los jugadores.
- [X] **RF09: Los jugadores deben poder elegir un personaje antes de iniciar la partida.**
- [X] **RF10: Los ataques de los personajes deben causar daño a los adversarios.**
- [X] **RF11: El juego debe funcionar con un servidor autoritativo.**
- [ ] RF12: Se debe implementar un sistema de airdash del personaje para hacer esquivas en el
aire en cualquier dirección.
- [ ] RF13: Los jugadores solo podrán realizar un airdash en el aire por vez y tendrán que tocar
el suelo para recuperarla.

<br>

**Requisitos No Funcionales:**

- [X] RNF01: El juego debe permitir crear salas públicas o privadas y unirse a ellas.
- [ ] RNF02: Se implementará un sistema de matchmaking para encontrar partidas disponibles
automáticamente.
- [ ] RNF03: Se debe incluir un chat dentro de las salas.
- [ ] RNF04: El creador de la sala debe poder elegir el número de rondas y la duración de estas.
- [X] RNF05: Se debe implementar un lobby
- [ ] RNF06: El servidor debe estar optimizado
- [ ] Los alumnos deberán crear un sistema de lobby para poder complementar las
funcionalidades propuestas que muestre las distintas salas públicas disponibles y el número
de jugadores (conectados y máximos) por sala. No es necesario que la interfaz gráfica sea
visualmente atractiva. Lo que se va a valorar es que sea funcional (se ofrezca la funcionalidad
requerida) y esté implementada de forma correcta (que no tenga errores).
- [X] Un jugador nunca podrá jugar solo. Solo se podrá comenzar el juego cuando haya al menos
dos jugadores. Cada jugador deberá expresamente indicar que está listo para jugar. El juego
comenzará automáticamente cuando la mayoría de los jugadores estén listos para jugar. Si
queda un único jugador, la partida finalizará de forma automática.
- [ ] Se valorará positivamente que el alumno desarrolle funcionalidades extra como ranking de
jugadores persistente, análisis del RTT, registro de usuarios, etc. así como aplicar técnicas
adicionales estudiadas en la asignatura para conseguir una mejor experiencia y además
reducir la latencia. 
