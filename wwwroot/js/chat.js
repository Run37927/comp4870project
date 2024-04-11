"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();
var currentLanguage = "en-US";

function getRandomColor() {
    const letters = '0123456789ABCDEF';
    let color = '#';
    for (let i = 0; i < 6; i++) {
        color += letters[Math.floor(Math.random() * 16)];
    }
    return color;
}

// Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user, message, messageId, language) {
    console.log("Received message: " + message + " from " + user + " with id " + messageId + " in language " + language)
    var timestamp = new Date().toLocaleTimeString(); // Add a timestamp to each message

    // Create elements for the message, header, and content
    var messageContainer = document.createElement("div");
    messageContainer.classList.add("message-container");
    messageContainer.setAttribute("id", messageId);


    // Calculate initials
    var initials = user.split(' ').map((n) => n[0]).join('').toUpperCase();

    var initialsCircle = document.createElement("div");
    initialsCircle.classList.add("initials-circle");
    initialsCircle.textContent = initials; // Assuming user is "FirstName LastName"
    initialsCircle.style.backgroundColor = getRandomColor();

    var messageHeader = document.createElement("div");
    messageHeader.classList.add("message-header");
    messageHeader.textContent = `${user} · ${timestamp}`;

    var messageContent = document.createElement("div");
    messageContent.classList.add("message-content");
    messageContent.textContent = message;


    // Append the header and content to the message container
    messageContainer.appendChild(initialsCircle);
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

connection.on("ReceiveChatHistory", function (messageList, language) {
    console.log(messageList);
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

connection.on("UpdateOnlineUsers", function (onlineUsers) {
    var usersList = document.getElementById("onlineUsersList");
    if (!usersList) {
        console.error("Online users list element not found.");
        return;
    }
    usersList.innerHTML = ''; // Clear existing list
    onlineUsers.forEach(function (user) {
        var li = document.createElement("li");
        li.textContent = user; // Assuming 'user' is a string with the user's name or identifier
        usersList.appendChild(li);
    });
});

connection.on("ReceiveTypingNotification", function (user) {
    var typingIndicator = document.getElementById("typingIndicator");
    if (!typingIndicator) {
        typingIndicator = document.createElement("div");
        typingIndicator.setAttribute("id", "typingIndicator");
        document.getElementById("messagesList").appendChild(typingIndicator);
    }
    typingIndicator.textContent = user + " is typing...";

    // Hide the typing indicator after a delay
    setTimeout(function () {
        typingIndicator.textContent = "";
    }, 3000); // Adjust the delay as needed
});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    console.log("Send button clicked");
    var user = document.getElementById("userInput").value;
    var message = document.getElementById("messageInput").value;
    if (message.trim() !== '') { // Ensure we don't send empty messages
        connection.invoke("SendMessage", user, message).catch(function (err) {
            return console.error(err.toString());
        });
        console.log("Clearing input field");
        // Clear the message input field after sending the message
        document.getElementById("messageInput").value = '';
    }
    event.preventDefault();
});

document.getElementById("messageInput").addEventListener("input", function () {
    var user = document.getElementById("userInput").value;
    if (this.value.length > 0) { // Check if the input field is not empty
        connection.invoke("SendTypingNotification", user).catch(function (err) {
            return console.error(err.toString());
        });
    }
});

document.getElementById("languageSelect").addEventListener("change", function () {
    currentLanguage = this.value;
    console.log("Selected language: " + this.value);
    updateLanguagePreference(this.value);

    var isSubscribed = document.body.getAttribute('data-is-subscribed') === 'True';
    console.log(isSubscribed)

    // Only redirect if the user is not subscribed
    if (!isSubscribed && (currentLanguage === 'ar-EG' || currentLanguage === 'ko-KR')) {
        console.log("Redirecting to /pricing");
        window.location.href = '/pricing';
    }
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

document.getElementById("summaryButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("SendMessage", user, "/summary").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});
