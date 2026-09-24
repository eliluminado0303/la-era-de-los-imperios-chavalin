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

    // Botón "Construir Cuartel": el humano siempre arranca en la esquina
    // (0,0), así que se construye a un par de celdas de ahí. Cuando quieras
    // elegir la celda con el mouse en vez de un punto fijo, esto se
    // reemplaza por una selección con clic como la de VistaInput.
    public void ConstruirCuartel()
    {
        var controladorMapaHumano = vistaMapa.Partida.ControladoresMapa[0];
        string civilizacion = vistaMapa.CivilizacionHumano;

        var unidadesDisponibles = UnidadesDeCivilizacion(civilizacion);
        var edificio = new EdificioEntrenamiento(civilizacion, costoOro: 150, costoMadera: 100, costoComida: 0, unidadesDisponibles);

        if (!controladorMapaHumano.SolicitarConstruccion(2, 2, edificio))
            Debug.Log("No se pudo construir (¿recursos insuficientes o celda ocupada?)");
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

        return new List<string> { exclusiva, "Defender", "Vanguard", "Ranger", "Healer" };
    }

    public void VolverAlMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}