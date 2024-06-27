module Commands

open Discord
open Discord.WebSocket

type CommandError<'a> =
    | InvokedByABot of error: string
    | ExternalError of error: string
    | UnexpectedExn of exn
    | UnsupportedCommand

type CommandType =
    | RollDie of RollDieCommand
    | BeLessIronic of BeLessIronicCommand
    | Balance of BalanceCommand
    | Transac of BalanceCommand

and RollDieCommand = { ReactionUser: IUser; Message: IMessage }
and BeLessIronicCommand = { Message: IMessage }
and BalanceCommand = SocketSlashCommand
