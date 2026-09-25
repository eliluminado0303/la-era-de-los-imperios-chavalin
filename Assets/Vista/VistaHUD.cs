using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD de la partida principal. Siempre trabaja sobre el jugador humano
// (índice 0 de ControladorPartida). Partes:
//   - Barra de recursos (oro/madera/comida).
//   - Lista de Aldeanos entrenados: se seleccionan ACÁ, en el HUD, y el
//     siguiente click en el mapa (VistaInput) decide a qué recurso lo mandás.
//   - Panel de la unidad militar seleccionada en el mapa (si hay una).
//   - Botones de acción: Entrenar Aldeano / Construir Cuartel.
//   - NecoArc: guía tutorial que va cambiando de mensaje según tu progreso.
//   - Panel de fin de partida.
public class VistaHUD : MonoBehaviour
{
    public VistaMapa vistaMapa;

    [Header("Textos de recursos")]
    public TextMeshProUGUI textoOro;
    public TextMeshProUGUI textoMadera;
    public TextMeshProUGUI textoComida;

    [Header("Lista de Aldeanos (panel del HUD, no del mapa)")]
    public Transform contenedorAldeanos;   // objeto vacío con un Vertical Layout Group
    public GameObject prefabBotonAldeano;  // prefab: un Button con un TextMeshProUGUI hijo
    private readonly List<(Aldeano aldeano, GameObject boton, TextMeshProUGUI texto)> botonesAldeanos = new List<(Aldeano, GameObject, TextMeshProUGUI)>();

    [Header("Panel de unidad militar seleccionada (opcional)")]
    public GameObject panelSeleccion;
    public TextMeshProUGUI textoSeleccion;

    [Header("NecoArc — guía tutorial")]
    public GameObject panelNecoArc;
    public TextMeshProUGUI textoNecoArc;
    private int pasoTutorialActual = -1;
    private bool tutorialCerrado = false;
    private int? totalRecursosAlTenerPrimerAldeano = null;

    private static readonly string[] MENSAJES_TUTORIAL =
    {
        "Soy NecoArc y sere tu guia.\nPara arrancar, toca \"Entrenar Aldeano\" para iniciar con tu civilizacion.",
        "Muy bien. Ahora elige tu aldeano de la lista, y después haz click en un árbol, una mina o un rebaño del mapa para mandarlo a recolectar.",
        "¡Eso es! Sigue juntando oro y madera. Cuando tengas suficiente, toca \"Construir Cuartel\" para poder entrenar soldados.",
        "¡Buen trabajo! Desde tu Cuartel ya puedes entrenar soldados para defender tu base y salir a explorar. Con esto sabes deberias poder sobrevivir,buranya",
    };

    [Header("Panel de fin de partida")]
    public GameObject panelFinDePartida;
    public TextMeshProUGUI textoResultado;

    private bool avisoReferenciasMostrado;

    void Awake()
    {
        if (vistaMapa == null)
            vistaMapa = FindObjectOfType<VistaMapa>();

        // Recupera referencias básicas si la escena fue guardada antes de que
        // se añadieran estos campos al componente VistaHUD.
        if (textoOro == null) textoOro = BuscarTexto("oro");
        if (textoMadera == null) textoMadera = BuscarTexto("madera");
        if (textoComida == null) textoComida = BuscarTexto("comida");
        if (panelNecoArc == null) panelNecoArc = BuscarObjeto("panelnecoarc");
        if (textoNecoArc == null) textoNecoArc = BuscarTexto("tutorial", "textonecoarc");
    }

    void Update()
    {
        if (vistaMapa == null)
        {
            if (!avisoReferenciasMostrado)
            {
                Debug.LogError("VistaHUD no encuentra un VistaMapa en la escena.");
                avisoReferenciasMostrado = true;
            }
            return;
        }

        if (vistaMapa.Partida == null) return;

        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;

        if (textoOro != null)
            textoOro.text = $"Oro: {jugadorHumano.Recursos[TipoRecurso.Oro]}";
        if (textoMadera != null)
            textoMadera.text = $"Madera: {jugadorHumano.Recursos[TipoRecurso.Madera]}";
        if (textoComida != null)
            textoComida.text = $"Comida: {jugadorHumano.Recursos[TipoRecurso.Comida]}";

        ActualizarListaDeAldeanos(jugadorHumano);
        ActualizarPanelSeleccion(jugadorHumano);
        if (!tutorialCerrado) ActualizarTutorial(jugadorHumano);

        if (panelFinDePartida != null && vistaMapa.Partida.Partida.Finalizada && !panelFinDePartida.activeSelf)
        {
            Jugador ganador = vistaMapa.Partida.Partida.Ganador;
            if (textoResultado != null)
                textoResultado.text = ganador == jugadorHumano ? "¡Ganaste!" : $"Perdiste. Ganó: {(ganador != null ? ganador.Nombre : "nadie")}";
            panelFinDePartida.SetActive(true);
        }
    }

    private GameObject BuscarObjeto(string nombreNormalizado)
    {
        foreach (Transform transformacion in FindObjectsOfType<Transform>(true))
        {
            if (NormalizarNombre(transformacion.name) == nombreNormalizado)
                return transformacion.gameObject;
        }

        return null;
    }

    private TextMeshProUGUI BuscarTexto(params string[] nombresNormalizados)
    {
        foreach (TextMeshProUGUI texto in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            string nombre = NormalizarNombre(texto.name);
            if (nombresNormalizados.Any(candidato => nombre == candidato))
                return texto;
        }

        return null;
    }

    private string NormalizarNombre(string nombre)
    {
        return nombre.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
    }

    private void ActualizarListaDeAldeanos(Jugador jugadorHumano)
    {
        if (contenedorAldeanos == null || prefabBotonAldeano == null) return;

        while (botonesAldeanos.Count < jugadorHumano.Aldeanos.Count)
        {
            var aldeano = jugadorHumano.Aldeanos[botonesAldeanos.Count];
            var botonGO = Instantiate(prefabBotonAldeano, contenedorAldeanos);
            var texto = botonGO.GetComponentInChildren<TextMeshProUGUI>();
            // GetComponentInChildren (no GetComponent): el Button real puede
            // vivir en un hijo del objeto asignado en "Prefab Boton Aldeano"
            // (por ejemplo si ese campo apunta al contenedor exterior y no
            // al botón en sí) — antes esto se rompía con
            // NullReferenceException apenas se entrenaba el primer aldeano,
            // y como el aldeano nunca llegaba a agregarse a la lista, el
            // while de arriba reintentaba la MISMA entrada en cada frame.
            var boton = botonGO.GetComponentInChildren<Button>();
            if (boton == null)
            {
                Debug.LogError("El prefab de \"Prefab Boton Aldeano\" no tiene un componente Button (ni en él ni en sus hijos). Revisá esa referencia en el Inspector de VistaHUD.");
                botonesAldeanos.Add((aldeano, botonGO, texto)); // igual lo contamos: evita el reintento infinito por frame
                continue;
            }
            boton.onClick.AddListener(() => SeleccionarAldeano(aldeano));
            botonesAldeanos.Add((aldeano, botonGO, texto));
        }

        for (int i = 0; i < botonesAldeanos.Count; i++)
        {
            var (aldeano, _, texto) = botonesAldeanos[i];
            if (texto == null) continue;
            bool esElSeleccionado = vistaMapa.AldeanoSeleccionado == aldeano;
            string estado = aldeano.Ocupado ? "Recolectando..." : "Libre";
            texto.text = $"{(esElSeleccionado ? "➤ " : "")}{aldeano.Nombre} {i + 1} — {estado}";
        }
    }

    private void SeleccionarAldeano(Aldeano aldeano)
    {
        if (aldeano.Ocupado)
        {
            Debug.Log($"{aldeano.Nombre} ya está ocupado recolectando.");
            return;
        }
        vistaMapa.AldeanoSeleccionado = vistaMapa.AldeanoSeleccionado == aldeano ? null : aldeano;
    }

    private void ActualizarPanelSeleccion(Jugador jugadorHumano)
    {
        if (panelSeleccion == null) return;

        if (vistaMapa.FilaSeleccionada == null)
        {
            panelSeleccion.SetActive(false);
            return;
        }

        Celda celda = vistaMapa.Partida.Mapa.ObtenerCelda(vistaMapa.FilaSeleccionada.Value, vistaMapa.ColumnaSeleccionada.Value);
        if (celda?.Unidad == null)
        {
            panelSeleccion.SetActive(false);
            return;
        }

        panelSeleccion.SetActive(true);
        if (textoSeleccion != null)
            textoSeleccion.text = $"{celda.Unidad.Civilizacion}\nVida: {celda.Unidad.Vida}/{celda.Unidad.VidaMaxima}\nAtaque: {celda.Unidad.Ataque}";
    }

    private void ActualizarTutorial(Jugador jugadorHumano)
    {
        int pasoNuevo = CalcularPasoTutorial(jugadorHumano);
        if (pasoNuevo == pasoTutorialActual) return;

        pasoTutorialActual = pasoNuevo;
        if (panelNecoArc != null) panelNecoArc.SetActive(true);
        if (textoNecoArc != null) textoNecoArc.text = MENSAJES_TUTORIAL[pasoTutorialActual];
    }

    private int CalcularPasoTutorial(Jugador jugadorHumano)
    {
        bool tieneCuartel = jugadorHumano.Edificios.OfType<EdificioEntrenamiento>().Any();
        if (tieneCuartel) return 3;

        if (jugadorHumano.Aldeanos.Count == 0) return 0;

        if (totalRecursosAlTenerPrimerAldeano == null)
            totalRecursosAlTenerPrimerAldeano = TotalRecursos(jugadorHumano);

        bool yaRecolecto = TotalRecursos(jugadorHumano) > totalRecursosAlTenerPrimerAldeano.Value;
        return yaRecolecto ? 2 : 1;
    }

    private int TotalRecursos(Jugador jugadorHumano) =>
        jugadorHumano.Recursos[TipoRecurso.Oro] + jugadorHumano.Recursos[TipoRecurso.Madera] + jugadorHumano.Recursos[TipoRecurso.Comida];

    public void CerrarTutorial()
    {
        tutorialCerrado = true;
        if (panelNecoArc != null) panelNecoArc.SetActive(false);
    }

    public void EntrenarAldeano()
    {
        var controladorEntrenamiento = vistaMapa.Partida.ControladoresEntrenamiento[0];
        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;
        var centroUrbano = jugadorHumano.Edificios.OfType<EdificioPrincipal>().FirstOrDefault();
        if (centroUrbano == null) { Debug.Log("No tienes Centro Urbano."); return; }

        if (!controladorEntrenamiento.SolicitarAldeano(centroUrbano))
            Debug.Log("No se pudo entrenar aldeano (¿recursos insuficientes?)");
    }

    public void ConstruirCuartel()
    {
        var controladorMapaHumano = vistaMapa.Partida.ControladoresMapa[0];
        Jugador jugadorHumano = controladorMapaHumano.Jugador;
        string civilizacion = vistaMapa.CivilizacionHumano;

        var centroUrbano = jugadorHumano.Edificios.OfType<EdificioPrincipal>().FirstOrDefault();
        if (centroUrbano == null || !BuscarPosicionDelEdificio(centroUrbano, out int filaBase, out int columnaBase))
        {
            Debug.Log("No se encontró tu Centro Urbano.");
            return;
        }

        if (!BuscarCeldaLibreCerca(filaBase, columnaBase, out int fila, out int columna))
        {
            Debug.Log("No hay espacio libre cerca de tu Centro Urbano para el cuartel.");
            return;
        }

        var unidadesDisponibles = UnidadesDeCivilizacion(civilizacion);
        var edificio = new EdificioEntrenamiento(civilizacion, costoOro: 150, costoMadera: 100, costoComida: 0, unidadesDisponibles);

        if (!controladorMapaHumano.SolicitarConstruccion(fila, columna, edificio))
            Debug.Log("No se pudo construir (¿recursos insuficientes o celda ocupada?)");
    }

    private bool BuscarPosicionDelEdificio(Edificio edificio, out int fila, out int columna)
    {
        var mapa = vistaMapa.Partida.Mapa;
        for (int f = 0; f < Mapa.FILAS; f++)
        {
            for (int c = 0; c < Mapa.COLUMNAS; c++)
            {
                if (mapa.ObtenerCelda(f, c).Edificio == edificio) { fila = f; columna = c; return true; }
            }
        }
        fila = columna = 0;
        return false;
    }

    private bool BuscarCeldaLibreCerca(int filaBase, int columnaBase, out int fila, out int columna)
    {
        var mapa = vistaMapa.Partida.Mapa;
        (int deltaFila, int deltaColumna)[] posicionesRelativas =
        {
            (2, 2), (2, -2), (-2, 2), (-2, -2), (0, 3), (3, 0), (0, -3), (-3, 0)
        };

        foreach (var (deltaFila, deltaColumna) in posicionesRelativas)
        {
            int f = filaBase + deltaFila;
            int c = columnaBase + deltaColumna;
            if (mapa.EsPosicionValida(f, c) && mapa.CeldaLibre(f, c)) { fila = f; columna = c; return true; }
        }

        fila = columna = 0;
        return false;
    }

    private List<string> UnidadesDeCivilizacion(string civilizacion)
    {
        string exclusiva = civilizacion == "Nipones" ? "Assassin"
                          : civilizacion == "Griegos" ? "Avenger"
                          : civilizacion == "Vikingos" ? "Berserker"
                          : "Caster";

        return new List<string> { exclusiva, "Defender", "Vanguard", "Ranger", "Healer", "NecoArc" };
    }

    public void VolverAlMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}