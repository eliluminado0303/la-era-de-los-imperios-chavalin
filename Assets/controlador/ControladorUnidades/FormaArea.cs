public enum FormaArea { Circulo, Linea }

public class AreaEfecto
{
    public FormaArea Forma { get; }
    public float RangoLanzamiento { get; }  // qué tan lejos del que ataca se puede colocar el centro (irrelevante en línea, que sale de sí mismo)
    public float Tamaño { get; }            // radio (círculo) o longitud (línea)
    public float Ancho { get; }             // solo aplica a línea

    public AreaEfecto(FormaArea forma, float rangoLanzamiento, float tamaño, float ancho = 0f)
    {
        Forma = forma; RangoLanzamiento = rangoLanzamiento; Tamaño = tamaño; Ancho = ancho;
    }
}