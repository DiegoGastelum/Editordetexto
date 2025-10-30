using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Editordetexto
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            compilarSoluciónToolStripMenuItem.Enabled = false;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            P_Reservadas = new List<string>
            {
                "void","int","float","double","char","short","long",
                "signed","unsigned","const","volatile","restrict",
                "auto","register","static","extern","typedef","inline",
                "if","else","switch","case","default",
                "for","while","do","break","continue","goto","return",
                "struct","union","enum","sizeof",
                "_Bool","_Complex","_Imaginary",
                "_Alignas","_Alignof","_Atomic","_Generic","_Noreturn",
                "_Static_assert","_Thread_local",
                "define","include","ifdef","ifndef","endif","undef",
                "error","pragma","line",
                "main","printf","scanf","gets","puts","fgets","fputs",
                "fopen","fclose","fread","fwrite",
                "malloc","calloc","realloc","free",
                "exit","abort","system","NULL"
            };
        }


        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.Filter = "Texto|*.c";
            if (VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaAbrir.FileName;
                using (StreamReader Leer = new StreamReader(archivo))
                {
                    CajaTxt1.Text = Leer.ReadToEnd();
                }

            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }
        private void guardar()
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";
            if (archivo != null)
            {
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(CajaTxt1.Text);
                }
            }
            else
            {
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName;
                    using (StreamWriter Escribir = new StreamWriter(archivo))
                    {
                        Escribir.Write(CajaTxt1.Text);
                    }
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
        }
        private void gurdarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            guardar();

        }

        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CajaTxt1.Clear();
            archivo = null;

        }

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";
            if (VentanaGuardar.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaGuardar.FileName;
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(CajaTxt1.Text);
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private char Tipo_caracter(int caracter)
        {
            if (caracter >= 65 && caracter <= 90 || caracter >= 97 && caracter <= 122) return 'l'; // Letra
            else if (caracter >= 48 && caracter <= 57) return 'd'; // Dígito
            else
            {
                switch (caracter)
                {
                    case 10: return 'n'; // Salto de línea
                    case 34: return '"'; // Comillas dobles
                    case 39: return 'c'; // Comilla simple
                    case 32: return 'e'; // Espacio
                    case 47: return '/'; // Posible comentario
                    default: return 's'; // Otro símbolo
                }
            }
        }
        private void Simbolo()
        {
            if (i_caracter == 10)
            {
                Numero_linea++;
                elemento = "LF\n";
            }
            else if (i_caracter == 33 ||
                     i_caracter >= 35 && i_caracter <= 38 ||
                     i_caracter >= 40 && i_caracter <= 45 ||
                     i_caracter == 47 ||
                     i_caracter >= 58 && i_caracter <= 62 ||
                     i_caracter == 91 || i_caracter == 93 ||
                     i_caracter == 94 || i_caracter == 123 ||
                     i_caracter == 124 || i_caracter == 125)
            {
                elemento = ((char)i_caracter).ToString() + "\n";
            }
            else
            {
                Error(i_caracter);
            }
        }

        private void Cadena()
        {
            do
            {
                i_caracter = Leer.Read();
                if (i_caracter == 10) Numero_linea++; // Contar saltos dentro de cadenas
            } while (i_caracter != 34 && i_caracter != -1);

            if (i_caracter == -1) Error(-1);
        }

        private void Caracter()
        {
            i_caracter = Leer.Read();
            if (i_caracter == 10) Numero_linea++; // Contar salto dentro de caracter
            if (i_caracter != 39) Error(39);
        }

        private void Error(int i_caracter)
        {
            TxtboxSalida.AppendText($"Error léxico '{(char)i_caracter}', línea {Numero_linea}\n");
            N_error++;
        }
        private void Error(string mensaje)
        {
            TxtboxSalida.AppendText($"Error sintáctico: {mensaje}, línea {Numero_linea}\n");
            N_error++;
        }
        private void Error(string token, string esperado)
        {
            TxtboxSalida.AppendText($"Error: se esperaba '{esperado}', pero se encontró '{token}', línea {Numero_linea}\n");
            N_error++;
        }

        private void Archivo_Libreria()
        {
            i_caracter = Leer.Read(); 

            if ((char)i_caracter == 'h')
            {
                Escribir.Write("libreria\n"); // token completo
                i_caracter = Leer.Read();
            }
            else
            {
                Error(i_caracter);
            }
        }

        private bool Palabra_Reservada()
        {
            if (P_Reservadas.IndexOf(elemento.ToLower()) >= 0) return true;
            return false;
        }
        private void Identificador()
        {
            do
            {
                elemento = elemento + (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.') { Archivo_Libreria(); }
            else
            {
                if (Palabra_Reservada()) Escribir.Write(elemento.ToLower() + "\n");
                else Escribir.Write("identificador\n");
            }
        }

        private void Numero_Real()
        {
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');

            Escribir.Write("numero_real\n");
        }
        private void Numero()
        {
            if ((char)i_caracter == '-')
            {
                i_caracter = Leer.Read();
            }

            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                Numero_Real();
                return;
            }

            Escribir.Write("numero_entero\n");
        }

        private void CajaTxt1_TextChanged(object sender, EventArgs e)
        {
            compilarSoluciónToolStripMenuItem.Enabled = true;

        }

        private bool Comentario()
        {
            i_caracter = Leer.Read();

            if (i_caracter == 10) Numero_linea++;

            switch (i_caracter)
            {
                case 47: // //
                    while (i_caracter != -1 && i_caracter != 10)
                    {
                        i_caracter = Leer.Read();
                    }
                    if (i_caracter == 10) Numero_linea++; // contar línea del salto final
                    return true;

                case 42: // /* */
                    int prev = 0;
                    while (i_caracter != -1)
                    {
                        prev = i_caracter;
                        i_caracter = Leer.Read();
                        if (i_caracter == 10) Numero_linea++; 
                        if (prev == 42 && i_caracter == 47) break; // fin de bloque
                    }
                    if (i_caracter == -1) Error(-1);
                    return true;

                default: return false;
            }
        }

        private void compilarSoluciónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TxtboxSalida.Text = "";
            guardar();

            elemento = "";
            N_error = 0;
            Numero_linea = 1;

            archivoback = archivo.Remove(archivo.Length - 1) + "back";
            Escribir = new StreamWriter(archivoback);
            Leer = new StreamReader(archivo);

            i_caracter = Leer.Read();

            do
            {
                elemento = "";

                if ((char)i_caracter == '/')
                {
                    if (Comentario())
                    {
                        Escribir.Write("Comentario\n");
                        continue;
                    }
                }

                switch (Tipo_caracter(i_caracter))
                {
                    case 'l': Identificador(); break;
                    case 'd': Numero(); break;
                    case 's': Simbolo(); Escribir.Write(elemento); i_caracter = Leer.Read(); break;
                    case '"': Cadena(); Escribir.Write("Cadena\n"); i_caracter = Leer.Read(); break;
                    case 'c': Caracter(); Escribir.Write("Caracter\n"); i_caracter = Leer.Read(); break;
                    case 'n': i_caracter = Leer.Read(); Numero_linea++; Escribir.Write("LF\n"); break;
                    case 'e': i_caracter = Leer.Read(); break;
                    default: Error(i_caracter); break;
                }
            } while (i_caracter != -1);

            Escribir.Write("Fin\n");

            TxtboxSalida.AppendText("Errores: " + N_error);
            Escribir.Close();
            Leer.Close();
            AnalizadorSintactico();
        }

        private void Cabecera()
        {
            token = Leer.ReadLine();
            if (token == null || token == "Fin") return;

            if (token == "LF") 
            {
                Numero_linea++;
                Cabecera();
                return;
            }

            switch (token)
            {
                case "#":
                    token = Leer.ReadLine();
                    if (token == null) { Error("Directiva incompleta después de '#'"); return; }
                    Directiva_proc();
                    Cabecera();
                    break;

                case "int":
                case "Tipo":
                    Declaracion();
                    Cabecera();
                    break;

                default:
                    Cabecera();
                    break;
            }
        }

        private void AnalizadorSintactico()
        {
            Numero_linea = 1;
            Leer = new StreamReader(archivoback);
            token = Leer.ReadLine();
            Cabecera();
            Leer.Close();
        }

        private int Directiva_include()
        {
            while (token == "LF")
            {
                Numero_linea++;
                token = Leer.ReadLine();
            }

            if (token == "<")
            {
                token = Leer.ReadLine(); // debe ser "libreria"
                if (token == "libreria")
                {
                    token = Leer.ReadLine(); // debe ser ">"
                    if (token == ">")
                    {
                        return 1;
                    }
                    else
                    {
                        Error("Falta '>' en include"); // Número de línea correcto
                        return 0;
                    }
                }
                else
                {
                    Error("Falta nombre de librería");
                    return 0;
                }
            }
            else if (token == "Cadena" || token == "cadena")
            {
                return 1;
            }
            else
            {
                Error("Sintaxis de include inválida");
                return 0;
            }
        }

        private int Directiva_proc()
        {
            while (token == "LF")
            {
                token = Leer.ReadLine();
            }

            if (token == null)
            {
                Error("Directiva incompleta después de '#'");
                return 0;
            }

            switch (token)
            {
                case "include":
                    token = Leer.ReadLine();
                    if (token != null) Numero_linea++;
                    while (token == "LF") token = Leer.ReadLine();
                    if (token == null)
                    {
                        Error("Include incompleto");
                        return 0;
                    }
                    return Directiva_include();

                case "define":
                    token = Leer.ReadLine();
                    if (token != null) Numero_linea++;
                    while (token == "LF") token = Leer.ReadLine();
                    if (token == null)
                    {
                        Error("Directiva 'define' incompleta. Formato sugerido: # define IDENTIFICADOR VALOR");
                        return 0;
                    }

                    if (token == "<")
                    {
                        token = Leer.ReadLine();
                        if (token == "libreria")
                        {
                            token = Leer.ReadLine();
                            if (token == ">")
                            {
                                return 1;
                            }
                            else
                            {
                                Error("Falta '>' en define con librería");
                                return 0;
                            }
                        }
                        else
                        {
                            Error("Nombre de librería inválido en define");
                            return 0;
                        }
                    }
                    else
                    {
                        string posibleValor = Leer.ReadLine();
                        return 1;
                    }

                default:
                    Error("Se esperaba 'include' o 'define' después de '#'");
                    return 0;
            }
        }

        private void Declaracion()
        {
            token = Leer.ReadLine();
            if (token != null) Numero_linea++;

            if (token == "identificador")
            {
                Dec_VGlobal();
            }
            else if (token == "main")
            {
                do
                {
                    token = Leer.ReadLine();
                    if (token == null || token == "Fin") break;
                } while (token != "{");
            }
            else if (token == ";")
            {
                token = Leer.ReadLine();
            }
            else
            {
                Error("Falta identificador en declaración");
                token = Leer.ReadLine();
            }
        }

        private void D_Arreglos()
        {
            while (token == "[")
            {
                token = Leer.ReadLine();
                if (token != null) Numero_linea++;

                if (token == "numero_entero" || token == "identificador")
                {
                    token = Leer.ReadLine();
                    if (token != null) Numero_linea++;
                    if (token != "]")
                    {
                        Error(token, "]");
                        return;
                    }
                    token = Leer.ReadLine();
                    if (token != null) Numero_linea++;
                }
                else
                {
                    Error(token, "número entero o identificador para tamaño del arreglo");
                    return;
                }
            }

            // Inicialización opcional
            if (token == "=")
            {
                token = Leer.ReadLine();
                if (token == "{")
                {
                    BloqueInicializacion();
                    if (token != ";")
                    {
                        Error(token, ";");
                    }
                    else
                    {
                        token = Leer.ReadLine();
                    }
                }
                else
                {
                    Error(token, "{");
                }
            }
            else if (token == ";")
            {
                token = Leer.ReadLine();
            }
            else
            {
                Error(token, "declaración válida para arreglos");
            }
        }

        private void Dec_VGlobal()
        {
            if (token != "identificador")
            {
                Error("Se esperaba un identificador");
                return;
            }
            token = Leer.ReadLine();

            if (token == "[")
            {
                D_Arreglos();
                return; 
            }

            // Inicialización opcional
            if (token == "=")
            {
                token = Leer.ReadLine();
                if (token != "numero_entero" && token != "numero_real" &&
                    token != "identificador" && token != "Cadena" && token != "caracter")
                {
                    Error(token, "valor válido para inicialización");
                    return;
                }
                token = Leer.ReadLine();
            }

            if (token != ";")
            {
                Error(token, ";");
                return;
            }

            token = Leer.ReadLine(); 
        }

        private void BloqueInicializacion()
        {
            if (token != "{")
            {
                Error(token, "{");
                return;
            }

            token = Leer.ReadLine(); 

            while (token != "}")
            {
                if (token == "{")
                {
                    BloqueInicializacion();
                }
                else if (token == "numero_entero" || token == "numero_real" || token == "identificador" || token == "Cadena" || token == "caracter")
                {
                    token = Leer.ReadLine();
                }
                else
                {
                    Error(token, "valor válido o sub-arreglo dentro de inicialización");
                    return;
                }

                if (token == ",")
                {
                    token = Leer.ReadLine();
                }
                else if (token == "}")
                {
                    break; 
                }
                else
                {
                    Error(token, "',' o '}'");
                    return;
                }
            }

            token = Leer.ReadLine();
        }
        private void TxtboxSalida_TextChanged(object sender, EventArgs e)
        {
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }
        private void compilarToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
        private void traducciónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CajaTxt1.Text)) return;

            string archivoTrad = archivo.Remove(archivo.Length - 1) + "trad";

            Dictionary<string, string> traducciones = new Dictionary<string, string>
    {
        // Tipos de datos y modificadores
        { "void", "vacío" }, { "int", "entero" }, { "float", "flotante" },
        { "double", "doble" }, { "char", "carácter" }, { "short", "corto" },
        { "long", "largo" }, { "signed", "conSigno" }, { "unsigned", "sinSigno" },
        { "const", "constante" }, { "volatile", "volátil" }, { "restrict", "restringido" },

        // Almacenamiento
        { "auto", "automático" }, { "register", "registro" }, { "static", "estático" },
        { "extern", "externo" }, { "typedef", "tipoDefinido" }, { "inline", "enLinea" },

        // Control de flujo
        { "if", "si" }, { "else", "sino" }, { "switch", "según" }, { "case", "caso" },
        { "default", "defecto" }, { "for", "para" }, { "while", "mientras" }, { "do", "hacer" },
        { "break", "romper" }, { "continue", "continuar" }, { "goto", "irA" }, { "return", "retornar" },

        // Estructuras de datos
        { "struct", "estructura" }, { "union", "union" }, { "enum", "enumeración" }, { "sizeof", "tamaño" },

        // Preprocesador
        { "define", "definir" }, { "include", "incluir" }, { "ifdef", "siDefinido" },
        { "ifndef", "siNoDefinido" }, { "endif", "finSi" }, { "undef", "noDefinir" },
        { "error", "error" }, { "pragma", "pragma" }, { "line", "linea" },

        // Funciones comunes
        { "main", "principal" }, { "printf", "imprimir" }, { "scanf", "leer" },
        { "gets", "obtener" }, { "puts", "imprimirLinea" }, { "fgets", "leerArchivo" },
        { "fputs", "imprimirArchivo" }, { "fopen", "abrirArchivo" }, { "fclose", "cerrarArchivo" },
        { "fread", "leerBinario" }, { "fwrite", "escribirBinario" }, { "malloc", "reservarMemoria" },
        { "calloc", "reservarMemoriaCero" }, { "realloc", "reubicarMemoria" }, { "free", "liberarMemoria" },
        { "exit", "salir" }, { "abort", "abortar" }, { "system", "sistema" }, { "NULL", "NULO" }
    };

            string contenido = CajaTxt1.Text;
            StringBuilder traducido = new StringBuilder();

            bool dentroComentarioLinea = false;
            bool dentroComentarioBloque = false;
            bool dentroCadena = false;

            for (int i = 0; i < contenido.Length; i++)
            {
                char c = contenido[i];

                // Detectar inicio y fin de comentarios y cadenas
                if (!dentroCadena && !dentroComentarioBloque && c == '/' && i + 1 < contenido.Length && contenido[i + 1] == '/')
                {
                    dentroComentarioLinea = true;
                    traducido.Append(c);
                    continue;
                }
                if (!dentroCadena && !dentroComentarioLinea && c == '/' && i + 1 < contenido.Length && contenido[i + 1] == '*')
                {
                    dentroComentarioBloque = true;
                    traducido.Append(c);
                    continue;
                }
                if (dentroComentarioLinea && c == '\n') dentroComentarioLinea = false;
                if (dentroComentarioBloque && c == '*' && i + 1 < contenido.Length && contenido[i + 1] == '/')
                {
                    dentroComentarioBloque = false;
                    traducido.Append(c);
                    i++; 
                    traducido.Append('/');
                    continue;
                }
                if (!dentroComentarioLinea && !dentroComentarioBloque && c == '"') dentroCadena = !dentroCadena;

                if (dentroComentarioLinea || dentroComentarioBloque || dentroCadena)
                {
                    traducido.Append(c);
                    continue;
                }

                // Detectar palabras completas para traducir
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < contenido.Length && (char.IsLetterOrDigit(contenido[i]) || contenido[i] == '_')) i++;
                    string palabra = contenido.Substring(start, i - start);
                    if (traducciones.ContainsKey(palabra))
                    {
                        traducido.Append(traducciones[palabra]);
                    }
                    else
                    {
                        traducido.Append(palabra);
                    }
                    i--; 
                }
                else
                {
                    traducido.Append(c);
                }
            }

            // Guardar en archivo .trad
            using (StreamWriter writer = new StreamWriter(archivoTrad))
            {
                writer.Write(traducido.ToString());
            }
        }
    }
}