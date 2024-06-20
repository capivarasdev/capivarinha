module Commands

open Discord
open Discord.WebSocket

type CommandError =
    | InvokedByABot of error: string
    | ExternalError of error: string
    | NotSupported

type CommandType =
    | RollDie of RollDieCommand
    | BeLessIronic of BeLessIronicCommand
    | Balance of BalanceCommand

and RollDieCommand = { ReactionUser: IUser; Message: IMessage }
and BeLessIronicCommand = { Message: IMessage }
and BalanceCommand = SocketSlashCommand
