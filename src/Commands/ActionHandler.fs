﻿module ActionHandler

open Discord
open Discord.WebSocket
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Capivarinha
open Commands

type Handler = CommandType -> Task<unit>

let tryUser (currentUserId: uint64) (actionUserId: uint64) (actionUserIsBot: bool) (value: 'a) =
    let isCurrentBot = actionUserId = actionUserId

    match (isCurrentBot, actionUserIsBot) with
    | true, _ -> Error (CommandError.InvokedByABot "command invoked by current bot")
    | _, true -> Error (CommandError.InvokedByABot "command invoked by a bot")
    | _ -> Ok value

let validate (handle: Handler) value =
    match value with
    | Ok action -> handle action
    | Error (ExternalError e) -> Task.FromResult (printfn "%s" e)
    | Error (InvokedByABot e) -> Task.FromResult (printfn "%s" e)
    | Error NotSupported -> Task.FromResult ()

let handle (deps: Setup.Dependencies) (queueAction: Task<unit> -> unit) cmd = task {
    match cmd with
    // On ReactionAdded
    | RollDie cmd -> queueAction (task { return () })
    // On MessageUpdate
    | BeLessIronic cmd -> ()
    // On SlashCommands: Economy
    | Balance cmd -> do! (Economy.Interface.Commands.getBalance deps cmd) |> AsyncResult.ignoreError
    | Transac cmd -> do! (Economy.Interface.Commands.makeTransactionCommand deps cmd) |> AsyncResult.ignoreError
}
