﻿// Learn more about F# at http://fsharp.org

open System
open System.Threading
open Clusterpack
open Clusterpack.Grpc
open FSharp.Control.Tasks.Builders
open FSharp.Core

type Greeter() =
    inherit UnboundedActor<string>()
    override this.Receive(msg) = unitVtask {
        printfn "Hello %s!" msg
    }

let start nodeId endpoint =
    let transport = new GrpcTransport(nodeId, endpoint)
    new Node(transport)
    
let run (cancel: CancellationToken) = unitVtask {    
    use a = start 1u "127.0.0.1:10001"
    use b = start 2u "127.0.0.1:10002"
    
    let! _ = a.Connect("127.0.0.1:10002", cancel)
    
    let local = a.Wrap(fun addr -> new Greeter())
    match b.Proxy(local.Address) with
    | None -> printfn "Couldn't target '%O' from node '%O'" local.Address b.Manifest.NodeId
    | Some remote ->
        do! remote.WriteAsync("remote")
    
    printfn "press any key to finish... "
    Console.Read() |> ignore
    do! a.DisposeAsync()
    do! b.DisposeAsync()
    printfn "done"
}

[<EntryPoint>]
let main argv =
    use cancel = new CancellationTokenSource(10_000)
    run(cancel.Token).GetAwaiter().GetResult()
    0 // return an integer exit code