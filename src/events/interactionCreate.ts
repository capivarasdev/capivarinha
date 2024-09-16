import { Interaction, Client } from 'discord.js';
import { Command } from '../types'; // Import the Command type

export default {
  name: 'interactionCreate',
  async execute(interaction: Interaction, client: Client & { commands: Map<string, Command> }) {
    if (!interaction.isCommand()) return;

    const command = client.commands.get(interaction.commandName);

    if (!command) return;

    try {
      await command.execute(interaction);
    } catch (error) {
      console.error(error);
      await interaction.reply({ content: 'There was an error executing this command!', ephemeral: true });
    }
  },
};
