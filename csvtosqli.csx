#r "nuget: CsvHelper, 33.1.0"

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

// =====================================================================
// csvtosqli.csx — Convierte un CSV en sentencias INSERT multi-fila.
//
// Uso:
//   dotnet script csvtosqli.csx NombreTabla [tamanoLote]
//
//   - Lee      ./csv/NombreTabla.csv
//   - Escribe  ./sql/NombreTabla.sql
//   - tamanoLote (opcional, default 500): filas por sentencia INSERT.
//
// El parsing usa CsvHelper: soporta campos citados con comas, comillas
// escapadas ("") y saltos de línea dentro de un campo.
// =====================================================================

string[] args = Environment.GetCommandLineArgs();

// Los argumentos del usuario empiezan en el índice 2
// ([0]=dotnet-script.dll, [1]=ruta del script).
string ObtenerArg(int indice) => args.Length > indice ? args[indice] : null;

string nombreTabla = ObtenerArg(2);
int tamanoLote = int.TryParse(ObtenerArg(3), out int lote) && lote > 0 ? lote : 500;

if (string.IsNullOrEmpty(nombreTabla))
{
    Console.WriteLine("Falta el nombre de la tabla. Uso: dotnet script csvtosqli.csx NombreTabla [tamanoLote]");
    return;
}

string rutaCsv = $"./csv/{nombreTabla}.csv";
string rutaSql = $"./sql/{nombreTabla}.sql";

if (!File.Exists(rutaCsv))
{
    Console.WriteLine($"No se encontró el archivo {rutaCsv}");
    return;
}

// --- 1. Leer el CSV con CsvHelper ---
using (StreamReader lector = new StreamReader(rutaCsv, Encoding.UTF8))
using (CsvReader csv = new CsvReader(lector, new CsvConfiguration(CultureInfo.InvariantCulture)))
{
    if (!csv.Read())
    {
        Console.WriteLine($"El archivo {rutaCsv} está vacío.");
        return;
    }
    csv.ReadHeader(); // HeaderRecord solo se llena tras ReadHeader()
    string[] columnas = csv.HeaderRecord;
    if (columnas == null || columnas.Length == 0)
    {
        Console.WriteLine($"El archivo {rutaCsv} no tiene encabezado.");
        return;
    }

    // --- 2. Preparar el esqueleto del INSERT ---
    string encabezadoInsert = $"INSERT INTO {nombreTabla} ({string.Join(", ", columnas)})\nVALUES\n";

    // --- 3. Procesar filas ---
    StringBuilder valores = new StringBuilder();
    bool primerLote = true;
    int filasEnLote = 0;
    int filasProcesadas = 0;
    int errores = 0;
    int numeroFila = 1; // el encabezado es la fila 1 del archivo

    string FormatearValor(string valor)
    {
        if (valor == "NULL") return "NULL";
        // Número con cultura invariante: "1,234.56" NO se considera número,
        // "1234.56" sí. Se descartan NaN e Infinity.
        if (double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out double num) && double.IsFinite(num))
            return valor;
        // Cadena: comillas simples con escape de apóstrofes (' -> '').
        return "'" + valor.Replace("'", "''") + "'";
    }

    async Task EscribirLote(bool final)
    {
        valores.Remove(valores.Length - 2, 2); // quitar la coma y el salto finales
        valores.Append(final ? ";" : ";\n\n");
        string statement = encabezadoInsert + valores;
        if (primerLote)
        {
            // El primer lote sobreescribe cualquier archivo previo.
            await File.WriteAllTextAsync(rutaSql, statement);
            primerLote = false;
        }
        else
        {
            await File.AppendAllTextAsync(rutaSql, statement);
        }
        valores.Clear();
        filasEnLote = 0;
    }

    while (true)
    {
        try
        {
            if (!csv.Read()) break;
        }
        catch (Exception ex)
        {
            // Datos malformados (p. ej. comillas sin cerrar): no se puede
            // continuar de forma fiable a partir de ese punto.
            errores++;
            Console.Error.WriteLine($"Fila {numeroFila + 1}: CSV malformado, se detiene el proceso. {ex.Message}");
            break;
        }
        numeroFila++;

        string[] campos;
        try
        {
            campos = csv.Parser.Record ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            errores++;
            Console.Error.WriteLine($"Fila {numeroFila}: no se pudo leer. {ex.Message}");
            continue;
        }

        if (campos.Length != columnas.Length)
        {
            errores++;
            Console.Error.WriteLine($"Fila {numeroFila}: se esperaban {columnas.Length} campos, se encontraron {campos.Length}. Fila omitida.");
            continue;
        }

        StringBuilder fila = new StringBuilder("\t(");
        for (int i = 0; i < campos.Length; i++)
        {
            if (i > 0) fila.Append(", ");
            fila.Append(FormatearValor(campos[i]));
        }
        fila.Append("),\n");
        valores.Append(fila);
        filasEnLote++;
        filasProcesadas++;

        if (filasEnLote >= tamanoLote)
        {
            await EscribirLote(final: false);
        }
    }

    // --- 4. Escribir el último lote (si queda algo pendiente) ---
    if (valores.Length > 0)
    {
        await EscribirLote(final: true);
    }

    Console.WriteLine($"Procesadas {filasProcesadas} filas, {errores} errores. Guardado en {rutaSql}.");
}
