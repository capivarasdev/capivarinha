namespace Capivarinha

open Discord
open Discord.WebSocket
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Command

module Handler =
    type CommandHandler = Command -> Task<unit>

    let tryCommandUser (client: DiscordSocketClient) (commandUser: IUser) (value: 'a) =
        let isCurrentBot = client.CurrentUser.Id = commandUser.Id

        match (isCurrentBot, commandUser.IsBot) with
        | true, _ -> Error (CommandError.InvokedByABot "command invoked by current bot")
        | _, true -> Error (CommandError.InvokedByABot "command invoked by a bot")
        | _ -> Ok value

    let tryCommandHandler (handler: CommandHandler) value =
        match value with
        | Ok cmd -> handler cmd
        | Error (ExternalError e) -> Task.FromResult ()
        | Error (InvokedByABot e) -> Task.FromResult (printfn "%s" e)
        | Error NotSupported -> Task.FromResult ()

    let handleCommand (deps: Model.Dependencies) (queueCommand: Task<unit> -> unit) cmd = task {
        match cmd with
        | RollDie cmd -> queueCommand (task { return () })
        | BeLessIronic cmd -> ()
        | Balance cmd -> do! (Economy.Interface.Commands.getBalanceCommand deps cmd) |> AsyncResult.ignoreError
    }
