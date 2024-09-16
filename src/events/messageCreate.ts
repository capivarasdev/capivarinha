import { Client, Message } from 'discord.js';

/**
 * Event handler for the `messageCreate` event.
 */
export default {
  name: 'messageCreate',
  once: false, // This should trigger on every message, not just once
  /**
   * Execute the event when a message is created.
   * @param message - The message that was sent.
   * @param client - The Discord client instance.
   */
  execute(message: Message, _: Client) {
    console.log(`Received message from ${message.author.tag}: ${message.content}`);

    const targetUserId = '357236983023796226'; // astroo
    const targetWord = 'ola';

    // Check if the message is from the specific user and contains the target word
    if (
      message.author.id === targetUserId &&
      message.content.toLowerCase().includes(targetWord) &&
      !message.author.bot
    ) {
      // Reply with "Olá @user" by mentioning the user
      message.reply(`Olá <@${message.author.id}>!`);
    }
  },
};
