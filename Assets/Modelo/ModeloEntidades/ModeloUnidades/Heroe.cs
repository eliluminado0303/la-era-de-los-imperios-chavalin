using System;
using System.Collections.Generic;

public abstract class Heroe : Unidad
{
    public abstract override void HabilidadEspecial(List<Unidad> objetivos);
    public AreaEfecto AreaHabilidad { get; protected set; }

    // La habilidad especial es fuerte (doble daño ignorando defensa, área
    // completa) a propósito, así que necesita recarga — si no, se podría
    // usar sin límite y dejaría de ser "especial". Quien la ejecuta
    // (Partida.EjecutarHabilidadEspecial) es responsable de chequear
    // PuedeUsarHabilidad() antes de llamar a HabilidadEspecial().
    private const float TIEMPO_RECARGA_SEGUNDOS = 15f;
    private DateTime ultimoUso = DateTime.MinValue;

    public bool PuedeUsarHabilidad() => (DateTime.UtcNow - ultimoUso).TotalSeconds >= TIEMPO_RECARGA_SEGUNDOS;
    public float TiempoRestanteRecarga()
    => Math.Max(0f, TIEMPO_RECARGA_SEGUNDOS - (float)(DateTime.UtcNow - ultimoUso).TotalSeconds);
    public void RegistrarUsoHabilidad() => ultimoUso = DateTime.UtcNow;
}
public class Gilgamesh : Heroe
{
    public Gilgamesh()
    {
        Vida = 250; 
        VidaMaxima = Vida;
        Ataque = 20; 
        Defensa = 15; 
        Velocidad = 5; 
        Rango = 1; 
        Civilizacion = "Sumerios";
        ProbabilidadEsquivar = 0f; 
        ProbabilidadCritico = 0.35f; 
        multiplicadorCritico = 2f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
        AreaHabilidad = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 3f, tamaño: 3.5f);
    }

    // Enuma Elish: área que IGNORA defensa
    public override void HabilidadEspecial(List<Unidad> objetivos)
    {
        float daño = Ataque * 2;
        foreach (var objetivo in objetivos)
            objetivo.RecibirAtaqueEspecial(daño, ignorarDefensa: true);
        Registro.Escribir($"{Civilizacion}: invocó a EA  sobre {objetivos.Count} unidades!");
    }
}

public class Godzilla : Heroe
{
    public Godzilla()
    {
        Vida = 300; 
        VidaMaxima = Vida;
        Ataque = 25; 
        Defensa = 20; 
        Velocidad = 3; 
        Rango = 1; 
        Civilizacion = "Nipones";
        ProbabilidadEsquivar = 0.30f; 
        ProbabilidadCritico = 0.30f; 
        multiplicadorCritico = 2.5f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
        AreaAtaqueBasico = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 0f, tamaño: 2.5f);
        AreaHabilidad    = new AreaEfecto(FormaArea.Linea, rangoLanzamiento: 0f, tamaño: 9f, ancho: 1.5f);

    }

    // Ataque básico en área: el rayo atómico golpea a cada objetivo
    // usando el daño y la probabilidad de crítico normales.
    public override void Atacar(List<Unidad> objetivos)
    {
        if (objetivos == null) throw new System.ArgumentNullException(nameof(objetivos));
        if (EstaAturdido) return;

        foreach (var objetivo in objetivos)
        {
            if (objetivo != null)
                Atacar(objetivo);
        }

        Registro.Escribir($"{Civilizacion}: ataque básico en área sobre {objetivos.Count} unidades.");
    }

    // Rayo atómico: área que ASEGURA crítico (pero sí respeta Defensa/Esquivar de cada objetivo)
    public override void HabilidadEspecial(List<Unidad> objetivos)
    {
        
        float daño = Ataque * 1.5f * multiplicadorCritico;
        foreach (var objetivo in objetivos)
            objetivo.RecibirAtaqueEspecial(daño);
        Registro.Escribir($"{Civilizacion}: rayo atómico crítico sobre {objetivos.Count} unidades!");
    }
}

public class Jormungandr : Heroe
{
    public Jormungandr()
    {
        Vida = 200; 
        VidaMaxima = Vida;
        Ataque = 15; 
        Defensa = 10; 
        Velocidad = 4; 
        Rango = 1; 
        Civilizacion = "Vikingos";
        ProbabilidadEsquivar = 0.15f; 
        ProbabilidadCritico = 0.25f; 
        multiplicadorCritico = 2f;
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
        AreaAtaqueBasico = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 0f, tamaño: 2f);
        AreaHabilidad    = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 4f, tamaño: 3f);
    }

    // Ataque básico en área: la embestida golpea individualmente a todos
    // los objetivos, aplicando el daño normal y sus críticos habituales.
    public override void Atacar(List<Unidad> objetivos)
    {
        if (objetivos == null) throw new System.ArgumentNullException(nameof(objetivos));
        if (EstaAturdido) return;

        foreach (var objetivo in objetivos)
        {
            if (objetivo != null)
                Atacar(objetivo);
        }

        Registro.Escribir($"{Civilizacion}: ataque básico en área sobre {objetivos.Count} unidades.");
    }

    // Ralentiza + veneno garantizado (ambos, sobre toda el área)
    public override void HabilidadEspecial(List<Unidad> objetivos)
    {
        foreach (var objetivo in objetivos)
        {
            objetivo.AgregarEfecto(new Veneno(0.05f, 5f, 1f));
            objetivo.AgregarEfecto(new Ralentizado(0.4f, 4f));
        }
        Registro.Escribir($"{Civilizacion}: envenenó y ralentizó a {objetivos.Count} unidades!");
    }
}

public class Medusa : Heroe
{
    private float probabilidadSangrado = 0.30f;
    private float probabilidadAturdimientoBasico = 0.08f;

    public Medusa()
    {
        Vida = 90; 
        VidaMaxima = Vida;
        Ataque = 12; 
        Defensa = 6; 
        Velocidad = 5; 
        Rango = 1; 
        Civilizacion = "Griegos";
        ProbabilidadEsquivar = 0.30f; 
        ProbabilidadCritico = 0.30f; 
        multiplicadorCritico = 2f;
        AreaHabilidad = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 4f, tamaño: 2.5f);
        CostoOro = 100;
        CostoMadera = 50;
        CostoComida = 25;
    }

    protected override void AplicarEfectoAlGolpear(Unidad objetivo)
    {
        if (Aleatorio.Valor() < probabilidadSangrado)
            objetivo.AgregarEfecto(new Sangrado(4f));
        if (Aleatorio.Valor() < probabilidadAturdimientoBasico)
            objetivo.AgregarEfecto(new Aturdimiento(1f));
    }

    // Petrificación: aturde + reduce defensa a TODA el área
    public override void HabilidadEspecial(List<Unidad> objetivos)
    {
        foreach (var objetivo in objetivos)
        {
            objetivo.AgregarEfecto(new Aturdimiento(3f));
            objetivo.AgregarEfecto(new ReduccionDefensa(5, 4f));
        }
    }
}

// nekodios llego a destruir el juego, su habilidad especial te mata a un familiar tambien
// tambien te desinstala system32 y lo reemplaza por system1, hace un ataque DDOs al mossad
// Te obliga a usar linux y te vuelve gay
// olvide mencionar que te embaraza y mata a tu hijo
// que loco el negroarc
    public class NekoArc : Heroe
    {
        public NekoArc(string civilizacion)
        {
            Vida = 1; 
            VidaMaxima = Vida;
            Ataque = 1000; 
            Defensa = 0; 
            Velocidad = 60; 
            Rango = 10; 
            Civilizacion = civilizacion;
            ProbabilidadEsquivar = 100f; 
            ProbabilidadCritico = 100f; 
            multiplicadorCritico = 2000f;
            AreaHabilidad = new AreaEfecto(FormaArea.Circulo, rangoLanzamiento: 3000f, tamaño: 2000f);
            CostoOro = 1000;
            CostoMadera = 500;
            CostoComida = 250;
        }

        // Habilidad especial: aumenta ataque y velocidad de TODA el área
        public override void HabilidadEspecial(List<Unidad> objetivos)
        {
            foreach (var objetivo in objetivos)
            {
            objetivo.AgregarEfecto(new Bendicion(deltaAtaque: 5000, deltaDefensa: 1, duracion: 100f));
            }
        }
    }