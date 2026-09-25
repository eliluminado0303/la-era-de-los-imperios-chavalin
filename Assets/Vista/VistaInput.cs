using UnityEngine;

// Traduce los clics del jugador humano en llamadas a sus controladores
// (siempre el índice 0 en las listas de ControladorPartida). Selección en
// dos clics: primero tu unidad, luego el destino/objetivo.
public class VistaInput : MonoBehaviour
{
    public VistaMapa vistaMapa;

    void Awake()
    {
        if (vistaMapa == null) vistaMapa = FindObjectOfType<VistaMapa>();
    }

    void Update()
    {
        if (vistaMapa == null) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (vistaMapa.Partida == null) return;

        Vector3 mundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mundo.z = 0;
        if (!vistaMapa.MundoACelda(mundo, out int fila, out int columna)) return;

        var controladorMapaHumano = vistaMapa.Partida.ControladoresMapa[0];
        var controladorCombateHumano = vistaMapa.Partida.ControladoresCombate[0];
        Jugador jugadorHumano = controladorMapaHumano.Jugador;
        Celda celda = vistaMapa.Partida.Mapa.ObtenerCelda(fila, columna);

        // Si hay un aldeano elegido desde la lista del HUD, este click decide
        // a qué recurso lo mandamos — corta acá y no sigue con la selección
        // normal de unidades militares (los Aldeanos no son Unidad, no
        // participan de esa lógica).
        if (vistaMapa.AldeanoSeleccionado != null)
        {
            if (celda?.Recurso != null && !celda.Recurso.EstaAgotado())
            {
                vistaMapa.AsignarRecoleccionPermanente(vistaMapa.AldeanoSeleccionado, fila, columna);
                Debug.Log($"{vistaMapa.AldeanoSeleccionado.Nombre} asignado a recolectar de forma continua.");
                vistaMapa.AldeanoSeleccionado = null;
            }
            else
            {
                Debug.Log("Esa celda no tiene un recurso. Elige un árbol, una mina o un rebaño.");
            }
            return;
        }

        if (vistaMapa.FilaSeleccionada == null)
        {
            if (celda?.Unidad != null && jugadorHumano.Unidades.Contains(celda.Unidad))
            {
                vistaMapa.FilaSeleccionada = fila;
                vistaMapa.ColumnaSeleccionada = columna;
                Debug.Log($"Unidad seleccionada en ({fila},{columna})");
            }
            return;
        }

        int fOrigen = vistaMapa.FilaSeleccionada.Value;
        int cOrigen = vistaMapa.ColumnaSeleccionada.Value;

        if (fila == fOrigen && columna == cOrigen)
        {
            vistaMapa.FilaSeleccionada = null;
            vistaMapa.ColumnaSeleccionada = null;
            return;
        }

        bool accionValida;
        if (celda?.Unidad != null || celda?.Edificio != null)
        {
            accionValida = controladorCombateHumano.SolicitarAtaque(fOrigen, cOrigen, fila, columna);
        }
        else
        {
            controladorMapaHumano.SolicitarMovimiento(fOrigen, cOrigen, fila, columna);
            accionValida = true;
        }

        if (!accionValida) Debug.Log("Acción no válida (fuera de rango, objetivo aliado, celda vacía sin nada que atacar, etc.)");

        vistaMapa.FilaSeleccionada = null;
        vistaMapa.ColumnaSeleccionada = null;
    }
}