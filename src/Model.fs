namespace Capivarinha

open System.Security.Cryptography

[<RequireQualifiedAccess>]
module Random =
    let range fromInclusive toInclusive =
        RandomNumberGenerator.GetInt32(fromInclusive, toInclusive + 1)

module Model =
    open System
    open Discord.WebSocket

    type Dependencies = {
        Logger: unit
        ConnectionString: string
        Client: DiscordSocketClient
        Settings: Settings
    }

    type UserId = string


    type UserLastRoll = { UserId: UserId; RolledAt: DateTimeOffset }