"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();
var currentLanguage = "en-US";

function getUsersColor(user) {
    // use the user's name to generate a unique color
    var hash = 0;
    for (var i = 0; i < user.length; i++) {
        hash = user.charCodeAt(i) + ((hash << 5) - hash);
    }
    var c = (hash & 0x00FFFFFF).toString(16).toUpperCase();
    return "#" + "00000".substring(0, 6 - c.length) + c;
}

// Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

function addMessageToChat(user, message, messageId, language, sentDate, isHistory = false) {
    console.log("Received message: " + message + " from " + user + " with id " + messageId + " in language " + language)
    var timestamp = new Date().toLocaleTimeString(); // Add a timestamp to each message
    if (sentDate) {
        // Adjust the date from UTC to local time
        const messageDate = new Date(sentDate);
        messageDate.setHours(messageDate.getHours() + 17);
        timestamp = messageDate.toLocaleTimeString();
    }

    // Create elements for the message, header, and content
    var messageContainer = document.createElement("div");
    messageContainer.classList.add("message-container");
    messageContainer.setAttribute("id", messageId);


    // Calculate initials
    var initials = user.split(' ').map((n) => n[0]).join('').toUpperCase();

    var initialsCircle = document.createElement("div");
    initialsCircle.classList.add("initials-circle");
    initialsCircle.textContent = initials; // Assuming user is "FirstName LastName"
    initialsCircle.style.backgroundColor = getUsersColor(user);

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

    if (language !== currentLanguage && !isHistory) {
        var translatedContent = document.createElement("div");
        translatedContent.classList.add("translated-content");
        translatedContent.textContent = "Translation: Loading...";
        messageContainer.appendChild(translatedContent);
    }

    // Append the message container to the message list
    document.getElementById("messagesList").appendChild(messageContainer);

    // Scroll to the bottom of the div
    var messagesDiv = document.getElementById("messagesList");
    messagesDiv.scrollTop = messagesDiv.scrollHeight - messagesDiv.clientHeight;
}

connection.on("ReceiveMessage", function (user, message, messageId, language) {
    addMessageToChat(user, message, messageId, language);
});

connection.on("ReceiveTranslation", function (messageId, translation, language) {
    var messageContainer = document.getElementById(messageId);
    var translatedContent = messageContainer.getElementsByClassName("translated-content")[0];
    translatedContent.textContent = `Translation: ${translation}`;
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
        var username = user.split('@')[0]; // Split the email and take the first part

        // Create list item
        var li = document.createElement("li");

        // Create initials circle
        var initialsCircle = document.createElement("div");
        initialsCircle.classList.add("initials-circle-online");
        var initials = username.split(' ').map((n) => n[0]).join('').toUpperCase();
        initialsCircle.textContent = initials;
        initialsCircle.style.backgroundColor = getUsersColor(username); // Assuming you have a function to assign colors

        // Append initials circle to list item
        li.appendChild(initialsCircle);

        // Create a text node for the username and append it to the list item
        var textNode = document.createTextNode(" " + username); // Add space for separation
        li.appendChild(textNode);

        // Append the list item to the users list
        usersList.appendChild(li);
    });
});
let typingTimeout = null;
connection.on("ReceiveTypingNotification", function (user) {
    // Cancel the previous timeout if it exists
    if (typingTimeout) {
        clearTimeout(typingTimeout);
    }
    var typingIndicator = document.getElementById("typingIndicator");
    typingIndicator.textContent = user + " is typing...";
    typingIndicator.style.color = "#888"

    // Hide the typing indicator after a delay
    typingTimeout = setTimeout(function () {
        typingIndicator.style.color = "transparent";
        typingIndicator.textContent = "Hidden Text";
    }, 3000); // Adjust the delay as needed
});

// Recieve the chat history from the server
connection.on("ReceiveChatHistory", function (messageList) {
    console.log("Received chat history: " + messageList);
    console.log(messageList)
    // MessageList is an array
    for (var i = messageList.length; i > 0; i--) {
        addMessageToChat(messageList[i - 1].senderName, messageList[i - 1].content, messageList[i - 1].id, messageList[i - 1].language, messageList[i - 1].sentDate, true);
    }
});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
    connection.invoke("GetChatHistory").catch(function (err) {
        return console.error(err.toString());
    });
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

document.getElementById("messageInput").addEventListener("keypress", function (event) {
    if (event.key === "Enter") {
        event.preventDefault();

        var user = document.getElementById("userInput").value;
        var message = document.getElementById("messageInput").value;
        if (message.trim() !== '') {
            connection.invoke("SendMessage", user, message).catch(function (err) {
                return console.error(err.toString());
            });
            // Clear the message input field after sending the message
            document.getElementById("messageInput").value = '';
        }
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

// document.getElementById("historyButton").addEventListener("click", function () {
//     connection.invoke("ChatHistory").catch(function (err) {
//         return console.error(err.toString());
//     });
// });

document.getElementById("summaryButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value;
    connection.invoke("SendMessage", user, "/summary").catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});
