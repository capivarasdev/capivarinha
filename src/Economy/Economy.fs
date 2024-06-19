module Economy

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
    
    module Commands =
        let getBalanceCommand (deps: Model.Dependencies) (command: SocketSlashCommand) = asyncResult {
            let! wallet =
                Repository.getUserWallet deps.ConnectionString (string command.User.Id)

            match wallet with
            | Some w -> do! command.RespondAsync (sprintf "Suas unidades de moeda: `%i`." w.Amount)
            | None -> do! command.RespondAsync ("Não foi possível buscar sua carteira.")
        }

        let makeTransactionCommand (deps: Model.Dependencies) (command: SocketSlashCommand) = asyncResult {
            let amount = command.Data.Options |> Seq.find (fun i -> i.Name = "amount")
            let transferToUser = command.Data.Options |> Seq.find (fun i -> i.Name = "user")

            let transac: Entity.Transaction = {
                FromDiscordUserId = string command.User.Id
                Amount = int (string amount.Value)
                ToDiscordUserId = string (transferToUser.Value :?> SocketGuildUser).Id
            }
        
            let! succ = Repository.createTransaction deps.ConnectionString transac

            match succ with
            | 1 -> do! command.RespondAsync (sprintf "Transação feita com sucesso!")
            | _ -> do! command.RespondAsync (sprintf "Não foi possível fazer sua transação.")
        }

    let private createBalanceCommand (deps: Model.Dependencies) = task {
        let globalCommand =
            SlashCommandBuilder()
                .WithName("balance")
                .WithDescription("Shows user balance")
                .Build()

        let! _ = deps.Client.CreateGlobalApplicationCommandAsync (globalCommand)

        ()
    }

    let private createTransacCommand (deps: Model.Dependencies) = task {
        let globalCommand =
            SlashCommandBuilder()
                .WithName("transac")
                .WithDescription("Makes a transaction")
                .AddOptions(
                    [ SlashCommandOptionBuilder()
                        .WithName("amount")
                        .WithType(ApplicationCommandOptionType.Number)
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

    let tryProcessCommand (deps: Model.Dependencies) (command: SocketSlashCommand) = task {
        match command.Data.Name with
        | "balance" -> do! Commands.getBalanceCommand deps command |> AsyncResult.ignoreError
        | "transac" -> do! Commands.makeTransactionCommand deps command |> AsyncResult.ignoreError
        | _ -> ()
    }
