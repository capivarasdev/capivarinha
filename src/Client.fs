namespace Capivarinha

open System
open System.Threading.Tasks
open Model

open Discord
open Discord.WebSocket

module Action =
    let dieReaction (deps: Dependencies) (guild: SocketGuild) (textChannel: SocketTextChannel) (message: IUserMessage) (channel: IChannel) (reaction: SocketReaction) = task {
        let userReactionId = reaction.UserId.ToString()
        
        let reactionUser = guild.GetUser(reaction.UserId)
        let messageUser =  guild.GetUser(message.Author.Id)

        let rollDie () = task {
            let die = Random.range 1 6
            if die = 6 then
                let! _ = message.ReplyAsync(sprintf "%s rolled the die on you and got a six, you are now muted!" reactionUser.Mention)
                do! messageUser.SetTimeOutAsync(TimeSpan.FromMinutes 5)
            else
                let! _ = textChannel.SendMessageAsync(sprintf "Oh no, %s! You rolled a %d" reactionUser.Mention die)
                let! _ = message.RemoveReactionAsync(reaction.Emote, reactionUser)
                ()
        }

        let! lastRoll = Database.Die.userLastRoll deps.ConnectionString userReactionId
        match lastRoll with
        | Ok (Some { RolledAt = rolledAt }) ->
            let diff = DateTimeOffset.Now - rolledAt
            let threshold = TimeSpan.FromMinutes 10
            if diff > threshold then
                do! rollDie ()
                let! _ = Database.Die.registerUserRoll deps.ConnectionString userReactionId
                ()
            else
                let diffStr = (threshold - diff).ToString(@"mm\:ss")
                let! _ = textChannel.SendMessageAsync(sprintf "%s needs to wait %s minutes before rolling a die again!" reactionUser.Mention diffStr)
                let! _ = message.RemoveReactionAsync(reaction.Emote, reactionUser)
                ()
        | Ok None ->
            let! _ = Database.Die.registerUserRoll deps.ConnectionString userReactionId
            do! rollDie ()
        | Error e ->  printfn "%A" e
    }

    let beMoreIronic (deps: Dependencies) (message: SocketMessage) = task {
        let normalized = message.Content.ToLower()

        if normalized.Contains("não-ironicamente") || normalized.Contains("nao-ironicamente") then
            do! message.DeleteAsync()
    }

module Client =
    let onReady = Func<Task>(fun _ -> task { printfn "[%s] bot is running" (DateTimeOffset.Now.ToString())  })

    let onReactionAdded (deps: Dependencies) (message: IUserMessage) channel (reaction: SocketReaction) = task {
        // ignore self messages
        let! user = deps.Client.GetUserAsync(message.Author.Id)
        if (not user.IsBot) then
            let botGuild = deps.Client.GetGuild(deps.Settings.GuildId)
            let botChannel = botGuild.GetTextChannel(deps.Settings.BotChannelId)

            if reaction.Emote = new Emoji("🎲") then
                do! Action.dieReaction deps botGuild botChannel message channel reaction
    }

    let onMessageReceived (deps: Dependencies) (message: SocketMessage) = task {
        let! user = deps.Client.GetUserAsync(message.Author.Id)
        if (not user.IsBot) then
            if (message.Author.Id = 866170272762953738UL) then
                do! Action.beMoreIronic deps message
    }