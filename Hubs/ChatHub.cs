#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using comp4870project.Model;
using DockerMVC.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SignalrChat.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatHub> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private static List<string> _chatHistory = new List<string>();
        private static Dictionary<string, UserPreferences> _userPreferences = new Dictionary<string, UserPreferences>();
        private static Dictionary<string, string> onlineUsers = new Dictionary<string, string>();
        public ChatHub(IConfiguration configuration, ILogger<ChatHub> logger, ApplicationDbContext context, UserManager<User> userManager)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
        }


        private async Task<string> TranslateMessageAsync(string message, string targetLanguage, string orginalLanguage = "en")
        {
            var subscriptionKey = _configuration["AzureTranslation:SubscriptionKey"];
            var endpoint = _configuration["AzureTranslation:Endpoint"];
            var location = _configuration["AzureTranslation:Location"];

            string route = $"/translate?api-version=3.0&from={orginalLanguage}&to={targetLanguage}";
            object[] body = new object[] { new { Text = message } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    // Read response as a string.
                    string result = await response.Content.ReadAsStringAsync();
                    var translationResult = JsonConvert.DeserializeObject<List<TranslationResult>>(result);
                    return translationResult[0].Translations[0].Text;
                }
                else
                {
                    throw new Exception($"Error translating message. Status Code: {response.StatusCode}");
                }
            }
        }


        public class TranslationResult
        {
            public List<Translation> Translations { get; set; }
        }

        public class Translation
        {
            public string Text { get; set; }
            public string To { get; set; }
        }


        public async Task SendMessage(string user, string message)
        {
            // Add the message to the chat history
            _chatHistory.Add($"{user}: {message}");
            Console.Write("Message of ID: " + Context.ConnectionId);
            _logger.LogInformation("Message of ID!!: " + Context.ConnectionId);
            var msgLanguage = _userPreferences[Context.ConnectionId].Language;

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
                var messageId = Guid.NewGuid();

                // Get list of all current used Languages from _userPreferences
                List<string> languages = new List<string>();
                foreach (var connectionId in _userPreferences.Keys)
                {
                    if (languages.Contains(_userPreferences[connectionId].Language) == false)
                    {
                        languages.Add(_userPreferences[connectionId].Language);
                    }
                }

                // Loop through all connected clients and send the message to them
                foreach (var connectionId in _userPreferences.Keys)
                {
                    // Check if the user wants to receive notifications
                    _logger.LogInformation("Connection ID and Language: " + connectionId + " " + _userPreferences[connectionId].Language);
                    if (_userPreferences[connectionId].ReceiveNotifications)
                    {
                        // Send the message to the user
                        await Clients.Client(connectionId).SendAsync("ReceiveMessage", user, message, messageId, msgLanguage);
                    }
                }

                // Mock getting a translation for each language
                // Make a Dictionary to store the translations
                Dictionary<string, string> translations = new Dictionary<string, string>();
                foreach (var language in languages)
                {
                    var translatedMessage = "";
                    // Translate the message to the user's language
                    if (language == _userPreferences[Context.ConnectionId].Language)
                    {
                        translatedMessage = message;
                    }
                    else
                    {
                        translatedMessage = await TranslateMessageAsync(message, language, msgLanguage);
                    }
                    translations.Add(language, translatedMessage);

                    // Add translations to database
                    var newMessage = new SavedMessage
                    {
                        ID = Guid.NewGuid(),
                        MessageId = messageId,
                        Language = language,
                        SenderName = user,
                        OriginalMessage = false,
                        Content = translatedMessage,
                        ConversationId = Guid.NewGuid(),
                        SentDate = DateTime.Now
                    };
                    if (language == msgLanguage)
                    {
                        newMessage.OriginalMessage = true;
                    }
                    _context.Messages.Add(newMessage);
                    _context.SaveChangesAsync();
                }

                // Send Translation to all users based on their language
                foreach (var connectionId in _userPreferences.Keys)
                {
                    // Check if the user wants to receive notifications
                    if (_userPreferences[connectionId].ReceiveNotifications)
                    {
                        var curLanguage = _userPreferences[connectionId].Language;
                        if (curLanguage != msgLanguage)
                        {
                            // Send the message to the user
                            await Clients.Client(connectionId).SendAsync("ReceiveTranslation", messageId, translations[curLanguage], curLanguage);
                        }
                    }
                }
            }
        }

        // Get the last 10 messages from the chat history that match there language
        // Get the messages from the Database
        public async Task ChatHistory()
        {
            // Get the language of requesting user
            var connectionId = Context.ConnectionId;
            var language = _userPreferences[connectionId].Language;

            // Get the last 10 messages from the database that match language
            var lastMessages = _context.Messages.Where(m => m.Language == language).OrderByDescending(m => m.SentDate).Take(10).ToList();


            // Check if the user wants to receive notifications
            if (_userPreferences[connectionId].ReceiveNotifications)
            {
                // Send the messages to the user
                await Clients.Client(connectionId).SendAsync("ReceiveChatHistory", lastMessages, _userPreferences[connectionId].Language);
            }
        }

        public async Task SendTypingNotification(string user)
        {
            // Broadcast to all clients except the sender
            await Clients.Others.SendAsync("ReceiveTypingNotification", user);
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

            // Fetch the last 10 messages from the database
            var lastMessages = _context.Messages
             .Where(m => m.OriginalMessage)
              .OrderByDescending(m => m.SentDate)
              .Take(10)
              .Select(m => new
              {
                  role = "user",
                  content = $"{m.SenderName} : {m.Content}"
              })
                                                               .ToList();
            lastMessages.Reverse();
            // Prepare the messages for the API request
            var apiMessages = new List<object>
            {
                new { role = "system", content = "You are a helpful assistant that summarizes the chats." }
            };
            apiMessages.AddRange(lastMessages);

            var requestBody = new
            {
                model = "gpt-3.5-turbo", // Use the model that best suits your needs
                messages = apiMessages
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {responseBody}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = System.Text.Json.JsonSerializer.Deserialize<SummaryResponse>(responseBody, options);
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

        // Send the User the last 10 messages in their language
        public async Task GetChatHistory()
        {
            // Get the language of requesting user
            var connectionId = Context.ConnectionId;
            var language = _userPreferences[connectionId].Language;

            // Get the last 10 messages from the database that match language
            var lastMessages = _context.Messages.Where(m => m.OriginalMessage == true).OrderByDescending(m => m.SentDate).Take(10).ToList();

            for (int i = 0; i < lastMessages.Count; i++)
            {
                if (lastMessages[i].Language != language)
                {
                    // Check if already translated in database
                    var translation = _context.Messages.Where(m => m.MessageId == lastMessages[i].MessageId && m.Language == language).FirstOrDefault();
                    if (translation != null)
                    {
                        lastMessages[i].Content = translation.Content;
                    }
                }
            }

            // Check if the user wants to receive notifications
            if (_userPreferences[connectionId].ReceiveNotifications)
            {
                // Send the messages to the user
                await Clients.Client(connectionId).SendAsync("ReceiveChatHistory", lastMessages, _userPreferences[connectionId].Language);
            }
        }

        // Override OnConnectedAsync to track connections
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Connection ID: " + Context.ConnectionId);
            _userPreferences.Add(Context.ConnectionId, new UserPreferences());

            var user = await _userManager.GetUserAsync(Context.User!);

            if (user != null)
            {
                var firstName = user.FirstName;
                var lastName = user.LastName;
                onlineUsers.Add(Context.ConnectionId, firstName + " " + lastName);
            }
            else
            {
                onlineUsers.Add(Context.ConnectionId, "Guest User");
            }

            // Broadcast the updated list of online users
            await Clients.All.SendAsync("UpdateOnlineUsers", onlineUsers.Values.ToList());

            await base.OnConnectedAsync();
        }

        // Override OnDisconnectedAsync to clean up when a connection is lost
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove the disconnected client's preferences
            _userPreferences.Remove(Context.ConnectionId);

            // Also remove from online users list
            if (onlineUsers.ContainsKey(Context.ConnectionId))
            {
                onlineUsers.Remove(Context.ConnectionId);

                // Broadcast the updated list of online users
                Clients.All.SendAsync("UpdateOnlineUsers", onlineUsers.Values.ToList());
            }

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
