using System;

public class Assassin: Unidad{

    public override void Atacar(){
    }

    public virtual void Esquivar(){
        Console.WriteLine("esquivo");
    }
}