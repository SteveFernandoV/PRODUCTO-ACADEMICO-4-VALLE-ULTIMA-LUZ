# Valle de la Última Luz - Producto Académico 4

Proyecto Unity 3D de exploración y recolección. Este repositorio nuevo parte de una copia guardada del PA3 para desarrollar y registrar por separado los avances del PA4.

## Base del proyecto

- Proyecto de origen: `PRODUCTO ACADEMICO 3 DESARROLLO DE VIDEOJUEGOS`.
- Commit usado como punto de partida: `15b3d0f` (`Guardar ajuste final del terreno`, 25/09/2026).
- Este repositorio es independiente del repositorio PA3. Los cambios futuros del PA4 se registrarán aquí.
- Versión de Unity declarada por el proyecto: `6000.7.0b1`.

## Objetivo PA4

Entregar una experiencia breve y completa con inicio, exploración, objetivo, mecánica principal y victoria o derrota. La versión actual conserva la base del PA3; los elementos siguientes son la ruta de trabajo y no se declaran terminados hasta probarlos.

1. Revisar la escena, movimiento, cámara, terreno, colisiones y objetivo de recolección.
2. Integrar un enemigo con al menos dos comportamientos, por ejemplo patrulla y persecución, usando una máquina de estados y navegación adecuada al nivel.
3. Integrar y probar interfaz, música o ambiente, efectos de sonido y VFX.
4. Elegir una optimización, medir antes y después con Unity Profiler y documentar el resultado. Opciones de clase: Object Pooling, LOD, Occlusion Culling y reducir cálculos innecesarios.
5. Ejecutar pruebas de movimiento, colisiones, IA, interfaz, objetivo y flujo completo.
6. Generar un Build, ejecutarlo fuera del Editor y registrar los resultados.
7. Preparar informe de 4 a 6 páginas y video demostrativo de 5 a 7 minutos que muestre gameplay, IA, UI, audio, efectos y optimización.

## Material de referencia

### Materiales de clase

- `U3.S6 - Iluminación, Shaders y Diseño Narrativo`: luz Baked y Realtime, dirección visual, Shader Graph con tiempo y narrativa ambiental.
- `U4.S7 - Inteligencia Artificial y Optimización para Videojuegos`: percepción, máquina de estados, patrulla/persecución/ataque, Raycast, NavMesh, pooling, LOD, culling y ciclo medir-identificar-optimizar-volver a medir.
- `PA4 - Final (Videojuegos)`: requisitos, entregables y rúbrica del producto final.

### Videos compartidos

- Ciclo día/noche en Unity (Dev Rychz): https://www.youtube.com/watch?v=BU5HVnMbqb8
- Shader Graph en Unity (Kostas): https://www.youtube.com/watch?v=fIOLrXL_jew
- FPS Movement, serie First Person Shooter Game (Natty GameDev): https://www.youtube.com/watch?v=rJqP5EesxLk&list=PLGUw8UNswJEOv8c5ZcoHarbON6mIEUFBC
- NavMesh Pathfinding (ElOctopus): https://www.youtube.com/watch?v=So-HOtcHAOY

## Registro de avances

Se realizarán commits pequeños y descriptivos por bloque: escena y flujo, IA, optimización, pruebas, Build y documentación. Cada hito deberá indicar qué cambió y qué se verificó. No se marcará como funcional una característica solo porque exista un script o prefab.

## Estado inicial de esta copia

- Reutiliza recursos y escenas del PA3.
- Pendiente: completar y comprobar el flujo jugable de PA4, la IA con dos comportamientos, una optimización medida, la pasada de pruebas y el Build ejecutable.
- La copia proviene del commit `15b3d0f`; el repositorio de PA3 no se modifica desde este proyecto.
