using System.Collections;
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
    private readonly HashSet<Unidad> unidadesEnTransito = new HashSet<Unidad>();
    private readonly HashSet<Aldeano> aldeanosEnTransito = new HashSet<Aldeano>();
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

    [System.Serializable]
    public class VisualEdificio
    {
        [Tooltip("Nombre exacto de la clase del edificio: EdificioPrincipal, EdificioEntrenamiento, TorreDefensa, Casa, Granja o Muro.")]
        public string tipoEdificio;
        public Sprite sprite;
    }

    [Header("Sprites de edificios")]
    public VisualEdificio[] edificios = new VisualEdificio[0];

    [Header("Unidades")]
    public float tamañoCelda = 1f;

    [System.Serializable]
    public class AnimacionAdicional
    {
        [Tooltip("Nombre para identificar esta animación adicional, por ejemplo Especial, Herida o Ataque 2.")]
        public string nombre;
        public RuntimeAnimatorController animator;
    }

    [System.Serializable]
    public class ConjuntoAnimacion
    {
        [Tooltip("Nombre exacto de la clase C#: Aldeano, Assassin, Avenger, Berserker, Caster, Defender, Gilgamesh, Godzilla, Healer, Jormungandr, Medusa, NecoArc, Ranger o Vanguard.")]
        public string tipoUnidad;
        [Tooltip("Sprite propio de esta unidad; se usa si no tiene un Animator asignado.")]
        public Sprite sprite;
        public RuntimeAnimatorController idle;
        public RuntimeAnimatorController caminar;
        public RuntimeAnimatorController atacar;
        public RuntimeAnimatorController habilidadEspecial;
        public RuntimeAnimatorController morir;
        [Tooltip("Asigna aquí otras animaciones/controller del personaje, como habilidades, ataques alternativos o caminar en otras direcciones.")]
        public AnimacionAdicional[] animacionesAdicionales;
    }

    [Header("Animator por unidad (usa los controllers de New Folder)")]
    public ConjuntoAnimacion[] conjuntosDeAnimacion;
    [Tooltip("Cuánto se mantiene la animación de Caminar después de detectar que la unidad cambió de celda.")]
    public float duracionCaminar = 0.35f;
    [Tooltip("Cuánto dura la animación de Atacar antes de volver a Idle.")]
    public float duracionAtacar = 0.5f;
    [Tooltip("Velocidad global de todas las animaciones (1 = normal, 2 = el doble de rápido).")]
    public float velocidadAnimaciones = 1f;
    [Tooltip("Cuánto dura la animación del Animator asignada a la habilidad especial.")]
    public float duracionHabilidadEspecial = 0.8f;
    [Tooltip("Velocidad del apagado gradual al morir.")]
    public float velocidadFadeMuerte = 2f;

    [Header("Decoración estética (sin efecto en el juego — arbustos, piedras sueltas, etc.)")]
    public Sprite[] spritesDecoracion;
    public Sprite spriteFlecha;
    [Range(0f, 0.3f)] public float densidadDecoracion = 0.04f;

    [Header("Niebla de guerra (solo afecta lo que VE el jugador humano)")]
    public Sprite spriteNiebla; // opcional — si lo dejás vacío usa un cuadrado de color
    public Color colorNieblaSinExplorar = Color.black;
    public Color colorNieblaExploradaSinVision = new Color(0f, 0f, 0f, 0.6f);
    public int radioVisionUnidad = 5;
    public int radioVisionEdificio = 8; // un poco más grande que antes (7), para que el hueco inicial alrededor del Centro Urbano alcance para construir cómodo

    [Header("Efectos de habilidades (arrastrá los sprites que ya tenés)")]
    public Sprite spriteRayoGilgamesh;         // remate de Enuma Elish
    public Sprite spriteBolaDeFuego;           // proyectil del ataque a distancia del Caster
    public Sprite spriteEmbestidaMedusa;       // Medusa cargando con Bellerofonte
    public Sprite spriteMordiscoJormungandr;   // mordisco de área

    public ControladorPartida Partida { get; private set; }

    private SpriteRenderer[,] fondos;
    private Color[,] coloresBaseFondo; // color "de reposo" de cada fondo, antes del resaltado por selección
    private SpriteRenderer[,] decoraciones;
    private SpriteRenderer[,] contenidos;
    private SpriteRenderer[,] aldeanosSR; // capa aparte: un Aldeano puede compartir celda con un recurso/edificio
    private SpriteRenderer[,] niebla;
    private bool[,] explorado;

    // Estado de animación por unidad (no por celda: cuando una unidad se
    // mueve de celda A a B, sigue siendo la MISMA unidad, y acá es donde
    // vive su "recuerdo" de en qué animación está — la grilla de
    // SpriteRenderers se reutiliza por posición, así que el estado no puede
    // vivir ahí).
    private class EstadoAnimacionUnidad
    {
        public int filaAnterior = int.MinValue, columnaAnterior = int.MinValue;
        public float tiempoRestanteCaminar;
        public float tiempoRestanteAtacar;
        public float tiempoRestanteHabilidadEspecial;
        public float alphaMuerte = 1f;
        public bool ataquePendiente;
        public bool habilidadEspecialPendiente;
        public bool reiniciarAnimacion; // true cuando empieza un ataque/habilidad nuevo: hay que volver al primer frame
    }
    private readonly Dictionary<Unidad, EstadoAnimacionUnidad> estadosDeAnimacion = new Dictionary<Unidad, EstadoAnimacionUnidad>();
    private readonly Dictionary<SpriteRenderer, Animator> animadoresPorContenido = new Dictionary<SpriteRenderer, Animator>();
    private readonly HashSet<string> advertenciasVisualesFaltantes = new HashSet<string>();

    // Mismo patrón que EstadoAnimacionUnidad, pero para Aldeano (que no es
    // Unidad: no tiene Vida ni eventos de ataque, así que no puede pasar
    // por DibujarUnidadAnimada). Solo necesita Idle/Caminar.
    private class EstadoAnimacionAldeano
    {
        public int filaAnterior = int.MinValue, columnaAnterior = int.MinValue;
        public float tiempoRestanteCaminar;
    }
    private readonly Dictionary<Aldeano, EstadoAnimacionAldeano> estadosDeAnimacionAldeano = new Dictionary<Aldeano, EstadoAnimacionAldeano>();

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
        // Cada vez que un aldeano (de cualquier civilización) empieza a
        // desplazarse, se dibuja un doble que se desliza hasta el destino.
        foreach (var controladorMapa in Partida.ControladoresMapa)
            controladorMapa.AldeanoEmpezoAMoverse += IniciarMovimientoVisualAldeano;

        // Cuando una unidad del humano camina hacia un enemigo para atacarlo,
        // se dibuja deslizándose igual que cuando la mueves a mano; y si es
        // a distancia, el proyectil sale al ejecutarse el ataque.
        var combateHumano = Partida.ControladoresCombate[0];
        combateHumano.UnidadSeAcerca += (unidad, fo, co, fd, cd) => IniciarMovimientoVisual(unidad, fo, co, fd, cd);
        combateHumano.AtaqueDiferidoRealizado += AlAtaqueDiferido;
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
    camara.orthographicSize = 9f; // ajustá a gusto — VistaCamara ya deja hacer zoom in/out desde acá con la rueda
    }

    void Update()
    {
        if (Partida == null) return; // todavía no se eligió civilización

        // La unidad que tenés seleccionada ahora mismo (si hay alguna) no
        // debe empezar a deambular sola justo cuando estás por darle una
        // orden — ver ControladorDeambulacion.
        Unidad unidadSeleccionada = null;
        if (FilaSeleccionada != null)
            unidadSeleccionada = Partida.Mapa.ObtenerCelda(FilaSeleccionada.Value, ColumnaSeleccionada.Value)?.Unidad;

        // Si el aldeano elegido en el HUD murió, se suelta la selección.
        if (AldeanoSeleccionado != null && !AldeanoSeleccionado.EstaVivo)
            AldeanoSeleccionado = null;

        bool termino = Partida.Actualizar(Time.deltaTime, unidadSeleccionada, AldeanoSeleccionado);
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
        aldeanosSR = new SpriteRenderer[filas, columnas];
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

                var aldeanoGO = new GameObject($"Aldeano_{fila}_{columna}");
                aldeanoGO.transform.SetParent(transform);
                aldeanoGO.transform.position = posicion;
                var aldeanoSR = aldeanoGO.AddComponent<SpriteRenderer>();
                aldeanoSR.sortingOrder = 3; // por encima del contenido: puede compartir celda con un recurso/edificio
                aldeanoSR.enabled = false;
                aldeanosSR[fila, columna] = aldeanoSR;

                var nieblaGO = new GameObject($"Niebla_{fila}_{columna}");
                nieblaGO.transform.SetParent(transform);
                nieblaGO.transform.position = posicion;
                var nieblaSR = nieblaGO.AddComponent<SpriteRenderer>();
                nieblaSR.sortingOrder = 4; // por encima de todo, incluido el aldeano
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

                if (celda.Aldeano != null && jugadorHumano.Aldeanos.Contains(celda.Aldeano))
                    fuentesDeVision.Add((fila, columna, radioVisionUnidad));
            }
        }

        ActualizarNiebla(fuentesDeVision);
    }

    private void RedibujarCelda(int fila, int columna, Celda celda)
    {
        var fondoSR = fondos[fila, columna];
        var contenidoSR = contenidos[fila, columna];
        var aldeanoSR = aldeanosSR[fila, columna];
        if (celda.Unidad == null)
            DesactivarAnimador(contenidoSR);
        if (celda.Aldeano == null)
            DesactivarAnimador(aldeanoSR);

        // El sprite del fondo ya quedó fijo desde ConstruirCuadricula; acá
        // solo se restaura su color de reposo y, si corresponde, se aplica
        // el resaltado de selección encima.
        fondoSR.color = coloresBaseFondo[fila, columna];
        bool estaSeleccionada = FilaSeleccionada == fila && ColumnaSeleccionada == columna;
        if (estaSeleccionada) fondoSR.color = Color.Lerp(fondoSR.color, Color.white, 0.6f);

        // El Aldeano se dibuja en SU PROPIA capa (ver aldeanosSR), no acá:
        // según Mapa.ColocarAldeano, un Aldeano puede compartir la celda con
        // un recurso o un edificio sin bloquear nada, así que necesita
        // poder dibujarse ENCIMA de cualquiera de los dos casos de abajo.
        if (celda.Aldeano != null && aldeanosEnTransito.Contains(celda.Aldeano))
        {
            // Sigue en su celda de origen en el Modelo mientras dura el
            // desplazamiento: se oculta y lo representa el doble deslizante.
            aldeanoSR.enabled = false;
            DesactivarAnimador(aldeanoSR);
        }
        else if (celda.Aldeano != null)
        {
            aldeanoSR.enabled = true;
            DibujarAldeanoAnimado(aldeanoSR, celda.Aldeano, fila, columna);
        }
        else
        {
            aldeanoSR.enabled = false;
        }

        // El contenido muestra, en orden de prioridad, lo primero que haya:
        // unidad > edificio > recurso (nunca coinciden dos a la vez en la
        // misma celda, así que el orden no genera conflicto real).
        if (celda.Unidad != null)
        {
            if (unidadesEnTransito.Contains(celda.Unidad))
            {
                contenidoSR.enabled = false;
                DesactivarAnimador(contenidoSR);
            }
            else
            {
                contenidoSR.enabled = true;
                DibujarUnidadAnimada(contenidoSR, celda.Unidad, fila, columna);
            }
        }
        else if (celda.Edificio != null)
        {
            contenidoSR.sprite = ObtenerSpriteEdificio(celda.Edificio);
            contenidoSR.enabled = contenidoSR.sprite != null;
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

    // ---------------------------------------------------------------
    // Los controladores tienen una animación cada uno; la vista cambia el
    // controller del Animator según el movimiento o ataque de la unidad.
    // ---------------------------------------------------------------

    private void DibujarUnidadAnimada(SpriteRenderer contenidoSR, Unidad unidad, int fila, int columna)
    {
        if (!estadosDeAnimacion.TryGetValue(unidad, out var estado))
        {
            estado = new EstadoAnimacionUnidad { filaAnterior = fila, columnaAnterior = columna };
            estadosDeAnimacion[unidad] = estado;
            unidad.RealizoAtaque += AlRealizarAtaque;
            unidad.RealizoHabilidadEspecial += AlRealizarHabilidadEspecial;
        }

        // ¿Cambió de celda desde el frame anterior? Dispara Caminar.
        if (estado.filaAnterior != fila || estado.columnaAnterior != columna)
        {
            estado.tiempoRestanteCaminar = duracionCaminar;
            estado.filaAnterior = fila;
            estado.columnaAnterior = columna;
        }

        // El Modelo notifica el ataque mediante el evento RealizoAtaque.
        // La vista solo consume la notificación para iniciar su animación.
        if (estado.ataquePendiente)
        {
            // La duración sale del clip real (antes era un 0.5s fijo que cortaba los combos de varios golpes).
            estado.tiempoRestanteAtacar = DuracionDe(ObtenerConjuntoDeAnimacion(unidad.GetType().Name)?.atacar, duracionAtacar);
            estado.reiniciarAnimacion = true;
            estado.ataquePendiente = false;
        }
        if (estado.habilidadEspecialPendiente)
        {
            estado.tiempoRestanteHabilidadEspecial = DuracionDe(ObtenerConjuntoDeAnimacion(unidad.GetType().Name)?.habilidadEspecial, duracionHabilidadEspecial);
            estado.reiniciarAnimacion = true;
            estado.habilidadEspecialPendiente = false;
        }

        string estadoActual = estado.tiempoRestanteHabilidadEspecial > 0 ? "HabilidadEspecial"
                             : estado.tiempoRestanteAtacar > 0 ? "Atacar"
                             : estado.tiempoRestanteCaminar > 0 ? "Caminar"
                             : "Idle";

        estado.tiempoRestanteCaminar = Mathf.Max(0, estado.tiempoRestanteCaminar - Time.deltaTime);
        estado.tiempoRestanteAtacar = Mathf.Max(0, estado.tiempoRestanteAtacar - Time.deltaTime);
        estado.tiempoRestanteHabilidadEspecial = Mathf.Max(0, estado.tiempoRestanteHabilidadEspecial - Time.deltaTime);

        string tipoUnidad = unidad.GetType().Name;
        var conjunto = ObtenerConjuntoDeAnimacion(tipoUnidad);
        RuntimeAnimatorController controlador = null;
        if (conjunto != null)
        {
            if (unidad.Vida <= 0) controlador = conjunto.morir;
            if (controlador == null && estadoActual == "HabilidadEspecial") controlador = conjunto.habilidadEspecial;
            if (controlador == null && estadoActual == "Atacar") controlador = conjunto.atacar;
            if (controlador == null && estadoActual == "Caminar") controlador = conjunto.caminar;
            if (controlador == null) controlador = conjunto.idle;
        }

        if (controlador != null)
        {
            contenidoSR.enabled = true;
            ActivarAnimador(contenidoSR, controlador);

            // Un ataque nuevo mientras ya se estaba en "Atacar" no cambia el controller, así que
            // el Animator seguiría donde iba: se lo devuelve al primer frame.
            if (estado.reiniciarAnimacion)
            {
                estado.reiniciarAnimacion = false;
                if (animadoresPorContenido.TryGetValue(contenidoSR, out var animadorAReiniciar))
                {
                    animadorAReiniciar.Rebind();
                    animadorAReiniciar.Update(0f);
                }
            }
        }
        else
        {
            DesactivarAnimador(contenidoSR);
            contenidoSR.sprite = conjunto != null ? conjunto.sprite : null;
            contenidoSR.enabled = contenidoSR.sprite != null;
            if (contenidoSR.sprite == null)
                AvisarVisualFaltante($"unidad:{tipoUnidad}",
                    conjunto == null
                        ? $"VistaMapa no tiene una configuración de animación para la unidad {tipoUnidad}."
                        : $"La unidad {tipoUnidad} no tiene Animator ni sprite propio asignado en VistaMapa.");
        }

        

        Color color = DebeColorearse(unidad) ? ColorDeCivilizacion(unidad.Civilizacion) : Color.white;
        if (unidad.Vida <= 0)
        {
            // Fade-out simple en vez de una animación de muerte real.
            estado.alphaMuerte = Mathf.Max(0, estado.alphaMuerte - Time.deltaTime * velocidadFadeMuerte);
            color.a = estado.alphaMuerte;
        }
        contenidoSR.color = color;
    }

    // Mismo patrón que DibujarUnidadAnimada, pero simplificado: Aldeano no
    // tiene Vida ni eventos de ataque/habilidad, así que solo maneja
    // Idle/Caminar. Buscá una entrada con tipoUnidad = "Aldeano" en
    // "Conjuntos De Animacion" para que esto tenga algo que mostrar.
    private void DibujarAldeanoAnimado(SpriteRenderer sr, Aldeano aldeano, int fila, int columna)
    {
        if (!estadosDeAnimacionAldeano.TryGetValue(aldeano, out var estado))
        {
            estado = new EstadoAnimacionAldeano { filaAnterior = fila, columnaAnterior = columna };
            estadosDeAnimacionAldeano[aldeano] = estado;
        }

        if (estado.filaAnterior != fila || estado.columnaAnterior != columna)
        {
            estado.tiempoRestanteCaminar = duracionCaminar;
            estado.filaAnterior = fila;
            estado.columnaAnterior = columna;
        }
        estado.tiempoRestanteCaminar = Mathf.Max(0, estado.tiempoRestanteCaminar - Time.deltaTime);

        bool caminando = estado.tiempoRestanteCaminar > 0;

        var conjunto = ObtenerConjuntoDeAnimacion("Aldeano");
        RuntimeAnimatorController controlador = null;
        if (conjunto != null)
        {
            if (caminando) controlador = conjunto.caminar;
            if (controlador == null) controlador = conjunto.idle;
        }

        if (controlador != null)
        {
            sr.enabled = true;
            ActivarAnimador(sr, controlador);
        }
        else
        {
            DesactivarAnimador(sr);
            sr.sprite = conjunto != null ? conjunto.sprite : null;
            sr.enabled = sr.sprite != null;
            if (sr.sprite == null)
                AvisarVisualFaltante("unidad:Aldeano",
                    conjunto == null
                        ? "VistaMapa no tiene una configuración de animación para Aldeano (agregá una entrada con tipoUnidad = \"Aldeano\" en Conjuntos De Animacion)."
                        : "Aldeano no tiene Animator ni sprite propio asignado en VistaMapa.");
        }

        sr.color = ColorDeCivilizacion(aldeano.Civilizacion);
    }

        // Héroes y unidades exclusivas de civilización ya tienen su propio
    // sprite distintivo — teñirlos washaría su arte. Solo se colorea lo
    // "genérico" (Defender, Vanguard, Ranger, Healer).
    private bool DebeColorearse(Unidad unidad)
        => !(unidad is Heroe) && !(unidad is Assassin) && !(unidad is Avenger) && !(unidad is Berserker) && !(unidad is Caster);

    private void AlRealizarAtaque(Unidad unidad, int cantidadDeGolpes, List<Unidad> objetivos)
    {
        if (estadosDeAnimacion.TryGetValue(unidad, out var estado))
            estado.ataquePendiente = true;

        // El Caster es a distancia (Rango 7) — su ataque básico se ve como
        // una bola de fuego viajando hasta el objetivo, no como un golpe
        // cuerpo a cuerpo.
        if (unidad is Caster && objetivos != null && objetivos.Count > 0)
            LanzarProyectilCaster(unidad, objetivos[0]);
    }

    private void AlRealizarHabilidadEspecial(Unidad unidad, int cantidadDeGolpes, List<Unidad> objetivos)
    {
        if (estadosDeAnimacion.TryGetValue(unidad, out var estado))
            estado.habilidadEspecialPendiente = true;

        if (unidad is Gilgamesh) LanzarEfectoGilgamesh(unidad, objetivos);
        else if (unidad is Medusa) LanzarEfectoMedusa(unidad, objetivos);
        else if (unidad is Jormungandr) LanzarEfectoJormungandr(unidad, objetivos);
        // Godzilla y NecoArc quedan afuera por ahora — no los pediste esta
        // vez. EfectoRayo ya está pensado para poder reusarse en el
        // aliento de Godzilla el día que quieras sumarlo (mismo patrón que
        // Gilgamesh, cambiando el sprite y el color).
    }

    // ---------------------------------------------------------------
    // Efectos visuales por habilidad. Cada uno es "mejor esfuerzo": si le
    // falta el sprite en el Inspector, o la unidad/objetivo todavía no
    // tienen una posición válida en el mapa (Fila/Columna en -1), no hace
    // nada — nunca rompe el combate por un tema puramente visual.
    // ---------------------------------------------------------------

    private Vector3 PosicionMundoDeUnidad(Unidad unidad)
        => new Vector3(unidad.Columna * tamañoCelda, -unidad.Fila * tamañoCelda, 0);

    private Vector3 PromedioDePosiciones(List<Unidad> unidades, Vector3 porDefecto)
    {
        Vector3 suma = Vector3.zero;
        int cantidad = 0;
        foreach (var u in unidades)
        {
            if (u == null || u.Fila < 0 || u.Columna < 0) continue;
            suma += PosicionMundoDeUnidad(u);
            cantidad++;
        }
        return cantidad > 0 ? suma / cantidad : porDefecto;
    }

    // Caster: bola de fuego viajando desde el Caster hasta el objetivo.
    private void LanzarProyectilCaster(Unidad unidad, Unidad objetivo)
    {
        if (spriteBolaDeFuego == null) return;
        if (unidad.Fila < 0 || objetivo == null || objetivo.Fila < 0) return;

        Proyectil.Disparar(PosicionMundoDeUnidad(unidad), PosicionMundoDeUnidad(objetivo), spriteBolaDeFuego, duracion: 0.35f);
    }

    // Gilgamesh: el rayo cae como REMATE — recién cuando termina el combo
    // de golpes de la animación de HabilidadEspecial, no al mismo tiempo
    // que arranca. Largo y ancho salen de AreaHabilidad (Enuma Elish es
    // FormaArea.Linea), así que si algún día cambiás esos números en
    // Gilgamesh.cs, el efecto se ajusta solo.
    private void LanzarEfectoGilgamesh(Unidad unidad, List<Unidad> objetivos)
    {
        if (spriteRayoGilgamesh == null) return;
        if (unidad.Fila < 0 || objetivos == null || objetivos.Count == 0) return;

        StartCoroutine(RemateGilgamesh(unidad, objetivos[0]));
    }

    // Llamado desde VistaInput cuando se pide un movimiento: oculta la
    // unidad real en su celda de origen (sigue ahí en el Modelo durante 1s)
    // y crea el doble que viaja visualmente hasta el destino.
    public void IniciarMovimientoVisual(Unidad unidad, int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, float duracion = -1f)
    {
        if (duracion <= 0f) duracion = GestorMovimiento.DuracionSegundos(unidad); // mismo tiempo que el Modelo
        unidadesEnTransito.Add(unidad);

        Vector3 origen = new Vector3(columnaOrigen * tamañoCelda, -filaOrigen * tamañoCelda, 0);
        Vector3 destino = new Vector3(columnaDestino * tamañoCelda, -filaDestino * tamañoCelda, 0);

        var conjunto = ObtenerConjuntoDeAnimacion(unidad.GetType().Name);
        Color color = DebeColorearse(unidad) ? ColorDeCivilizacion(unidad.Civilizacion) : Color.white;

        UnidadEnTransito.Crear(origen, destino, conjunto?.caminar, conjunto?.sprite, color, duracion);
        StartCoroutine(QuitarDeTransitoLuegoDe(unidad, duracion));
    }

    // Igual que IniciarMovimientoVisual pero para un Aldeano. Lo dispara
    // ControladorMapa.AldeanoEmpezoAMoverse, así que funciona tanto para
    // una orden del jugador como para cuando el aldeano deambula solo.
    public void IniciarMovimientoVisualAldeano(Aldeano aldeano, int filaOrigen, int columnaOrigen, int filaDestino, int columnaDestino, float duracion)
    {
        // duracion = lo que tarda GestorMovimiento en aplicar el desplazamiento
        aldeanosEnTransito.Add(aldeano);

        Vector3 origen = new Vector3(columnaOrigen * tamañoCelda, -filaOrigen * tamañoCelda, 0);
        Vector3 destino = new Vector3(columnaDestino * tamañoCelda, -filaDestino * tamañoCelda, 0);

        var conjunto = ObtenerConjuntoDeAnimacion("Aldeano");
        UnidadEnTransito.Crear(origen, destino, conjunto?.caminar, conjunto?.sprite, ColorDeCivilizacion(aldeano.Civilizacion), duracion + 0.1f, 3); // termina justo cuando el aldeano real reaparece
        StartCoroutine(QuitarAldeanoDeTransitoLuegoDe(aldeano, duracion));
    }

    private void AlAtaqueDiferido(Unidad atacante, int filaObjetivo, int columnaObjetivo)
    {
        if (atacante == null || atacante.Rango <= 1 || spriteFlecha == null) return;
        Proyectil.Disparar(PosicionMundoDeUnidad(atacante), new Vector3(columnaObjetivo * tamañoCelda, -filaObjetivo * tamañoCelda, 0), spriteFlecha);
    }

    private IEnumerator QuitarAldeanoDeTransitoLuegoDe(Aldeano aldeano, float duracion)
    {
        // Un poco más que la duración del deslizamiento: el Modelo aplica
        // el movimiento justo al cumplirse el segundo, y así el aldeano
        // real no reaparece un frame en la celda vieja.
        yield return new WaitForSeconds(duracion + 0.1f);
        aldeanosEnTransito.Remove(aldeano);
    }

    private IEnumerator QuitarDeTransitoLuegoDe(Unidad unidad, float duracion)
    {
        yield return new WaitForSeconds(duracion);
        unidadesEnTransito.Remove(unidad);
    }

    private IEnumerator RemateGilgamesh(Unidad unidad, Unidad objetivo)
    {
        yield return new WaitForSeconds(duracionHabilidadEspecial * 0.7f);
        if (unidad.Fila < 0) yield break; // pudo haber muerto/desaparecido mientras esperábamos

        Vector3 origen = PosicionMundoDeUnidad(unidad);
        Vector3 direccion = objetivo != null && objetivo.Fila >= 0
            ? (PosicionMundoDeUnidad(objetivo) - origen)
            : Vector3.right;
        if (direccion.sqrMagnitude < 0.0001f) direccion = Vector3.right;
        direccion.Normalize();

        var heroe = unidad as Heroe;
        float longitud = (heroe != null && heroe.AreaHabilidad != null) ? heroe.AreaHabilidad.Tamaño : 8f;
        float ancho = (heroe != null && heroe.AreaHabilidad != null) ? heroe.AreaHabilidad.Ancho : 1.5f;

        EfectoRayo.Crear(origen, direccion, longitud * tamañoCelda, ancho * tamañoCelda, Color.white, duracion: 0.4f, sprite: spriteRayoGilgamesh);
    }

    // Medusa: una única embestida con Bellerofonte viajando desde Medusa
    // hasta el centro del grupo de objetivos — el efecto mecánico
    // (aturdimiento + reducción de defensa) ya lo aplica
    // Medusa.HabilidadEspecial() en el Modelo, esto es solo la parte visual.
    private void LanzarEfectoMedusa(Unidad unidad, List<Unidad> objetivos)
    {
        if (spriteEmbestidaMedusa == null) return;
        if (unidad.Fila < 0 || objetivos == null || objetivos.Count == 0) return;

        Vector3 origen = PosicionMundoDeUnidad(unidad);
        Vector3 destino = PromedioDePosiciones(objetivos, origen);
        Proyectil.Disparar(origen, destino, spriteEmbestidaMedusa, duracion: 0.4f);
    }

    // Jormungandr: un mordisco EN CADA objetivo del área (no uno solo en
    // el centro), para que se sienta como que muerde a todo el grupo.
    // Mismo criterio que Medusa: el veneno + ralentización ya los aplica
    // Jormungandr.HabilidadEspecial() en el Modelo.
    private void LanzarEfectoJormungandr(Unidad unidad, List<Unidad> objetivos)
    {
        if (spriteMordiscoJormungandr == null || objetivos == null) return;

        foreach (var objetivo in objetivos)
        {
            if (objetivo == null || objetivo.Fila < 0) continue;
            EfectoCuracion.Crear(PosicionMundoDeUnidad(objetivo), Color.white, duracion: 0.35f, sprite: spriteMordiscoJormungandr);
        }
    }

    private void OnDestroy()
    {
        foreach (var unidad in estadosDeAnimacion.Keys)
        {
            unidad.RealizoAtaque -= AlRealizarAtaque;
            unidad.RealizoHabilidadEspecial -= AlRealizarHabilidadEspecial;
        }
    }

    // Busca solo la configuración del tipo exacto; una unidad nunca hereda
    // la animación de otra.
    private ConjuntoAnimacion ObtenerConjuntoDeAnimacion(string tipoUnidad)
    {
        if (conjuntosDeAnimacion == null) return null;

        foreach (var conjunto in conjuntosDeAnimacion)
            if (conjunto != null && conjunto.tipoUnidad == tipoUnidad) return conjunto;
        return null;
    }

    private Sprite ObtenerSpriteEdificio(Edificio edificio)
    {
        if (edificios != null)
        {
            string tipoEdificio = edificio.GetType().Name;
            foreach (var visual in edificios)
                if (visual != null && visual.tipoEdificio == tipoEdificio && visual.sprite != null)
                    return visual.sprite;
        }

        string tipo = edificio.GetType().Name;
        AvisarVisualFaltante($"edificio:{tipo}",
            $"El edificio {tipo} no tiene un sprite propio asignado en VistaMapa.");
        return null;
    }

    private void AvisarVisualFaltante(string clave, string mensaje)
    {
        if (advertenciasVisualesFaltantes.Add(clave))
            Debug.LogWarning(mensaje, this);
    }

    // Duración real del clip más largo del controller (ajustada por velocidadAnimaciones).
    private float DuracionDe(RuntimeAnimatorController controlador, float respaldo)
    {
        if (controlador == null) return respaldo;
        float max = 0f;
        foreach (var clip in controlador.animationClips) max = Mathf.Max(max, clip.length);
        if (max <= 0f) return respaldo;
        return max / Mathf.Max(0.01f, velocidadAnimaciones);
    }

    private void ActivarAnimador(SpriteRenderer renderer, RuntimeAnimatorController controlador)
    {
        if (!animadoresPorContenido.TryGetValue(renderer, out var animator))
        {
            animator = renderer.gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animadoresPorContenido[renderer] = animator;
        }

        animator.enabled = true;
        animator.speed = velocidadAnimaciones;
        if (animator.runtimeAnimatorController != controlador)
            animator.runtimeAnimatorController = controlador;
    }

    private void DesactivarAnimador(SpriteRenderer renderer)
    {
        if (animadoresPorContenido.TryGetValue(renderer, out var animator))
            animator.enabled = false;
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
    public bool ModoHabilidadEspecial { get; set; }

    // El edificio que el jugador acaba de "comprar" desde el HUD (Cuartel,
    // etc.) y que está esperando a que se elija dónde colocarlo con un
    // click en el mapa. El Centro Urbano NO pasa por acá: ese se coloca
    // automáticamente al arrancar la partida (ver ControladorPartida).
    public Edificio EdificioPendienteDeColocar { get; set; }

    // Zona de construcción: además de que la celda esté libre y sea Tierra
    // (eso ya lo valida el Modelo), el jugador humano solo puede construir
    // en una celda que ya haya explorado — no tiene sentido plantar un
    // edificio en medio de la niebla que todavía no vio.
    public bool EstaExplorada(int fila, int columna)
    {
        if (explorado == null) return false;
        if (fila < 0 || fila >= explorado.GetLength(0) || columna < 0 || columna >= explorado.GetLength(1)) return false;
        return explorado[fila, columna];
    }
}