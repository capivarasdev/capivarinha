namespace Capivarinha

open System.Threading.Tasks
open System.Threading
open Discord
open Discord.WebSocket
open FSharp.Control
open Model

module Main =

    [<EntryPoint>]
    let main _ = 
        let settings = Settings.load ()
        let connectionString = Settings.databaseConnectionString settings

        Database.Infrastructure.migrate (connectionString)
        
        task {
            let config = new DiscordSocketConfig(GatewayIntents=(GatewayIntents.AllUnprivileged ||| GatewayIntents.MessageContent))
            let client = new DiscordSocketClient(config)

            let deps = {
                Logger = () 
                ConnectionString = connectionString
                Client = client
                Settings = settings
            }

            client.add_Ready Client.onReady
            client.add_ReactionAdded (fun message channel reaction -> task {
                let! msg = message.GetOrDownloadAsync()
                let! chan = channel.GetOrDownloadAsync()
                do! Client.onReactionAdded deps msg chan reaction
            })
            
            client.add_MessageReceived (fun message -> task {
                do! Client.onMessageReceived deps message
            })

            do! client.LoginAsync(TokenType.Bot, settings.DiscordToken)
            do! client.StartAsync()

            do! Task.Delay(Timeout.Infinite)
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

        0