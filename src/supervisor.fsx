#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#load "utilities.fsx"
#load "node.fsx"

open Akka.Actor
open Akka.FSharp
open System
open System.Threading
open Node.node
open Utilities.utilities
let system = ActorSystem.Create("FSharp")

let arguments = Environment.GetCommandLineArgs()
let numberOfNodes = arguments.[3] |> int
let numberOfRequests = arguments.[4] |> int
let numberOfDigits = Math.Ceiling(Math.Log(numberOfNodes|>float)/Math.Log(16.0)) |> int

let mutable nodeId: string = ""
let mutable hexID: string = ""
// let mutable hexLen: int = 0

let randomNumberGenerator = Random()

nodeId <- String.replicate (numberOfDigits|>int) "0"
let mutable nodeRef = spawn system nodeId Node

nodeRef <! (nodeId, numberOfDigits, 0)
nodeMap <- nodeMap.Add(nodeId, nodeRef)
printfn "Building network, this might take a while..."
for i in [1 .. numberOfNodes-1] do
    hexID <- i.ToString("X")
    let hexLen = hexID.Length
    nodeId <- (String.replicate ((numberOfDigits|>int)-hexLen) "0") + hexID
    nodeRef <- spawn system nodeId Node
    nodeRef <! (nodeId, numberOfDigits, 0)
    nodeMap <- nodeMap.Add(nodeId, nodeRef)
    let zeroth = String.replicate (numberOfDigits|>int) "0"
    let found = nodeMap.TryFind zeroth
    match found with
    | Some chosenSource -> 
        chosenSource <! (nodeId, 0, 1)
        ()
    | None -> printfn "Value not found."
    Thread.Sleep 5

Thread.Sleep 1000
printfn "Network setup complete"

let actorsArray = nodeMap |> Map.toSeq |> Seq.map fst |> Seq.toArray
printfn "Sending requests"
let mutable k=1
let mutable destinationId=""
let mutable counter = 0
while(k<=numberOfRequests) do
    for source in actorsArray do
        counter <- counter + 1
        destinationId <- source
        while(destinationId = source) do
            destinationId <- actorsArray.[randomNumberGenerator.Next actorsArray.Length]

        let found = nodeMap.TryFind source
        match found with
        | Some chosenSource -> 
            chosenSource <! (destinationId, source, 0)
            ()
        | None -> printfn "Value not found."

        // Thread.Sleep 5
    printfn "%d request sent by each Node" k
    Thread.Sleep 1000
    k <- k+1


Thread.Sleep 1000
printfn "Requests processed"
let mutable hopsDone:double = 0.0
// printfn "Computing average hop size"
let nodeAvg = nodeHopMap |> Map.toSeq |> Seq.map snd |> Seq.toArray
for hop in nodeAvg do
    hopsDone <- hopsDone + hop.[0]
printfn "Average Hop Size: %A" (hopsDone/(nodeHopMap.Count|>double))