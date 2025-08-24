using MCPAgent.MCP;
using MCPAgent.Services;

internal class Program
{
    private static async Task Main()
    {
        try
        {
            // Create LLM client using configuration service
            using var client = ConfigurationService.CreateLLMClient();
            // print provider info

            Console.WriteLine($"{client.GetAllSettings()}");
            Console.WriteLine($"-------------");

            // Set system message for the generator
            string systemPrompt = @"You are an intelligent **SQL Server database** agent that helps users query and manipulate data using natural language.

AVAILABLE TOOL:
SQLQuery - Execute any SQL query you write
Format:
Tool: SQLQuery
Args: <your_sql_query>

IMPORTANT: Use the exact format above without any backticks or code blocks.

WORKFLOW:
1. When user asks about data, FIRST discover the database schema by querying the proper query based on database engine to list all tables
2. Identify the relevant table(s) for the user's question from the schema results
3. Write and execute the final query to answer the user's question using the discovered table names
4. You can chain multiple SQLQuery tool calls in one response

CRITICAL RULES:
- NEVER invent or guess table names
- ONLY use table names that you discover from schema queries
- If a table doesn't exist in the schema results, DO NOT use it
- Always verify table names exist before using them in queries";

            //systemPrompt = "you are an AI assistant.";
            client.SetSystemMessage(systemPrompt);

            // Pass both client and schema to Agent
            var generator = new Generator(client);

            Console.WriteLine("SQL Database Agent Ready!");
            Console.WriteLine("Commands: 'exit' to quit, 'history' to show conversation, 'clear' to clear history");
            Console.WriteLine("Enter your prompt:");

            while (true)
            {
                Console.Write("\n> ");
                string userPrompt = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userPrompt))
                    continue;

                try
                {
                    string response = await generator.HandlePromptAsync("User Question:" + userPrompt);
                    //string response = await client.GenerateTextAsync(userPrompt);
                    Console.WriteLine("\nAgent Response:");
                    Console.WriteLine(response);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine(ex.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialization Error: {ex.Message}");
        }
        finally
        {
            Console.ResetColor();
        }
    }
}