module Economy
 
module Interface =
    open Capivarinha
    open Discord

    let createBalanceCommand (deps: Model.Dependencies) = task {
        let globalCommand = new SlashCommandBuilder()
        globalCommand.WithName "balance" |> ignore
        globalCommand.WithDescription "Shows user balance" |> ignore

        let! _ = deps.Client.CreateGlobalApplicationCommandAsync (globalCommand.Build ())

        ()
    }

    let createCommands (deps: Model.Dependencies) = task {
        let! _ = createBalanceCommand deps
        
        ()
    }
