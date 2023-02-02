A bot that utilises QnA Maker, LUIS and Orchestrator.

This was completed as part of my dissertation project that aimed to integrate a chat bot into a financial services platform.

QnA Maker is used to simply return answers to a user based on a question answer knowledge base. The chat bot will read the knowledge base and generate many variations of asking the same question meaning a user doesn't have to ask it the exact question stored.

My implementation using QnA Maker also allows the chat bot to learn new information dynamically. In this implementation, a user can tell the chat bot to add information to its knowledge base.

For example, a user could tell the chat bot:

'Can you please add 1+1=2 to our knowledge base?'

The chat bot will then add it to the knowledge base and perform a learning process.

Once the learning process is complete, another user could successfully ask it something like:

'What is one plus one?'

The chat bot would successfully return 2.

See this working in action here:

https://user-images.githubusercontent.com/25060863/216345881-6a55ac25-9908-455e-98a7-e000b77d1c92.mp4

LUIS is used for context, what this means is that the chat bot can infer an action that the user would like to do.

For example, a user could ask the chat bot:

'Can you please generate a fund code called TEST?'

LUIS would be able to match the users query to a programmatic method and pass 'TEST' into the method as a parameter.

Orchestrator is used to calculate and select the highest probability of the users intent i.e. is the user asking for something from QnAMaker (asking a question) or is the user asking for something from LUIS (asking the chat bot to do something).
