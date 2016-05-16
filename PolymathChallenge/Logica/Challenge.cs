using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using PolymathChallenge.Modelos;

namespace PolymathChallenge.Logica
{
    public class Challenge
    {
        /// <summary>
        /// Propiedad que gestiona la conexion a base de datos
        /// </summary>
        private SQLiteConnection _conexionDb;

        /// <summary>
        /// Genera el HTML de una categoria, renderizado como listas
        /// </summary>
        /// <param name="idCategoria">Id de la categoria desde donde se desea comenzar a procesar el arbol</param>
        public void GenerarHtmlCategorizacion(double idCategoria)
        {
            using (_conexionDb = new SQLiteConnection(Configuracion.CadenaConexion))
            {
                _conexionDb.Open();

                using (var cmd = new SQLiteCommand(_conexionDb))
                {
                    using (var transaction = _conexionDb.BeginTransaction())
                    {
                        //Cargar la info desde SQLite
                        string sql = "SELECT * FROM Categoria";
                        cmd.CommandText = sql;
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        //La solucion propuesta requiere del metodo "Select" que solo esta presente en el objeto datatable
                        //por eso se procede a realizar la conversion del reader a datatable
                        DataTable table = new DataTable();
                        table.Load(reader);
                        DataRow[] parentMenus = table.Select($"ParentId = {idCategoria}");

                        //Armar recursivamente el arbol
                        var sb = new StringBuilder();
                        string html = GenerarArbol(parentMenus, table, sb);
                        
                        //Guardar el html
                        File.WriteAllText(string.Format(Configuracion.PathHtml, idCategoria), string.Format(Configuracion.TemplateHtml, html));

                        transaction.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Genera recursivamente un arbol con listas
        /// </summary>
        /// <param name="menu">Items parent</param>
        /// <param name="table">Tabla con todas las categorias</param>
        /// <param name="sb">Maneja el HTML generado</param>
        public string GenerarArbol(DataRow[] menu, DataTable table, StringBuilder sb)
        {
            sb.AppendLine("<ul>");

            if (menu.Length > 0)
            {
                foreach (DataRow dr in menu)
                {
                    string line = $"<li><label>{dr["Id"]} - {dr["Name"]}</label>[<small>Level: {dr["Level"]} | Best offer: {dr["BestOfferEnabled"]}</small>]";
                    sb.Append(line);

                    string pid = dr["Id"].ToString();
                    string parentId = dr["ParentId"].ToString();

                    DataRow[] subMenu = table.Select($"ParentId = {pid}");
                    if (subMenu.Length > 0 && !pid.Equals(parentId))
                    {
                        var subMenuBuilder = new StringBuilder();
                        sb.Append(GenerarArbol(subMenu, table, subMenuBuilder));
                    }

                    sb.Append("</li>");
                }
            }
            sb.Append("</ul>");
            return sb.ToString();
        }

        /// <summary>
        /// Basado en el XML temporal generado por el API, carga las categorias a la DB
        /// </summary>
        public void CargarCategoriasDb()
        {
            //Leer el XML y cargarlo en el DS, es la forma más facil de procesar un archivo XML
            DataSet ds = new DataSet();
            ds.ReadXml(new XmlTextReader(Configuracion.PathXml));

            using (_conexionDb = new SQLiteConnection(Configuracion.CadenaConexion))
            {
                _conexionDb.Open();

                using (var cmd = new SQLiteCommand(_conexionDb))
                {
                    //Colocar la ejecucion del comando bajo una transaccion, es lo más parecido a ejecutar un bulk...
                    using (var transaction = _conexionDb.BeginTransaction())
                    {
                        foreach (DataRow row in ds.Tables["Category"].Rows)
                        {
                            string sql = String.Format
                            ("INSERT INTO Categoria (ID, ParentID, Name, Level, BestOfferEnabled) VALUES ({0}, {1}, '{2}', {3}, {4})",
                                row["CategoryID"],
                                row["CategoryParentID"],
                                row["CategoryName"].ToString().Replace("'", "''"),
                                row["CategoryLevel"],
                                row["BestOfferEnabled"].ToString() == "true" ? "1" : "0"
                            );

                            cmd.CommandText = sql;
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Arma la base de datos en blanco
        /// </summary>
        public void ConstruirDb()
        {
            //Crear db
            SQLiteConnection.CreateFile(Configuracion.NombreDb);

            using (_conexionDb = new SQLiteConnection(Configuracion.CadenaConexion))
            {
                _conexionDb.Open();

                using (var cmd = new SQLiteCommand(_conexionDb))
                {
                    using (var transaction = _conexionDb.BeginTransaction())
                    {
                        string sql = "CREATE TABLE Categoria (Id INT, ParentId INT, Name VARCHAR(100), Level INT, BestOfferEnabled BIT)";
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                }
            }
        }


        /// <summary>
        /// Consulta la información de las categorias del catalogo de eBay
        /// </summary>
        public void ConsultarApi()
        {
            //Crear peticion
            var r = WebRequest.Create("https://api.sandbox.ebay.com/ws/api.dll");

            r.Method = "POST";
            r.ContentType = "text/xml";
            r.Timeout = 1000000;

            //Asociar encabezados segun ejemplo enviado
            r.Headers.Add("X-EBAY-API-CALL-NAME", "GetCategories");
            r.Headers.Add("X-EBAY-API-APP-NAME", "EchoBay62-5538-466c-b43b-662768d6841");
            r.Headers.Add("X-EBAY-API-CERT-NAME", "00dd08ab-2082-4e3c-9518-5f4298f296db");
            r.Headers.Add("X-EBAY-API-DEV-NAME", "16a26b1b-26cf-442d-906d-597b60c41c19");
            r.Headers.Add("X-EBAY-API-SITEID", "0");
            r.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "861");

            //Armar cuerpo de la peticion
            string body = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            body += "<GetCategoriesRequest xmlns=\"urn:ebay:apis:eBLBaseComponents\">";
            body += "<RequesterCredentials><eBayAuthToken>AgAAAA**AQAAAA**aAAAAA**PMIhVg**nY+sHZ2PrBmdj6wVnY+sEZ2PrA2dj6wFk4GhCpaCpQWdj6x9nY+seQ**L0MCAA**AAMAAA**IahulXaONmBwi/Pzhx0hMqjHhVAz9/qrFLIkfGH5wFH8Fjwj8+H5FN4NvzHaDPFf0qQtPMFUaOXHpJ8M7c2OFDJ7LBK2+JVlTi5gh0r+g4I0wpNYLtXnq0zgeS8N6KPl8SQiGLr05e9TgLRdxpxkFVS/VTVxejPkXVMs/LCN/Jr1BXrOUmVkT/4Euuo6slGyjaUtoqYMQnmBcRsK4xLiBBDtiow6YHReCJ0u8oxBeVZo3S2jABoDDO9DHLt7cS73vPQyIbdm2nP4w4BvtFsFVuaq6uMJAbFBP4F/v/U5JBZUPMElLrkXLMlkQFAB3aPvqZvpGw7S8SgL7d2s0GxnhVSbh4QAqQrQA0guK7OSqNoV+vl+N0mO24Aw8whOFxQXapTSRcy8wI8IZJynn6vaMpBl5cOuwPgdLMnnE+JvmFtQFrxa+k/9PRoVFm+13iGoue4bMY67Zcbcx65PXDXktoM3V+sSzSGhg5M+R6MXhxlN3xYfwq8vhBQfRlbIq+SU2FhicEmTRHrpaMCk4Gtn8CKNGpEr1GiNlVtbfjQn0LXPp7aYGgh0A/b8ayE1LUMKne02JBQgancNgMGjByCIemi8Dd1oU1NkgICFDbHapDhATTzgKpulY02BToW7kkrt3y6BoESruIGxTjzSVnSAbGk1vfYsQRwjtF6BNbr5Goi52M510DizujC+s+lSpK4P0+RF9AwtrUpVVu2PP8taB6FEpe39h8RWTM+aRDnDny/v7wA/GkkvfGhiioCN0z48</eBayAuthToken></RequesterCredentials>";
            body += "<CategorySiteID>0</CategorySiteID><DetailLevel>ReturnAll</DetailLevel>";
            body += "</GetCategoriesRequest>";

            byte[] buf = Encoding.UTF8.GetBytes(body);
            r.GetRequestStream().Write(buf, 0, buf.Length);

            //Procesar resultado
            StreamReader responseReader = new StreamReader(r.GetResponse().GetResponseStream());
            var responseData = responseReader.ReadToEnd();

            //guardar XML que servira para hacer el bulk
            File.WriteAllText(Configuracion.PathXml, responseData, Encoding.UTF8);
        }
    }
}