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
        FromUserId: int
        ToUserId: int
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
        let! result =
            conn
            |> Sql.connect
            |> Sql.query
                @"SELECT
                    u.id AS user_id,
                    u.discord_id,
                    COALESCE(SUM(CASE WHEN t.to_user_id = u.id THEN t.amount ELSE 0 END), 0) -
                    COALESCE(SUM(CASE WHEN t.from_user_id = u.id THEN t.amount ELSE 0 END), 0) AS amount
                FROM
                    [user] u
                LEFT JOIN
                    [transaction] t ON u.id = t.from_user_id OR u.id = t.to_user_id
                WHERE
	                u.discord_id = @discordId"
            |> Sql.parameters [ "@discordId", Sql.string discordId ]
            |> Sql.executeRowAsync(fun read ->
                let r: Entity.Wallet = {
                    UserId = read.int "user_id";
                    DiscordId = read.string "discord_id";
                    Amount = read.int "amount";
                }
                
                r
            )

        return result
    }

    let createTransaction conn (transaction: Entity.Transaction) : Result<int, exn> =
        conn
        |> Sql.connect
        |> Sql.query
            @"INSERT INTO [transaction] (from_user_id, to_user_id, amount)
                VALUES (@fromUserId, @toUserId, @amount)"
        |> Sql.parameters
            [ "@fromUserId", Sql.int transaction.FromUserId
              "@toUserId", Sql.int transaction.ToUserId
              "@amount", Sql.int transaction.Amount ]
        |> Sql.executeNonQuery
    
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

module Discord =
    open Capivarinha
    open Discord.WebSocket
    open FsToolkit.ErrorHandling

    let runBalanceCommand (deps: Model.Dependencies) (command: SocketSlashCommand) = asyncResult {
        let! wallet =
            Repository.getUserWallet deps.ConnectionString (string command.User.Id)

        match wallet with
        | Some w -> do! command.RespondAsync (sprintf "Moedas: `%i`." w.Amount)
        | None -> do! command.RespondAsync ("Não foi possível buscar sua carteira.")
    }

    let tryProcessCommand (deps: Model.Dependencies) (command: SocketSlashCommand) = task {
        match command.Data.Name with
        | "balance" -> do! runBalanceCommand deps command |> AsyncResult.ignoreError
        | _ -> ()
    }
