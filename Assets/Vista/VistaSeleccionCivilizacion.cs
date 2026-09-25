using UnityEngine;

// Vive en la escena del MENÚ (no en Partida). Maneja el panel donde el
// jugador elige su civilización antes de arrancar:
//
//  1) El botón "Crear partida" del menú principal debería llamar a
//     MostrarPanel() (en vez de a ControladorMenu.CrearPatida() directo) —
//     así primero aparece este panel con los 4 botones de civilización.
//  2) Cada botón de civilización llama a su Elegir*() correspondiente, que
//     guarda la elección a través de ControladorMenu.IrAPartidaConCivilizacion
//     y recién ahí carga la escena Partida. VistaMapa la lee al arrancar
//     con PlayerPrefs.GetString(VistaMapa.claveCivilizacion, ...).
public class VistaSeleccionCivilizacion : MonoBehaviour
{
    public ControladorMenu controladorMenu;
    public GameObject panelSeleccion;     // el objeto que agrupa los 4 botones de civilización
    public GameObject panelMenuPrincipal; // opcional: el panel de "Crear/Unirse/Salir" — se oculta al mostrar la selección para que no quede tapando

    public void MostrarPanel()
    {
        Debug.Log("VistaSeleccionCivilizacion.MostrarPanel() se ejecutó."); // si esto NO aparece en la Console al clickear, el botón no tiene bien cableado el OnClick()

        if (panelSeleccion == null)
        {
            Debug.LogError("panelSeleccion está vacío en el Inspector de VistaSeleccionCivilizacion — asignale el Panel de civilizaciones.");
            return;
        }

        if (panelMenuPrincipal != null) panelMenuPrincipal.SetActive(false);
        panelSeleccion.SetActive(true);
    }

    public void OcultarPanel()
    {
        panelSeleccion.SetActive(false);
        if (panelMenuPrincipal != null) panelMenuPrincipal.SetActive(true);
    }

    public void ElegirNipones() => Elegir("Nipones");
    public void ElegirGriegos() => Elegir("Griegos");
    public void ElegirVikingos() => Elegir("Vikingos");
    public void ElegirSumerios() => Elegir("Sumerios");

    private void Elegir(string civilizacion)
    {
        Debug.Log($"VistaSeleccionCivilizacion.Elegir({civilizacion}) se ejecutó.");
        controladorMenu.IrAPartidaConCivilizacion(civilizacion);
    }
}