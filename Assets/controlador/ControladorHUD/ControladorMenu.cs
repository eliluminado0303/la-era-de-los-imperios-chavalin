using UnityEngine;
using UnityEngine.SceneManagement;

// Vive en la escena del Menú. Centraliza la carga de la escena Partida
// (con o sin civilización elegida) y la validación de Build Settings.
public class ControladorMenu : MonoBehaviour
{
    private const string NOMBRE_ESCENA_PARTIDA = "Partida";

    public void CrearPatida()
    {
        Debug.Log("Creando partida....");
        CargarPartida();
    }

    public void UnirsePartida()
    {
        Debug.Log("Unirse a partida....");
        CargarPartida();
    }

    // Llamado por VistaSeleccionCivilizacion (un botón por civilización en
    // el panel del menú): guarda la elección en PlayerPrefs y recién ahí
    // carga la escena Partida. VistaMapa la lee al arrancar con
    // PlayerPrefs.GetString(VistaMapa.claveCivilizacion, ...).
    public void IrAPartidaConCivilizacion(string civilizacion)
    {
        PlayerPrefs.SetString(VistaMapa.claveCivilizacion, civilizacion);
        PlayerPrefs.Save();
        Debug.Log($"Civilización elegida: {civilizacion}. Cargando partida...");
        CargarPartida();
    }

    public void JugarPorDefecto()
    {
        IrAPartidaConCivilizacion("Sumerios");
    }

    public void SalirJuego()
    {
        Debug.Log("Saliendo del juego....");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CargarPartida()
    {
        if (!EstaHabilitadaEnBuild(NOMBRE_ESCENA_PARTIDA))
        {
            Debug.LogError($"La escena \"{NOMBRE_ESCENA_PARTIDA}\" no está habilitada en Build Settings (File > Build Settings). Agregala (Assets/Scenes/Partida.unity) y probá de nuevo.");
            return;
        }

        AsyncOperation operacion = SceneManager.LoadSceneAsync(NOMBRE_ESCENA_PARTIDA);
        if (operacion == null)
            Debug.LogError($"No se pudo cargar la escena \"{NOMBRE_ESCENA_PARTIDA}\". Verificá que exista en Assets/Scenes y esté habilitada en Build Settings.");
    }

    private static bool EstaHabilitadaEnBuild(string nombreEscena)
    {
#if UNITY_EDITOR
        foreach (var entrada in UnityEditor.EditorBuildSettings.scenes)
        {
            if (!entrada.enabled) continue;
            string nombre = System.IO.Path.GetFileNameWithoutExtension(entrada.path);
            if (nombre == nombreEscena) return true;
        }
        return false;
#else
        return true;
#endif
    }
}