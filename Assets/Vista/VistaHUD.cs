using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD de la partida principal. Siempre trabaja sobre el jugador humano
// (índice 0 de ControladorPartida). Partes:
//   - Barra de recursos (oro/madera/comida/población).
//   - Lista de Aldeanos entrenados: se seleccionan ACÁ, en el HUD, y el
//     siguiente click en el mapa (VistaInput) decide a qué recurso lo mandás.
//   - Lista de Unidades militares: mismo patrón que la de Aldeanos, para
//     seleccionar desde el HUD en vez de clickear el mapa.
//   - Lista DESPLEGABLE de Construcción: un botón "Construir" que abre un
//     panel con una fila por cada estructura disponible, con su costo y un
//     botón. Al tocar una fila, NO se construye todavía: se deja el
//     edificio "pendiente" (vistaMapa.EdificioPendienteDeColocar) y es el
//     siguiente click sobre el mapa (en VistaInput) el que elige dónde y
//     dispara la construcción de verdad.
//   - Lista DESPLEGABLE de Entrenamiento: mismo patrón de filas con costo,
//     para Aldeano + cada unidad militar disponible para tu civilización.
//   - Panel de la unidad militar seleccionada en el mapa (si hay una).
//   - NecoArc: guía tutorial que va cambiando de mensaje según tu progreso.
//   - Panel de fin de partida.
public class VistaHUD : MonoBehaviour
{
    public VistaMapa vistaMapa;

    [Serializable]
    public struct IconoPorNombre
    {
        public string nombre;  // tiene que ser IGUAL al nombre que se le pasa a CrearFilaOpcion: "Aldeano", "Cuartel", "Gilgamesh", etc.
        public Sprite sprite;
    }

    [Header("Íconos (nombre exacto -> sprite, para las filas de Construir/Entrenar)")]
    public List<IconoPorNombre> listaIconos = new List<IconoPorNombre>();

    private Sprite ObtenerIcono(string nombre)
    {
        foreach (var entrada in listaIconos)
            if (entrada.nombre == nombre)
                return entrada.sprite;
        return null;
    }
        [Header("Botones desplegables (Aldeano / Unidad)")]
    public GameObject panelAldeanos;  // el contenedor con la lista de aldeanos / entrenar aldeano
    public GameObject panelUnidades;  // el contenedor con la lista de entrenar unidades

    // Cada botón abre su panel y cierra el otro — así solo uno está
    // desplegado a la vez, en vez de tener todo visible siempre.
    public void AlternarPanelAldeanos()
    {
        bool nuevoEstado = !panelAldeanos.activeSelf;
        panelAldeanos.SetActive(nuevoEstado);
        if (nuevoEstado) panelUnidades.SetActive(false);
    }

    public void AlternarPanelUnidades()
    {
        bool nuevoEstado = !panelUnidades.activeSelf;
        panelUnidades.SetActive(nuevoEstado);
        if (nuevoEstado) panelAldeanos.SetActive(false);
    }

    [Header("Textos de recursos")]
    public TextMeshProUGUI textoOro;
    public TextMeshProUGUI textoMadera;
    public TextMeshProUGUI textoComida;
    public TextMeshProUGUI textoPoblacion; // opcional — "Población: 8/10"

    [Header("Lista de Aldeanos (panel del HUD, no del mapa)")]
    public Transform contenedorAldeanos;   // objeto vacío con un Vertical Layout Group
    public GameObject prefabBotonAldeano;  // prefab: un Button con un TextMeshProUGUI hijo
    private readonly List<(Aldeano aldeano, GameObject boton, TextMeshProUGUI texto)> botonesAldeanos = new List<(Aldeano, GameObject, TextMeshProUGUI)>();

    // ---------------------------------------------------------------
    // Lista de Unidades militares (alternativa a clickear el mapa)
    // ---------------------------------------------------------------

    [Header("Lista de Unidades militares (alternativa a clickear el mapa)")]
    public Transform contenedorUnidades;   // objeto vacío con un Vertical Layout Group
    public GameObject prefabBotonUnidad;   // podés usar el MISMO prefab que prefabBotonAldeano
    private readonly List<(Unidad unidad, GameObject boton, TextMeshProUGUI texto)> botonesUnidades = new List<(Unidad, GameObject, TextMeshProUGUI)>();

    private void ActualizarListaDeUnidades(Jugador jugadorHumano)
    {
        if (contenedorUnidades == null || prefabBotonUnidad == null) return;

        // A diferencia de los Aldeanos, acá el conteo SÍ puede bajar (mueren
        // en combate) — por eso se reconstruye entera cuando cambia, en vez
        // de solo "agregar los que falten" (como hace ActualizarListaDeAldeanos).
        var unidadesVivas = jugadorHumano.Unidades.Where(u => u.Vida > 0).ToList();
        if (botonesUnidades.Count != unidadesVivas.Count)
            ReconstruirListaDeUnidades(unidadesVivas);

        for (int i = 0; i < botonesUnidades.Count; i++)
        {
            var (unidad, _, texto) = botonesUnidades[i];
            if (texto == null) continue;
            bool esLaSeleccionada = EsLaUnidadSeleccionada(unidad);
            texto.text = $"{(esLaSeleccionada ? "➤ " : "")}{unidad.GetType().Name} {i + 1} — Vida {unidad.Vida:0}/{unidad.VidaMaxima:0}";
        }
    }
    void Awake()
    {
        if (panelAldeanos != null) panelAldeanos.SetActive(false);
        if (panelUnidades != null) panelUnidades.SetActive(false);
    }
    private void ReconstruirListaDeUnidades(List<Unidad> unidadesVivas)
    {
        foreach (var (_, boton, _) in botonesUnidades) Destroy(boton);
        botonesUnidades.Clear();

        foreach (var unidad in unidadesVivas)
        {
            var botonGO = Instantiate(prefabBotonUnidad, contenedorUnidades);
            var texto = botonGO.GetComponentInChildren<TextMeshProUGUI>();
            var boton = botonGO.GetComponentInChildren<Button>();
            if (boton == null)
            {
                Debug.LogError("El prefab \"Prefab Boton Unidad\" no tiene un Button (ni en él ni en sus hijos).");
                botonesUnidades.Add((unidad, botonGO, texto));
                continue;
            }

            // Mismo truco que CrearFilaOpcion: si el prefab tiene un hijo
            // "Icono"/"icono" con Image, le busca el sprite por nombre de tipo.
            var imagenIcono = (botonGO.transform.Find("Icono") ?? botonGO.transform.Find("icono"))?.GetComponent<Image>();
            if (imagenIcono != null)
            {
                imagenIcono.sprite = ObtenerIcono(unidad.GetType().Name);
                imagenIcono.enabled = imagenIcono.sprite != null;
            }

            boton.onClick.AddListener(() => SeleccionarUnidad(unidad));
            botonesUnidades.Add((unidad, botonGO, texto));
        }
    }

    // Seleccionar desde la lista hace EXACTAMENTE lo mismo que clickear la
    // unidad en el mapa (llena FilaSeleccionada/ColumnaSeleccionada) — así
    // que el siguiente click en el mapa para moverla/atacar ya funciona
    // solo, sin tocar VistaInput.
    private void SeleccionarUnidad(Unidad unidad)
    {
        if (unidad.Vida <= 0) return;
        if (!BuscarPosicionDeUnidad(unidad, out int fila, out int columna)) return;

        bool yaEstaSeleccionada = vistaMapa.FilaSeleccionada == fila && vistaMapa.ColumnaSeleccionada == columna;
        vistaMapa.FilaSeleccionada = yaEstaSeleccionada ? (int?)null : fila;
        vistaMapa.ColumnaSeleccionada = yaEstaSeleccionada ? (int?)null : columna;
    }

    private bool EsLaUnidadSeleccionada(Unidad unidad)
    {
        if (vistaMapa.FilaSeleccionada == null) return false;
        var celda = vistaMapa.Partida.Mapa.ObtenerCelda(vistaMapa.FilaSeleccionada.Value, vistaMapa.ColumnaSeleccionada.Value);
        return celda?.Unidad == unidad;
    }

    private bool BuscarPosicionDeUnidad(Unidad unidad, out int fila, out int columna)
    {
        var mapa = vistaMapa.Partida.Mapa;
        for (int f = 0; f < Mapa.FILAS; f++)
            for (int c = 0; c < Mapa.COLUMNAS; c++)
                if (mapa.ObtenerCelda(f, c).Unidad == unidad) { fila = f; columna = c; return true; }
        fila = columna = 0;
        return false;
    }

    // ---------------------------------------------------------------
    // Listas desplegables de Construcción / Entrenamiento
    // ---------------------------------------------------------------

    [Header("Lista desplegable: Construcción")]
    public GameObject panelListaConstruccion;      // se activa/desactiva con el botón "Construir"
    public Transform contenedorListaConstruccion;  // objeto vacío con un Vertical Layout Group

    [Header("Lista desplegable: Entrenamiento")]
    public GameObject panelListaEntrenamiento;      // se activa/desactiva con el botón "Entrenar"
    public Transform contenedorListaEntrenamiento;  // objeto vacío con un Vertical Layout Group

    [Header("Prefab de fila compartido por ambas listas")]
    // El prefab tiene que tener, colgando de él, un GameObject hijo llamado
    // exactamente "Nombre" y otro "Costo" con TextMeshProUGUI, y un Button
    // (en la raíz o en un hijo) para disparar la acción.
    public GameObject prefabFilaOpcion;

    private struct FilaOpcion
    {
        public TextMeshProUGUI textoCosto;
        public Button boton;
        public int costoOro, costoMadera, costoComida;
        public Func<bool> disponible; // además del costo, ¿está desbloqueada? (ej: hace falta el Cuartel)
    }

    private readonly List<FilaOpcion> filasConstruccion = new List<FilaOpcion>();
    private readonly List<FilaOpcion> filasEntrenamiento = new List<FilaOpcion>();

    // Costos — constantes acá para que las filas de la lista y los métodos
    // de construcción/entrenamiento nunca queden desincronizados.
    private const int CUARTEL_COSTO_ORO = 150, CUARTEL_COSTO_MADERA = 100, CUARTEL_COSTO_COMIDA = 0;
    private const int TORRE_COSTO_ORO = 100, TORRE_COSTO_MADERA = 50, TORRE_COSTO_COMIDA = 0;
    private const int CASA_COSTO_ORO = 0, CASA_COSTO_MADERA = 30, CASA_COSTO_COMIDA = 0;
    private const int GRANJA_COSTO_ORO = 20, GRANJA_COSTO_MADERA = 60, GRANJA_COSTO_COMIDA = 0;
    private const int MURO_COSTO_ORO = 0, MURO_COSTO_MADERA = 20, MURO_COSTO_COMIDA = 0;

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
        "Soy NecoArc y sere tu guia.\nPara arrancar, abrí \"Entrenar\" y elegí Aldeano.",
        "Muy bien. Ahora elige tu aldeano de la lista, y después haz click en un árbol, una mina o un rebaño del mapa para mandarlo a recolectar.",
        "¡Eso es! Sigue juntando oro y madera. Cuando tengas suficiente, abrí \"Construir\", elegí Cuartel, y hacé click en una celda ya explorada para ubicarlo.",
        "¡Buen trabajo! Desde \"Entrenar\" ya puedes sacar soldados para defender tu base y salir a explorar. Con esto ya deberías poder sobrevivir.",
    };

    [Header("Panel de fin de partida")]
    public GameObject panelFinDePartida;
    public TextMeshProUGUI textoResultado;

    void Update()
    {
        if (vistaMapa.Partida == null) return;

        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;

        textoOro.text = $"Oro: {jugadorHumano.Recursos[TipoRecurso.Oro]}";
        textoMadera.text = $"Madera: {jugadorHumano.Recursos[TipoRecurso.Madera]}";
        textoComida.text = $"Comida: {jugadorHumano.Recursos[TipoRecurso.Comida]}";
        if (textoPoblacion != null) textoPoblacion.text = $"Población: {jugadorHumano.PoblacionActual}/{jugadorHumano.LimitePoblacion}";

        ActualizarListaDeAldeanos(jugadorHumano);
        ActualizarListaDeUnidades(jugadorHumano);

        // Las listas de Construcción/Entrenamiento solo se arman la primera
        // vez que se abren (ver AlternarPanelConstruccion/Entrenamiento), y
        // solo se refrescan (costo/interactable) mientras están visibles.
        if (panelListaConstruccion != null && panelListaConstruccion.activeSelf)
            ActualizarFilasOpcion(filasConstruccion, jugadorHumano);
        if (panelListaEntrenamiento != null && panelListaEntrenamiento.activeSelf)
            ActualizarFilasOpcion(filasEntrenamiento, jugadorHumano);

        ActualizarPanelSeleccion(jugadorHumano);
        if (!tutorialCerrado) ActualizarTutorial(jugadorHumano);

        if (vistaMapa.Partida.Partida.Finalizada && !panelFinDePartida.activeSelf)
        {
            Jugador ganador = vistaMapa.Partida.Partida.Ganador;
            textoResultado.text = ganador == jugadorHumano ? "¡Ganaste!" : $"Perdiste. Ganó: {(ganador != null ? ganador.Nombre : "nadie")}";
            panelFinDePartida.SetActive(true);
        }
    }

    // ---------------------------------------------------------------
    // Lista de Aldeanos
    // ---------------------------------------------------------------

    private void ActualizarListaDeAldeanos(Jugador jugadorHumano)
    {
        if (contenedorAldeanos == null || prefabBotonAldeano == null) return;

        // Los aldeanos pueden morir, así que el conteo puede bajar: si la
        // lista de botones ya no coincide con los aldeanos vivos, se
        // reconstruye entera (mismo criterio que la lista de unidades).
        var aldeanosVivos = jugadorHumano.Aldeanos.Where(a => a.EstaVivo).ToList();
        bool coincide = botonesAldeanos.Count == aldeanosVivos.Count;
        for (int i = 0; coincide && i < aldeanosVivos.Count; i++)
            if (botonesAldeanos[i].aldeano != aldeanosVivos[i]) coincide = false;
        if (!coincide) ReconstruirListaDeAldeanos(aldeanosVivos);

        for (int i = 0; i < botonesAldeanos.Count; i++)
        {
            var (aldeano, _, texto) = botonesAldeanos[i];
            if (texto == null) continue;
            bool esElSeleccionado = vistaMapa.AldeanoSeleccionado == aldeano;
            string estado = aldeano.Ocupado ? "Recolectando..." : "Libre";
            texto.text = $"{(esElSeleccionado ? "➤ " : "")}{aldeano.Nombre} {i + 1} — {estado} — Vida {aldeano.Vida}/{aldeano.VidaMaxima}";
        }
    }

    private void ReconstruirListaDeAldeanos(List<Aldeano> aldeanosVivos)
    {
        foreach (var (_, boton, _) in botonesAldeanos) Destroy(boton);
        botonesAldeanos.Clear();

        foreach (var aldeano in aldeanosVivos)
        {
            var botonGO = Instantiate(prefabBotonAldeano, contenedorAldeanos);
            var texto = botonGO.GetComponentInChildren<TextMeshProUGUI>();
            var boton = botonGO.GetComponentInChildren<Button>();
            if (boton == null)
            {
                Debug.LogError("El prefab \"Prefab Boton Aldeano\" no tiene un Button (ni en él ni en sus hijos).");
                botonesAldeanos.Add((aldeano, botonGO, texto));
                continue;
            }
            var aldeanoDelBoton = aldeano; // copia local para el closure
            boton.onClick.AddListener(() => SeleccionarAldeano(aldeanoDelBoton));
            botonesAldeanos.Add((aldeano, botonGO, texto));
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

    // ---------------------------------------------------------------
    // Listas desplegables de Construcción / Entrenamiento
    // ---------------------------------------------------------------

    // Cableá el botón "Construir" del Canvas a este método.
    public void AlternarPanelConstruccion()
    {
        if (panelListaConstruccion == null) return;
        if (filasConstruccion.Count == 0) PoblarListaDeConstruccion();

        bool nuevoEstado = !panelListaConstruccion.activeSelf;
        panelListaConstruccion.SetActive(nuevoEstado);
        if (nuevoEstado && panelListaEntrenamiento != null) panelListaEntrenamiento.SetActive(false); // una lista abierta a la vez
    }

    // Cableá el botón "Entrenar" del Canvas a este método.
    public void AlternarPanelEntrenamiento()
    {
        if (panelListaEntrenamiento == null) return;
        if (filasEntrenamiento.Count == 0) PoblarListaDeEntrenamiento();

        bool nuevoEstado = !panelListaEntrenamiento.activeSelf;
        panelListaEntrenamiento.SetActive(nuevoEstado);
        if (nuevoEstado && panelListaConstruccion != null) panelListaConstruccion.SetActive(false);
    }

    // Cada fila deja el Edificio correspondiente como "pendiente de
    // colocar" — el click en el mapa (VistaInput) decide dónde y recién ahí
    // dispara SolicitarConstruccion.
    private void PoblarListaDeConstruccion()
    {
        if (contenedorListaConstruccion == null || prefabFilaOpcion == null) return;

        string civilizacion = vistaMapa.CivilizacionHumano;

        CrearFilaOpcion(contenedorListaConstruccion, filasConstruccion,
            "Cuartel", CUARTEL_COSTO_ORO, CUARTEL_COSTO_MADERA, CUARTEL_COSTO_COMIDA,
            () => PrepararConstruccion(new EdificioEntrenamiento(civilizacion, CUARTEL_COSTO_ORO, CUARTEL_COSTO_MADERA, CUARTEL_COSTO_COMIDA, UnidadesDeCivilizacion(civilizacion))));

        CrearFilaOpcion(contenedorListaConstruccion, filasConstruccion,
            "Torre de Defensa", TORRE_COSTO_ORO, TORRE_COSTO_MADERA, TORRE_COSTO_COMIDA,
            () => PrepararConstruccion(new TorreDefensa(civilizacion, TORRE_COSTO_ORO, TORRE_COSTO_MADERA, TORRE_COSTO_COMIDA)));

        CrearFilaOpcion(contenedorListaConstruccion, filasConstruccion,
            "Casa (+5 población)", CASA_COSTO_ORO, CASA_COSTO_MADERA, CASA_COSTO_COMIDA,
            () => PrepararConstruccion(new Casa(civilizacion, CASA_COSTO_ORO, CASA_COSTO_MADERA, CASA_COSTO_COMIDA)),
            nombreIcono: "Casa");

        CrearFilaOpcion(contenedorListaConstruccion, filasConstruccion,
            "Granja", GRANJA_COSTO_ORO, GRANJA_COSTO_MADERA, GRANJA_COSTO_COMIDA,
            () => PrepararConstruccion(new Granja(civilizacion, GRANJA_COSTO_ORO, GRANJA_COSTO_MADERA, GRANJA_COSTO_COMIDA)));

        CrearFilaOpcion(contenedorListaConstruccion, filasConstruccion,
            "Muro", MURO_COSTO_ORO, MURO_COSTO_MADERA, MURO_COSTO_COMIDA,
            () => PrepararConstruccion(new Muro(civilizacion, MURO_COSTO_ORO, MURO_COSTO_MADERA, MURO_COSTO_COMIDA)));
    }

    // Aldeano (siempre disponible, desde el Centro Urbano) + una fila por
    // cada unidad militar habilitada para tu civilización (bloqueadas hasta
    // que exista el Cuartel).
    private void PoblarListaDeEntrenamiento()
    {
        if (contenedorListaEntrenamiento == null || prefabFilaOpcion == null) return;

        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;
        string civilizacion = vistaMapa.CivilizacionHumano;

        var aldeanoPlantilla = new Aldeano("Aldeano", civilizacion, velocidad: 4, velocidadRecoleccion: 5);
        CrearFilaOpcion(contenedorListaEntrenamiento, filasEntrenamiento,
            "Aldeano", aldeanoPlantilla.CostoOro, aldeanoPlantilla.CostoMadera, aldeanoPlantilla.CostoComida,
            EntrenarAldeano);

        foreach (var tipoUnidad in UnidadesDeCivilizacion(civilizacion))
        {
            string tipo = tipoUnidad; // copia local: sin esto, todas las filas entrenarían la última unidad del foreach
            Unidad plantilla;
            try { plantilla = FabricaUnidades.Crear(tipo, civilizacion); }
            catch { continue; } // por si algún tipo no aplica a esta civilización

            CrearFilaOpcion(contenedorListaEntrenamiento, filasEntrenamiento,
                tipo, plantilla.CostoOro, plantilla.CostoMadera, plantilla.CostoComida,
                () => EntrenarUnidad(tipo),
                disponible: () => jugadorHumano.Edificios.OfType<EdificioEntrenamiento>().Any());
        }
    }

    private void CrearFilaOpcion(Transform contenedor, List<FilaOpcion> lista, string nombre, int oro, int madera, int comida, UnityEngine.Events.UnityAction accion, Func<bool> disponible = null, string nombreIcono = null)
    {
        var filaGO = Instantiate(prefabFilaOpcion, contenedor);

        var textoNombre = filaGO.transform.Find("Nombre")?.GetComponent<TextMeshProUGUI>();
        if (textoNombre != null) textoNombre.text = nombre;

        var textoCosto = filaGO.transform.Find("Costo")?.GetComponent<TextMeshProUGUI>();

        var imagenIcono = (filaGO.transform.Find("Icono") ?? filaGO.transform.Find("icono"))?.GetComponent<Image>();
        if (imagenIcono != null)
        {
            imagenIcono.sprite = ObtenerIcono(nombreIcono ?? nombre);
            imagenIcono.enabled = imagenIcono.sprite != null;
        }

        var boton = filaGO.GetComponent<Button>() ?? filaGO.GetComponentInChildren<Button>();
        boton.onClick.AddListener(accion);

        lista.Add(new FilaOpcion
        {
            textoCosto = textoCosto,
            boton = boton,
            costoOro = oro,
            costoMadera = madera,
            costoComida = comida,
            disponible = disponible ?? (() => true),
        });
    }

    // Refresca el texto del costo y si el botón se puede apretar (alcanza
    // con los recursos actuales Y está desbloqueada). Solo se llama mientras
    // el panel correspondiente está visible (ver Update()).
    private void ActualizarFilasOpcion(List<FilaOpcion> lista, Jugador jugadorHumano)
    {
        foreach (var fila in lista)
        {
            bool alcanzaRecursos = jugadorHumano.Recursos[TipoRecurso.Oro] >= fila.costoOro
                                 && jugadorHumano.Recursos[TipoRecurso.Madera] >= fila.costoMadera
                                 && jugadorHumano.Recursos[TipoRecurso.Comida] >= fila.costoComida;
            bool disponible = fila.disponible();

            if (fila.textoCosto != null)
                fila.textoCosto.text = $"Oro {fila.costoOro} / Madera {fila.costoMadera} / Comida {fila.costoComida}" + (disponible ? "" : " (bloqueado)");

            if (fila.boton != null) fila.boton.interactable = alcanzaRecursos && disponible;
        }
    }

    // ---------------------------------------------------------------
    // Panel de la unidad militar seleccionada en el mapa
    // ---------------------------------------------------------------

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

    // ---------------------------------------------------------------
    // NecoArc — tutorial
    // ---------------------------------------------------------------

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

    // ---------------------------------------------------------------
    // Acciones reales (llamadas desde las filas de las listas)
    // ---------------------------------------------------------------

    public void EntrenarAldeano()
    {
        var controladorEntrenamiento = vistaMapa.Partida.ControladoresEntrenamiento[0];
        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;
        var centroUrbano = jugadorHumano.Edificios.OfType<EdificioPrincipal>().FirstOrDefault();
        if (centroUrbano == null) { Debug.Log("No tienes Centro Urbano."); return; }

        if (controladorEntrenamiento.SolicitarAldeano(centroUrbano))
            Debug.Log("Aldeano en entrenamiento (2s)...");
        else
            Debug.Log("No se pudo entrenar aldeano (¿recursos insuficientes o sin espacio de población?)");
    }

    public void EntrenarUnidad(string tipoUnidad)
    {
        var controladorEntrenamiento = vistaMapa.Partida.ControladoresEntrenamiento[0];
        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;
        var cuartel = jugadorHumano.Edificios.OfType<EdificioEntrenamiento>().FirstOrDefault();
        if (cuartel == null) { Debug.Log("Necesitás construir un Cuartel primero."); return; }

        if (controladorEntrenamiento.SolicitarUnidad(cuartel, tipoUnidad))
            Debug.Log($"{tipoUnidad} en entrenamiento (3s)...");
        else
            Debug.Log($"No se pudo entrenar {tipoUnidad} (¿recursos insuficientes o sin espacio de población?)");
    }

    // Deja el edificio como "pendiente de colocar": el próximo click sobre
    // una celda ya explorada (VistaInput) decide dónde y recién ahí se
    // gastan los recursos de verdad (ver GestorConstruccion.IniciarConstruccion).
    private void PrepararConstruccion(Edificio edificio)
    {
        vistaMapa.EdificioPendienteDeColocar = edificio;
        Debug.Log($"Elegí una celda ya explorada para construir {edificio.Nombre}.");
    }

    // Cada civilización tiene 1 héroe + 1 unidad exclusiva propia, más las 4
    // genéricas (cualquier civilización) y NecoArc (el meme, también genérica).
    private List<string> UnidadesDeCivilizacion(string civilizacion)
    {
        string heroe, exclusiva;
        switch (civilizacion)
        {
            case "Sumerios": heroe = "Gilgamesh";    exclusiva = "Caster";    break;
            case "Nipones":  heroe = "Godzilla";     exclusiva = "Assassin";  break;
            case "Griegos":  heroe = "Medusa";       exclusiva = "Avenger";   break;
            default:         heroe = "Jormungandr";  exclusiva = "Berserker"; break; // Vikingos
        }

        return new List<string> { heroe, exclusiva, "Defender", "Vanguard", "Ranger", "Healer", "NecoArc" };
    }

    public void VolverAlMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}