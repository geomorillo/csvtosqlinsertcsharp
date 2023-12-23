#r "nuget:System.IO.FileSystem, 4.3.0"
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class FileUtils
{
    public static void CreateIfNot(string filePath)
    {
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close(); // Cierra el archivo inmediatamente después de crearlo
        }
    }
}

async Task WriteSQL(string statement, string saveFileAs = "", bool isAppend = false)
{
    try
    {
        string destinationFile = Environment.GetCommandLineArgs().Length > 2 ? Environment.GetCommandLineArgs()[2] : saveFileAs;
        if (string.IsNullOrEmpty(destinationFile))
        {
            throw new Exception("Missing saveFileAs parameter");
        }

        FileUtils.CreateIfNot($"./sql/{destinationFile}.sql");

        if (isAppend)
        {
            await File.AppendAllTextAsync($"sql/{Environment.GetCommandLineArgs()[2]}.sql", statement);
        }
        else
        {
            await File.WriteAllTextAsync($"sql/{Environment.GetCommandLineArgs()[2]}.sql", statement);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

async Task ReadCSV(string csvFileName = "", int batchSize = 0)
{
    try
    {
        string fileAndTableName = Environment.GetCommandLineArgs().Length > 2 ? Environment.GetCommandLineArgs()[2] : csvFileName;

batchSize = int.TryParse(Environment.GetCommandLineArgs().Length > 3 ? Environment.GetCommandLineArgs()[3] : null, out int parsedBatchSize) ?
    parsedBatchSize : batchSize > 0 ? batchSize : 500;

        bool isAppend = false;

        if (string.IsNullOrEmpty(fileAndTableName))
        {
            throw new Exception("Missing csvFileName parameter");
        }

        string filePath = $"./csv/{fileAndTableName}.csv";
    
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found");
            return;
        }

        string data = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        string[] linesArray = data.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        string[] columnNames = linesArray[0].Split(',');

        StringBuilder beginSQLInsert = new StringBuilder($"INSERT INTO {fileAndTableName} (");
        foreach (string name in columnNames)
        {
            beginSQLInsert.Append($"{name}, ");
        }
        beginSQLInsert.Remove(beginSQLInsert.Length - 2, 2).Append(")\nVALUES\n");

        StringBuilder values = new StringBuilder();
        for (int index = 1; index < linesArray.Length; index++)
        {
            string line = linesArray[index];
            string[] arr = line.Split(',');

            if (arr.Length > columnNames.Length)
            {
                Console.WriteLine(arr);
                throw new Exception("Too Many Values in row");
            }
            else if (arr.Length < columnNames.Length)
            {
                Console.WriteLine(arr);
                throw new Exception("Too Few Values in row");
            }

            if (index > 1 && index % batchSize == 1)
            {
                values.Remove(values.Length - 2, 2).Append(";\n\n");

                string sqlStatement = beginSQLInsert.ToString() + values;
                await WriteSQL(sqlStatement, fileAndTableName, isAppend);
                values.Clear();
                isAppend = true;
            }

            StringBuilder valueLine = new StringBuilder("\t(");
            foreach (string value in arr)
            {
                if (value == "NULL" || double.TryParse(value, out _))
                {
                    valueLine.Append($"{value}, ");
                }
                else
                {
                    if (value[0] == '"') valueLine.Append($"{value}, ");
                    else valueLine.Append($"\"{value}\", ");
                }
            }
            valueLine.Remove(valueLine.Length - 2, 2).Append("),\n");
            values.Append(valueLine);
        }

        values.Remove(values.Length - 2, 2).Append(";");
        string finalSQLStatement = beginSQLInsert.ToString() + values;
        await WriteSQL(finalSQLStatement, fileAndTableName, isAppend);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

await ReadCSV();
Console.WriteLine("Finished!");