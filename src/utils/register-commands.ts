import { Client } from 'discord.js';
import fs from 'fs';
import path from 'path';
import { Command } from '../types.js'; // Adjust the import if needed
import { fileURLToPath } from 'url';

// Get the current directory for module path resolution
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Register all commands by dynamically importing them from the commands directory.
 * @param {string} client - The Discord client to register commands on
 */
export const registerCommands = async (client: Client & { commands: Map<string, Command> }) => {
  const commandsPath = path.join(__dirname, '../commands');
  const commandFiles = fs.readdirSync(commandsPath).filter(file => file.endsWith('.js') || file.endsWith('.ts'));

  for (const file of commandFiles) {
    const command = (await import(`../commands/${file}`)).default as Command;
    client.commands.set(command.data.name, command);
  }

  console.log(`${commandFiles.length} commands loaded.`);
};
