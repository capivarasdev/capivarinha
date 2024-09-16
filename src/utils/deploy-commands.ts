import { REST, Routes } from 'discord.js';
import { config } from 'dotenv';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

config();

// Convert import.meta.url to a file path and derive __dirname equivalent
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// This will hold the slash commands data
const commands = [];

// Path to the commands folder
const commandsPath = path.join(__dirname, '../commands');
const commandFiles = fs.readdirSync(commandsPath).filter(file => file.endsWith('.ts'));

// Dynamically import each command file
(async () => {
  for (const file of commandFiles) {
    const command = (await import(`../commands/${file}`)).default; // Ensure you access .default

    if (!command || !command.data) {
      console.error(`Command file ${file} is missing "data" property.`);
      continue; // Skip commands that don't have a "data" property
    }

    commands.push(command.data.toJSON()); // Push the serialized slash command data
  }

  const rest = new REST({ version: '10' }).setToken(process.env.DISCORD_TOKEN!);

  try {
    console.log('Started refreshing application (/) commands.');

    // Deploying the commands to the guild (server)
    await rest.put(
      Routes.applicationGuildCommands(process.env.CLIENT_ID!, process.env.GUILD_ID!),
      { body: commands }, // Pass the commands array here
    );

    console.log('Successfully reloaded application (/) commands.');
  } catch (error) {
    console.error(error);
  }
})();
