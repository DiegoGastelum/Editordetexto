using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO; // Librería para manejo de archivos
using System.Linq;
using System.Text;
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

        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CajaTxt1.Clear(); // Limpia el contenido del RichTextBox
            archivo = null;   // Resetea la variable que almacena la ruta del archivo
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog(); // Crea un diálogo para seleccionar archivo
            VentanaAbrir.Filter = "Texto|*.c";
            if (VentanaAbrir.ShowDialog() == DialogResult.OK)    
            {
                archivo = VentanaAbrir.FileName; // Guarda la ruta del archivo seleccionado
                using (StreamReader Leer = new StreamReader(archivo)) // Abre y lee el archivo
                {
                    CajaTxt1.Text = Leer.ReadToEnd(); // Muestra el contenido en el RichTextBox
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
            compilarSoluciónToolStripMenuItem.Enabled = true;
            this.Text = Path.GetFileName(archivo); // Muestra el nombre del archivo en la barra de título
        }

        private void gurdarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            guardar();
        }

        private void guardar()
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog(); // Crea un diálogo para guardar archivo
            VentanaGuardar.Filter = "Texto|*.c";
            if (archivo != null)
            {
                using (StreamWriter Escribir = new StreamWriter(archivo)) // Abre el archivo para escribir
                {
                    Escribir.Write(CajaTxt1.Text); // Guarda el contenido del RichTextBox
                }
                this.Text = Path.GetFileName(archivo); // Actualiza el nombre en la barra de título
            }
            else
            {
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName; // Guarda la ruta del nuevo archivo
                    using (StreamWriter Escribir = new StreamWriter(archivo))
                    {
                        Escribir.Write(CajaTxt1.Text); // Guarda el contenido
                    }
                    this.Text = Path.GetFileName(archivo); // Actualiza el título del formulario
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
        }

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog(); // Crea un diálogo para guardar archivo con nuevo nombre
            VentanaGuardar.Filter = "Texto|*.c";
            if (VentanaGuardar.ShowDialog() == DialogResult.OK) 
            {
                archivo = VentanaGuardar.FileName; // Guarda la ruta seleccionada
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(CajaTxt1.Text); // Guarda el contenido del RichTextBox
                }
                this.Text = Path.GetFileName(archivo); 
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Cierra la aplicación
        }

        private char Tipo_caracter(int caracter)
        {
            if (caracter >= 65 && caracter <= 90 || caracter >= 97 && caracter <= 122) { return 'l'; } //letra 
            else
            {
                if (caracter >= 48 && caracter <= 57) { return 'd'; } //digito 
                else
                {
                    switch (caracter)
                    {
                        case 10: return 'n'; //salto de linea
                        case 34: return '"';//inicio de cadena
                        case 39: return 'c';//inicio de caracter
                        case 32: return 'e';//espacio
                        case 33: // !
                        case 35: // #
                        case 36: // $
                        case 37: // %
                        case 38: // &
                        case 40: // (
                        case 41: // )
                        case 42: // *
                        case 43: // +
                        case 44: // ,
                        case 45: // -
                        case 46: // .
                        case 47: // /
                        case 58: // :
                        case 59: // ;
                        case 60: // <
                        case 61: // =
                        case 62: // >
                        case 91: // [
                        case 93: // ]
                        case 94: // ^
                        case 123: // {
                        case 124: // |
                        case 125: // }
                            return 's'; // símbolo
                        default:
                            return 's'; // cualquier otro lo tratamos como símbolo
                    }
                    ;

                }
            }

        }

        private void Simbolo()
        {
            if (i_caracter == 33 ||
                i_caracter >= 35 && i_caracter <= 38 ||
                i_caracter >= 40 && i_caracter <= 45 ||
                i_caracter == 47 ||
                i_caracter >= 58 && i_caracter <= 62 ||
                i_caracter == 91 ||
                i_caracter == 93 ||
                i_caracter == 94 ||
                i_caracter == 123 ||
                i_caracter == 124 ||
                i_caracter == 125 ||
                i_caracter == 46 || // punto .
                i_caracter == 63 || // signo de interrogación ?
                i_caracter == 126   // tilde ~
                ) 
            { 
                elemento = "Simbolo\n"; // simbolos validos
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
                if (i_caracter == 10) Numero_linea++;

            } while (i_caracter != 34 && i_caracter != -1);
            if (i_caracter == -1) Error(-1);
        }

        private void Caracter()
        {
            i_caracter = Leer.Read();

            if (i_caracter == '\\')
            {
                i_caracter = Leer.Read(); // leemos el siguiente después de '\'
                if (i_caracter == 'n' ||  // salto de línea
                    i_caracter == 't' ||  // tabulación
                    i_caracter == 'r' ||  // retorno de carro
                    i_caracter == '0' ||  // carácter nulo
                    i_caracter == '\\' || // barra invertida
                    i_caracter == '\'' || // comilla simple
                    i_caracter == '\"')   // comilla doble
                {
                    i_caracter = Leer.Read();
                }
                else
                {
                    Error(i_caracter); 
                }
            }
            else
            {
                i_caracter = Leer.Read();
            }

            // Validar que termine con comilla simple '
            if (i_caracter != 39)
                Error(39);
        }

        private bool Comentario()
        {
            i_caracter = Leer.Read();
            switch (i_caracter)
            {
                case 47:
                    do
                    {
                        i_caracter = Leer.Read();
                    } while (i_caracter != 10);
                    return true;
                case 42: 
                    do
                    {
                        do
                        {
                            i_caracter = Leer.Read();
                            if (i_caracter == 10) { Numero_linea++; }
                        } while (i_caracter != 42 && i_caracter != -1);
                        i_caracter= Leer.Read();
                    } while (i_caracter != 47 && i_caracter != -1);
                    if (i_caracter == -1) { Error(i_caracter);}
                    i_caracter = Leer.Read();
                    return true;
                default:return false;
            }
        }
        private void Error(int i_caracter)
        {
            TxtboxSalida.AppendText("Error léxico " + (char)i_caracter + ", línea " + Numero_linea + "\n");
            N_error++;
        }

        private void Archivo_Libreria()
        {
            i_caracter = Leer.Read();
            if ((char)i_caracter == 'h') { elemento = "Archivo Libreria\n"; i_caracter = Leer.Read(); }
            else { Error(i_caracter); }
        }
        private bool Palabra_Reservada()
        {
            if (P_Reservadas.IndexOf(elemento) >= 0) return true;
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
                if (Palabra_Reservada()) elemento = "Palabra Reservada\n";
                else elemento = "identificador\n";
            }

        }

        private void Numero_Real()
        {
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');
            elemento = "numero_real\n";
        }
        private void Numero()
        {
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');
            if ((char)i_caracter == '.') { Numero_Real(); }
            else
            {
                elemento = "numero_entero\n";
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

                switch (Tipo_caracter(i_caracter))
                {
                    case 'l':
                        Identificador();
                        Escribir.Write(elemento);
                        break;

                    case 'd':
                        Numero();
                        Escribir.Write(elemento);
                        break;

                    case 's':
                        if (i_caracter == 47) // '/'
                        {
                            int siguiente = Leer.Peek(); // Ver el siguiente caracter 
                            if (siguiente == 47 || siguiente == 42) // "//" o "/*"
                            {
                                if (Comentario())
                                {
                                    Escribir.Write("comentario\n");
                                    break;
                                }
                            }
                            else
                            {
                                // Operador de división
                                elemento = "Simbolo\n";
                                Escribir.Write(elemento);
                                i_caracter = Leer.Read();
                                break;
                            }
                        }
                        Simbolo();
                        Escribir.Write(elemento);
                        i_caracter = Leer.Read();
                        break;

                    case '"':
                        Cadena();
                        Escribir.Write("cadena\n");
                        i_caracter = Leer.Read();
                        break;

                    case 'c':
                        Caracter();
                        Escribir.Write("caracter\n");
                        i_caracter = Leer.Read();
                        break;

                    case 'n':
                        i_caracter = Leer.Read();
                        Numero_linea++;
                        break;

                    case 'e':
                        i_caracter = Leer.Read();
                        break;

                    default:
                        Error(i_caracter);
                        break;
                }

            } while (i_caracter != -1);

            TxtboxSalida.AppendText("Errores: " + N_error);
            Escribir.Close();
            Leer.Close();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            P_Reservadas = new List<string>
            {
                // Tipos de datos y modificadores
                "void", "int", "float", "double", "char", "short", "long",
                "signed", "unsigned", "const", "volatile", "restrict",

                // Almacenamiento
                "auto", "register", "static", "extern", "typedef", "inline",

                // Control de flujo
                "if", "else", "switch", "case", "default",
                "for", "while", "do", "break", "continue", "goto", "return",

                // Estructuras de datos
                "struct", "union", "enum", "sizeof",

                // Extensiones C99
                "_Bool", "_Complex", "_Imaginary",

                // Extensiones C11
                "_Alignas", "_Alignof", "_Atomic", "_Generic", "_Noreturn",
                "_Static_assert", "_Thread_local",

                // Preprocesador (directivas)
                "define", "include", "ifdef", "ifndef", "endif", "undef",
                "error", "pragma", "line",

                // Funciones comunes de la biblioteca estándar
                "main", "printf", "scanf", "gets", "puts", "fgets", "fputs",
                "fopen", "fclose", "fread", "fwrite",
                "malloc", "calloc", "realloc", "free",
                "exit", "abort", "system","NULL"
            };
        }

        private void TxtboxSalida_TextChanged(object sender, EventArgs e)
        {
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }

        private void CajaTxt1_TextChanged(object sender, EventArgs e)
        {

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
                    i++; // saltar el '/'
                    traducido.Append('/');
                    continue;
                }
                if (!dentroComentarioLinea && !dentroComentarioBloque && c == '"') dentroCadena = !dentroCadena;

                // Si estamos dentro de comentario o cadena, se copia tal cual
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
                    i--; // ajustar posición
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
