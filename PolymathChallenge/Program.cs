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
                    WriteLine("Inicio consulta categorias API...");
                    ConsultarAPI();

                    ForegroundColor = ConsoleColor.DarkGreen;
                    WriteLine("Fin consulta categorias API");

                    ForegroundColor = ConsoleColor.White;
                    WriteLine("Reconstruyendo base de datos....");
                    ConstruirDB();

                    //WriteLine("Recompilar");
                    break;
                case Transaccion.Render:
                    //GenerarHTMLCategorizacion(123);

                    ForegroundColor = ConsoleColor.White;
                    WriteLine($"Render {idCat}");
                    break;
            }

        }


        private static void ConstruirDB()
        {
            SQLiteConnection.CreateFile("db.sqlite");

            var m_dbConnection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
            m_dbConnection.Open();

            string sql = "CREATE TABLE Categoria (name varchar(20), score int)";

CategoryID
CategoryName
CategoryLevel
BestOfferEnabled

            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            command.ExecuteNonQuery();
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

            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "categorias.xml", responseData, Encoding.UTF8);
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

        private static List<Categoria> ConsultarApiCategorias()
        {
            List<Categoria> retorno = new List<Categoria>();



            return retorno;
        }
    }
}
