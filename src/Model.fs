namespace Capivarinha

open Discord.WebSocket
open System

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
    open System.Security.Cryptography

    let range fromInclusive toInclusive =
        RandomNumberGenerator.GetInt32(fromInclusive, toInclusive + 1)

module Setup =
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

