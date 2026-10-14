using System;

public static class DatosResultados
{
    public static string rankingJSON = "";
    public static bool partidaCancelada = false;
    public static string mensajeFinal = "";
    public static void Limpiar()
    {
        rankingJSON = "";
        partidaCancelada = false;
        mensajeFinal = "";
    }

}