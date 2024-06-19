namespace Capivarinha

open System.Security.Cryptography
open Discord
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
        | Balance of Balance

    and RollDieCommand =
        { ReactionUser: IUser
          Message: IMessage }
    and BeLessIronicCommand = { Message: IMessage }
    and Balance = { SlashCommand: SocketSlashCommand }
