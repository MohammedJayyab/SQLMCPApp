using MCPAgent.MCP;
using MCPAgent.Services;

namespace MCPAgent;

internal class Program
{
    private const string SystemPrompt = @"You are an intelligent **SQL Server database** agent that helps users query and manipulate data using natural language.

AVAILABLE TOOL:
SQLQuery - Execute any SQL query you write
Format:
Tool: SQLQuery
Args: <your_sql_query>

IMPORTANT: Use the exact format above without any backticks or code blocks.

WORKFLOW:
1. When user asks about data, FIRST discover the database schema by querying INFORMATION_SCHEMA.TABLES to list all tables
2. Identify the relevant table(s) for the user's question from the schema results
3. Explore the table structure to understand column meanings and data patterns
4. Write and execute the final query to answer the user's question using the discovered table names
5. You can chain multiple SQLQuery tool calls in one response

CRITICAL RULES:
- NEVER invent or guess table names
- ONLY use table names that you discover from schema queries
- If a table doesn't exist in the schema results, DO NOT use it
- Always verify table names exist before using them in queries
- When you find relevant columns, explore the data to understand what values represent disabled/enabled states
- Take initiative to investigate data patterns rather than asking for clarification";

    private static async Task Main()
    {
        try
        {
            await RunApplicationAsync();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal Error: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task RunApplicationAsync()
    {
        // Display application header
        DisplayApplicationHeader();

        // Initialize services
        using var llmClient = await InitializeServicesAsync();

        // Display application info
        DisplayApplicationInfo(llmClient);

        // Start interactive session
        await RunInteractiveSessionAsync(llmClient);
    }

    private static void DisplayApplicationHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    MCPAgent - SQL Database Agent             ║");
        Console.WriteLine("║              Natural Language to SQL Query Converter         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();

        Console.WriteLine("Developed By: Mohammed Abujayyab");
        Console.WriteLine("GitHub: https://github.com/MohammedJayyab");
        Console.WriteLine();
    }

    private static async Task<LLMKit.LLMClient> InitializeServicesAsync()
    {
        Console.WriteLine("Initializing services...");

        var client = ConfigurationService.CreateLLMClient();
        client.SetSystemMessage(SystemPrompt);

        var generator = new Generator(client);

        Console.WriteLine("(OK) Services initialized successfully");
        Console.WriteLine();

        return client;
    }

    private static void DisplayApplicationInfo(LLMKit.LLMClient client)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Configuration:");
        Console.WriteLine(client.GetAllSettings());
        Console.WriteLine("─".PadRight(50, '─'));
        Console.ResetColor();
        Console.WriteLine();
    }

    private static async Task RunInteractiveSessionAsync(LLMKit.LLMClient client)
    {
        var generator = new Generator(client);

        DisplaySessionInfo();

        while (true)
        {
            try
            {
                var userInput = GetUserInput();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (IsCommand(userInput))
                {
                    HandleCommand(userInput, client);
                    continue;
                }

                await ProcessUserQueryAsync(generator, userInput);
            }
            catch (Exception ex)
            {
                DisplayError(ex);
            }
        }
    }

    private static void DisplaySessionInfo()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("🚀 SQL Database Agent Ready!");
        Console.WriteLine("Commands: 'exit' to quit, 'history' to show conversation, 'clear' to clear history");
        Console.WriteLine("Enter your prompt:");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static string GetUserInput()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("> ");
        Console.ResetColor();

        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static bool IsCommand(string input)
    {
        return input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
               input.Equals("history", StringComparison.OrdinalIgnoreCase) ||
               input.Equals("clear", StringComparison.OrdinalIgnoreCase);
    }

    private static void HandleCommand(string command, LLMKit.LLMClient client)
    {
        switch (command.ToLower())
        {
            case "exit":
                Console.WriteLine("👋 Goodbye!");
                Environment.Exit(0);
                break;

            case "history":
                DisplayConversationHistory(client);
                break;

            case "clear":
                ClearConversationHistory(client);
                break;
        }
    }

    private static void DisplayConversationHistory(LLMKit.LLMClient client)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("-> Conversation History:");
        Console.WriteLine("─".PadRight(50, '─'));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(client.GetFormattedConversation());
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("─".PadRight(50, '─'));
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void ClearConversationHistory(LLMKit.LLMClient client)
    {
        client.ClearConversation();
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("->  Conversation history cleared");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static async Task ProcessUserQueryAsync(Generator generator, string userQuery)
    {
        Console.WriteLine();

        var response = await generator.HandlePromptAsync($"User Question: {userQuery}");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(":: Agent Response:");
        Console.WriteLine("─".PadRight(50, '─'));
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine(response);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("─".PadRight(50, '─'));
        Console.ResetColor();
        Console.WriteLine();
    }

    private static void DisplayError(Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"-> Error: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"   Details: {ex.InnerException.Message}");
        }

        Console.ResetColor();
        Console.WriteLine();
    }
}