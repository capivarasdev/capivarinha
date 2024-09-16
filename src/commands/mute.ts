import { SlashCommandBuilder } from '@discordjs/builders';
import { ChatInputCommandInteraction, CommandInteraction, GuildMember, PermissionsBitField } from 'discord.js';
import { Command } from '../types.js';  // Adjust the path to your types file

const command: Command = {
  data: new SlashCommandBuilder()
    .setName('mute')
    .setDescription('Mute a member using the timeout feature')
    .addUserOption(option => 
      option.setName('target')
        .setDescription('The member to mute')
        .setRequired(true))
    .addIntegerOption(option =>
      option.setName('duration')
        .setDescription('Duration of the mute in minutes')
        .setRequired(true))
    .addStringOption(option =>
      option.setName('reason')
        .setDescription('Reason for muting the member')
        .setRequired(false)) as SlashCommandBuilder,

  async execute(interaction: ChatInputCommandInteraction) {
    const target = interaction.options.getUser('target');
    const duration = interaction.options.getInteger('duration');
    const reason = interaction.options.getString('reason') || 'No reason provided';

    // Fetch the target member from the guild
    const member = interaction.guild?.members.cache.get(target?.id as string) as GuildMember;

    // Check if the command invoker has the correct permissions
    if (!interaction.memberPermissions?.has(PermissionsBitField.Flags.ModerateMembers)) {
      return interaction.reply({ content: 'You do not have permission to mute members.', ephemeral: true });
    }

    // Check if the bot has the correct permissions
    if (!interaction.guild?.members.me?.permissions.has(PermissionsBitField.Flags.ModerateMembers)) {
      return interaction.reply({ content: 'I do not have permission to mute members.', ephemeral: true });
    }

    // Check if the member exists and is valid
    if (!member) {
      return interaction.reply({ content: 'That user is not in the server!', ephemeral: true });
    }

    try {
      // Set the timeout duration (in milliseconds)
      const timeoutDuration = duration! * 60 * 1000;

      // Apply the timeout
      await member.timeout(timeoutDuration, reason);

      await interaction.reply({ content: `${member.user.tag} has been muted for ${duration} minutes. Reason: ${reason}` });
    } catch (error) {
      console.error(error);
      await interaction.reply({ content: 'There was an error muting the member.', ephemeral: true });
    }
  },
};

export default command;
