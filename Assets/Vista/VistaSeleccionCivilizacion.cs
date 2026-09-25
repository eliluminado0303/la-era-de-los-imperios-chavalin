using UnityEngine;

// Pantalla de selección de civilización: 4 botones, uno por civilización.
// Al elegir uno, oculta el panel y recién ahí arranca la partida.
public class VistaSeleccionCivilizacion : MonoBehaviour
{
    public VistaMapa vistaMapa;
    public GameObject panelSeleccion; // el objeto que agrupa los 4 botones

    public void ElegirNipones() => Elegir("Nipones");
    public void ElegirGriegos() => Elegir("Griegos");
    public void ElegirVikingos() => Elegir("Vikingos");
    public void ElegirSumerios() => Elegir("Sumerios");

    private void Elegir(string civilizacion)
    {
        panelSeleccion.SetActive(false);
        vistaMapa.IniciarPartida(civilizacion);
    }
}