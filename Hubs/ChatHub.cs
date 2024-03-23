using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace SignalrChat.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatHub> _logger;
        private static List<string> _chatHistory = new List<string>();
        private static Dictionary<string, UserPreferences> _userPreferences = new Dictionary<string, UserPreferences>();

        public ChatHub(IConfiguration configuration, ILogger<ChatHub> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendMessage(string user, string message)
        {
            // Add the message to the chat history
            _chatHistory.Add($"{user}: {message}");
            Console.Write("Message of ID: " + Context.ConnectionId);
            _logger.LogInformation("Message of ID!!: " + Context.ConnectionId);

            // If the message is a summary request, handle it separately
            if (message.ToLower().Trim() == "/summary")
            {
                var summary = await GenerateSummaryAsync(_chatHistory);
                // Send the summary only to the user who requested it
                await Clients.Caller.SendAsync("ReceiveSummary", summary);
            }
            else
            {
                // Create a GUID for the message
                var messageId = Guid.NewGuid().ToString();

                // Get list of all current used Languages from _userPreferences
                List<string> languages = new List<string>();
                foreach (var connectionId in _userPreferences.Keys)
                {
                    languages.Add(_userPreferences[connectionId].Language);
                }

                // Loop through all connected clients and send the message to them
                foreach (var connectionId in _userPreferences.Keys)
                {
                    // Check if the user wants to receive notifications
                    _logger.LogInformation("Connection ID and Language: " + connectionId + " " + _userPreferences[connectionId].Language);
                    if (_userPreferences[connectionId].ReceiveNotifications)
                    {
                        // Send the message to the user
                        await Clients.Client(connectionId).SendAsync("ReceiveMessage", user, message + " Meow " + _userPreferences[connectionId].Language, messageId);
                    }
                }

                // Mock getting a translation for each language
                // Make a dictoinary to store the translations
                Dictionary<string, string> translations = new Dictionary<string, string>();
                foreach (var language in languages)
                {
                    // Sleep to mock time to get translation
                    await Task.Delay(1000);
                    var translation = $"Translation in {language}: {message}";
                    translations.Add(language, translation);

                    // TODO add translations to database
                }

                // Send Translation to all users based on their language
                foreach (var connectionId in _userPreferences.Keys)
                {
                    // Check if the user wants to receive notifications
                    if (_userPreferences[connectionId].ReceiveNotifications)
                    {
                        // Send the message to the user
                        await Clients.Client(connectionId).SendAsync("ReceiveTranslation", messageId, translations[_userPreferences[connectionId].Language], _userPreferences[connectionId].Language);
                    }
                }
            }
        }

        public async Task UpdateUserPreferences(string preference)
        {
            _logger.LogInformation("UpdateUserPreferences: " + preference);
            var connectionId = Context.ConnectionId;
            _logger.LogInformation("Connection ID: " + connectionId);
            _userPreferences[connectionId] = new UserPreferences
            {
                ReceiveNotifications = true,
                Language = preference
            };

            // Acknowledge the preference update to the user
            await Clients.Caller.SendAsync("PreferenceUpdated", preference);
        }


        private async Task<string> GenerateSummaryAsync(List<string> messages)
        {
            var apiKey = _configuration.GetSection("OpenAI")["ApiKey"];
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            // Prepare the messages for the API request
            var apiMessages = new List<object>
            {
                new { role = "system", content = "You are a helpful assistant that summarizes the chats." }
            };
            apiMessages.AddRange(messages.Select(m => new { role = "user", content = m }));

            var requestBody = new
            {
                model = "gpt-3.5-turbo", // Use the model that best suits your needs
                messages = apiMessages
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {responseBody}");


                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<SummaryResponse>(responseBody, options);
                Console.WriteLine($"Result: {result}");

                if (result?.Choices != null)
                {
                    Console.WriteLine($"Choices count: {result.Choices.Length}");
                }

                // Check if the Choices property is not null and has at least one item
                if (result?.Choices.Length > 0)
                {
                    return result.Choices[0].Message.Content; // Assuming you want the first choice
                }
                else
                {
                    // Handle the case where Choices is null or empty
                    return "No summary available.";
                }
            }
            else
            {
                Console.WriteLine($"Error generating summary. Status Code: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error Content: {errorContent}");

                // Handle error
                return "Error generating summary.";
            }
        }

        // Override OnConnectedAsync to track connections
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Connection ID: " + Context.ConnectionId);
            _userPreferences.Add(Context.ConnectionId, new UserPreferences());
            return base.OnConnectedAsync();
        }

        // Override OnDisconnectedAsync to clean up when a connection is lost
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove the disconnected client's preferences
            _userPreferences.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        // Define a class to deserialize the response
        public class SummaryResponse
        {
            public Choice[] Choices { get; set; }
        }

        public class Choice
        {
            public Message Message { get; set; }
        }

        public class Message
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        public class UserPreferences
        {
            public bool ReceiveNotifications { get; set; } = true;
            public string Language { get; set; } = "en-US";
        }
    }
}
