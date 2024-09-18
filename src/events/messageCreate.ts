import { Client, GuildMember, Message, PermissionsBitField, TextChannel } from 'discord.js';

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
  execute(message: Message, client: Client) {
    if (message.author.bot) return;

    handleAstrooMessage(message);
    // handleQuestionSpam(message, client);
  },
};

/**
 * Function to check if the message contains the target word "ola" 
 * and if it's from the specific target user, then reply.
 * 
 * @param {Message} message - The message that was sent.
 */
function handleAstrooMessage(message: Message) {
    const targetUserId = '357236983023796226'; // astroo
    const targetWord = 'ola';

    // Check if the message is from the specific user and contains the target word
    if (message.author.id === targetUserId &&
        message.content.toLowerCase().includes(targetWord)) {
        message.reply(`Olá <@${message.author.id}>!`);
    }
}

/**
 * Function to check if the last two messages from the same user in a specific channel
 * both contain a "?", and if so, mute the user for 30 minutes and delete the messages.
 * 
 * @param {Message} message - The message that was sent.
 * @param {Client} client - The Discord client instance.
 */
async function handleQuestionSpam(message: Message, client: Client) {
    const askLuiz = '1283895078867173476';
    const charactersToCheck = ['?', '¿', '‽', '؟', '⸮', '﹖', '？', 'ʔ', '?', '？'];
    const messageRateLimit = 3;

    // Only proceed if the message is in the specified channel
    if (message.channel.id !== askLuiz) return;
  
    const channel = message.channel as TextChannel;
    const fetchedMessages = await channel.messages.fetch({ limit: 5 });
    const userMessages = fetchedMessages.filter(msg => msg.author.id === message.author.id).first(messageRateLimit);
  
    // Check if messages contain a "?"
    const allMessagesContainCharacter = userMessages.every(msg => 
        charactersToCheck.some(character => msg.content.includes(character))
      );

    if (userMessages.length >= messageRateLimit && allMessagesContainCharacter) {
        const member = message.guild?.members.cache.get(message.author.id) as GuildMember;

        // Check if the bot has the correct permissions to mute and delete messages
        if (!member || !message.guild?.members.me?.permissions.has(PermissionsBitField.Flags.ModerateMembers)) {
            return console.log('I do not have permission to mute members or delete messages.');
        }

        try {
            // Mute the user for 30 minutes (30 * 60 * 1000 milliseconds)
            await member.timeout(30 * 60 * 1000, 'Sent two messages with "?" in a row');
            
            // Delete the two messages
        for (const msg of userMessages) {
            await msg.delete();
        }

        await channel.send(`<@${message.author.id}> EU PEÇO CALMA`);
        } catch (error) {
            console.error('Error muting the user or deleting messages:', error);
            await channel.send('There was an error muting the member or deleting the messages.');
        }
    }
}