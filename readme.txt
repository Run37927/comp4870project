TODO need to add sqlite Database

Messages will be stored as the following format
Columns:
  ID: GUID - Primary Key
  MessageId: int - all versions of same message in different languages share this
  Language: string - Language of the message
  SenderName: string - Name of Sender
  OrginalMessage: boolean - if this is an untranslated message
  Content: string - content of the message
  conversationId: Id of the conversation
  sentDate: Date - time that the message was sent

TODO Choose and Implement a translation service, Eg. OpenAi, Azure translation.

TODO Allow users to get past messages.