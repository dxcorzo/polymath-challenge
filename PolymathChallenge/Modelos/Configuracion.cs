using System;

namespace PolymathChallenge.Modelos
{
    /// <summary>
    /// Parametros de configuracion del app
    /// </summary>
    public static class Configuracion
    {
        public static string PathXml = AppDomain.CurrentDomain.BaseDirectory + "categorias.xml";
        public static string NombreDb = "db.sqlite";
        public static string CadenaConexion = "Data Source=db.sqlite;Version=3;";
        public static string PathHtml = AppDomain.CurrentDomain.BaseDirectory + "{0}.html";
        public static string TemplateHtml = "<!DOCTYPE html><html><head><style>body{{font-size:12px;font-family:arial;}}label{{padding:3px;margin-right:5px;}}small{{color:#999;}}</style></head><body>{0}</body></html>";
    }
}