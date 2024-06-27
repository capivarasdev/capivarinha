namespace Capivarinha.Tests

open Expecto
open Expecto.Flip

open System.Threading.Tasks
open System
open System.Threading.Channels

[<RequireQualifiedAccess>]
module Utility =
    open Capivarinha
    open Discord

    let withMessageReceived messageReceived = task {
        let channel = Channel.CreateUnbounded<Result<unit, exn>>();

        let testSetup, testHandler = messageReceived
        let handlers = { Client.EventHandlers.init with MessageReceived = (testHandler channel) };
        let client = Client.setup handlers

        do! client.LoginAsync(TokenType.Bot, "")
        do! client.StartAsync()
        do! Task.Delay(TimeSpan.FromSeconds 10)

        do! testSetup client

        let! result = channel.Reader.ReadAsync()
        match result with
        | Ok _ -> ()
        | Error ex -> raise ex

        do! client.LogoutAsync()
        do! client.DisposeAsync()
        do! Task.Delay(TimeSpan.FromSeconds 10)
    }

module ActionHandler =
    let tests = testList "Guards" [
        yield! testFixtureTask Utility.withMessageReceived [
            "tryUser", (
                (fun client -> task {
                    printfn "%A" client.CurrentUser.Id
                    try
                        ()
                    with ex -> 
                        printfn "%A" ex
                }),
                (fun (channel: Channel<Result<unit,exn>>) client message -> task {
                    try
                        ActionHandler.tryUser client message.Author ()
                        |> Expect.isOk "should return ok"
                    with ex -> do! channel.Writer.WriteAsync(Error ex)
                }));
            "tryUser2", (
                (fun client -> task {
                    printfn "%A" client.CurrentUser.Id
                    try
                        ()
                    with ex -> 
                        printfn "%A" ex
                }), 
                (fun (channel: Channel<Result<unit,exn>>) client message -> task {
                    try
                        ActionHandler.tryUser client message.Author ()
                        |> Expect.isOk "should return ok"
                    with ex -> do! channel.Writer.WriteAsync(Error ex)
                }))
        ]
    ]
    

module Main =
    [<EntryPoint>]
    let main _ =
        let tests = testList "Capivarinha" [
            ActionHandler.tests
        ]

        runTestsWithCLIArgs [ ] [||] tests
