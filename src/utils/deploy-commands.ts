import { REST, Routes } from 'discord.js';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// Resolve the current directory path for module path resolution
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Deploys all commands to Discord by reading the commands directory,
 * converting each command to JSON, and then registering them with the Discord API.
 *
 * @param {string} token - The bot's token used to authenticate with Discord API.
 * @param {string} clientId - The client ID of the bot.
 * @param {string} guildId - The guild (server) ID where the commands should be registered.
 */
export const deployCommands = async (token: string, clientId: string, guildId: string) => {
  try {
    // Initialize an array to hold the slash commands data
    const commands = [];

    // Path to the commands directory
    const commandsPath = path.join(__dirname, '../commands');
    // Get all command files (both .ts and .js for dev and production environments)
    const commandFiles = fs.readdirSync(commandsPath).filter(file => file.endsWith('.js') || file.endsWith('.ts'));

    // Dynamically import each command and convert to JSON
    for (const file of commandFiles) {
      const command = (await import(`../commands/${file}`)).default;

      // Check if the command has the required "data" property
      if (!command || !command.data) {
        console.warn(`Command file ${file} is missing "data" property. Skipping...`);
        continue;
      }

      // Push the serialized command data to the array
      commands.push(command.data.toJSON());
    }

    // Log the number of commands being deployed
    console.log(`Deploying ${commands.length} command(s) to Discord...`);

    // Set up the REST API client for interacting with Discord
    const rest = new REST({ version: '10' }).setToken(token);

    // Register commands with Discord via the REST API
    await rest.put(Routes.applicationGuildCommands(clientId, guildId), { body: commands });

    // Log success message
    console.log('Successfully reloaded application (/) commands.');
  } catch (error) {
    console.error('Error deploying commands:', error);
  }
};