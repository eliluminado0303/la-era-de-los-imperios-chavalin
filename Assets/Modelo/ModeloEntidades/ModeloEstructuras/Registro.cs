using System;
public static class Registro
{
  public static Action<string> Escribir = mensaje => Console.WriteLine(mensaje);
}
