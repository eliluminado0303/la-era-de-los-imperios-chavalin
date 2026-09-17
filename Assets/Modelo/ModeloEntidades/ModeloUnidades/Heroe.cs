/// <summary>Clase base para unidades con habilidades especiales.</summary>
public abstract class Heroe: Unidad
{
    /// <summary>Ejecuta la habilidad especial contra una unidad objetivo.</summary>
     public abstract void HabilidadEspecial(Unidad objetivo);
    
}

/// <summary>Héroe sumerio con ataque especial potenciado.</summary>
public class Gilgamesh: Heroe
{
    /// <summary>Inicializa las estadísticas de Gilgamesh.</summary>
    public Gilgamesh()
    {
        Vida = 250;
        Ataque = 20;
        Defensa = 15;
        Velocidad = 5;
        Rango = 1;
        Civilizacion = "Sumerios";
        ProbabilidadEsquivar = 0.05f;
        ProbabilidadCritico = 0.35f;
        multiplicadorCritico = 2f;
    }

    public override void HabilidadEspecial(Unidad objetivo)
    {
        // Habilidad especial de Gilgamesh: Invoca a EA para un ataque en area devastador ignorando la defensa del oponente 
        float daño = Ataque * 2;
        if (EsCritico())
        {
            daño *= multiplicadorCritico;
            UnityEngine.Debug.Log($"{Civilizacion}: hizo un golpe crítico con su habilidad especial!");
        }
        objetivo.RecibirDaño(daño);
    }
}   

/// <summary>Héroe nipón con ataque masivo que ignora defensa.</summary>
public class Godzilla: Heroe
{
    /// <summary>Inicializa las estadísticas de Godzilla.</summary>
    public Godzilla()
    {
        Vida = 300;
        Ataque = 25;
        Defensa = 20;
        Velocidad = 3;
        Rango = 1;
        Civilizacion = "Nipones";
        ProbabilidadEsquivar = 0.10f;
        ProbabilidadCritico = 0.30f;
        multiplicadorCritico = 2.5f;
    }

    public override void HabilidadEspecial(Unidad objetivo)
    {
        // Habilidad especial de Godzilla: Ataque masivo en linea recta que asegura critico
        float daño = Ataque * 1.5f; // Daño aumentado
        objetivo.Vida -= daño; // Ignora la defensa del objetivo
        UnityEngine.Debug.Log($"{Civilizacion}: hizo un ataque masivo con su habilidad especial!");
    }
}

/// <summary>Héroe vikingo que aplica daño de veneno.</summary>
public class jormungandr: Heroe
{
    /// <summary>Inicializa las estadísticas de Jormungandr.</summary>
    public jormungandr()
    {
        Vida = 200;
        Ataque = 15;
        Defensa = 10;
        Velocidad = 4;
        Rango = 1;
        Civilizacion = "Vikingos";
        ProbabilidadEsquivar = 0.15f;
        ProbabilidadCritico = 0.25f;
        multiplicadorCritico = 2f;
    }

    public override void HabilidadEspecial(Unidad objetivo)
    {
        // Habilidad especial de Jormungandr: Causa envenamiento como dots a los enemigos y los relentiza
        float dañoPorTurno = Ataque * 0.5f; // Daño por turno
        objetivo.Vida -= dañoPorTurno; // Aplica el daño por turno
        UnityEngine.Debug.Log($"{Civilizacion}: envenenó al enemigo con su habilidad especial!");
    }
}

/// <summary>Héroe griego especializado en petrificar enemigos.</summary>
public class Medusa: Heroe
{
    /// <summary>Inicializa las estadísticas de Medusa.</summary>
    public Medusa()
    {
        Vida = 180;
        Ataque = 18;
        Defensa = 12;
        Velocidad = 4;
        Rango = 1;
        Civilizacion = "Griegos";
        ProbabilidadEsquivar = 0.10f;
        ProbabilidadCritico = 0.30f;
        multiplicadorCritico = 2f;
    }

    public override void HabilidadEspecial(Unidad objetivo)
    {
        // Habilidad especial de Medusa: Petrificación que inmoviliza a los enemigos y reduce su defensa, tambien los embiste con su caballito
        UnityEngine.Debug.Log($"{Civilizacion}: petrificó al enemigo con su habilidad especial!");
        // Aquí podrías implementar la lógica para inmovilizar al objetivo por un turno
    }
}