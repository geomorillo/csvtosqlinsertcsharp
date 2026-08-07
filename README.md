# csvtosqlinsertcsharp
Provide table data as a CSV (comma-separated values) file and output a SQL insert statement for a table with the same name as the file.

## Requirements
- [dotnet-script](https://github.com/dotnet-script/dotnet-script): `dotnet tool install -g dotnet-script`
- CsvHelper is restored automatically on the first run (see `nuget.config`).

## Usage 
1. Confirm you have a directory named `csv`
2. Confirm you have a directory named `sql`
3. Save your input CSV file in the `csv` directory
4. In a terminal window, run `dotnet script csvtosqli.csx ExampleTable [batchSize]`
5. Watch the terminal window for any error messages
6. Your SQL insert statement will be saved in `sql/YourFileName.sql`

`batchSize` (optional) sets how many rows each `INSERT` statement contains (default 500).

## CSV support
- Quoted fields with commas, escaped quotes (`""`) and embedded newlines.
- String literals use single quotes (apostrophes are escaped as `''`); numbers are emitted unquoted using the invariant culture, so `1,234.56` or `25,5` are always treated as strings unless the CSV quotes them as numbers are expected.
- The literal `NULL` is emitted as SQL `NULL`.
- Rows with too many/few fields are skipped and reported on the console; processing continues.
