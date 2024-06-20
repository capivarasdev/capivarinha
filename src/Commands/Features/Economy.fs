module Economy

// This module will be used later, when we embellish the messages
module Domain =
    type Currency = Currency of int

    type Bronze = Bronze of Currency
    module Bronze =
        let toSilver = 100u;

    type Silver = Silver of Currency
    module Silver =
        let toGold = 10u;

    type Gold = Gold of Currency

module Entity =
    type Transaction = {
        FromDiscordUserId: string
        ToDiscordUserId: string
        Amount: int
    }

    type Wallet = {
        UserId: int
        DiscordId: string
        Amount: int
    }

module Repository =
    open Fumble
    open Capivarinha.Database
    open FsToolkit.ErrorHandling

    let getUserWallet conn discordId = asyncResult {
        return!
            conn
            |> Sql.connect
            |> Sql.query
                @"SELECT u.id AS user_id, u.discord_id AS discord_id,
                    COALESCE(SUM(CASE WHEN t.to_user_id = u.id THEN t.amount ELSE 0 END) -
                             SUM(CASE WHEN t.from_user_id = u.id THEN t.amount ELSE 0 END), 0) AS amount
                FROM [user] u
                LEFT JOIN [transaction] t ON u.id = t.from_user_id OR u.id = t.to_user_id
                WHERE u.discord_id = @discordId
                GROUP BY u.id, u.discord_id"
            |> Sql.parameters [ "@discordId", Sql.string discordId ]
            |> Sql.executeRowAsync(fun read ->
                let r: Entity.Wallet = {
                    UserId = read.int "user_id";
                    DiscordId = read.string "discord_id";
                    Amount = read.int "amount";
                }
                
                r
            )
    }

    let createTransaction conn (transaction: Entity.Transaction) = asyncResult {
        return!
            conn
            |> Sql.connect
            |> Sql.query
                @"INSERT INTO [transaction] (from_user_id, to_user_id, amount)
                SELECT fromUser.id, toUser.id, @amount
                FROM [user] fromUser, [user] toUser
                WHERE fromUser.discord_id = @fromDiscUserId
                AND toUser.discord_id = @toDiscUserId"
            |> Sql.parameters
                [ "@fromDiscUserId", Sql.string transaction.FromDiscordUserId
                  "@toDiscUserId", Sql.string transaction.ToDiscordUserId
                  "@amount", Sql.int transaction.Amount ]
            |> Sql.executeNonQuery
    }
    
module Interface =
    open Capivarinha
    open Discord
    open Discord.WebSocket
    open FsToolkit.ErrorHandling
    open Commands.Domain
    
    module Commands =
        let getBalanceCommand (deps: Model.Dependencies) (command: BalanceCommand) = asyncResult {
            let! wallet =
                Repository.getUserWallet deps.ConnectionString (string command.User.Id)

            match wallet with
            | Some w -> do! command.RespondAsync (sprintf "You have %i coins." w.Amount)
            | None -> do! command.RespondAsync ("Could not fetch your wallet.")
        }

        let makeTransactionCommand (deps: Model.Dependencies) (command: SocketSlashCommand) = asyncResult {
            let amount = (command.Data.Options |> Seq.find (fun i -> i.Name = "amount")).Value |> string |> int
            let transferToUser = (command.Data.Options |> Seq.find (fun i -> i.Name = "user")).Value :?> SocketGuildUser

            if command.User.Id = transferToUser.Id then
                do! command.RespondAsync "You cannot transfer to yourself."
            else if amount < 1 then
                do! command.RespondAsync "Amount has to be greater than 0."

            let! wallet =
                Repository.getUserWallet deps.ConnectionString (string command.User.Id)

            match wallet with
            | Some w ->
                if w.Amount >= int amount then
                    let transac: Entity.Transaction = {
                        FromDiscordUserId = string command.User.Id
                        Amount = amount
                        ToDiscordUserId = string transferToUser.Id
                    }
        
                    let! succ = Repository.createTransaction deps.ConnectionString transac

                    match succ with
                    | 1 -> do! command.RespondAsync "Transaction was successful!"
                    | _ -> do! command.RespondAsync "Transaction failed."
                else
                    do! command.RespondAsync "You don't have enough funds."
            | None -> do! command.RespondAsync "Could not fetch your wallet."
        }

    type CommandNames =
        | Balance
        | Transac
        | Unknown of string

    module CommandNames =
        let fromString str =
            match str with
            | "balance" -> Balance
            | "transac" -> Transac
            | _ -> Unknown str

        let toString command =
            match command with
            | Balance -> "balance"
            | Transac -> "transac"
            | Unknown str -> str


    let private createBalanceCommand (deps: Model.Dependencies) = task {
        let globalCommand =
            SlashCommandBuilder()
                .WithName(CommandNames.toString CommandNames.Balance)
                .WithDescription("Shows user balance")
                .Build()

        let! _ = deps.Client.CreateGlobalApplicationCommandAsync (globalCommand)

        ()
    }

    let private createTransacCommand (deps: Model.Dependencies) = task {
        let globalCommand =
            SlashCommandBuilder()
                .WithName(CommandNames.toString CommandNames.Transac)
                .WithDescription("Makes a transaction")
                .AddOptions(
                    [ SlashCommandOptionBuilder()
                        .WithName("amount")
                        .WithType(ApplicationCommandOptionType.Integer)
                        .WithDescription("The amount to be transferred")
                        .WithRequired(true)
                        
                      SlashCommandOptionBuilder()
                        .WithName("user")
                        .WithType(ApplicationCommandOptionType.User)
                        .WithDescription("The user to receive the transfer")
                        .WithRequired(true) ]
                    |> List.toArray )
                .Build()

        let! _ = deps.Client.CreateGlobalApplicationCommandAsync (globalCommand)

        ()
    }

    let createCommands (deps: Model.Dependencies) = task {
        let! _ = createBalanceCommand deps
        let! _ = createTransacCommand deps
        
        ()
    }
