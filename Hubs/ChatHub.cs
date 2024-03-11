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
        private static List<string> _chatHistory = new List<string>();

        public ChatHub(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendMessage(string user, string message)
        {
            // Add the message to the chat history
            _chatHistory.Add($"{user}: {message}");

            // If the message is a summary request, handle it separately
            if (message.ToLower().Trim() == "/summary")
            {
                var summary = await GenerateSummaryAsync(_chatHistory);
                // Send the summary only to the user who requested it
                await Clients.Caller.SendAsync("ReceiveSummary", summary);
            }
            else
            {
                // For all other messages, broadcast to all clients
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }
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


    }
}
