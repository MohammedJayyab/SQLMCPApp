using LLMKit;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.RegularExpressions;

namespace MCPAgent.MCP
{
    public class Generator
    {
        public static string ConnectionString => MCPAgent.Services.ConfigurationService.GetConnectionString();
        private LLMClient _llmClient;

        public Generator(LLMClient lLMClient)
        {
            _llmClient = lLMClient;
        }

        internal async Task<string> HandlePromptAsync(string userPrompt)
        {
            string response = await _llmClient.GenerateTextAsync(userPrompt);

            // Process the response and execute any tool calls in a loop until complete
            response = await ProcessToolCallsLoop(response, userPrompt);

            return response;
        }

        private async Task<string> ProcessToolCallsLoop(string response, string userPrompt)
        {
            string currentResponse = response;
            int maxIterations = 10; // Prevent infinite loops
            int iteration = 0;

            while (iteration < maxIterations)
            {
                // Process any tool calls in the current response
                string processedResponse = await ProcessToolCalls(currentResponse);

                // Check if there are more tool calls to process
                var moreToolCalls = Regex.Matches(processedResponse, @"Tool:\s*SQLQuery\s*\nArgs:\s*(.*?)(?=\n\n|\nTool:|$)", RegexOptions.Singleline);

                if (moreToolCalls.Count == 0)
                {
                    // No more tool calls, ask LLM to continue with the results
                    string continuationPrompt = @$"Here are the SQL query results:\n{processedResponse}
                                                \n\n Based on these results, please continue to answer the user's question: '{userPrompt}'.
                                                \n\n If you need more information (like table columns), write additional SQL queries.
                                                \n\n If you have enough information, provide the final answer.";
                    string continuationResponse = await _llmClient.GenerateTextAsync(continuationPrompt);

                    // Check if the continuation has tool calls
                    var continuationToolCalls = Regex.Matches(continuationResponse, @"Tool:\s*SQLQuery\s*\nArgs:\s*(.*?)(?=\n\n|\nTool:|$)", RegexOptions.Singleline);

                    if (continuationToolCalls.Count > 0)
                    {
                        // Continue the loop with the new response
                        currentResponse = continuationResponse;
                        iteration++;

                        /*Console.WriteLine("Continuation Response with more tool calls found, continuing processing...");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(continuationResponse);
                        Console.ResetColor();*/

                        continue;
                    }
                    else
                    {
                        // No more tool calls, return the complete response
                        return $"{processedResponse}\n\n{continuationResponse}";
                    }
                }
                else
                {
                    // Continue processing with the current response
                    currentResponse = processedResponse;
                    iteration++;
                }
            }

            return currentResponse;
        }

        private async Task<string> ProcessToolCalls(string response)
        {
            // Look for tool call patterns in the response - handle both formats
            var toolCallPatterns = new[]
            {
                @"Tool:\s*SQLQuery\s*\nArgs:\s*(.*?)(?=\n\n|\nTool:|$)", // Standard format
                @"```tool_code\s*\nTool:\s*SQLQuery\s*\nArgs:\s*(.*?)\s*\n```" // Backtick format
            };

            var allMatches = new List<Match>();
            foreach (var pattern in toolCallPatterns)
            {
                var matches = Regex.Matches(response, pattern, RegexOptions.Singleline);
                allMatches.AddRange(matches.Cast<Match>());
            }

            if (allMatches.Count == 0)
            {
                // No tool calls found, return as is
                return response;
            }

            var resultBuilder = new StringBuilder(response);

            foreach (Match match in allMatches)
            {
                string sqlQuery = match.Groups[1].Value.Trim();

                // Clean up the SQL query - remove any remaining backticks or formatting
                sqlQuery = Regex.Replace(sqlQuery, @"```.*?```", "", RegexOptions.Singleline);
                sqlQuery = Regex.Replace(sqlQuery, @"`", "", RegexOptions.Singleline); // Remove any remaining backticks
                sqlQuery = sqlQuery.Trim();

                if (!string.IsNullOrEmpty(sqlQuery))
                {
                    // Execute the SQL query
                    /*Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\nExecuting SQL Query:\n{sqlQuery}\n");
                    Console.ResetColor();   */

                    string queryResult = await ExecuteSQLQuery(sqlQuery);

                    // Replace the tool call with the result
                    resultBuilder.Replace(match.Value, $"SQL Query Result:\n{queryResult}");
                }
            }

            return resultBuilder.ToString();
        }

        private async Task<string> ExecuteSQLQuery(string query)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                SqlCommand cmd = new SqlCommand(query, conn);
                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();
                StringBuilder sb = new StringBuilder();

                // Add column headers
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    sb.Append(reader.GetName(i) + "\t");
                }
                sb.AppendLine();

                // Add data rows
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        sb.Append(reader.GetValue(i).ToString() + "\t");
                    }
                    sb.AppendLine();
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nError executing SQL query: {ex.Message}\n");
                return $"SQL Error: {ex.Message}";
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}