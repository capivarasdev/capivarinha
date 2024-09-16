import { Client, GatewayIntentBits, Collection } from 'discord.js';
import { config } from 'dotenv';
import { Command } from './types.js';
import { deployCommands } from './utils/deploy-commands.js';
import { registerCommands } from './utils/register-commands.js';
import { registerEvents } from './utils/register-events.js';

config(); // Load .env variables

// Extend Client to include commands
const client = new Client({
  intents: [
    GatewayIntentBits.Guilds, 
    GatewayIntentBits.GuildMessages,
    GatewayIntentBits.MessageContent],
}) as Client & { commands: Collection<string, Command> };

client.commands = new Collection();

(async () => {
  await deployCommands(process.env.DISCORD_TOKEN!, process.env.CLIENT_ID!, process.env.GUILD_ID!);
  await registerCommands(client);
  await registerEvents(client);

  client.login(process.env.DISCORD_TOKEN);
})();
