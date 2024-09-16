import { REST, Routes } from 'discord.js';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

// Convert import.meta.url to a file path and derive __dirname equivalent
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// This function will deploy the commands
export async function deployCommands(token: string, clientId: string, guildId: string) {
  // This will hold the slash commands data
  const commands = [];

  // Path to the commands folder
  const commandsPath = path.join(__dirname, '../commands');
  const commandFiles = fs.readdirSync(commandsPath).filter(file => file.endsWith('.ts') || file.endsWith(".js"));

  // Dynamically import each command file
  for (const file of commandFiles) {
    const command = (await import(`../commands/${file}`)).default;

    if (!command || !command.data) {
      console.error(`Command file ${file} is missing "data" property.`);
      continue; // Skip commands that don't have a "data" property
    }

    commands.push(command.data.toJSON()); // Push the serialized slash command data
  }

  // Set up the REST API for Discord.js
  const rest = new REST({ version: '10' }).setToken(token);

  try {
    console.log('Started refreshing application (/) commands.');

    // Deploy the commands to the guild (server)
    await rest.put(
      Routes.applicationGuildCommands(clientId, guildId),
      { body: commands }, // Pass the commands array here
    );

    console.log('Successfully reloaded application (/) commands.');
  } catch (error) {
    console.error('Failed to deploy commands:', error);
  }
}
