# Local LLM Database Chat with .NET and Semantic Kernel

This project is a C# console application that demonstrates how to chat with a local SQLite database using natural language. It leverages the power of a local Large Language Model (LLM) served by **Ollama** and the AI orchestration capabilities of **Microsoft Semantic Kernel**.

The application follows a simple, two-step RAG (Retrieval-Augmented Generation) pattern:
1.  It converts a user's question into an SQL query.
2.  It executes the query, retrieves the data, and then generates a friendly, natural language answer based on the results.

## Demo

Here is an example of what a session with the application looks like:

```
Database 'local_company.db' created and populated.
Database Schema:
CREATE TABLE Employees (
            Id INTEGER PRIMARY KEY,
            Name TEXT NOT NULL,
            Department TEXT NOT NULL,
            Salary INTEGER NOT NULL,
            HireDate TEXT NOT NULL
        )

Chat with your database! Type 'exit' to quit.
> Who are the engineers?

🤖 Generated SQL: SELECT Name FROM Employees WHERE Department = 'Engineering';
🗃️ Query Result:
Name
Alice Johnson
Charlie Brown

💬 Answer: The engineers are Alice Johnson and Charlie Brown.

> What is the average salary in the Sales department?

🤖 Generated SQL: SELECT AVG(Salary) FROM Employees WHERE Department = 'Sales'
🗃️ Query Result:
AVG(Salary)
80000

💬 Answer: The average salary for the Sales department is 80000.

> exit
```

## Features

-   **Natural Language to SQL:** Ask complex questions in plain English.
-   **Two-Step RAG Pipeline:** Ensures the LLM's final answer is grounded in real data from your database.
-   **Local-First:** Runs entirely on your machine, with no need for cloud-based AI services. Your data stays private.
-   **Powered by .NET & Semantic Kernel:** Built on the latest **.NET 9** and the flexible Semantic Kernel library for AI orchestration.
-   **Easy to Customize:** Simple to modify the prompts, change the LLM model, or connect to a different database.

## How It Works

The application's logic is straightforward:

1.  **Database Setup:** On first run, it creates a simple `local_company.db` SQLite database with an `Employees` table and populates it with sample data.
2.  **Schema Retrieval:** It reads the `CREATE TABLE` schema from the database. This schema is crucial context for the LLM.
3.  **Step 1: Generate SQL Query:**
    -   You ask a question (e.g., "Who earns more than 90000?").
    -   The application sends your question and the database schema to the LLM using a specialized "Text-to-SQL" prompt.
    -   The LLM returns a single, executable SQL query (`SELECT * FROM Employees WHERE Salary > 90000;`).
4.  **Step 2: Execute Query & Generate Final Answer:**
    -   The application executes the generated SQL against the SQLite database.
    -   The raw data results are retrieved.
    -   The application sends the original question *and* the data results to the LLM with a second "Final Answer" prompt.
    -   The LLM uses this information to formulate a friendly, human-readable response ("Alice Johnson and Charlie Brown earn more than 90000.").

## Prerequisites

Before you begin, ensure you have the following installed:

1.  **.NET 9 SDK:** [Download .NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) (Note: Preview versions may be required).
2.  **Ollama:** [Install Ollama](https://ollama.com/) on your machine and make sure the service is running.
3.  **An Ollama Model:** This demo is configured to use `phi3`. Pull it by running the following command in your terminal:
    ```sh
    ollama run phi3
    ```

## Getting Started

1.  **Clone the repository:**
    ```sh
    git clone <your-repository-url>
    cd <your-repository-directory>
    ```

2.  **Run the application:**
    ```sh
    dotnet run
    ```

The application will start, set up the database, and present you with a `>` prompt.

## Usage

-   Type your question about the employees and press `Enter`.
-   The application will show you the generated SQL, the raw data, and the final natural language answer.
-   Type `exit` and press `Enter` to quit the application.

## Customization

### Using a Different LLM

To use a different model from Ollama (e.g., `llama3` or `mistral`), simply change the `modelId` in the `BuildKernel()` method in `Program.cs`.

```csharp
// Program.cs

Kernel BuildKernel()
{
    var builder = Kernel.CreateBuilder();

    builder.AddOllamaChatCompletion(
        // Change the model ID here
        modelId: "llama3", // For example, use llama3
        endpoint: new Uri("http://localhost:11434")
    );

    return builder.Build();
}
```
Remember to pull the new model first with `ollama run <model_name>`.

### Using a Different Database

To adapt this project for your own SQLite database:

1.  **Update the connection string:** Change the `dbPath` constant to point to your database file.
2.  **Update Schema Retrieval:** Modify the `GetDatabaseSchema()` method to correctly extract the `CREATE TABLE` statements for all relevant tables in your database.
3.  **Adjust Prompts:** You may need to tweak the prompts in `CreateTextToSqlFunction` if your schema is complex or has unique conventions.

**NOTE: This project is designed to run entirely on your local machine. The AI Models require a fast and powerful computer for quick responses. It does not require any cloud services or external APIs, ensuring complete data privacy and control.**

## 📞 Contact

For any questions, feedback, or inquiries, please feel free to reach out.

**Don Potts** - [Don.Potts@DonPotts.com](mailto:Don.Potts@DonPotts.com)
