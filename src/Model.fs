namespace Capivarinha

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