"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();
var currentLanguage = "en-US";

// Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user, message, messageId, language) {
    console.log("Received message: " + message + " from " + user + " with id " + messageId + " in language " + language)
    var timestamp = new Date().toLocaleTimeString(); // Add a timestamp to each message

    // Create elements for the message, header, and content
    var messageContainer = document.createElement("div");
    messageContainer.classList.add("message-container");
    messageContainer.setAttribute("id", messageId);

    var messageHeader = document.createElement("div");
    messageHeader.classList.add("message-header");
    messageHeader.textContent = `${user} · ${timestamp}`;

    var messageContent = document.createElement("div");
    messageContent.classList.add("message-content");
    messageContent.textContent = message;


    // Append the header and content to the message container
    messageContainer.appendChild(messageHeader);
    messageContainer.appendChild(messageContent);

    if (language !== currentLanguage) {
        var translatedContent = document.createElement("div");
        translatedContent.classList.add("translated-content");
        translatedContent.textContent = "Translation: Loading...";
        messageContainer.appendChild(translatedContent);
    }

    // Append the message container to the message list
    document.getElementById("messagesList").appendChild(messageContainer);
});

connection.on("ReceiveTranslation", function (messageId, translation, language) {
    var messageContainer = document.getElementById(messageId);
    var translatedContent = messageContainer.getElementsByClassName("translated-content")[0];
    translatedContent.textContent = `Translation: ${translation}`;
});

connection.on("ReceiveChatHistory", function (messages, language) {
    console.log("i see messages", messages);
    var messagesList = document.getElementById("messagesList");
    messages.forEach(function (message) {
        // Create elements for the message, header, and content
        var messageContainer = document.createElement("div");
        messageContainer.classList.add("message-container");

        var messageHeader = document.createElement("div");
        messageHeader.classList.add("message-header");
        var timestamp = new Date(message.sentDate).toLocaleTimeString(); // Assuming 'sentDate' is the correct property
        messageHeader.textContent = `${message.senderName} · ${timestamp}`;

        var messageContent = document.createElement("div");
        messageContent.classList.add("message-content");
        messageContent.textContent = message.content; // Assuming 'content' is the correct property

        // Append the header and content to the message container
        messageContainer.appendChild(messageHeader);
        messageContainer.appendChild(messageContent);

        // Check if translation is needed (based on the current language and message language)
        if (language !== currentLanguage && message.language !== currentLanguage) {
            var translatedContent = document.createElement("div");
            translatedContent.classList.add("translated-content");
            translatedContent.textContent = "Translation: Loading..."; // Placeholder text
            messageContainer.appendChild(translatedContent);
            // You might need to handle actual translation loading here
        }

        // Append the message container to the message list
        messagesList.appendChild(messageContainer);
    });

    // Scroll to the bottom of the chat window to show the most recent messages
    messagesList.scrollTop = messagesList.scrollHeight;
});

connection.on("ReceiveSummary", function (summary) {
    var messageContainer = document.createElement("div");
    messageContainer.classList.add("message-container", "summary-message");

    var messageContent = document.createElement("div");
    messageContent.classList.add("message-content");
    messageContent.innerHTML = summary; // summary messages are safe from user input

    messageContainer.appendChild(messageContent);
    document.getElementById("messagesList").appendChild(messageContainer);

    // Scroll to the latest message
    var messagesList = document.getElementById("messagesList");
    messagesList.scrollTop = messagesList.scrollHeight;
});



connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;
    if (message.trim() !== '') { // Ensure we don't send empty messages
        connection.invoke("SendMessage", user, message).catch(function (err) {
            return console.error(err.toString());
        });
        // Clear the message input field after sending the message
        document.getElementById("messageInput").value = '';
    }
    event.preventDefault(); // Prevent the form from submitting in a traditional way
});
document.getElementById("languageSelect").addEventListener("change", function () {
    currentLanguage = this.value;
    updateLanguagePreference(this.value);
});

// Example: Sending language preference to the server
function updateLanguagePreference(language) {
    console.log("updated language preference: " + language)
    connection.invoke("UpdateUserPreferences", language).catch(function (err) {
        return console.error(err.toString());
    });
}

document.getElementById("historyButton").addEventListener("click", function () {
    connection.invoke("ChatHistory").catch(function (err) {
        return console.error(err.toString());
    });
});