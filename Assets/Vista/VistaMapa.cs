using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Crea la partida (Modelo + Controlador) al empezar, maneja la fase de
// colocación inicial del Centro Urbano, dibuja el tablero como una
// cuadrícula de sprites (con niebla de guerra encima), y llama a
// ControladorPartida.Actualizar() una vez por frame — es el único lugar
// donde este proyecto toca UnityEngine para "arrancar" el juego (el
// Modelo sigue sin ninguna dependencia de Unity).
//
// Capas de dibujo, de abajo hacia arriba (sortingOrder):
//   0 fondo (tierra)
//   1 decoración estética (arbustos/piedras sueltas, sin efecto en el juego)
//   2 contenido real (unidad > edificio > recurso)
//   3 niebla de guerra
public class VistaMapa : MonoBehaviour
{
    [Header("Configuración de la partida")]
    public string nombreJugadorHumano = "Jugador";
    public string CivilizacionHumano { get; private set; }

    [Header("Sprites (opcional — si los dejás vacíos, usa cuadrados de color)")]
    public Sprite spriteTierra;

    [Header("Recursos (variantes — poné 1 o varias por tipo, se elige una fija por celda)")]
    public Sprite[] spritesRecursoOro;      // ej: Gold Stones / Gold Resource
    public Sprite[] spritesRecursoMadera;   // ej: Trees (varias variantes)
    public Sprite[] spritesRecursoComida;   // ej: Meat Resource / Sheep

    [Header("Edificios y unidades")]
    public Sprite spriteEdificioPrincipal;
    public Sprite spriteEdificioEntrenamiento;
    public Sprite spriteUnidadGenerica;
    public float tamañoCelda = 1f;

    [Header("Decoración estética (sin efecto en el juego — arbustos, piedras sueltas, etc.)")]
    public Sprite[] spritesDecoracion;
    [Range(0f, 0.3f)] public float densidadDecoracion = 0.04f;

    public ControladorPartida Partida { get; private set; }

    private SpriteRenderer[,] fondos;
    private SpriteRenderer[,] decoraciones;
    private SpriteRenderer[,] contenidos;
    private SpriteRenderer[,] niebla;
    private bool[,] explorado;

    // Una costura por columna, ubicada exactamente sobre la fila del
    // acantilado que toca el pasto (ver CrearCosturaAcantilado). Si
    // spritesCosturaAcantilado está vacío, quedan creadas pero deshabilitadas.
    private SpriteRenderer[] costuraAcantilado;

    [Header("Agua (rodea el tablero directamente, sin anillos intermedios)")]
    public Sprite[] spritesAgua;
    // Grosor generoso a propósito: así, aunque la cámara se aleje bastante,
    // siempre hay agua real dibujada y nunca se asoma el color de fondo de
    // la cámara (que solo actúa como último respaldo, ver AplicarColorDeCamaraDeRespaldo).
    public int grosorAgua = 4;

    [Header("Acantilado sur (da la sensación de altura: el tablero \"flota\" sobre el agua, como en la imagen de referencia de Tiny Swords)")]
    // Fila pegada al pasto: variantes con el borde/reborde visible (la fila
    // "de arriba" del bloque de roca del tileset).
    public Sprite[] spritesAcantiladoBorde;
    // Filas siguientes hacia abajo: relleno de roca liso, se repite tantas
    // veces como falte para completar alturaAcantilado.
    public Sprite[] spritesAcantiladoRelleno;
    // Opcional: sprite fijo para la esquina inferior-izquierda / inferior-derecha
    // del acantilado (si el tileset trae una pieza de esquina especial). Si
    // se deja vacío, esa columna elige una variante normal como cualquier otra.
    public Sprite spriteAcantiladoEsquinaIzquierda;
    public Sprite spriteAcantiladoEsquinaDerecha;
    // Cuántas filas "cuelgan" del borde sur antes de que empiece el agua.
    public int alturaAcantilado = 2;

    [Header("Costa norte/oeste/este (borde de pasto justo antes del agua — el lado sur usa el Acantilado, no esto)")]
    // Franja de transición pegada al pasto en los otros 3 lados del mapa
    // (arriba, izquierda, derecha). Si se dejan vacíos, esos lados pasan
    // directo a agua sin transición (como hasta ahora).
    public Sprite[] spritesCostaNorte;
    public Sprite[] spritesCostaOeste;
    public Sprite[] spritesCostaEste;
    // Grosor de esa franja (normalmente 1 alcanza).
    public int grosorCosta = 1;

    [Header("Esquinas del mapa (arriba-izquierda / arriba-derecha — las de abajo las resuelve el Acantilado)")]
    public Sprite spriteEsquinaNoroeste;
    public Sprite spriteEsquinaNoreste;

    [Header("Costura acantilado-pasto (la línea donde el agua choca contra la roca, justo donde el acantilado se junta con el tablero). Poné 1 solo sprite si la querés fija, o varios en orden para que se animen en loop.")]
    public Sprite[] spritesCosturaAcantilado;
    public float fpsCosturaAcantilado = 8f;

    [Header("Niebla de guerra (solo afecta lo que VE el jugador humano)")]
    public Sprite spriteNiebla; // opcional — si lo dejás vacío usa un cuadrado de color
    public Color colorNieblaSinExplorar = Color.black;
    public Color colorNieblaExploradaSinVision = new Color(0f, 0f, 0f, 0.6f);
    public int radioVisionUnidad = 5;
    public int radioVisionEdificio = 7;

    [Header("Fase de colocación (opcional)")]
    public TMPro.TextMeshProUGUI textoFaseColocacion; // podés dejarlo vacío, solo se usa si lo asignás

    void Start()
    {
        IniciarPartida("Sumerios"); // TEMPORAL: solo para probar sin el panel de botones todavía
    }

    // Ya no se ejecuta automáticamente al iniciar la escena. La pantalla de
    // selección de civilización llama a esto cuando el jugador elige.
    // A partir de acá el juego queda en "fase de colocación": todavía no
    // hay Centro Urbano de nadie, y Partida.Actualizar() no hace nada hasta
    // que el jugador haga clic en una celda válida (ver VistaInput /
    // IntentarColocarCentroHumano).
    public void IniciarPartida(string civilizacionElegida)
    {
        CivilizacionHumano = civilizacionElegida;
        Partida = new ControladorPartida(nombreJugadorHumano, civilizacionElegida);
        ConstruirCuadricula();
        ConstruirAnillosExteriores();
        AplicarColorDeCamaraDeRespaldo();
        RedibujarTodo();

        if (textoFaseColocacion != null)
            textoFaseColocacion.text = "Elige dónde poner tu Centro Urbano (clic en el mapa)";
        Debug.Log("Fase de colocación: hacé clic en una celda del tablero para ubicar tu Centro Urbano.");
    }

    // Llamado por VistaInput cuando el jugador hace clic mientras todavía
    // está en fase de colocación. Devuelve true si el clic fue válido (ahí
    // ya arrancó la partida de verdad); false si hay que probar otra celda.
    public bool IntentarColocarCentroHumano(int fila, int columna)
    {
        if (!EnFaseDeColocacion) return false;

        bool colocado = Partida.ColocarCentroUrbanoHumano(fila, columna);
        if (colocado)
        {
            if (textoFaseColocacion != null) textoFaseColocacion.gameObject.SetActive(false);
            RedibujarTodo();
        }
        return colocado;
    }

    public bool EnFaseDeColocacion => Partida != null && !Partida.PartidaEnCurso;

    void Update()
    {
        if (Partida == null) return; // todavía no se eligió civilización

        bool termino = Partida.Actualizar(); // no hace nada mientras EnFaseDeColocacion sea true
        RedibujarTodo(); // simple a propósito: redibujar todo el tablero cada frame es
                          // barato comparado con el costo real del juego. Si en algún
                          // momento se siente lento, se optimiza a "solo redibujar lo
                          // que cambió" usando los eventos de Unidad.Muerte/Edificio.FueAtacado.
        if (termino)
        {
            Debug.Log($"Partida terminada. Ganador: {(Partida.Partida.Ganador != null ? Partida.Partida.Ganador.Nombre : "nadie")}");
            enabled = false;
        }
    }

    // ---------------------------------------------------------------
    // Construcción de la cuadrícula jugable (0..FILAS-1, 0..COLUMNAS-1)
    // ---------------------------------------------------------------

    private void ConstruirCuadricula()
    {
        int filas = Mapa.FILAS;
        int columnas = Mapa.COLUMNAS;
        fondos = new SpriteRenderer[filas, columnas];
        decoraciones = new SpriteRenderer[filas, columnas];
        contenidos = new SpriteRenderer[filas, columnas];
        niebla = new SpriteRenderer[filas, columnas];
        explorado = new bool[filas, columnas];

        for (int fila = 0; fila < filas; fila++)
        {
            for (int columna = 0; columna < columnas; columna++)
            {
                Vector3 posicion = new Vector3(columna * tamañoCelda, -fila * tamañoCelda, 0);

                var fondoGO = new GameObject($"Fondo_{fila}_{columna}");
                fondoGO.transform.SetParent(transform);
                fondoGO.transform.position = posicion;
                var fondoSR = fondoGO.AddComponent<SpriteRenderer>();
                fondoSR.sortingOrder = 0;
                fondos[fila, columna] = fondoSR;

                var decoracionGO = new GameObject($"Decoracion_{fila}_{columna}");
                decoracionGO.transform.SetParent(transform);
                decoracionGO.transform.position = posicion;
                var decoracionSR = decoracionGO.AddComponent<SpriteRenderer>();
                decoracionSR.sortingOrder = 1;
                decoracionSR.enabled = false;
                decoraciones[fila, columna] = decoracionSR;
                AsignarDecoracionSiCorresponde(decoracionSR, fila, columna);

                var contenidoGO = new GameObject($"Contenido_{fila}_{columna}");
                contenidoGO.transform.SetParent(transform);
                contenidoGO.transform.position = posicion;
                var contenidoSR = contenidoGO.AddComponent<SpriteRenderer>();
                contenidoSR.sortingOrder = 2;
                contenidoSR.enabled = false;
                contenidos[fila, columna] = contenidoSR;

                var nieblaGO = new GameObject($"Niebla_{fila}_{columna}");
                nieblaGO.transform.SetParent(transform);
                nieblaGO.transform.position = posicion;
                var nieblaSR = nieblaGO.AddComponent<SpriteRenderer>();
                nieblaSR.sortingOrder = 3;
                AsignarSpriteConRespaldo(nieblaSR, spriteNiebla, colorNieblaSinExplorar);
                niebla[fila, columna] = nieblaSR;
            }
        }
    }

    // Decoración puramente estética (arbustos, piedras sueltas): se decide
    // UNA sola vez acá (no en cada RedibujarCelda) porque no depende del
    // estado del juego, solo de la posición. Como puede quedar "debajo" de
    // un recurso/edificio/unidad real más adelante, no hace falta chequear
    // si la celda está libre: el contenido real se dibuja en una capa por
    // encima y la tapa sin problema.
    private void AsignarDecoracionSiCorresponde(SpriteRenderer sr, int fila, int columna)
    {
        if (spritesDecoracion == null || spritesDecoracion.Length == 0) return;
        if (Hash01(fila, columna, salto: 1) >= densidadDecoracion) return;

        sr.sprite = ElegirVariante(spritesDecoracion, fila, columna, salto: 2);
        sr.color = Color.white;
        sr.enabled = sr.sprite != null;
    }

    // ---------------------------------------------------------------
    // Exterior del tablero: agua directamente alrededor de las 4 caras, sin
    // anillos intermedios raros. En el lado sur, antes de que empiece el
    // agua, se cuelgan "alturaAcantilado" filas de roca (el acantilado) para
    // dar la sensación de que el tablero flota en alto, tal como se ve en la
    // imagen de referencia de Tiny Swords. Todo esto queda fuera del rango
    // 0..FILAS-1 / 0..COLUMNAS-1, así que nunca lo toca la lógica del juego
    // (Mapa, ControladorPartida, etc.) — es puramente decorativo.
    // ---------------------------------------------------------------

    private void ConstruirAnillosExteriores()
    {
        int filas = Mapa.FILAS;
        int columnas = Mapa.COLUMNAS;

        // El acantilado solo "cuelga" del lado sur; los otros 3 lados tienen
        // su propia franja de costa (grosorCosta) antes de que empiece el
        // agua profunda (grosorAgua).
        int filaMinima = -(grosorCosta + grosorAgua);
        int filaMaxima = filas + alturaAcantilado + grosorAgua;
        int columnaMinima = -(grosorCosta + grosorAgua);
        int columnaMaxima = columnas + grosorCosta + grosorAgua;

        for (int fila = filaMinima; fila < filaMaxima; fila++)
        {
            for (int columna = columnaMinima; columna < columnaMaxima; columna++)
            {
                bool dentroDelTablero = fila >= 0 && fila < filas && columna >= 0 && columna < columnas;
                if (dentroDelTablero) continue;

                Vector3 posicion = new Vector3(columna * tamañoCelda, -fila * tamañoCelda, 0);
                var celdaGO = new GameObject($"Exterior_{fila}_{columna}");
                celdaGO.transform.SetParent(transform);
                celdaGO.transform.position = posicion;
                var sr = celdaGO.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 0;

                bool esFilaDeAcantilado = fila >= filas && fila < filas + alturaAcantilado
                                        && columna >= 0 && columna < columnas;
                bool esFilaDeCostaNorte = fila >= -grosorCosta && fila < 0
                                        && columna >= 0 && columna < columnas;
                bool esColumnaDeCostaOeste = columna >= -grosorCosta && columna < 0
                                        && fila >= 0 && fila < filas;
                bool esColumnaDeCostaEste = columna >= columnas && columna < columnas + grosorCosta
                                        && fila >= 0 && fila < filas;
                bool esEsquinaNoroeste = fila >= -grosorCosta && fila < 0 && columna >= -grosorCosta && columna < 0;
                bool esEsquinaNoreste = fila >= -grosorCosta && fila < 0 && columna >= columnas && columna < columnas + grosorCosta;

                if (esFilaDeAcantilado)
                {
                    int profundidad = fila - filas; // 0 = pegado al pasto, 1, 2, ... hacia abajo
                    AsignarSpriteDeAcantilado(sr, columna, columnas, profundidad);
                }
                else if (esEsquinaNoroeste)
                {
                    AsignarSpriteConRespaldo(sr, spriteEsquinaNoroeste, new Color(0.6f, 0.8f, 0.5f));
                }
                else if (esEsquinaNoreste)
                {
                    AsignarSpriteConRespaldo(sr, spriteEsquinaNoreste, new Color(0.6f, 0.8f, 0.5f));
                }
                else if (esFilaDeCostaNorte)
                {
                    AsignarSpriteConRespaldo(sr, ElegirVariante(spritesCostaNorte, fila, columna, salto: 8), new Color(0.6f, 0.8f, 0.5f));
                }
                else if (esColumnaDeCostaOeste)
                {
                    AsignarSpriteConRespaldo(sr, ElegirVariante(spritesCostaOeste, fila, columna, salto: 9), new Color(0.6f, 0.8f, 0.5f));
                }
                else if (esColumnaDeCostaEste)
                {
                    AsignarSpriteConRespaldo(sr, ElegirVariante(spritesCostaEste, fila, columna, salto: 10), new Color(0.6f, 0.8f, 0.5f));
                }
                else
                {
                    AsignarSpriteConRespaldo(sr, ElegirVariante(spritesAgua, fila, columna, salto: 3), new Color(0.20f, 0.45f, 0.75f));
                }
            }
        }
    }

    private void AsignarSpriteDeAcantilado(SpriteRenderer sr, int columna, int columnas, int profundidad)
    {
        // Solo la primera fila (la que toca el pasto) usa las esquinas
        // especiales, si se asignaron; el resto de las filas hacia abajo
        // es relleno de roca liso.
        if (profundidad == 0)
        {
            if (columna == 0 && spriteAcantiladoEsquinaIzquierda != null)
            {
                AsignarSpriteConRespaldo(sr, spriteAcantiladoEsquinaIzquierda, new Color(0.45f, 0.42f, 0.40f));
                return;
            }
            if (columna == columnas - 1 && spriteAcantiladoEsquinaDerecha != null)
            {
                AsignarSpriteConRespaldo(sr, spriteAcantiladoEsquinaDerecha, new Color(0.45f, 0.42f, 0.40f));
                return;
            }
            AsignarSpriteConRespaldo(sr, ElegirVariante(spritesAcantiladoBorde, 0, columna, salto: 6), new Color(0.45f, 0.42f, 0.40f));
        }
        else
        {
            AsignarSpriteConRespaldo(sr, ElegirVariante(spritesAcantiladoRelleno, profundidad, columna, salto: 7), new Color(0.40f, 0.38f, 0.36f));
        }
    }

    // ---------------------------------------------------------------
    // Redibujado por frame
    // ---------------------------------------------------------------

    private void RedibujarTodo()
    {
        var jugadorHumano = Partida.ControladoresMapa[0].Jugador;
        var fuentesDeVision = new List<(int fila, int columna, int radio)>();

        for (int fila = 0; fila < Mapa.FILAS; fila++)
        {
            for (int columna = 0; columna < Mapa.COLUMNAS; columna++)
            {
                Celda celda = Partida.Mapa.ObtenerCelda(fila, columna);
                RedibujarCelda(fila, columna, celda);

                if (celda.Unidad != null && jugadorHumano.Unidades.Contains(celda.Unidad))
                    fuentesDeVision.Add((fila, columna, radioVisionUnidad));
                else if (celda.Edificio != null && jugadorHumano.Edificios.Contains(celda.Edificio))
                    fuentesDeVision.Add((fila, columna, radioVisionEdificio));
            }
        }

        ActualizarNiebla(fuentesDeVision);
    }

    private void RedibujarCelda(int fila, int columna, Celda celda)
    {
        var fondoSR = fondos[fila, columna];
        var contenidoSR = contenidos[fila, columna];

        // El fondo SIEMPRE es tierra — así nunca se asoma el color de fondo
        // de la cámara cuando el sprite de un recurso/edificio aún no está
        // asignado.
        AsignarSpriteConRespaldo(fondoSR, spriteTierra, new Color(0.6f, 0.8f, 0.5f));

        bool estaSeleccionada = FilaSeleccionada == fila && ColumnaSeleccionada == columna;
        if (estaSeleccionada) fondoSR.color = Color.Lerp(fondoSR.color, Color.white, 0.6f);

        // El contenido muestra, en orden de prioridad, lo primero que haya:
        // unidad > edificio > recurso (nunca coinciden dos a la vez en la
        // misma celda, así que el orden no genera conflicto real).
        if (celda.Unidad != null)
        {
            contenidoSR.enabled = true;
            contenidoSR.sprite = spriteUnidadGenerica;
            contenidoSR.color = ColorDeCivilizacion(celda.Unidad.Civilizacion);
        }
        else if (celda.Edificio != null)
        {
            contenidoSR.enabled = true;
            contenidoSR.sprite = celda.Edificio is EdificioPrincipal ? spriteEdificioPrincipal : spriteEdificioEntrenamiento;
            contenidoSR.color = ColorDeCivilizacion(celda.Edificio.Civilizacion);
        }
        else if (celda.Recurso != null && !celda.Recurso.EstaAgotado())
        {
            contenidoSR.enabled = true;
            Sprite[] variantes = celda.Recurso.Tipo == TipoRecurso.Oro ? spritesRecursoOro
                                : celda.Recurso.Tipo == TipoRecurso.Madera ? spritesRecursoMadera
                                : spritesRecursoComida;
            contenidoSR.sprite = ElegirVariante(variantes, fila, columna, salto: 5);
            contenidoSR.color = contenidoSR.sprite == null ? ColorDeRecurso(celda.Recurso.Tipo) : Color.white;
        }
        else
        {
            contenidoSR.enabled = false;
        }
    }

    // ---------------------------------------------------------------
    // Niebla de guerra (solo desde el punto de vista del jugador humano;
    // las 3 IA siguen "viendo" todo el mapa internamente, esto es nada
    // más una capa visual sobre lo que el humano tiene derecho a ver).
    // ---------------------------------------------------------------

    private void ActualizarNiebla(List<(int fila, int columna, int radio)> fuentesDeVision)
    {
        // Durante la fase de colocación no hay niebla: el jugador necesita
        // ver el tablero completo para elegir dónde ubicarse.
        if (EnFaseDeColocacion)
        {
            for (int fila = 0; fila < Mapa.FILAS; fila++)
                for (int columna = 0; columna < Mapa.COLUMNAS; columna++)
                    niebla[fila, columna].enabled = false;
            return;
        }

        bool[,] visibleAhora = new bool[Mapa.FILAS, Mapa.COLUMNAS];

        foreach (var (filaFuente, columnaFuente, radio) in fuentesDeVision)
        {
            int filaMin = Mathf.Max(0, filaFuente - radio);
            int filaMax = Mathf.Min(Mapa.FILAS - 1, filaFuente + radio);
            int columnaMin = Mathf.Max(0, columnaFuente - radio);
            int columnaMax = Mathf.Min(Mapa.COLUMNAS - 1, columnaFuente + radio);

            for (int fila = filaMin; fila <= filaMax; fila++)
            {
                for (int columna = columnaMin; columna <= columnaMax; columna++)
                {
                    int deltaFila = fila - filaFuente;
                    int deltaColumna = columna - columnaFuente;
                    if (deltaFila * deltaFila + deltaColumna * deltaColumna > radio * radio) continue; // radio circular

                    visibleAhora[fila, columna] = true;
                    explorado[fila, columna] = true;
                }
            }
        }

        for (int fila = 0; fila < Mapa.FILAS; fila++)
        {
            for (int columna = 0; columna < Mapa.COLUMNAS; columna++)
            {
                var sr = niebla[fila, columna];
                if (!explorado[fila, columna])
                {
                    sr.enabled = true;
                    sr.color = colorNieblaSinExplorar;
                }
                else if (!visibleAhora[fila, columna])
                {
                    sr.enabled = true;
                    sr.color = colorNieblaExploradaSinVision;
                }
                else
                {
                    sr.enabled = false;
                }
            }
        }
    }

    // ---------------------------------------------------------------
    // Utilidades
    // ---------------------------------------------------------------

    // Si sprite es null, usa un cuadrado blanco 1x1 (el que ya trae Unity,
    // no genera ningún asset nuevo) teñido del color de respaldo. Así una
    // celda SIEMPRE dibuja algo, aunque todavía no le hayas arrastrado el
    // asset final — nunca vuelve a asomarse el azul de fondo de la cámara
    // por un sprite sin asignar.
    private void AsignarSpriteConRespaldo(SpriteRenderer sr, Sprite sprite, Color colorDeRespaldo)
    {
        if (sprite != null)
        {
            sr.sprite = sprite;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = ObtenerCuadradoBlanco();
            sr.color = colorDeRespaldo;
        }
    }

    private static Sprite cuadradoBlanco;
    private static Sprite ObtenerCuadradoBlanco()
    {
        if (cuadradoBlanco == null)
        {
            var textura = Texture2D.whiteTexture;
            cuadradoBlanco = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f), textura.width);
        }
        return cuadradoBlanco;
    }

    // Elige una variante fija (determinística, no cambia entre frames) a
    // partir de la posición, para que el mismo tipo de recurso/decoración
    // no se vea repetido en bloque por todo el mapa. "salto" solo separa
    // los distintos usos entre sí (agua, montaña, decoración, recursos)
    // para que no elijan siempre el mismo índice en la misma celda.
    private Sprite ElegirVariante(Sprite[] variantes, int fila, int columna, int salto)
    {
        if (variantes == null || variantes.Length == 0) return null;
        int indice = Mathf.FloorToInt(Hash01(fila, columna, salto) * variantes.Length);
        if (indice >= variantes.Length) indice = variantes.Length - 1;
        return variantes[indice];
    }

    // Hash determinístico simple (0..1) a partir de (fila, columna, salto).
    // No depende de UnityEngine.Random, así que da el mismo resultado
    // siempre que se llama con los mismos parámetros, sin guardar estado.
    private float Hash01(int fila, int columna, int salto)
    {
        unchecked
        {
            uint h = (uint)fila * 374761393u + (uint)columna * 668265263u + (uint)salto * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h % 100000u) / 100000f;
        }
    }

    // Ajusta el color de fondo de la cámara (lo que se ve si en algún
    // momento la vista queda por fuera de todos los anillos dibujados) a
    // un tono de agua en vez del azul por defecto de Unity. Solo lo toca
    // si la cámara usa un color sólido — si el proyecto ya usa un skybox
    // u otra configuración, no se mete con eso.
    private void AplicarColorDeCamaraDeRespaldo()
    {
        var camara = Camera.main;
        if (camara == null) return;
        if (camara.clearFlags == CameraClearFlags.SolidColor || camara.clearFlags == CameraClearFlags.Depth)
        {
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.20f, 0.45f, 0.75f);
        }
    }

    private Color ColorDeCivilizacion(string civilizacion)
    {
        return ColoresPorCivilizacion.TryGetValue(civilizacion, out Color color) ? color : Color.gray;
    }

    private Color ColorDeRecurso(TipoRecurso tipo)
    {
        if (tipo == TipoRecurso.Oro) return Color.yellow;
        if (tipo == TipoRecurso.Madera) return new Color(0.55f, 0.35f, 0.15f);
        return Color.red;
    }

    // Convierte una posición del mundo (por ejemplo, un clic) en fila/columna.
    public bool MundoACelda(Vector3 posicionMundo, out int fila, out int columna)
    {
        columna = Mathf.RoundToInt(posicionMundo.x / tamañoCelda);
        fila = Mathf.RoundToInt(-posicionMundo.y / tamañoCelda);
        return Partida.Mapa.EsPosicionValida(fila, columna);
    }

    // Un color fijo por civilización, para reconocer de un vistazo qué es
    // tuyo y qué es enemigo.
    private static readonly Dictionary<string, Color> ColoresPorCivilizacion =
        new Dictionary<string, Color> {
            { "Sumerios", new Color(0.9f, 0.7f, 0.2f) },
            { "Nipones",  new Color(0.85f, 0.2f, 0.2f) },
            { "Griegos",  new Color(0.3f, 0.5f, 0.9f) },
            { "Vikingos", new Color(0.4f, 0.8f, 0.4f) },
        };

    // La celda que VistaInput tiene seleccionada ahora mismo (null si ninguna).
    public int? FilaSeleccionada { get; set; }
    public int? ColumnaSeleccionada { get; set; }
}