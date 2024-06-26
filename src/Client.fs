namespace Capivarinha

open System
open System.Threading.Tasks
open Capivarinha.Setup
open Discord
open Discord.WebSocket
open FsToolkit.ErrorHandling
open Commands

module Action =
    let dieReaction (deps: Dependencies) (guild: SocketGuild) (textChannel: SocketTextChannel) (message: IUserMessage) (reaction: SocketReaction) = taskResult {
        let isTimeThresholdMet (time: DateTimeOffset) (threshold: int) =
            let threshold = TimeSpan.FromDays threshold
            let diff = DateTimeOffset.Now.Subtract(time)

            if (diff > threshold) then
                CanRoll
            else
                CannotRoll (threshold - diff)

        let muteUser (reactionUser: SocketGuildUser) (userToBeMuted: SocketGuildUser) value = task {
            let! _ = message.ReplyAsync(sprintf "%s rolled the die on you and got a %d, %s is now muted!" reactionUser.Mention value userToBeMuted.Mention)
            do! userToBeMuted.SetTimeOutAsync(TimeSpan.FromMinutes 5)
            let! _ = Database.Die.registerUserRoll deps.ConnectionString (reactionUser.Id.ToString())
            ()
        }

        let missedRoll (user: SocketGuildUser) value = task {
            let! _ = textChannel.SendMessageAsync(sprintf "Oh no, %s! You rolled a %d" user.Mention value)
            let! _ = message.RemoveReactionAsync(reaction.Emote, user)
            let! _ = Database.Die.registerUserRoll deps.ConnectionString (user.Id.ToString())
            ()
        }

        let userReactionId = reaction.UserId.ToString()
        let reactionUser = guild.GetUser(reaction.UserId)
        let messageUser =  guild.GetUser(message.Author.Id)

        let die = Random.range 1 6

        let! canRoll =
            let bind = function
                | None -> CanRoll
                | Some { RolledAt = rolledAt } -> (isTimeThresholdMet rolledAt 1)

            Database.Die.userLastRoll deps.ConnectionString userReactionId
            |> AsyncResult.map bind

        match (die, canRoll) with
        | value, CanRoll when value = 6 || value = 1 ->
            let userToBeMuted =
                if value = 6
                then messageUser
                else reactionUser

            do! muteUser reactionUser userToBeMuted value
        | value, CanRoll ->
            do! missedRoll reactionUser value
        | _, CannotRoll timeLeft ->
            let diffStr = timeLeft.ToString(@"%d' days'\ hh' hours'\ mm' mins'")
            let! _ = textChannel.SendMessageAsync(sprintf "%s needs to wait %s before rolling a die again!" reactionUser.Mention diffStr)
            let! _ = message.RemoveReactionAsync(reaction.Emote, reactionUser)
            ()
    }

    let beLessIronic (deps: Dependencies) (message: SocketMessage) = task {
        if (message.Author.Id = 866170272762953738UL) then
            let content = String.normalize message.Content

            let isForbidden =
                deps.Settings.ForbiddenWords
                |> List.map String.normalize
                |> List.exists (fun word -> content.Contains(word))

            if isForbidden then
                do! message.DeleteAsync()
    }

    let saveMessageMetadata (deps: Dependencies) (message: IMessage) = task {
        let! result =
            Database.Message.saveMetadata
                deps.ConnectionString
                (message.Author.Id.ToString())
                message.Content.Length

        match result with
        | Ok _ -> ()
        | Error e ->
            printfn "error writing message to database: %A" e
    }

module Client =
    let onReady (deps: Dependencies) = Func<Task>(fun _ -> task {
        do! Economy.Interface.createCommands deps

        printfn "[%s] bot is running" (DateTimeOffset.Now.ToString())
        printfn "Current forbidden words: %A" deps.Settings.ForbiddenWords  })

    // I don't like that we have to map "balance" here just to
    // build a CommandType value obj, but then map it again on
    // the ActionHandler handle func..
    let trySlashCommand (command: SocketSlashCommand) =
        match command.CommandName with
        | name when name = "balance" -> Ok (CommandType.Balance command )
        | name when name = "transac" -> Ok (CommandType.Transac command )
        | name when name = "" -> Error (CommandError.NotSupported)
        | _ -> Error (CommandError.NotSupported)

    let tryReactionAdded (_reaction: IReaction) (reactionUser:IUser) (message: IMessage) (emote: IEmote) =
        match emote with
        | emote when emote = Emoji("🎲") ->
            Ok (CommandType.RollDie { ReactionUser = reactionUser; Message = message })
        | _ -> Error CommandError.NotSupported

    let tryMessageReceived (message: IMessage) =
        let alchimistaId = 866170272762953738UL
        match message with
        | message when message.Author.Id = alchimistaId ->
            Ok (CommandType.BeLessIronic { Message = message })
        | message when message.Content.Length <> 0 ->
            Error CommandError.NotSupported
        | _ -> Error CommandError.NotSupported

    let onMessageReceived (deps: Dependencies) (message: SocketMessage) = task {
        let! user = deps.Client.GetUserAsync(message.Author.Id)
        if (not user.IsBot) then
                do! Action.beLessIronic deps message
                do! Action.saveMessageMetadata deps message
    }


