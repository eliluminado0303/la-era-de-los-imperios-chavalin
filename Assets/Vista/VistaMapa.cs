using UnityEngine;

// Crea la partida (Modelo + Controlador) al empezar, dibuja el tablero
// como una cuadrícula de sprites, y llama a ControladorPartida.Actualizar()
// una vez por frame — es el único lugar donde este proyecto toca UnityEngine
// para "arrancar" el juego (el Modelo sigue sin ninguna dependencia de Unity).
public class VistaMapa : MonoBehaviour
{
    [Header("Configuración de la partida")]
    public string nombreJugadorHumano = "Jugador";
    public string CivilizacionHumano { get; private set; }


    [Header("Sprites (opcional — si los dejas vacíos, usa cuadrados de color)")]
    public Sprite spriteTierra;
    
    public Sprite spriteRecursoOro;
    public Sprite spriteRecursoMadera;
    public Sprite spriteRecursoComida;
    public Sprite spriteEdificioPrincipal;
    public Sprite spriteEdificioEntrenamiento;
    public Sprite spriteUnidadGenerica;
    public float tamañoCelda = 1f;

    public ControladorPartida Partida { get; private set; }

    private SpriteRenderer[,] fondos;
    private SpriteRenderer[,] contenidos;
        [Header("Borde decorativo (solo visual, no afecta el juego)")]
    public Sprite spriteBorde;
    public Sprite spriteEsquina;
    public int grosorBorde = 2;

        void Start()
    {
        IniciarPartida("Sumerios"); // TEMPORAL: solo para probar sin el panel de botones todavía
    }

    // Dibuja un marco alrededor del tablero jugable. Las 4 esquinas del
    // marco (donde fila Y columna están fuera del 15x15 al mismo tiempo)
    // usan un sprite/color distinto al resto del borde, para que se noten
    // como "esquinas" separadas del borde recto. Todo esto queda fuera del
    // rango 0..14, así que nunca lo toca la lógica del juego.
    private void ConstruirBordeDecorativo()
    {
        int filas = Mapa.FILAS;
        int columnas = Mapa.COLUMNAS;

        for (int fila = -grosorBorde; fila < filas + grosorBorde; fila++)
        {
            for (int columna = -grosorBorde; columna < columnas + grosorBorde; columna++)
            {
                bool dentroDelTablero = fila >= 0 && fila < filas && columna >= 0 && columna < columnas;
                if (dentroDelTablero) continue;

                bool esEsquina = (fila < 0 || fila >= filas) && (columna < 0 || columna >= columnas);

                Vector3 posicion = new Vector3(columna * tamañoCelda, -fila * tamañoCelda, 0);
                var bordeGO = new GameObject($"Borde_{fila}_{columna}");
                bordeGO.transform.SetParent(transform);
                bordeGO.transform.position = posicion;
                var sr = bordeGO.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 0;

                if (esEsquina)
                {
                    sr.sprite = spriteEsquina;
                    sr.color = spriteEsquina == null ? new Color(0.5f, 0.4f, 0.2f) : Color.white; // café/tierra por defecto
                }
                else
                {
                    sr.sprite = spriteBorde;
                    sr.color = spriteBorde == null ? new Color(0.15f, 0.35f, 0.55f) : Color.white; // azul agua por defecto
                }
            }
        }
    }

       // Ya no se ejecuta automáticamente al iniciar la escena. La pantalla de
    // selección de civilización llama a esto cuando el jugador elige, y
    // recién ahí arranca la partida.
    public void IniciarPartida(string civilizacionElegida)
    {
        CivilizacionHumano = civilizacionElegida;   // <-- nueva línea
        Partida = new ControladorPartida(nombreJugadorHumano, civilizacionElegida);
        ConstruirCuadricula();
        ConstruirBordeDecorativo();
        RedibujarTodo();
    }

    void Update()
    {
        if (Partida == null) return; // todavía no se eligió civilización   <-- nueva línea

        bool termino = Partida.Actualizar();
        RedibujarTodo(); // simple a propósito: en un tablero de 15x15 redibujar
                          // todo cada frame es barato. Si más adelante se siente
                          // lento, se optimiza a "solo redibujar lo que cambió"
                          // usando los eventos de Unidad.Muerte/Edificio.FueAtacado.
        if (termino)
        {
            Debug.Log($"Partida terminada. Ganador: {(Partida.Partida.Ganador != null ? Partida.Partida.Ganador.Nombre : "nadie")}");
            enabled = false;
        }
    }

    private void ConstruirCuadricula()
    {
        int filas = Mapa.FILAS;
        int columnas = Mapa.COLUMNAS;
        fondos = new SpriteRenderer[filas, columnas];
        contenidos = new SpriteRenderer[filas, columnas];

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

                var contenidoGO = new GameObject($"Contenido_{fila}_{columna}");
                contenidoGO.transform.SetParent(transform);
                contenidoGO.transform.position = posicion;
                var contenidoSR = contenidoGO.AddComponent<SpriteRenderer>();
                contenidoSR.sortingOrder = 1;
                contenidoSR.enabled = false;
                contenidos[fila, columna] = contenidoSR;
            }
        }
    }

    private void RedibujarTodo()
    {
        for (int fila = 0; fila < Mapa.FILAS; fila++)
            for (int columna = 0; columna < Mapa.COLUMNAS; columna++)
                RedibujarCelda(fila, columna, Partida.Mapa.ObtenerCelda(fila, columna));
    }

        private void RedibujarCelda(int fila, int columna, Celda celda)
    {
        var fondoSR = fondos[fila, columna];
        var contenidoSR = contenidos[fila, columna];

        if (celda.Recurso != null && !celda.Recurso.EstaAgotado())
        {
            fondoSR.sprite = celda.Recurso.Tipo == TipoRecurso.Oro ? spriteRecursoOro
                            : celda.Recurso.Tipo == TipoRecurso.Madera ? spriteRecursoMadera
                            : spriteRecursoComida;
            fondoSR.color = fondoSR.sprite == null ? ColorDeRecurso(celda.Recurso.Tipo) : Color.white;
        }
        else
        {
            fondoSR.sprite = spriteTierra;
            fondoSR.color = spriteTierra == null ? new Color(0.6f, 0.8f, 0.5f) : Color.white;
        }

        // La celda seleccionada se aclara (se mezcla con blanco) para que
        // resalte, sin necesitar un sprite ni GameObject extra.
        bool estaSeleccionada = FilaSeleccionada == fila && ColumnaSeleccionada == columna;
        if (estaSeleccionada) fondoSR.color = Color.Lerp(fondoSR.color, Color.white, 0.6f);

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
        else
        {
            contenidoSR.enabled = false;
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
    // tuyo y qué es enemigo — sin esto, todas las unidades/edificios se
    // veían igual y era fácil atacar a los propios sin querer.
    private static readonly System.Collections.Generic.Dictionary<string, Color> ColoresPorCivilizacion =
        new System.Collections.Generic.Dictionary<string, Color> {
            { "Sumerios", new Color(0.9f, 0.7f, 0.2f) },
            { "Nipones",  new Color(0.85f, 0.2f, 0.2f) },
            { "Griegos",  new Color(0.3f, 0.5f, 0.9f) },
            { "Vikingos", new Color(0.4f, 0.8f, 0.4f) },
        };

    // La celda que VistaInput tiene seleccionada ahora mismo (null si ninguna).
    public int? FilaSeleccionada { get; set; }
    public int? ColumnaSeleccionada { get; set; }
}