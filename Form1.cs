using System.Globalization;
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
            public string valor;
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

            if (s == "=" && siguiente == 61) { s = "=="; Leer.Read(); }
            else if (s == "!" && siguiente == 61) { s = "!="; Leer.Read(); }
            else if (s == "<" && siguiente == 61) { s = "<="; Leer.Read(); }
            else if (s == ">" && siguiente == 61) { s = ">="; Leer.Read(); }

            if ("(){}[],;=+-*/%<>!&|#:".Contains(((char)i_caracter).ToString()) || s.Length > 1)
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

            if (i_caracter == -1 || i_caracter == 39)
            {
                ErrorLexico("Carácter vacío o incompleto");
                if (i_caracter == 39) i_caracter = Leer.Read();
                return;
            }

            elemento = ((char)i_caracter).ToString();

            int cierre = Leer.Read();
            if (cierre != 39)
            {
                ErrorLexico("Se esperaba comilla simple de cierre");
                i_caracter = cierre;
                return;
            }

            Escribir.Write("caracter:" + elemento + "\n");
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
            elemento = "";

            do
            {
                elemento += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                Numero_Real();
                return;
            }

            Escribir.Write("numero_entero:" + elemento + "\n");
        }

        private void Numero_Real()
        {
            elemento += ".";
            i_caracter = Leer.Read();

            while (Tipo_caracter(i_caracter) == 'd')
            {
                elemento += (char)i_caracter;
                i_caracter = Leer.Read();
            }

            Escribir.Write("numero_real:" + elemento + "\n");
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
                elemento = t.Substring("identificador:".Length);
                return "identificador";
            }

            if (t != null && t.StartsWith("numero_entero:"))
            {
                elemento = t.Substring("numero_entero:".Length);
                return "numero_entero";
            }

            if (t != null && t.StartsWith("numero_real:"))
            {
                elemento = t.Substring("numero_real:".Length);
                return "numero_real";
            }

            if (t != null && t.StartsWith("caracter:"))
            {
                elemento = t.Substring("caracter:".Length);
                return "caracter";
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

                        NodoArbol raizReturn = AnalizarExpresionMatematica();
                        if (raizReturn != null)
                        {
                            string tipoResultado;
                            bool calculable;
                            string valorResultado = EvaluarArbol(raizReturn, out tipoResultado, out calculable);

                            MostrarResultadoArbol(raizReturn, tipoResultado, valorResultado, calculable);

                            SimboloEntrada funcion = TablaSimbolos.Find(s => s.nombre == AmbitoActual && s.categoria == "funcion");
                            if (funcion != null)
                            {
                                if (funcion.tipo == "void")
                                {
                                    TxtboxSalida.AppendText($"Error semántico: la función '{AmbitoActual}' es void y no debe devolver valor, línea {linea_del_token}\n");
                                    N_error++;
                                }
                                else if (!TiposCompatibles(funcion.tipo, tipoResultado))
                                {
                                    TxtboxSalida.AppendText($"Error semántico: return de tipo {tipoResultado} incompatible con función '{AmbitoActual}' de tipo {funcion.tipo}, línea {linea_del_token}\n");
                                    N_error++;
                                }
                            }
                        }
                        else
                        {
                            while (token != ";" && token != "Fin" && token != null)
                                token = NextToken();
                        }

                        if (token != ";") Error(token, ";");
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
            string id = (token == "identificador") ? elemento : token;
            token = NextToken();

            // Llamada a función
            if (token == "(")
            {
                // Verificar que la función exista
                if (!ExisteSimbolo(id) && id != "printf" && id != "scanf")
                {
                    TxtboxSalida.AppendText($"Error semántico: función '{id}' no declarada, línea {linea_del_token}\n");
                    N_error++;
                }

                token = NextToken();

                if (token != ")")
                {
                    while (true)
                    {
                        NodoArbol argArbol = AnalizarExpresionMatematica();
                        if (argArbol != null)
                        {
                            string tipoArg; bool calcArg;
                            string valArg = EvaluarArbol(argArbol, out tipoArg, out calcArg);
                            MostrarResultadoArbol(argArbol, tipoArg, valArg, calcArg);
                        }

                        if (token == ",")
                        {
                            token = NextToken();
                            if (token == ")") { Error("Coma extra antes de ')'"); break; }
                            continue;
                        }
                        else if (token == ")") break;
                        else
                        {
                            Error(token, "',' o ')'");
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

            // Asignación
            else if (token == "=")
            {
                if (!ExisteSimbolo(id))
                {
                    TxtboxSalida.AppendText($"Error semántico: variable '{id}' no declarada, línea {linea_del_token}\n");
                    N_error++;
                    token = NextToken();
                    while (token != ";" && token != "Fin" && token != null) token = NextToken();
                    if (token == ";") token = NextToken();
                    return;
                }

                token = NextToken();

                NodoArbol raiz = AnalizarExpresionMatematica();

                if (raiz != null)
                {
                    string tipoResultado; bool calculable;
                    string valorResultado = EvaluarArbol(raiz, out tipoResultado, out calculable);

                    MostrarResultadoArbol(raiz, tipoResultado, valorResultado, calculable);

                    SimboloEntrada sim = ObtenerSimbolo(id);
                    if (sim != null)
                    {
                        if (!TiposCompatibles(sim.tipo, tipoResultado))
                        {
                            TxtboxSalida.AppendText($"Error semántico: no se puede asignar tipo '{tipoResultado}' a '{id}' de tipo '{sim.tipo}', línea {linea_del_token}\n");
                            N_error++;
                        }
                        else if (calculable)
                        {
                            sim.valor = valorResultado; // actualizar valor en tabla de símbolos
                        }
                    }
                }
                else
                {
                    // raiz es null porque hubo error en la expresión
                    while (token != ";" && token != "Fin" && token != null)
                        token = NextToken();
                }

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
                   t == ">=" || t == "<=" || t == "&&" || t == "||" || t == "!";
        }
        private class NodoArbol
        {
            public string dato;
            public NodoArbol izquierda;
            public NodoArbol derecha;
        }

        private bool EsTipoNumerico(string tipo)
        {
            return tipo == "int" || tipo == "float" || tipo == "double" || tipo == "char";
        }

        private bool EsTipoEntero(string tipo)
        {
            return tipo == "int" || tipo == "char";
        }

        private bool TiposCompatibles(string destino, string origen)
        {
            if (destino == null || origen == null || origen == "desconocido") return false;
            if (destino == origen) return true;

            if ((destino == "float" || destino == "double") &&
                (origen == "int" || origen == "char" || origen == "float" || origen == "double"))
                return true;

            if (destino == "int" && (origen == "int" || origen == "char"))
                return true;

            if (destino == "char" && (origen == "char" || origen == "int"))
                return true;

            return false;
        }

        private string NombreNodo(string dato)
        {
            if (dato == null) return "";
            if (dato.StartsWith("num:")) return dato.Substring(4);
            if (dato.StartsWith("car:")) return "'" + dato.Substring(4) + "'";
            if (dato.StartsWith("id:")) return dato.Substring(3);
            if (dato.StartsWith("fun:")) return dato.Substring(4) + "()";
            if (dato == "neg") return "(negativo)";
            return dato;
        }

        private void MostrarArbol(NodoArbol nodo, string nivel)
        {
            if (nodo == null) return;
            TxtboxSalida.AppendText($"{nivel}{NombreNodo(nodo.dato)}\n");
            MostrarArbol(nodo.izquierda, nivel + "  |--");
            MostrarArbol(nodo.derecha, nivel + "  |--");
        }

        private bool ArbolTieneOperadores(NodoArbol nodo)
        {
            if (nodo == null) return false;
            if (nodo.izquierda != null || nodo.derecha != null) return true;
            return false;
        }

        private void MostrarResultadoArbol(NodoArbol raiz, string tipoResultado, string valorResultado, bool calculable)
        {
            if (raiz == null) return;
            if (!ArbolTieneOperadores(raiz)) return;

            TxtboxSalida.AppendText("\n--- Árbol de expresión ---\n");
            MostrarArbol(raiz, "");
            TxtboxSalida.AppendText($"Nodo raíz    : {NombreNodo(raiz.dato)}\n");
            TxtboxSalida.AppendText($"Tipo inferido: {tipoResultado}\n");

            if (calculable)
                TxtboxSalida.AppendText($"Resultado    : {valorResultado}\n");
            else
                TxtboxSalida.AppendText("Resultado    : no calculable (variables sin valor conocido)\n");

            TxtboxSalida.AppendText("--------------------------\n");
        }

        private string FormatearNumero(double valor, string tipo)
        {
            if (tipo == "int")
                return ((long)Math.Round(valor)).ToString(CultureInfo.InvariantCulture);

            return valor.ToString(CultureInfo.InvariantCulture);
        }

        private NodoArbol AnalizarExpresionMatematica()
        {
            NodoArbol raiz = AnalizarExpresionSuma();

            // Operadores relacionales y lógicos
            while (token == "==" || token == "!=" || token == "<" || token == ">" ||
                   token == "<=" || token == ">=" || token == "&&" || token == "||")
            {
                string op = token;
                token = NextToken();

                NodoArbol derecho = AnalizarExpresionSuma();
                if (derecho == null)
                {
                    Error($"Se esperaba operando después de '{op}'");
                    return raiz;
                }

                raiz = new NodoArbol { dato = op, izquierda = raiz, derecha = derecho };
            }

            return raiz;
        }

        private NodoArbol AnalizarExpresionSuma()
        {
            NodoArbol raiz = AnalizarTerminoMatematico();

            while (token == "+" || token == "-")
            {
                string op = token;
                token = NextToken();

                NodoArbol derecho = AnalizarTerminoMatematico();
                if (derecho == null)
                {
                    Error($"Se esperaba operando después de '{op}'");
                    return raiz;
                }

                raiz = new NodoArbol { dato = op, izquierda = raiz, derecha = derecho };
            }

            return raiz;
        }

        private NodoArbol AnalizarTerminoMatematico()
        {
            NodoArbol raiz = AnalizarFactorMatematico();

            while (token == "*" || token == "/" || token == "%")
            {
                string op = token;
                token = NextToken();

                NodoArbol derecho = AnalizarFactorMatematico();
                if (derecho == null)
                {
                    Error($"Se esperaba operando después de '{op}'");
                    return raiz;
                }

                raiz = new NodoArbol { dato = op, izquierda = raiz, derecha = derecho };
            }

            return raiz;
        }

        private NodoArbol AnalizarFactorMatematico()
        {
            // Negativo unario
            if (token == "-")
            {
                token = NextToken();
                NodoArbol hijo = AnalizarFactorMatematico();
                if (hijo == null) { Error("Se esperaba valor después de '-'"); return null; }
                return new NodoArbol { dato = "neg", derecha = hijo };
            }

            // NOT lógico
            if (token == "!")
            {
                token = NextToken();
                NodoArbol hijo = AnalizarFactorMatematico();
                if (hijo == null) { Error("Se esperaba valor después de '!'"); return null; }
                return new NodoArbol { dato = "!", derecha = hijo };
            }

            // Paréntesis
            if (token == "(")
            {
                token = NextToken();
                NodoArbol nodo = AnalizarExpresionMatematica();
                if (token != ")")
                {
                    Error(token, ")");
                    return nodo; // intentar continuar
                }
                token = NextToken();
                return nodo;
            }

            // Número entero o real
            if (token == "numero_entero" || token == "numero_real")
            {
                NodoArbol nodo = new NodoArbol { dato = "num:" + elemento };
                token = NextToken();
                return nodo;
            }

            // Literal carácter
            if (token == "caracter")
            {
                NodoArbol nodo = new NodoArbol { dato = "car:" + elemento };
                token = NextToken();
                return nodo;
            }

            if (token == "Cadena")
            {
                NodoArbol nodo = new NodoArbol { dato = "cadena" };
                token = NextToken();
                return nodo;
            }

            // Identificador o llamada a función
            if (token == "identificador")
            {
                string nombre = elemento;

                // Verificar que la variable/función esté declarada
                if (!ExisteSimbolo(nombre))
                {
                    TxtboxSalida.AppendText($"Error semántico: '{nombre}' no declarado, línea {linea_del_token}\n");
                    N_error++;
                }
                else
                {
                    SimboloEntrada sim = ObtenerSimbolo(nombre);
                    if (sim != null && sim.categoria == "funcion")
                    {

                    }
                }

                token = NextToken();
                if (token == "(")
                {
                    token = NextToken();

                    if (token != ")")
                    {
                        // Analizar argumentos
                        while (true)
                        {
                            NodoArbol arg = AnalizarExpresionMatematica();

                            if (token == ",")
                            {
                                token = NextToken();
                                if (token == ")")
                                {
                                    Error("Coma extra antes de ')'");
                                    break;
                                }
                                continue;
                            }
                            else if (token == ")")
                            {
                                break;
                            }
                            else
                            {
                                Error(token, "',' o ')'");
                                while (token != ")" && token != ";" && token != "Fin" && token != null)
                                    token = NextToken();
                                if (token == ")") break;
                                return null;
                            }
                        }
                    }

                    token = NextToken();

                    return new NodoArbol { dato = "fun:" + nombre };
                }
                else
                {
                    SimboloEntrada sim = ObtenerSimbolo(nombre);
                    if (sim != null && sim.categoria == "funcion")
                    {
                        TxtboxSalida.AppendText($"Error semántico: '{nombre}' es una función, se esperaban paréntesis '()', línea {linea_del_token}\n");
                        N_error++;
                    }

                    return new NodoArbol { dato = "id:" + nombre };
                }
            }

            // Si llega aquí, el token no es un valor válido
            if (token != ";" && token != ")" && token != "}" && token != "," && token != "Fin" && token != null)
            {
                Error(token, "valor o variable");
            }
            return null;
        }

        private string ObtenerValorHoja(string dato, out string tipo, out bool calculable)
        {
            calculable = true;
            tipo = "desconocido";

            if (dato.StartsWith("num:"))
            {
                string valor = dato.Substring(4);
                tipo = valor.Contains(".") ? "double" : "int";
                return valor;
            }

            if (dato.StartsWith("car:"))
            {
                tipo = "char";
                string ch = dato.Substring(4);
                if (ch.Length > 0)
                    return ((int)ch[0]).ToString(CultureInfo.InvariantCulture);
                calculable = false;
                return null;
            }

            if (dato == "cadena")
            {
                tipo = "char*";
                calculable = false;
                return null;
            }

            if (dato.StartsWith("id:"))
            {
                string nombre = dato.Substring(3);
                SimboloEntrada s = ObtenerSimbolo(nombre);
                if (s == null) { calculable = false; return null; }

                tipo = s.tipo;

                if (string.IsNullOrEmpty(s.valor)) { calculable = false; return null; }

                // Intentar parsear el valor almacenado como número
                double tmp;
                if (double.TryParse(s.valor, NumberStyles.Float, CultureInfo.InvariantCulture, out tmp))
                    return s.valor;

                // Si es un carácter guardado como letra, convertir a entero
                if (s.valor.Length > 0)
                    return ((int)s.valor[0]).ToString(CultureInfo.InvariantCulture);

                calculable = false;
                return null;
            }

            if (dato.StartsWith("fun:"))
            {
                string nombre = dato.Substring(4);
                SimboloEntrada s = ObtenerSimbolo(nombre);
                tipo = (s != null) ? s.tipo : "desconocido";
                calculable = false;
                return null;
            }

            calculable = false;
            return null;
        }

        private string EvaluarArbol(NodoArbol nodo, out string tipo, out bool calculable)
        {
            tipo = "desconocido";
            calculable = true;

            if (nodo == null) { calculable = false; return null; }

            // ---- Nodo hoja ----
            if (nodo.izquierda == null && nodo.derecha == null)
                return ObtenerValorHoja(nodo.dato, out tipo, out calculable);

            // ---- Negativo unario ----
            if (nodo.dato == "neg")
            {
                string tipoH; bool calcH;
                string valorH = EvaluarArbol(nodo.derecha, out tipoH, out calcH);
                tipo = tipoH;
                if (!calcH || !EsTipoNumerico(tipoH) || valorH == null) { calculable = false; return null; }

                double num;
                if (!double.TryParse(valorH, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                { calculable = false; return null; }

                return FormatearNumero(-num, tipo);
            }

            // ---- NOT lógico ----
            if (nodo.dato == "!")
            {
                string tipoH; bool calcH;
                string valorH = EvaluarArbol(nodo.derecha, out tipoH, out calcH);
                tipo = "int";
                if (!calcH || valorH == null) { calculable = false; return null; }

                double num;
                if (!double.TryParse(valorH, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                { calculable = false; return null; }

                return (num == 0 ? "1" : "0");
            }

            // ---- Operadores binarios ----
            string ti, td; bool ci, cd;
            string vi = EvaluarArbol(nodo.izquierda, out ti, out ci);
            string vd = EvaluarArbol(nodo.derecha, out td, out cd);

            // Operadores relacionales y lógicos 
            if (nodo.dato == "==" || nodo.dato == "!=" || nodo.dato == "<" || nodo.dato == ">" ||
                nodo.dato == "<=" || nodo.dato == ">=" || nodo.dato == "&&" || nodo.dato == "||")
            {
                tipo = "int";

                // Verificar que los operandos sean numéricos
                if (!EsTipoNumerico(ti) || !EsTipoNumerico(td))
                {
                    TxtboxSalida.AppendText($"Error semántico: operador '{nodo.dato}' requiere operandos numéricos, línea {linea_del_token}\n");
                    N_error++;
                    calculable = false;
                    return null;
                }

                calculable = ci && cd && vi != null && vd != null;
                if (!calculable) return null;

                double a, b;
                if (!double.TryParse(vi, NumberStyles.Float, CultureInfo.InvariantCulture, out a) ||
                    !double.TryParse(vd, NumberStyles.Float, CultureInfo.InvariantCulture, out b))
                { calculable = false; return null; }

                bool resultado = false;
                switch (nodo.dato)
                {
                    case "==": resultado = (a == b); break;
                    case "!=": resultado = (a != b); break;
                    case "<": resultado = (a < b); break;
                    case ">": resultado = (a > b); break;
                    case "<=": resultado = (a <= b); break;
                    case ">=": resultado = (a >= b); break;
                    case "&&": resultado = (a != 0 && b != 0); break;
                    case "||": resultado = (a != 0 || b != 0); break;
                }
                return resultado ? "1" : "0";
            }

            // Operadores aritméticos
            if (!EsTipoNumerico(ti) || !EsTipoNumerico(td))
            {
                TxtboxSalida.AppendText($"Error semántico: operador '{nodo.dato}' requiere operandos numéricos (se encontró '{ti}' y '{td}'), línea {linea_del_token}\n");
                N_error++;
                tipo = "desconocido";
                calculable = false;
                return null;
            }

            // Módulo
            if (nodo.dato == "%" && (!EsTipoEntero(ti) || !EsTipoEntero(td)))
            {
                TxtboxSalida.AppendText($"Error semántico: el operador '%' solo acepta operandos enteros (se encontró '{ti}' y '{td}'), línea {linea_del_token}\n");
                N_error++;
                tipo = "desconocido";
                calculable = false;
                return null;
            }

            // Tipo resultante
            tipo = (ti == "double" || td == "double") ? "double" :
                   (ti == "float" || td == "float") ? "float" : "int";

            calculable = ci && cd && vi != null && vd != null;
            if (!calculable) return null;

            double numA, numB;
            if (!double.TryParse(vi, NumberStyles.Float, CultureInfo.InvariantCulture, out numA) ||
                !double.TryParse(vd, NumberStyles.Float, CultureInfo.InvariantCulture, out numB))
            { calculable = false; return null; }

            double res = 0;
            switch (nodo.dato)
            {
                case "+": res = numA + numB; break;
                case "-": res = numA - numB; break;
                case "*": res = numA * numB; break;

                case "/":
                    if (Math.Abs(numB) < 0.0000001)
                    {
                        TxtboxSalida.AppendText($"Error semántico: división entre cero, línea {linea_del_token}\n");
                        N_error++;
                        calculable = false;
                        return null;
                    }
                    if (EsTipoEntero(ti) && EsTipoEntero(td))
                        res = Math.Truncate(numA / numB);
                    else
                        res = numA / numB;
                    break;

                case "%":
                    long la = (long)numA, lb = (long)numB;
                    if (lb == 0)
                    {
                        TxtboxSalida.AppendText($"Error semántico: módulo entre cero, línea {linea_del_token}\n");
                        N_error++;
                        calculable = false;
                        return null;
                    }
                    return (la % lb).ToString(CultureInfo.InvariantCulture);

                default:
                    calculable = false;
                    return null;
            }

            return FormatearNumero(res, tipo);
        }

        private void Expresion()
        {
            bool esperaOperando = true;
            int parentesis = 0;

            if (token == ")" || token == ";")
            {
                Error(token, "valor o variable");
                return;
            }

            while (token != null && token != "Fin")
            {
                if (token == ";" || token == "}") break;
                if (token == "," && parentesis == 0) break;

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
                        return;
                    }
                    parentesis--;
                    esperaOperando = false;
                    token = NextToken();
                    continue;
                }

                if (EsOperador(token))
                {
                    if (esperaOperando && token != "-" && token != "!")
                    {
                        Error(token, "valor antes del operador");
                    }
                    esperaOperando = true;
                    token = NextToken();
                    continue;
                }

                if (token == "identificador")
                {
                    string nombreVar = elemento; // nombre real del identificador
                    if (!ExisteSimbolo(nombreVar))
                    {
                        TxtboxSalida.AppendText($"Error semántico: variable '{nombreVar}' no declarada, línea {linea_del_token}\n");
                        N_error++;
                    }

                    token = NextToken();

                    if (token == "(")
                    {
                        parentesis++;
                        token = NextToken();
                        esperaOperando = true;
                        continue;
                    }
                    else
                    {
                        esperaOperando = false;
                        continue;
                    }
                }

                if (token == "numero_entero" || token == "numero_real" || token == "Cadena" || token == "caracter")
                {
                    if (!esperaOperando) Error(token, "operador");
                    esperaOperando = false;
                    token = NextToken();
                    continue;
                }
                break;
            }

            if (esperaOperando) Error("Expresión incompleta");

            if (parentesis > 0) Error("Paréntesis sin cerrar en la expresión");
        }

        private void Declaracion_Local()
        {
            // token actualmente es el tipo
            string tipoLocal = token;

            token = NextToken(); // ID esperado
            if (token != "identificador")
            {
                Error(token, "identificador");
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
                    NodoArbol raiz = AnalizarExpresionMatematica();

                    if (raiz != null)
                    {
                        string tipoResultado;
                        bool calculable;
                        string valorResultado = EvaluarArbol(raiz, out tipoResultado, out calculable);

                        MostrarResultadoArbol(raiz, tipoResultado, valorResultado, calculable);

                        SimboloEntrada sim = ObtenerSimbolo(identificador_actual);
                        if (sim != null)
                        {
                            if (!TiposCompatibles(sim.tipo, tipoResultado))
                            {
                                TxtboxSalida.AppendText($"Error semántico: no se puede asignar una expresión de tipo {tipoResultado} a '{identificador_actual}' de tipo {sim.tipo}, línea {linea_del_token}\n");
                                N_error++;
                            }
                            else if (calculable)
                            {
                                sim.valor = valorResultado;
                            }
                        }
                    }
                    else
                    {
                        while (token != ";" && token != "Fin" && token != null)
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