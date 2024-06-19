namespace Capivarinha

open System.Security.Cryptography
open System.Threading.Tasks
open Discord
open Discord.Net
open Discord.WebSocket

[<RequireQualifiedAccess>]
module String =
    open System.Text
    open System.Globalization

    let normalize (str: string) =
        let str = str.ToLowerInvariant()
        let stringBuilder = StringBuilder()
        let normalizedString = str.Normalize(NormalizationForm.FormD)

        for i in 0 .. normalizedString.Length - 1 do
            let character = normalizedString[i]

            if CharUnicodeInfo.GetUnicodeCategory(character) <> UnicodeCategory.NonSpacingMark then
                stringBuilder.Append(character) |> ignore

        stringBuilder.ToString().ToLower()

[<RequireQualifiedAccess>]
module Random =
    let range fromInclusive toInclusive =
        RandomNumberGenerator.GetInt32(fromInclusive, toInclusive + 1)

module Model =
    open System

    type UserRollTimeout =
        | CanRoll
        | CannotRoll of TimeSpan

    type Dependencies =
        { Logger: unit
          ConnectionString: string
          Client: DiscordSocketClient
          Settings: Settings }

    type UserId = string

    type UserLastRoll =
        { UserId: UserId
          RolledAt: DateTimeOffset }

module Command =
    type CommandError =
        | InvokedByABot of error: string
        | ExternalError of error: string
        | NotSupported

    type Command =
        | RollDie of RollDieCommand
        | BeLessIronic of BeLessIronicCommand

    and RollDieCommand =
        { ReactionUser: IUser
          Message: IMessage }
    and BeLessIronicCommand = { Message: IMessage }

    type CommandHandler = Command -> unit

    let tryCommandUser (client: DiscordSocketClient) (commandUser: IUser) (value: 'a) =
        let isCurrentBot = client.CurrentUser.Id = commandUser.Id

        match (isCurrentBot, commandUser.IsBot) with
        | true, _ -> Error (CommandError.InvokedByABot "command invoked by current bot")
        | _, true -> Error (CommandError.InvokedByABot "command invoked by a bot")
        | _ -> Ok value

    let tryCommandHandler (handler: CommandHandler) value =
        match value with
        | Ok cmd -> handler cmd
        | Error (ExternalError e) -> ()
        | Error (InvokedByABot e) -> printfn "%s" e
        | Error NotSupported -> ()

    let handleCommand (queueCommand: Task<unit> -> unit) cmd =
        match cmd with
        | RollDie cmd -> queueCommand (task { return () })
        | BeLessIronic cmd -> ()
