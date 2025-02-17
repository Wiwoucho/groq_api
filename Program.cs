using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private const string apiUrl = "https://api.groq.com/openai/v1"; // correct API URL
    private const string endpointUrl = "/chat/completions"; // Correct endpoint URL
    private const int maxRetries = 3;

    // added additional feature to select your own model from the list below
    private static int modelChoice;
    private static List<string> models =
        ["gemma2-9b-it", "llama-3.3-70b-versatile", 
        "llama-3.1-8b-instant", "llama-guard-3-8b", 
        "llama3-70b-8192", "llama3-8b-8192",
        "mixtral-8x7b-32768"];

    static void LogChatToFile(string userInput, string aiResponse)
    {
        try
        {
            //retrieve the desktop path or where you want to store the log file
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            //define the log file path
            string logFilePath = Path.Combine(desktopPath, "chat_log.txt");

            //Prepare the log entry with timestamp, user input and AI response
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - User: {userInput} \n AI: {aiResponse}\n\n";

            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }
    }
    static async Task<string> GetAIResponse(string userInput)
    {
        // Retrieve the API key from the environment variable(not directly from string) as GROQ requirements
        string apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            return "Error: API Key is not set in environment variables.";
        }

        // Clear the Authorization header before each request
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var requestData = new
        {
            model = models[modelChoice],
            messages = new[] { new { role = "user", content = userInput } }
        };

        var jsonRequest = JsonConvert.SerializeObject(requestData);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        int retryCount = 0;
        string fullUrl = $"{apiUrl}{endpointUrl}";
        while (retryCount < maxRetries)
        {
            try
            {
                var response = await client.PostAsync(fullUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    // Print the raw response for debugging purposes
                    //Console.WriteLine("Raw API Response: " + responseString);

                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);

                    // Check if jsonResponse is not null and contains expected structure
                    if (jsonResponse != null && jsonResponse.choices != null && jsonResponse.choices.Count > 0)
                    {
                        return jsonResponse.choices[0].message.content.ToString();
                    }
                    else
                    {
                        return "Error: Unexpected response structure.";
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    retryCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                retryCount++;
            }
        }

        return "Error: Maximum retries exceeded.";
    }

    static async Task Main()
    {
        Console.WriteLine("Choose your AI model ");
        Console.WriteLine($"Select a number to proceed");
        for (int i = 0; i < models.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {models[i]}");
        }
        modelChoice = int.Parse(Console.ReadLine()) - 1;




        Console.WriteLine("Welcome to your AI chatbot! Type 'exit' to quit.");
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string logFilePath = Path.Combine(desktopPath, "chat_log.txt");
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Chat started\n";
        string chatEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";


        try
        {
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }

        while (true)
        {
            chatEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            Console.Write($"\n{chatEntry} You: ");
            string userInput = Console.ReadLine();
            if (string.IsNullOrEmpty(userInput) || userInput.ToLower() == "exit" || userInput.ToLower() == "quit")
            {
                if (string.IsNullOrEmpty(userInput))
                {

                    Console.WriteLine("Please enter a message.");

                }
                else
                {
                    Console.WriteLine("\nGoodbye! 👋");
                    break;
                }
            }
            string response = await GetAIResponse(userInput);
            LogChatToFile(userInput, response);

            Console.WriteLine($"{chatEntry} AI: " + response);
        }
    }
}