using UnityEngine;

// Efecto visual breve sobre una posición: aparece y se desvanece en
// 'duracion' segundos. Sirve para curación (verde), un buff (dorado), etc.
public class EfectoCuracion : MonoBehaviour
{
    // sprite: opcional, mismo criterio que en EfectoRayo — si es null,
    // sigue mostrando el círculo de color liso de siempre.
    public static void Crear(Vector3 posicion, Color color, float duracion = 0.5f, Sprite sprite = null)
    {
        var go = new GameObject("EfectoCuracion");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : ObtenerSpriteBlanco();
        sr.color = color;
        sr.sortingOrder = 10;
        go.transform.position = posicion;
        go.transform.localScale = Vector3.one * 0.3f; // arranca chico y crece

        var efecto = go.AddComponent<EfectoCuracion>();
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
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, progreso);
        var color = spriteRenderer.color;
        color.a = Mathf.Lerp(1f, 0f, progreso);
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