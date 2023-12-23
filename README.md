# csvtosqlinsertcsharp
Provide table data as a CSV (comma-separated values) file and output a SQL insert statement for a table with the same name as the file.


## Usage ⚙
1. Confirm you have a directory named `csv`
2. Confirm you have a directory named `sql`
3. Save your input CSV file in the `csv` directory
4. In a terminal window, run `dotnet script csvtosqli.csx ExampleTable`
5. Watch the terminal window for any error messages
6. Your SQL insert statement will be saved in `sql/YourFileName.sql`