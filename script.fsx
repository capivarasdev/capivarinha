#r "nuget:Microsoft.Data.Sqlite"
#r "nuget:Fumble"

open Fumble
open Microsoft.Data.Sqlite

let connstr = "Data Source=/tmp/sqlitetables.db"

let x =
    connstr
    |> Sql.connect
    |> Sql.query "SELECT name FROM sqlite_master WHERE type = 'table'"
    |> Sql.execute (fun read -> read.string "name")

x

conn.Open()
let cmd = conn.CreateCommand()
cmd.CommandText <- """
    CREATE TABLE foo (
        bar text not null
    )
"""
cmd.ExecuteNonQuery()
conn.Close()

conn.Open()
let cmd = conn.CreateCommand()
cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table'"

let x = cmd.ExecuteReader()
while x.Read() do
    printfn "%A" (x.GetValue(0))
printfn "%A" x

conn.Close()