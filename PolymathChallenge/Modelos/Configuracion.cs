using System;

namespace PolymathChallenge.Modelos
{
    public static class Configuracion
    {
        public static string PathXml = AppDomain.CurrentDomain.BaseDirectory + "categorias.xml";
        public static string NombreDb = "db.sqlite";
        public static string CadenaConexion = "Data Source=db.sqlite;Version=3;";
        public static string PathHtml = AppDomain.CurrentDomain.BaseDirectory + "{0}.html";
    }
}