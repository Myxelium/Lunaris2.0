## Ollama - Large Language Model Chat - Handler

This handler "owns" the logic for accessing the ollama api, which runs the transformer model.

> How to get started with a local chat bot see: [Run LLMs Locally using Ollama](https://marccodess.medium.com/run-llms-locally-using-ollama-8f04dd9b14f9)

Assuming you are on the same network as the Ollama server you should configure it to be accessible to other machines on the network, however this is only required if you aren't running it from localhost relative to the bot.
See: [How do I configure Ollama server?](https://github.com/ollama/ollama/blob/main/docs/faq.md#how-do-i-configure-ollama-server)
