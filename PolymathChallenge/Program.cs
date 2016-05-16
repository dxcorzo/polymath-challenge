using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolymathChallenge.Modelos;
using static System.Console;
using System.Net;
using System.IO;
using System.Xml;
using System.Data;
using System.Data.SQLite;

namespace PolymathChallenge
{
    class Program
    {
        static SQLiteConnection _conexionDb;

        static void Main(string[] args)
        {
            ForegroundColor = ConsoleColor.Cyan;
            WriteLine("POLYMATH CHALLENGE");

            double idCat = 0;
            Transaccion tx = DeterminarTarea(args, ref idCat);
            switch (tx)
            {
                case Transaccion.Recompilar:
                    ForegroundColor = ConsoleColor.White;
                    Write("Consultando API...");
                    ConsultarAPI();

                    ForegroundColor = ConsoleColor.Green;
                    WriteLine(" / OK");

                    ForegroundColor = ConsoleColor.White;
                    Write("Reconstruyendo db...");
                    ConstruirDB();

                    ForegroundColor = ConsoleColor.Green;
                    WriteLine(" / OK");

                    ForegroundColor = ConsoleColor.White;
                    Write("Cargando info a db ...");
                    CargarCategoriasDB();

                    ForegroundColor = ConsoleColor.Green;
                    WriteLine(" / OK");
                    break;
                case Transaccion.Render:
                    ForegroundColor = ConsoleColor.White;
                    Write("Renderizando categoria...");
                    GenerarHTMLCategorizacion(idCat);

                    ForegroundColor = ConsoleColor.Green;
                    WriteLine(" / OK");
                    break;
            }
        }

        private static void GenerarHTMLCategorizacion(double idCategoria)
        {
            using (_conexionDb = new SQLiteConnection(Configuracion.CadenaConexion))
            {
                _conexionDb.Open();
                using (var cmd = new SQLiteCommand(_conexionDb))
                {
                    using (var transaction = _conexionDb.BeginTransaction())
                    {
                        string sql = $"SELECT * FROM Categoria";
                        cmd.CommandText = sql;
                        SQLiteDataReader reader = cmd.ExecuteReader();

                        DataTable table = new DataTable();
                        table.Load(reader);
                        DataRow[] parentMenus = table.Select($"ParentId = {idCategoria}");

                        var sb = new StringBuilder();
                        string unorderedList = GenerarArbol(parentMenus, table, sb);

                        File.WriteAllText(string.Format(Configuracion.PathHtml, idCategoria), unorderedList);

                        transaction.Commit();
                    }
                }
            }
        }

        private static string GenerarArbol(DataRow[] menu, DataTable table, StringBuilder sb)
        {
            sb.AppendLine("<ul>");

            if (menu.Length > 0)
            {
                foreach (DataRow dr in menu)
                {
                    string menuText = dr["Name"].ToString();
                    string line = $@"<li><label>{menuText}</label>";
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

        private static void CargarCategoriasDB()
        {
            DataSet ds = new DataSet();
            ds.ReadXml(new XmlTextReader(Configuracion.PathXml));

            using (_conexionDb = new SQLiteConnection(Configuracion.CadenaConexion))
            {
                _conexionDb.Open();

                using (var cmd = new SQLiteCommand(_conexionDb))
                {
                    using (var transaction = _conexionDb.BeginTransaction())
                    {
                        foreach (DataRow row in ds.Tables["Category"].Rows)
                        {
                            string sql = string.Format
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

        private static void ConstruirDB()
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

        private static void ConsultarAPI()
        {
            //Crear peticion
            var r = WebRequest.Create("https://api.sandbox.ebay.com/ws/api.dll");

            r.Method = "POST";
            r.ContentType = "text/xml";
            r.Timeout = 1000000;

            //Asociar encabezados
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

            File.WriteAllText(Configuracion.PathXml, responseData, Encoding.UTF8);
        }
        
        private static Transaccion DeterminarTarea(string[] args, ref double idCat)
        {
            Transaccion retorno = Transaccion.NoDefinido;

            if (args.Length == 0)
            {
                ForegroundColor = ConsoleColor.Yellow;
                WriteLine("Ups! no me fue suministrado ningun comando.");
            }
            else
            {
                if (args.Length == 1)
                {
                    if (args[0].ToLower().Equals("--rebuild"))
                    {
                        retorno = Transaccion.Recompilar;
                    }
                }

                if (args.Length == 2)
                {
                    if (args[0].ToLower().Equals("--render"))
                    {
                        retorno = Transaccion.Render;
                        idCat = double.Parse(args[1]);
                    }
                }
            }

            return retorno;
        }
    }
}
