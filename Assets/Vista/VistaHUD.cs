using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

// HUD de la partida principal: muestra los recursos del jugador humano,
// botones para entrenar/construir, y el mensaje de fin de partida.
// Trabaja siempre sobre el jugador humano (índice 0 de ControladorPartida).
public class VistaHUD : MonoBehaviour
{
    public VistaMapa vistaMapa;

    [Header("Textos de recursos")]
    public TextMeshProUGUI textoOro;
    public TextMeshProUGUI textoMadera;
    public TextMeshProUGUI textoComida;

    [Header("Panel de fin de partida")]
    public GameObject panelFinDePartida;
    public TextMeshProUGUI textoResultado;

    void Update()
    {
        if (vistaMapa.Partida == null) return; // todavía no arrancó la partida

        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;
        textoOro.text = $"Oro: {jugadorHumano.Recursos[TipoRecurso.Oro]}";
        textoMadera.text = $"Madera: {jugadorHumano.Recursos[TipoRecurso.Madera]}";
        textoComida.text = $"Comida: {jugadorHumano.Recursos[TipoRecurso.Comida]}";

        if (vistaMapa.Partida.Partida.Finalizada && !panelFinDePartida.activeSelf)
        {
            Jugador ganador = vistaMapa.Partida.Partida.Ganador;
            textoResultado.text = ganador == jugadorHumano ? "¡Ganaste!" : $"Perdiste. Ganó: {(ganador != null ? ganador.Nombre : "nadie")}";
            panelFinDePartida.SetActive(true);
        }
    }

    // Botón "Entrenar Aldeano": usa el Centro Urbano propio del humano.
    public void EntrenarAldeano()
    {
        var controladorEntrenamiento = vistaMapa.Partida.ControladoresEntrenamiento[0];
        Jugador jugadorHumano = vistaMapa.Partida.ControladoresMapa[0].Jugador;
        var centroUrbano = jugadorHumano.Edificios.OfType<EdificioPrincipal>().FirstOrDefault();
        if (centroUrbano == null) { Debug.Log("No tienes Centro Urbano."); return; }

        if (!controladorEntrenamiento.SolicitarAldeano(centroUrbano))
            Debug.Log("No se pudo entrenar aldeano (¿recursos insuficientes?)");
    }

    // Botón "Construir Cuartel": ahora el Centro Urbano del humano puede
    // estar en cualquier lado (se eligió en la fase de colocación), así que
    // ya no se puede asumir la esquina (0,0). Se ubica el Centro Urbano en
    // el Mapa y se construye el cuartel en la primera celda libre cerca de
    // él, probando un puñado de posiciones relativas típicas.
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

    // El Mapa no guarda la posición dentro del propio Edificio, así que
    // para encontrarla hay que buscar la celda que lo contiene. Solo se usa
    // al apretar el botón (no por frame), así que un recorrido de la
    // cuadrícula no tiene costo real.
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

    // El cuartel entrena la unidad exclusiva de tu civilización más las 4
    // genéricas (Defender, Vanguard, Ranger, Healer), que puede entrenar
    // cualquier civilización.
    private List<string> UnidadesDeCivilizacion(string civilizacion)
    {
        string exclusiva = civilizacion == "Nipones" ? "Assassin"
                          : civilizacion == "Griegos" ? "Avenger"
                          : civilizacion == "Vikingos" ? "Berserker"
                          : "Caster"; // Sumerios

        return new List<string> { exclusiva, "Defender", "Vanguard", "Ranger", "Healer", "NekoArc" };
    }

    public void VolverAlMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}