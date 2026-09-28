using UnityEngine;

// Efecto visual de un "rayo" (línea recta con ancho) — para habilidades en
// forma de Línea como el Enuma Elish de Gilgamesh o el aliento de Godzilla.
// Se autodestruye solo después de un tiempo corto.
public class EfectoRayo : MonoBehaviour
{
    // sprite: opcional — si le pasás el rayo/aliento real (por ejemplo el
    // que ya tenés para Gilgamesh), lo usa; si lo dejás null, sigue
    // funcionando como antes (un rectángulo de color liso).
    public static void Crear(Vector3 origen, Vector3 direccionNormalizada, float longitud, float ancho, Color color, float duracion = 0.4f, Sprite sprite = null)
    {
        var go = new GameObject("EfectoRayo");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : ObtenerSpriteBlanco();
        sr.color = color;
        sr.sortingOrder = 10;

        go.transform.position = origen + direccionNormalizada * (longitud / 2f);
        go.transform.right = direccionNormalizada; // orienta el rayo hacia el objetivo
        go.transform.localScale = new Vector3(longitud, ancho, 1f);

        var efecto = go.AddComponent<EfectoRayo>();
        efecto.duracion = duracion;
    }

    private float duracion;
    private float tiempoTranscurrido;
    private SpriteRenderer spriteRenderer;

    void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    void Update()
    {
        tiempoTranscurrido += Time.deltaTime;
        float progreso = tiempoTranscurrido / duracion;
        var color = spriteRenderer.color;
        color.a = Mathf.Lerp(1f, 0f, progreso); // se desvanece
        spriteRenderer.color = color;
        if (progreso >= 1f) Destroy(gameObject);
    }

    private static Sprite spriteBlancoCache;
    private static Sprite ObtenerSpriteBlanco()
    {
        if (spriteBlancoCache != null) return spriteBlancoCache;
        var textura = Texture2D.whiteTexture;
        spriteBlancoCache = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f), textura.width);
        return spriteBlancoCache;
    }
}