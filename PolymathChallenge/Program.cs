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
using PolymathChallenge.Logica;

namespace PolymathChallenge
{
    class Program
    {
        static void Main(string[] args)
        {
            Challenge reto = new Challenge();
            MostrarMensaje(Texto.Titulo, "POLYMATH CHALLENGE");

            double idCat = 0;
            Transaccion tx = DeterminarTarea(args, ref idCat);
            switch (tx)
            {
                case Transaccion.Recompilar:
                    
                    MostrarMensaje(Texto.Mensaje, "Consultando categorías de eBay...");
                    reto.ConsultarApi();
                    
                    MostrarMensaje(Texto.OK);

                    MostrarMensaje(Texto.Mensaje, "Reconstruyendo el repositorio local...");
                    reto.ConstruirDb();

                    MostrarMensaje(Texto.OK);

                    MostrarMensaje(Texto.Mensaje, "Cargando las categorías de eBay al repositorio...");
                    reto.CargarCategoriasDb();

                    MostrarMensaje(Texto.OK);

                    MostrarMensaje(Texto.Mensaje, "Hecho!\n");
                    break;
                case Transaccion.Render:
                    MostrarMensaje(Texto.Mensaje, "Renderizando categoría...");
                    reto.GenerarHtmlCategorizacion(idCat);
                    
                    MostrarMensaje(Texto.OK);

                    MostrarMensaje(Texto.Mensaje, $"Hecho!, puedes abrir el archivo '{idCat}.html' en tu browser.\n");
                    break;
            }
        }

        /// <summary>
        /// Muestra un mensaje en el app
        /// </summary>
        /// <param name="tipo">Indica el tipo de mensaje para colocarle un color</param>
        /// <param name="mensaje">Mensaje a mostrar en consola</param>
        private static void MostrarMensaje(Texto tipo, string mensaje = "")
        {
            switch (tipo)
            {
                case Texto.Titulo:
                    ForegroundColor = ConsoleColor.Cyan;
                    WriteLine(mensaje);
                    break;
                case Texto.Advertencia:
                    ForegroundColor = ConsoleColor.Yellow;
                    WriteLine(mensaje);
                    break;
                case Texto.OK:
                    ForegroundColor = ConsoleColor.Green;
                    WriteLine(" / OK");
                    break;
                case Texto.Mensaje:
                    ForegroundColor = ConsoleColor.White;
                    Write(mensaje);
                    break;
            }

            ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Procesa los parametros de entrada y determina que operacion se debe realizar
        /// </summary>
        /// <param name="args">Argumentos suministrados</param>
        /// <param name="idCat">Id de la categoria a renderizar</param>
        private static Transaccion DeterminarTarea(string[] args, ref double idCat)
        {
            Transaccion retorno = Transaccion.NoDefinido;

            if (args.Length == 0)
            {
                MostrarMensaje(Texto.Advertencia, "Ups! no se que hacer. lánzame con alguno de los siguientes comandos:\n--rebuild\n--render <CategoryId>");
            }
            else
            {
                switch (args.Length)
                {
                    case 1:
                        if (args[0].ToLower().Equals("--rebuild"))
                        {
                            retorno = Transaccion.Recompilar;
                        }
                        else if (args[0].ToLower().Equals("--render"))
                        {
                            MostrarMensaje(Texto.Advertencia, "no me indicaste cual categoría debo procesar.");
                        }

                        break;
                    case 2:
                        if (args[0].ToLower().Equals("--render"))
                        {
                            retorno = Transaccion.Render;
                            idCat = double.Parse(args[1]);
                        }

                        break;
                }
            }

            return retorno;
        }
    }
}
