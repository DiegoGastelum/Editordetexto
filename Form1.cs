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
        // ==========================================
        //           TABLA DE SÍMBOLOS 
        // ==========================================
        private class SimboloEntrada
        {
            public string nombre;
            public string tipo;      // tipo de dato (int/float/void/..., o descripción)
            public string categoria; // "variable", "funcion", "parametro"
            public string ambito;    // "global" o nombre de la función
            public int linea;
        }

        private List<SimboloEntrada> TablaSimbolos = new List<SimboloEntrada>();
        private string AmbitoActual = "global";
        private string archivoTabla = "tabla_simbolos.csv";

        public Form1()
        {
            InitializeComponent();
            compilarSoluciónToolStripMenuItem.Enabled = false;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            P_Reservadas = new List<string>
            {
                // Tipos de datos
                "void","int","float","double","char","short","long",
                "signed","unsigned","const","volatile",
        
                // Control de flujo
                "if","else","switch","case","default",
                "for","while","do","break","continue","return",
        
                // Otros
                "struct","union","enum","sizeof",
                "define","include"
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
        private void compilarSoluciónToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TxtboxSalida.Clear(); 
            if (archivo == null) guardar(); 
            else guardar(); 
            Numero_linea = 1;
            N_error = 0;
            elemento = "";

            archivoback = archivo.Remove(archivo.Length - 1) + "back";

            try
            {
                AnalizadorLexico();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error crítico en el compilador: " + ex.Message);
            }

            if (N_error == 0)
            {
                TxtboxSalida.AppendText("\r\nCompilación Exitosa. 0 Errores.\r\n");
            }
            else
            {
                TxtboxSalida.AppendText($"\r\nCompilación Finalizada con {N_error} errores.\r\n");
            }
        }

        // ==========================================
        //           ANALIZADOR LÉXICO
        // ==========================================
        private char Tipo_caracter(int caracter)
        {
            if ((caracter >= 65 && caracter <= 90) || (caracter >= 97 && caracter <= 122) || caracter == 95)
                return 'l'; // Letra
            else if (caracter >= 48 && caracter <= 57)
                return 'd'; // Dígito
            else
            {
                switch (caracter)
                {
                    case 10: return 'n';
                    case 34: return '"';
                    case 39: return 'c';
                    case 32: return 'e';
                    case 13: return 'e'; // Ignorar retorno
                    case 9: return 'e';  // Ignorar tabulador
                    default: return 's'; // Símbolo
                }
            }
        }

        private void Simbolo()
        {
            string s = ((char)i_caracter).ToString();
            int siguiente = Leer.Peek();

            // Operadores dobles
            if (s == "=" && siguiente == 61) { s = "=="; Leer.Read(); }
            else if (s == "!" && siguiente == 61) { s = "!="; Leer.Read(); }
            else if (s == "<" && siguiente == 61) { s = "<="; Leer.Read(); }
            else if (s == ">" && siguiente == 61) { s = ">="; Leer.Read(); }

            // 🔥 NUEVOS (CLAVE)
            else if (s == "+" && siguiente == 43) { s = "++"; Leer.Read(); }
            else if (s == "-" && siguiente == 45) { s = "--"; Leer.Read(); }
            else if (s == "&" && siguiente == 38) { s = "&&"; Leer.Read(); }
            else if (s == "|" && siguiente == 124) { s = "||"; Leer.Read(); }

            // Validación
            if ("(){}[],;=+-*/%<>!&|#:".Contains(s) || s.Length > 1)
            {
                Escribir.Write(s + "\n");
            }
            else
            {
                ErrorLexico($"Símbolo desconocido '{s}'");
            }
        }

        private void Cadena()
        {
            i_caracter = Leer.Read(); 

            while (i_caracter != -1 && (char)i_caracter != '"')
            {
                char c = (char)i_caracter;

                // Validación de Saltos de línea
                if (c == 10 || c == 13)
                {
                    ErrorLexico("Cadena sin cerrar (Salto de línea encontrado).");
                    Escribir.Write("Cadena\n"); 
                    return;
                }

                i_caracter = Leer.Read();
            }

            if (i_caracter == -1)
            {
                ErrorLexico("Cadena sin cerrar (Fin de archivo).");
                Escribir.Write("Cadena\n");
                return;
            }

            Escribir.Write("Cadena\n");
            i_caracter = Leer.Read();
        }

        private void Caracter()
        {
            i_caracter = Leer.Read(); 

            if (i_caracter == -1 || i_caracter == 39) // 39 es '
            {
                ErrorLexico("Carácter vacío o incompleto");
                if (i_caracter == 39) i_caracter = Leer.Read();
                return;
            }

            int cierre = Leer.Read();
            if (cierre != 39)
            {
                ErrorLexico("Se esperaba comilla simple de cierre");
                i_caracter = cierre;
                return;
            }

            Escribir.Write("caracter\n");

            i_caracter = Leer.Read();
        }

        private void Archivo_Libreria()
        {
            elemento += ".";
            i_caracter = Leer.Read();

            while (Tipo_caracter(i_caracter) == 'l')
            {
                elemento += (char)i_caracter;
                i_caracter = Leer.Read();
            }

            Escribir.Write("libreria\n");
        }

        private bool Palabra_Reservada()
        {
            if (P_Reservadas.IndexOf(elemento.ToLower()) >= 0) return true;
            return false;
        }

        private void Identificador()
        {
            string nombre = "";

            do
            {
                nombre += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                Archivo_Libreria();
            }
            else
            {
                elemento = nombre; // guardar el nombre real
                if (Palabra_Reservada())
                    Escribir.Write(elemento.ToLower() + "\n");
                else
                {
                    // Guardar token y nombre en archivo para el sintáctico
                    Escribir.Write($"identificador:{elemento}\n");
                }
            }
        }

        private void Numero()
        {
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

        private void Numero_Real()
        {
            i_caracter = Leer.Read();

            while (Tipo_caracter(i_caracter) == 'd')
            {
                i_caracter = Leer.Read();
            }

            Escribir.Write("numero_real\n");
        }

        private bool Comentario()
        {
            int siguiente = Leer.Read();

            if (siguiente == 47) // Caso //
            {
                do { i_caracter = Leer.Read(); } while (i_caracter != 10 && i_caracter != -1);
                return true;
            }
            else if (siguiente == 42) // Caso /*
            {
                bool cerrado = false;
                i_caracter = Leer.Read();
                do
                {
                    if (i_caracter == 10)
                    {
                        Numero_linea++;
                        Escribir.Write("LF\n"); 
                    }

                    if (i_caracter == 42) // *
                    {
                        if (Leer.Peek() == 47)
                        {
                            Leer.Read(); 
                            cerrado = true;
                            break;
                        }
                    }
                    i_caracter = Leer.Read();
                } while (i_caracter != -1);

                if (!cerrado) ErrorLexico("Comentario de bloque sin cerrar");

                i_caracter = Leer.Read();
                return true;
            }
            else 
            {
                Escribir.Write("/\n");

                i_caracter = siguiente;
                return true; 
            }
        }

        // ==========================================
        //           MANEJO DE ERRORES Y UTILIDADES
        // ==========================================

        private void Error(int i_caracter)
        {
            // Para errores léxicos 
            TxtboxSalida.AppendText($"Error léxico '{(char)i_caracter}', línea {Numero_linea}\n");
            N_error++;
        }

        private void Error(string mensaje)
        {
            // Para errores sintácticos
            TxtboxSalida.AppendText($"Error sintáctico: {mensaje}, línea {linea_del_token}\n");
            N_error++;
        }

        private void Error(string token, string esperado)
        {
            if (esperado == "redeclaración")
                TxtboxSalida.AppendText($"Error semántico: la variable o función '{token}' ya fue declarada, línea {linea_del_token}\n");
            else if (esperado == "función declarada")
                TxtboxSalida.AppendText($"Error semántico: la función '{token}' no está declarada, línea {linea_del_token}\n");
            else
                TxtboxSalida.AppendText($"Error: se esperaba '{esperado}', pero se encontró '{token}', línea {linea_del_token}\n");

            N_error++;
        }

        // Función específica para errores detectados durante la lectura de caracteres 
        private void ErrorLexico(string msg)
        {
            TxtboxSalida.AppendText($"Error léxico: {msg}, línea {Numero_linea}\n");
            N_error++;
        }

        private string NextToken()
        {
            string t = Leer.ReadLine();
            while (t == "LF")
            {
                Numero_linea++;
                t = Leer.ReadLine();
            }

            linea_del_token = Numero_linea;

            if (t != null && t.StartsWith("identificador:"))
            {
                elemento = t.Split(':')[1]; // guarda el nombre real del identificador
                return "identificador";
            }

            return t;
        }

        // ==========================================
        //           ANALIZADORES
        // ==========================================

        private void AnalizadorLexico()
        {
            Numero_linea = 1;
            N_error = 0;

            Leer = new StreamReader(archivo);
            string archivoSalida = archivo.Remove(archivo.Length - 1) + "back";
            Escribir = new StreamWriter(archivoSalida);

            i_caracter = Leer.Read();

            while (i_caracter != -1)
            {
                elemento = "";

                if ((char)i_caracter == '/')
                {
                    if (Comentario())
                    {
                        continue;
                    }
                }

                switch (Tipo_caracter(i_caracter))
                {
                    case 'l': // Letra -> Identificador o Palabra Reservada
                        Identificador();
                        break;

                    case 'd': // Dígito -> Número Entero o Real
                        Numero();
                        break;

                    case '"': // Comillas dobles -> Cadena
                        Cadena();
                        break;

                    case 'c': // Comilla simple -> Carácter
                        Caracter();
                        break;

                    case 'n': // Salto de línea (\n)
                        Numero_linea++;
                        Escribir.Write("LF\n");
                        i_caracter = Leer.Read();
                        break;

                    case 'e': // Espacio en blanco
                        i_caracter = Leer.Read();
                        break;

                    case 's': // Símbolo
                        Simbolo();
                        i_caracter = Leer.Read();
                        break;

                    default:
                        Error(i_caracter);
                        i_caracter = Leer.Read();
                        break;
                }
            }

            Escribir.Write("Fin\n");

            Escribir.Close();
            Leer.Close();

            AnalizadorSintactico();

            TxtboxSalida.AppendText($"\nProceso finalizado. Errores: {N_error}\n");
        }

        private void AnalizadorSintactico()
        {
            // Reiniciar tabla de símbolos
            TablaSimbolos.Clear();
            AmbitoActual = "global";

            Numero_linea = 1;
            Leer = new StreamReader(archivoback);
            token = NextToken();
            Cabecera();
            Leer.Close();

            // Verificar que exista la función main en el archivo fuente original
            Leer = new StreamReader(archivo);
            string contenidoFuente = Leer.ReadToEnd();
            Leer.Close();

            if (!contenidoFuente.Contains("main("))
            {
                Error("Función 'main' ausente");
            }

            // Exportar tabla de símbolos en CSV
            ExportarTablaSimbolosCSV();
        }

        private void Cabecera()
        {
            if (token == null || token == "Fin") return;

            switch (token)
            {
                case "#":
                    token = NextToken();
                    if (token == null) { Error("Directiva incompleta después de '#'"); return; }
                    Directiva_proc();
                    token = NextToken();
                    Cabecera();
                    break;

                case "int":
                case "float":
                case "double":
                case "char":
                case "void":
                case "Tipo":
                    string tipo = token;
                    token = NextToken();
                    string id = elemento; // elemento contiene el nombre real del identificador

                    token = NextToken();

                    if (token == "(")
                    {
                        AgregarSimbolo(id, tipo, "funcion", "global", linea_del_token);

                        AmbitoActual = id;

                        Parametros();
                        BloqueDeSentencias();
                        LimpiarAmbito(AmbitoActual);
                        AmbitoActual = "global";

                        token = NextToken();
                        Cabecera();
                    }
                    else
                    {
                        // Declaración global de variable
                        AgregarSimbolo(id, tipo, "variable", "global", linea_del_token);
                        Declaracion_Variable_Global_Logica(id);
                        token = NextToken();
                        Cabecera();
                    }
                    break;

                case "identificador":
                    Error(token, "tipo de dato");

                    token = NextToken();

                    if (token == "(")
                    {
                        Parametros();
                        BloqueDeSentencias();
                        token = NextToken();
                        Cabecera();
                    }
                    else
                    {
                        Declaracion_Variable_Global_Logica("identificador");
                        token = NextToken();
                        Cabecera();
                    }
                    break;

                default:
                    token = NextToken();
                    Cabecera();
                    break;
            }
        }

        private void Parametros()
        {
            token = NextToken();
            if (token == ")") { token = NextToken(); return; } // Función sin parámetros

            while (token != ")" && token != "Fin")
            {
                if (token != "int" && token != "float" && token != "char" && token != "double")
                    Error(token, "tipo de dato");

                string tipoParam = token;
                token = NextToken();

                if (token != "identificador")
                {
                    Error(token, "identificador");
                    if (token == ",")
                    {
                        token = NextToken();
                        continue;
                    }
                    if (token == ")") { token = NextToken(); return; }
                    token = NextToken();
                }
                else
                {
                    // Registrar parámetro en la tabla con nombre real
                    AgregarSimbolo(elemento, tipoParam, "parametro", AmbitoActual, linea_del_token);
                    token = NextToken();
                }

                if (token == ",")
                {
                    token = NextToken();
                    if (token == ")")
                    {
                        Error(",", "identificador");
                        return;
                    }
                    continue;
                }
                else if (token != ")")
                {
                    Error(token, "',' o ')'");
                    return;
                }
            }
            token = NextToken();
        }

        // ==========================================
        //           BLOQUES Y SENTENCIAS
        // ==========================================

        private void BloqueDeSentencias()
        {
            if (token != "{") { Error(token, "{"); return; }
            token = NextToken();

            while (token != "}" && token != "Fin" && token != null)
            {
                switch (token)
                {
                    case "int":
                    case "float":
                    case "double":
                    case "char":
                        Declaracion_Local();
                        break;

                    case "if": EstructuraIf(); break;
                    case "while": EstructuraWhile(); break;
                    case "do": EstructuraDoWhile(); break;
                    case "for": EstructuraFor(); break;
                    case "switch": EstructuraSwitch(); break;

                    case "break":
                    case "continue":
                        token = NextToken();
                        if (token != ";") Error(token, ";");
                        token = NextToken();
                        break;

                    case "return":
                        token = NextToken();
                        if (token == ";")
                        {
                            token = NextToken();
                            break;
                        }
                        if (token == "}" || token == "Fin")
                        {
                            Error("return incompleto");
                            break;
                        }
                        Expresion();
                        if (token != ";")
                        {
                            Error(token, ";");
                        }

                        token = NextToken();
                        break;

                    case "identificador":
                    case "printf":
                        Sentencia();
                        break;

                    case ";": token = NextToken(); break;

                    case "{":
                        BloqueDeSentencias();
                        token = NextToken();
                        break;

                    default:
                        Error($"Instrucción no reconocida o inválida: '{token}'");
                        token = NextToken(); 
                        break;
                }
            }
            if (token != "}") Error("Se esperaba '}'");
        }

        private void Sentencia()
        {
            string id = elemento; // nombre real del identificador
            token = NextToken();

            if (token == "(")
            {
                // Llamada a función: verificar existencia
                var sym = ObtenerSimbolo(id);
                if (sym == null && id != "printf" && id != "scanf")
                {
                    TxtboxSalida.AppendText($"Error: función '{id}' no declarada, línea {linea_del_token}\n");
                    N_error++;
                }

                token = NextToken();
                if (token != ")")
                {
                    while (true)
                    {
                        Expresion();

                        if (token == ",")
                        {
                            token = NextToken();
                            if (token == ")")
                            {
                                Error(",", "identificador");
                                return;
                            }
                            continue;
                        }
                        else if (token == ")")
                        {
                            break;
                        }
                        else
                        {
                            Error(token, ", o )");
                            while (token != "," && token != ")" && token != ";" && token != "Fin" && token != null)
                                token = NextToken();

                            if (token == ",") { token = NextToken(); continue; }
                            else if (token == ")") break;
                            else return;
                        }
                    }
                }
                token = NextToken();
                if (token != ";") Error(token, ";");
                token = NextToken();
            }
            else if (token == "=")
            {
                // Asignación: verificar existencia de variable
                if (!ExisteSimbolo(id))
                {
                    Error(id, "declaración previa");
                    // Recuperación: saltar la asignación
                    token = NextToken();
                    while (token != ";" && token != "Fin" && token != null) token = NextToken();
                    if (token == ";") token = NextToken();
                    return;
                }

                token = NextToken();
                Expresion();
                if (token != ";") Error(token, ";");
                token = NextToken();
            }
            else
            {
                Error(token, "'=' o '('");
            }
        }

        // ==========================================
        //           ESTRUCTURAS DE CONTROL
        // ==========================================

        private void EstructuraIf()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            Expresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();

            BloqueDeSentencias();

            token = NextToken(); 
            if (token == "else")
            {
                token = NextToken();
                BloqueDeSentencias();
                token = NextToken();
            }
        }

        private void EstructuraWhile()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            Expresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();
            BloqueDeSentencias();
            token = NextToken();
        }

        private void EstructuraFor()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();

            if (token == "int" || token == "float") Declaracion_Local();
            else if (token == "identificador") Sentencia();
            else if (token == ";") token = NextToken();
            else Error(token, "inicialización for");

            if (token != ";") Expresion();
            if (token != ";") { Error(token, ";"); return; }
            token = NextToken();

            if (token != ")")
            {
                bool esperaOperando = true;
                while (token != ")" && token != "Fin")
                {
                    if (token == "identificador" || token == "numero_entero" || token == "numero_real")
                    {
                        if (!esperaOperando) { Error(token, "operador"); }
                        esperaOperando = false;
                        token = NextToken();
                    }
                    else if (token == "=" || token == "+" || token == "-" || token == "*" || token == "/")
                    {
                        esperaOperando = true;
                        token = NextToken();
                    }
                    else if (token == "++" || token == "--")
                    {
                        esperaOperando = false;
                        token = NextToken();
                    }
                    else
                    {
                        Error(token, "expresión de incremento");
                        token = NextToken();
                    }
                }
                if (esperaOperando) Error("Incremento incompleto");
            }

            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();
            BloqueDeSentencias();
            token = NextToken();
        }

        private void EstructuraDoWhile()
        {
            token = NextToken();
            BloqueDeSentencias();
            token = NextToken();

            if (token != "while") { Error(token, "while"); return; }
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            Expresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();
            if (token != ";") { Error(token, ";"); return; }
            token = NextToken();
        }

        private void EstructuraSwitch()
        {
            token = NextToken();
            if (token != "(") { Error(token, "("); return; }
            token = NextToken();
            Expresion();
            if (token != ")") { Error(token, ")"); return; }
            token = NextToken();
            if (token != "{") { Error(token, "{"); return; }
            token = NextToken();

            while (token != "}" && token != "Fin" && token != null)
            {
                if (token == "case")
                {
                    token = NextToken();
                    if (token != "numero_entero" && token != "caracter") Error(token, "constante");
                    token = NextToken();
                    if (token != ":") { Error(token, ":"); return; }
                    token = NextToken();
                    CuerpoDelCase();
                }
                else if (token == "default") 
                {
                    token = NextToken();
                    if (token != ":") { Error(token, ":"); return; } 
                    token = NextToken();
                    CuerpoDelCase();
                }
                else
                {
                    Error($"Se esperaba 'case' o 'default', pero se encontró '{token}'");
                    token = NextToken();
                }
            }
        }

        private void CuerpoDelCase()
        {
            while (token != "case" && token != "default" && token != "}" && token != "Fin")
            {
                if (token == "break")
                {
                    token = NextToken();
                    if (token != ";") Error(token, ";");
                    token = NextToken();
                }
                else if (token == "identificador" || token == "printf") Sentencia();
                else if (token == "if") EstructuraIf();
                else if (token == "while") EstructuraWhile();
                else if (token == "for") EstructuraFor();
                else if (token == "{")
                {
                    BloqueDeSentencias();
                    token = NextToken();
                }
                else token = NextToken();
            }
        }

        // ==========================================
        //           AUXILIARES Y DECLARACIONES
        // ==========================================
        private bool EsOperador(string t)
        {
            return t == "+" || t == "-" || t == "*" || t == "/" || t == "%" ||
                   t == "=" || t == "==" || t == "!=" || t == ">" || t == "<" ||
                   t == ">=" || t == "<=" || t == "&&" || t == "||" || t == "!" ||
                   t == "++" || t == "--";
        }

        private void Expresion()
        {
            bool esperaOperando = true;
            int parentesis = 0;

            while (token != null && token != "Fin")
            {
                if (token == ";" || token == "}") break;
                if (token == "," && parentesis == 0) break;

                // Paréntesis
                if (token == "(")
                {
                    parentesis++;
                    esperaOperando = true;
                    token = NextToken();
                    continue;
                }

                if (token == ")")
                {
                    if (parentesis == 0) break;

                    if (esperaOperando)
                    {
                        Error(token, "valor o variable");
                        esperaOperando = false; // 🔥 evitar cascada
                    }

                    parentesis--;
                    esperaOperando = false;
                    token = NextToken();
                    continue;
                }

                // Operadores
                if (EsOperador(token))
                {
                    if (esperaOperando)
                    {
                        // ✔ Unarios válidos
                        if (token == "++" || token == "--" || token == "-" || token == "!")
                        {
                            token = NextToken();
                            continue;
                        }

                        // ❌ '+' inválido como unario
                        Error(token, "valor o variable");

                        // 🔥 CLAVE: forzar recuperación SIN salir
                        esperaOperando = true;
                        token = NextToken();
                        continue;
                    }
                    else
                    {
                        // ✔ Postfijo válido
                        if (token == "++" || token == "--")
                        {
                            esperaOperando = false;
                            token = NextToken();
                            continue;
                        }

                        // ✔ Binario
                        esperaOperando = true;
                        token = NextToken();
                        continue;
                    }
                }

                // Identificadores
                if (token == "identificador")
                {
                    string nombreVar = elemento;

                    if (!ExisteSimbolo(nombreVar))
                    {
                        TxtboxSalida.AppendText($"Error semántico: variable '{nombreVar}' no declarada, línea {linea_del_token}\n");
                        N_error++;
                    }

                    token = NextToken();

                    if (token == "(")
                    {
                        parentesis++;
                        esperaOperando = true;
                        token = NextToken();
                        continue;
                    }

                    esperaOperando = false;
                    continue;
                }

                // Literales
                if (token == "numero_entero" || token == "numero_real" || token == "Cadena" || token == "caracter")
                {
                    if (!esperaOperando)
                    {
                        Error(token, "operador");
                    }

                    esperaOperando = false;
                    token = NextToken();
                    continue;
                }

                break;
            }

            if (esperaOperando)
                Error("Expresión incompleta");

            if (parentesis > 0)
                Error("Paréntesis sin cerrar en la expresión");
        }

        private void Declaracion_Local()
        {
            // token actualmente es el tipo (ej. "int")
            string tipoLocal = token;

            token = NextToken(); // ID esperado
            if (token != "identificador")
            {
                Error(token, "identificador");
                // Recuperar: avanzar hasta ';' o '}' o 'Fin'
                while (token != ";" && token != "}" && token != "Fin" && token != null)
                    token = NextToken();
                if (token == ";") token = NextToken();
                return;
            }

            string id = elemento; // nombre real del identificador

            // Registrar variable en el ámbito actual
            AgregarSimbolo(elemento, tipoLocal, "variable", AmbitoActual, linea_del_token);

            token = NextToken();
            Declaracion_Variable_Global_Logica(id);
            token = NextToken();
        }

        private void Declaracion_Variable_Global_Logica(string identificador_actual)
        {
            while (token == "[")
            {
                token = NextToken();
                if (token != "numero_entero" && token != "identificador")
                {
                    Error(token, "tamaño arreglo");
                    return;
                }
                token = NextToken();
                if (token != "]") { Error(token, "]"); return; }
                token = NextToken();
            }

            if (token == "=")
            {
                token = NextToken();
                if (token == "{")
                {
                    BloqueInicializacion();
                }
                else
                {
                    if (token == "-") token = NextToken();
                    if (token != "numero_entero" && token != "numero_real" &&
                        token != "Cadena" && token != "caracter" && token != "identificador")
                    {
                        Error(token, "valor inicialización");
                        return;
                    }
                    token = NextToken();
                    if (token == ".")
                    {
                        token = NextToken();
                        if (token != "numero_entero") { Error(token, "decimal"); return; }
                        token = NextToken();
                    }
                }
            }

            if (token != ";") Error(token, ";");
        }

        private void BloqueInicializacion()
        {
            if (token != "{") { Error(token, "{"); return; }
            token = NextToken();

            while (token != "}")
            {
                if (token == "{") BloqueInicializacion();
                else if (token == "numero_entero" || token == "numero_real" || token == "identificador" || token == "Cadena" || token == "caracter")
                {
                    token = NextToken();
                }
                else { Error(token, "valor o sub-arreglo"); return; }

                if (token == ",") token = NextToken();
                else if (token == "}") break;
                else { Error(token, "',' o '}'"); return; }
            }
            token = NextToken();
        }

        private int Directiva_proc()
        {
            while (token == "LF") token = Leer.ReadLine();
            if (token == null) { Error("Directiva incompleta"); return 0; }

            switch (token)
            {
                case "include":
                    token = Leer.ReadLine();
                    while (token == "LF") token = Leer.ReadLine();
                    if (token == null) { Error("Include incompleto"); return 0; }
                    return Directiva_include();

                case "define":
                    token = Leer.ReadLine();
                    while (token == "LF") token = Leer.ReadLine();
                    if (token == null) { Error("define incompleto"); return 0; }
                    return 1;

                default:
                    Error("include o define");
                    return 0;
            }
        }

        private int Directiva_include()
        {
            while (token == "LF") { Numero_linea++; token = Leer.ReadLine(); }
            if (token == null) return 0;

            if (token == "<")
            {
                token = Leer.ReadLine();
                if (token == null) { Error("libreria inválida"); return 0; }
                token = Leer.ReadLine();
                if (token != ">") { Error(token, ">"); return 0; }
                return 1;
            }
            else if (token == "Cadena") return 1;

            Error("Formato include");
            return 0;
        }

        // ==========================================
        //      MANEJO DE TABLA DE SÍMBOLOS
        // ==========================================
        private bool ExisteSimboloEnAmbito(string nombre, string ambito)
        {
            return TablaSimbolos.Exists(s => s.nombre == nombre && s.ambito == ambito);
        }

        private bool ExisteSimbolo(string nombre)
        {
            // Busca en ambito actual primero, luego en global
            if (ExisteSimboloEnAmbito(nombre, AmbitoActual)) return true;
            if (ExisteSimboloEnAmbito(nombre, "global")) return true;
            return false;
        }

        private SimboloEntrada ObtenerSimbolo(string nombre)
        {
            var s = TablaSimbolos.Find(x => x.nombre == nombre && x.ambito == AmbitoActual);
            if (s != null) return s;
            return TablaSimbolos.Find(x => x.nombre == nombre && x.ambito == "global");
        }

        private void AgregarSimbolo(string nombre, string tipo, string categoria, string ambito, int linea)
        {
            // Ignorar funciones conocidas del sistema
            if (nombre == "printf" || nombre == "scanf") return;

            if (ExisteSimboloEnAmbito(nombre, ambito))
            {
                TxtboxSalida.AppendText($"Error: redeclaración de '{nombre}' en ámbito '{ambito}', línea {linea}\n");
                N_error++;
                return;
            }

            TablaSimbolos.Add(new SimboloEntrada
            {
                nombre = nombre,
                tipo = tipo,
                categoria = categoria,
                ambito = ambito,
                linea = linea
            });
        }

        private void LimpiarAmbito(string ambito)
        {
            // Por si acaso
        }

        // ==========================================
        //           EXPORTAR TABLA DE SÍMBOLOS
        // ==========================================
        private void ExportarTablaSimbolosCSV()
        {
            try
            {
                // Crear la ruta completa en la misma carpeta del archivo fuente 
                string carpeta = Path.GetDirectoryName(archivo);
                string nombreArchivo = Path.GetFileNameWithoutExtension(archivo) + "_tabla.csv";
                archivoTabla = Path.Combine(carpeta, nombreArchivo);

                using (StreamWriter tabla = new StreamWriter(archivoTabla))
                {
                    tabla.WriteLine("Nombre,Tipo,Categoría,Ámbito,Línea");
                    foreach (var s in TablaSimbolos)
                    {
                        tabla.WriteLine($"{s.nombre},{s.tipo},{s.categoria},{s.ambito},{s.linea}");
                    }
                }

                TxtboxSalida.AppendText($"\nTabla de símbolos guardada en '{archivoTabla}'\n");
            }
            catch (Exception ex)
            {
                TxtboxSalida.AppendText($"\nError al guardar tabla de símbolos: {ex.Message}\n");
            }
        }


        private void TxtboxSalida_TextChanged(object sender, EventArgs e)
        {
            compilarSoluciónToolStripMenuItem.Enabled = true;
        }
        private void compilarToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
        private void CajaTxt1_TextChanged(object sender, EventArgs e)
        {
            compilarSoluciónToolStripMenuItem.Enabled = true;

        }
    }
}