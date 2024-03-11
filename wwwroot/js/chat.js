"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user, message) {
    var timestamp = new Date().toLocaleTimeString(); // Add a timestamp to each message

    // Create elements for the message, header, and content
    var messageContainer = document.createElement("div");
    messageContainer.classList.add("message-container");

    var messageHeader = document.createElement("div");
    messageHeader.classList.add("message-header");
    messageHeader.textContent = `${user} · ${timestamp}`;

    var messageContent = document.createElement("div");
    messageContent.classList.add("message-content");
    messageContent.textContent = message;

    // Append the header and content to the message container
    messageContainer.appendChild(messageHeader);
    messageContainer.appendChild(messageContent);

    // Append the message container to the message list
    document.getElementById("messagesList").appendChild(messageContainer);
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
    connection.invoke("SendMessage", user, message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});
