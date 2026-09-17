using System;
using System.IO;
using System.Text;

public class GestorArchivos {

    private readonly string rutaConfiguracion = "configuracion.txt";
    private readonly string rutaLog = "log_partida.txt";
    private readonly string rutaResultado = "resultado_final.txt";

    public void GuardarConfiguracion(Mapa mapa, Jugador jugador) {
        StringBuilder contenido = new StringBuilder();

        contenido.AppendLine("=== Configuracion inicial ===");
        contenido.AppendLine($"Jugador: {jugador.Nombre}");
        contenido.AppendLine($"Oro inicial: {jugador.Recursos[TipoRecurso.Oro]}");
        contenido.AppendLine($"Madera inicial: {jugador.Recursos[TipoRecurso.Madera]}");
        contenido.AppendLine($"Comida inicial: {jugador.Recursos[TipoRecurso.Comida]}");
        contenido.AppendLine($"Mapa: {Mapa.FILAS}x{Mapa.COLUMNAS}");

        File.WriteAllText(rutaConfiguracion, contenido.ToString());
    }

    public void RegistrarEvento(string jugador, string accion, string resultado) {
        string linea = $"Turno: {jugador}{Environment.NewLine}" +
                        $"Accion: {accion}{Environment.NewLine}" +
                        $"Resultado: {resultado}{Environment.NewLine}{Environment.NewLine}";

        File.AppendAllText(rutaLog, linea);
    }

    public void GuardarResultadoFinal(Partida partida) {
        StringBuilder contenido = new StringBuilder();

        contenido.AppendLine("=== Resultado final ===");
        contenido.AppendLine($"Ganador: {(partida.Ganador != null ? partida.Ganador.Nombre : "Sin definir")}");
        contenido.AppendLine($"Fecha: {DateTime.Now}");

        File.WriteAllText(rutaResultado, contenido.ToString());
    }
}