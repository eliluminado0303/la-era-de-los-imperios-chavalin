using UnityEngine;

// Traduce los clics del jugador humano en llamadas a sus controladores
// (siempre el índice 0 en las listas de ControladorPartida). Selección en
// dos clics: primero tu unidad, luego el destino/objetivo.
//
// Mientras la partida está en FASE DE COLOCACIÓN (todavía no se ubicó el
// Centro Urbano del humano), los clics se redirigen a
// VistaMapa.IntentarColocarCentroHumano en vez de a la selección normal.
public class VistaInput : MonoBehaviour
{
    public VistaMapa vistaMapa;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (vistaMapa.Partida == null) return;

        Vector3 mundo = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mundo.z = 0;
        if (!vistaMapa.MundoACelda(mundo, out int fila, out int columna)) return;

        if (vistaMapa.EnFaseDeColocacion)
        {
            bool colocado = vistaMapa.IntentarColocarCentroHumano(fila, columna);
            if (!colocado)
                Debug.Log("Ubicación no válida para el Centro Urbano (muy cerca del borde o celda ocupada). Probá otra.");
            return;
        }

        var controladorMapaHumano = vistaMapa.Partida.ControladoresMapa[0];
        var controladorCombateHumano = vistaMapa.Partida.ControladoresCombate[0];
        Jugador jugadorHumano = controladorMapaHumano.Jugador;
        Celda celda = vistaMapa.Partida.Mapa.ObtenerCelda(fila, columna);

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
            accionValida = true; // el movimiento es asíncrono: el resultado llega después por la cola
        }

        if (!accionValida) Debug.Log("Acción no válida (fuera de rango, objetivo aliado, celda vacía sin nada que atacar, etc.)");

        vistaMapa.FilaSeleccionada = null;
        vistaMapa.ColumnaSeleccionada = null;
    }
}