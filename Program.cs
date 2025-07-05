using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using System.Text;
using System.Text.Json;

var kernel = BuildKernel();
SetupDatabase();
string dbSchema = GetDatabaseSchema();
const string dbPath = "local_company.db";

Console.WriteLine("Database Schema:");
Console.WriteLine(dbSchema);
Console.WriteLine("\nChat with your database! Type 'exit' to quit.");

var textToSqlFunction = CreateTextToSqlFunction(kernel, dbSchema);
var finalAnswerFunction = CreateFinalAnswerFunction(kernel);

while (true)
{
    Console.Write("> ");
    string userInput = Console.ReadLine() ?? "";
    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    try
    {
        var sqlResult = await textToSqlFunction.InvokeAsync(kernel, new() { ["input"] = userInput });
        string sqlQuery = sqlResult.GetValue<string>()!.Trim();
        Console.WriteLine($"\n🤖 Generated SQL: {sqlQuery}");

        string dbData = ExecuteQueryAndFormatResults(dbPath, sqlQuery);
        if (string.IsNullOrWhiteSpace(dbData))
        {
            Console.WriteLine("🤖 I couldn't find any data for that query.\n");
            continue;
        }
        Console.WriteLine($"🗃️ Query Result:\n{dbData}");

        var finalAnswerResult = await finalAnswerFunction.InvokeAsync(kernel, new()
        {
            ["input"] = userInput,
            ["data"] = dbData
        });

        Console.WriteLine($"\n💬 Answer: {finalAnswerResult.GetValue<string>()}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nAn error occurred: {ex.Message}\n");
    }
}

#pragma warning disable SKEXP0070

Kernel BuildKernel()
{
    var builder = Kernel.CreateBuilder();

    builder.AddOllamaChatCompletion(
        modelId: "phi3",
        endpoint: new Uri("http://localhost:11434")
    );

    return builder.Build();
}

KernelFunction CreateTextToSqlFunction(Kernel kernel, string dbSchema)
{
    const string prompt = @"
Given the following database schema, your job is to convert the user's question into a valid SQLite SQL query.
- ONLY output the SQL query.
- Do not add any other text, explanations, or markdown formatting like ```sql.
- Be careful with data types and column names.

Schema:
---
{{$schema}}
---

User Question: {{$input}}

SQL Query:
";

    var executionSettings = new PromptExecutionSettings()
    {
        ExtensionData = new Dictionary<string, object>()
        {
            { "temperature", 0.0 }
        }
    };

    var promptConfig = new PromptTemplateConfig
    {
        Template = prompt.Replace("{{$schema}}", dbSchema),
        ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
        {
            { "default", executionSettings }
        }
    };

    return KernelFunctionFactory.CreateFromPrompt(promptConfig);
}

KernelFunction CreateFinalAnswerFunction(Kernel kernel)
{
    const string prompt = @"
Answer the following user's question based ONLY on the provided data.
If the data is empty or irrelevant, say you could not find an answer.
Be friendly and concise.

Data:
---
{{$data}}
---

User Question: {{$input}}

Answer:
";

    var executionSettings = new PromptExecutionSettings()
    {
        ExtensionData = new Dictionary<string, object>()
        {
            { "temperature", 0.2 }
        }
    };

    var promptConfig = new PromptTemplateConfig
    {
        Template = prompt,
        ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
        {
            { "default", executionSettings }
        }
    };

    return KernelFunctionFactory.CreateFromPrompt(promptConfig);
}

string ExecuteQueryAndFormatResults(string dbPath, string sqlQuery)
{
    var resultBuilder = new StringBuilder();
    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText = sqlQuery;

    using var reader = command.ExecuteReader();

    for (int i = 0; i < reader.FieldCount; i++)
    {
        resultBuilder.Append(reader.GetName(i) + (i == reader.FieldCount - 1 ? "" : "\t"));
    }
    resultBuilder.AppendLine();

    while (reader.Read())
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            resultBuilder.Append(reader.GetValue(i) + (i == reader.FieldCount - 1 ? "" : "\t"));
        }
        resultBuilder.AppendLine();
    }

    return resultBuilder.ToString();
}

static void SetupDatabase()
{
    const string dbPath = "local_company.db";
    if (File.Exists(dbPath)) File.Delete(dbPath);

    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    var createTableCommand = connection.CreateCommand();
    createTableCommand.CommandText =
    @"
        CREATE TABLE Employees (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Department TEXT NOT NULL,
            Salary INTEGER NOT NULL,
            HireDate TEXT NOT NULL
        );
    ";
    createTableCommand.ExecuteNonQuery();

    var insertCommand = connection.CreateCommand();
    insertCommand.CommandText =
    @"
        INSERT INTO Employees (Name, Department, Salary, HireDate) VALUES
        ('Alice Johnson', 'Engineering', 95000, '2022-01-15'),
        ('Bob Smith', 'Sales', 82000, '2021-11-30'),
        ('Charlie Brown', 'Engineering', 110000, '2020-05-20'),
        ('Diana Prince', 'Sales', 78000, '2022-08-01'),
        ('Eve Adams', 'HR', 65000, '2023-02-10');
    ";
    insertCommand.ExecuteNonQuery();

    Console.WriteLine("Database 'local_company.db' created and populated.");
}

static string GetDatabaseSchema()
{
    const string dbPath = "local_company.db";
    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='Employees';";

    using var reader = command.ExecuteReader();
    if (reader.Read())
    {
        return reader.GetString(0);
    }
    return string.Empty;
}