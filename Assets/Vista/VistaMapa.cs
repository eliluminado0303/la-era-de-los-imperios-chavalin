using System.Collections.Generic;
using UnityEngine;

// Crea la partida (Modelo + Controlador) al empezar — las 4 bases quedan
// colocadas automáticamente en las esquinas dentro del constructor de
// ControladorPartida, así que acá no hay fase de colocación manual — y
// dibuja el tablero como una cuadrícula de sprites (con niebla de guerra
// encima). Llama a ControladorPartida.Actualizar() una vez por frame — es
// el único lugar donde este proyecto toca UnityEngine para "arrancar" el
// juego (el Modelo sigue sin ninguna dependencia de Unity).
//
// Capas de dibujo, de abajo hacia arriba (sortingOrder):
//   0 fondo (tierra / agua)
//   1 decoración estética (arbustos/piedras sueltas, sin efecto en el juego)
//   2 contenido real (unidad > edificio > recurso)
//   3 niebla de guerra
public class VistaMapa : MonoBehaviour
{
    [Header("Configuración de la partida")]
    public string nombreJugadorHumano = "Jugador";
    public string CivilizacionHumano { get; private set; }

    // Clave de PlayerPrefs donde el menú guarda la civilización elegida
    // antes de cargar la escena Partida (ver ControladorMenu).
    public const string claveCivilizacion = "civilizacion";

    [Header("Terreno (tileset de 9 piezas: 4 esquinas + 4 bordes + relleno)")]
    // El relleno cubre casi todo el tablero; las otras 8 solo se usan en la
    // fila/columna 0 y FILAS-1/COLUMNAS-1 — la fila y columna que quedan
    // pegadas al agua — para que el pasto termine con un borde prolijo en
    // vez de cortar de golpe contra el agua.
    public Sprite spriteTierraRelleno;
    public Sprite spriteTierraBordeNorte;
    public Sprite spriteTierraBordeSur;
    public Sprite spriteTierraBordeOeste;
    public Sprite spriteTierraBordeEste;
    public Sprite spriteTierraEsquinaNoroeste;
    public Sprite spriteTierraEsquinaNoreste;
    public Sprite spriteTierraEsquinaSuroeste;
    public Sprite spriteTierraEsquinaSureste;

    [Header("Agua (rodea el tablero jugable directamente)")]
    public Sprite[] spritesAgua;
    public int grosorAgua = 3;

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

    [Header("Niebla de guerra (solo afecta lo que VE el jugador humano)")]
    public Sprite spriteNiebla; // opcional — si lo dejás vacío usa un cuadrado de color
    public Color colorNieblaSinExplorar = Color.black;
    public Color colorNieblaExploradaSinVision = new Color(0f, 0f, 0f, 0.6f);
    public int radioVisionUnidad = 5;
    public int radioVisionEdificio = 8; // un poco más grande que antes (7), para que el hueco inicial alrededor del Centro Urbano alcance para construir cómodo

    public ControladorPartida Partida { get; private set; }

    private SpriteRenderer[,] fondos;
    private Color[,] coloresBaseFondo; // color "de reposo" de cada fondo, antes del resaltado por selección
    private SpriteRenderer[,] decoraciones;
    private SpriteRenderer[,] contenidos;
    private SpriteRenderer[,] niebla;
    private bool[,] explorado;

    void Start()
    {
        // Si venimos del menú con una civilización elegida, usamos esa; si
        // no (por ejemplo al probar la escena suelta), caemos en la
        // predeterminada.
        string civilizacion = PlayerPrefs.GetString(claveCivilizacion, "Sumerios");
        IniciarPartida(civilizacion);
    }

    // Arranca la partida con la civilización elegida entre las 4
    // disponibles. Las 4 bases (la del humano y las 3 de la IA) quedan
    // colocadas automáticamente en las esquinas del mapa apenas se crea
    // ControladorPartida — no hace falta ningún paso manual acá.
    public void IniciarPartida(string civilizacionElegida)
    {
        CivilizacionHumano = civilizacionElegida;
        Partida = new ControladorPartida(nombreJugadorHumano, civilizacionElegida);
        ConstruirCuadricula();
        ConstruirAguaAlrededor();
        AplicarColorDeCamaraDeRespaldo();
        CentrarCamaraEnBaseHumana();
        RedibujarTodo();
        Debug.Log($"Partida iniciada con {civilizacionElegida}. Tus enemigos: las 3 IA en las otras esquinas.");
    }

    // Sin esto, la cámara se queda mirando el (0,0) del mundo (donde la
    // pusiste en el editor), que normalmente NO es donde quedó tu Centro
    // Urbano — así que aunque la niebla se despeje bien alrededor de tu
    // base, la ves fuera de cámara y da la sensación de que "todo el mapa
    // sigue tapado". Esto mueve la cámara (conservando su zoom/rotación,
    // solo cambia X/Y) para que arranque centrada justo en tu base.
    private void CentrarCamaraEnBaseHumana()
    {
        var camara = Camera.main;
        if (camara == null) return;

        var (fila, columna) = Partida.PosicionBaseHumana;
        Vector3 posicionBase = new Vector3(columna * tamañoCelda, -fila * tamañoCelda, 0);
        camara.transform.position = new Vector3(posicionBase.x, posicionBase.y, camara.transform.position.z);
    }

    void Update()
    {
        if (Partida == null) return; // todavía no se eligió civilización

        bool termino = Partida.Actualizar();
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
        coloresBaseFondo = new Color[filas, columnas];
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
                // El sprite de tierra es fijo por celda (no depende del
                // estado del juego), así que se asigna UNA sola vez acá en
                // vez de en cada RedibujarCelda.
                AsignarSpriteConRespaldo(fondoSR, ElegirSpriteDeTierra(fila, columna), new Color(0.6f, 0.8f, 0.5f));
                coloresBaseFondo[fila, columna] = fondoSR.color;
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

    // Tileset de 9 piezas: las 4 esquinas y los 4 bordes solo se usan en la
    // fila/columna que toca directamente el agua (fila 0, fila FILAS-1,
    // columna 0, columna COLUMNAS-1); todo lo demás usa el relleno.
    private Sprite ElegirSpriteDeTierra(int fila, int columna)
    {
        int filas = Mapa.FILAS;
        int columnas = Mapa.COLUMNAS;
        bool esNorte = fila == 0;
        bool esSur = fila == filas - 1;
        bool esOeste = columna == 0;
        bool esEste = columna == columnas - 1;

        if (esNorte && esOeste) return spriteTierraEsquinaNoroeste;
        if (esNorte && esEste) return spriteTierraEsquinaNoreste;
        if (esSur && esOeste) return spriteTierraEsquinaSuroeste;
        if (esSur && esEste) return spriteTierraEsquinaSureste;
        if (esNorte) return spriteTierraBordeNorte;
        if (esSur) return spriteTierraBordeSur;
        if (esOeste) return spriteTierraBordeOeste;
        if (esEste) return spriteTierraBordeEste;
        return spriteTierraRelleno;
    }

    // Decoración puramente estética (arbustos, piedras sueltas): se decide
    // UNA sola vez acá (no en cada RedibujarCelda) porque no depende del
    // estado del juego, solo de la posición. Puede quedar "debajo" de un
    // recurso/edificio/unidad real más adelante — no hace falta chequear
    // si la celda está libre, porque el contenido real se dibuja en una
    // capa por encima y la tapa sin problema.
    private void AsignarDecoracionSiCorresponde(SpriteRenderer sr, int fila, int columna)
    {
        if (spritesDecoracion == null || spritesDecoracion.Length == 0) return;
        if (Hash01(fila, columna, salto: 1) >= densidadDecoracion) return;

        sr.sprite = ElegirVariante(spritesDecoracion, fila, columna, salto: 2);
        sr.color = Color.white;
        sr.enabled = sr.sprite != null;
    }

    // ---------------------------------------------------------------
    // Agua: rodea el tablero jugable directamente en las 4 caras. Todo esto
    // queda fuera del rango 0..FILAS-1 / 0..COLUMNAS-1, así que nunca lo
    // toca la lógica del juego (Mapa, ControladorPartida, etc.) — es
    // puramente decorativo.
    // ---------------------------------------------------------------

    private void ConstruirAguaAlrededor()
    {
        int filas = Mapa.FILAS;
        int columnas = Mapa.COLUMNAS;

        for (int fila = -grosorAgua; fila < filas + grosorAgua; fila++)
        {
            for (int columna = -grosorAgua; columna < columnas + grosorAgua; columna++)
            {
                bool dentroDelTablero = fila >= 0 && fila < filas && columna >= 0 && columna < columnas;
                if (dentroDelTablero) continue;

                Vector3 posicion = new Vector3(columna * tamañoCelda, -fila * tamañoCelda, 0);
                var celdaGO = new GameObject($"Agua_{fila}_{columna}");
                celdaGO.transform.SetParent(transform);
                celdaGO.transform.position = posicion;
                var sr = celdaGO.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 0;
                AsignarSpriteConRespaldo(sr, ElegirVariante(spritesAgua, fila, columna, salto: 3), new Color(0.20f, 0.45f, 0.75f));
            }
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

        // El sprite del fondo ya quedó fijo desde ConstruirCuadricula; acá
        // solo se restaura su color de reposo y, si corresponde, se aplica
        // el resaltado de selección encima.
        fondoSR.color = coloresBaseFondo[fila, columna];
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
    // las 3 IA siguen "viendo" todo el mapa internamente puertas adentro
    // del Modelo — esto es nada más una capa visual sobre lo que el humano
    // tiene derecho a ver). Se alinea con la misma fórmula de posición que
    // fondo/contenido, así que siempre queda exactamente sobre su celda.
    // ---------------------------------------------------------------

    private void ActualizarNiebla(List<(int fila, int columna, int radio)> fuentesDeVision)
    {
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
    // los distintos usos entre sí (agua, decoración, recursos) para que no
    // elijan siempre el mismo índice en la misma celda.
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
    // momento la vista queda por fuera de todo lo dibujado) a un tono de
    // agua en vez del azul por defecto de Unity. Solo lo toca si la cámara
    // usa un color sólido — si el proyecto ya usa un skybox u otra
    // configuración, no se mete con eso.
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

    // El aldeano elegido desde la lista del HUD (VistaHUD), esperando a que
    // el jugador haga click en una celda con recurso en el mapa. Los
    // Aldeanos no viven en la cuadrícula (no son Unidad ni tienen
    // fila/columna), así que esta selección se maneja aparte de
    // FilaSeleccionada/ColumnaSeleccionada.
    public Aldeano AldeanoSeleccionado { get; set; }
}