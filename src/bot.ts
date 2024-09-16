import { Client, GatewayIntentBits, Collection } from 'discord.js';
import { config } from 'dotenv';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url'; // Import utility to convert URLs to file paths
import { Command } from './types';  // Import the Command type

config(); // Load .env variables

// Convert import.meta.url to file path and get the current directory
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Extend Client to include commands
const client = new Client({
  intents: [GatewayIntentBits.Guilds],
}) as Client & { commands: Collection<string, Command> };

client.commands = new Collection();

const commandsPath = path.join(__dirname, 'commands');
const commandFiles = fs.readdirSync(commandsPath).filter(file => file.endsWith('.ts'));

// Dynamically import each command file
(async () => {
  for (const file of commandFiles) {
    const command = (await import(`./commands/${file}`)).default as Command;
    client.commands.set(command.data.name, command);
  }

  const eventsPath = path.join(__dirname, 'events');
  const eventFiles = fs.readdirSync(eventsPath).filter(file => file.endsWith('.ts'));

  for (const file of eventFiles) {
    const event = (await import(`./events/${file}`)).default;
    if (event.once) {
      client.once(event.name, (...args) => event.execute(...args, client));
    } else {
      client.on(event.name, (...args) => event.execute(...args, client));
    }
  }

  client.login(process.env.DISCORD_TOKEN);
})();
