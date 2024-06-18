namespace Capivarinha.Database

open Capivarinha.Model

open DbUp
open Fumble
open System

open FsToolkit.ErrorHandling

module Infrastructure =
    open System.Reflection
    let migrate (connectionString: string) =
        let engine =
            DeployChanges
                .To.SQLiteDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .WithTransaction()
                .Build()
        let result = engine.PerformUpgrade()

        if not result.Successful then
            failwithf "failed to migrate database: %s" (result.Error.ToString())

[<RequireQualifiedAccess>]
module Sql =
    let executeRowAsync read props = asyncResult {
        match! Sql.executeAsync read props with
        | [] -> return None
        | data::_ -> return Some data
    }

[<RequireQualifiedAccess>]
module Message =
    let saveMetadata conn userId messageContentLength =
        let sql = """
            INSERT INTO user_message
                (user_id, message_content_length)
            VALUES
                (@user_id, @message_content_length)
        """

        conn
        |> Sql.connect
        |> Sql.query sql
        |> Sql.parameters [
            "@user_id", Sql.string userId
            "@message_content_length", Sql.int messageContentLength
         ]
        |> Sql.executeNonQueryAsync

    let averageMessageLength conn userId =
        let sql = """
            SELECT average_content_length
            FROM user_average_message_content_length
            WHERE user_id = @user_id
        """

        conn
        |> Sql.connect
        |> Sql.query sql
        |> Sql.parameters [
            "@user_id", Sql.string userId
         ]
        |> Sql.executeRowAsync (fun reader -> reader.int "average_content_length")
        |> AsyncResult.map (Option.defaultValue 0)

module Die =
    let registerUserRoll conn userId =
        let sql = """
            INSERT INTO user_die_roll
                (user_id, rolled_at)
            VALUES (@user_id, @rolled_at)
            ON CONFLICT (user_id)
                DO UPDATE SET
                    rolled_at = @rolled_at
        """

        conn
        |> Sql.connect
        |> Sql.query sql
        |> Sql.parameters [
            "@user_id", Sql.string userId
            "@rolled_at", Sql.dateTimeOffset (DateTimeOffset.Now)
         ]
        |> Sql.executeNonQueryAsync

    let userLastRoll conn userId =
        let sql = """
            SELECT user_id, rolled_at
            FROM user_die_roll
            WHERE user_id = @user_id
        """

        conn
        |> Sql.connect
        |> Sql.query sql
        |> Sql.parameters [ "@user_id", Sql.string userId ]
        |> Sql.executeRowAsync (fun read -> { UserId = read.string "user_id"; RolledAt = read.dateTimeOffset "rolled_at" })
