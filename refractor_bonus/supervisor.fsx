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
let numberOfFailureNodes = arguments.[5] |> int

let mutable nodeId: string = ""


nodeId <- String.replicate (numberOfDigits|>int) "0"
let mutable nodeRef = spawn system nodeId Node

nodeRef <! (nodeId, numberOfDigits, 0)
nodeMap <- nodeMap.Add(nodeId, nodeRef)
printfn "Building network, this might take a while..."
for i in [1 .. numberOfNodes-1] do
    let hexID = i.ToString("X")
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
let mutable counter = 0
let mutable deadNode = String.replicate (numberOfDigits|>int) "0"
while counter < numberOfFailureNodes do
    while deadNode = String.replicate (numberOfDigits|>int) "0" || deadNodeSet.Contains deadNode do
        deadNode <- actorsArray.[randomNumberGenerator.Next actorsArray.Length]
    Thread.Sleep 5
    deadNodeSet <- deadNodeSet.Add deadNode
    counter <- counter+1 

Thread.Sleep 3000
printfn "Processing requests"
Thread.Sleep 3000

let mutable k=1
let mutable destinationId=""
counter <- 0
while(k<=numberOfRequests) do
    for source in actorsArray do
        if not (deadNodeSet.Contains source) then
            counter <- counter + 1
            destinationId <- source
            while(destinationId = source || (deadNodeSet.Contains destinationId)) do
                destinationId <- actorsArray.[randomNumberGenerator.Next actorsArray.Length]
            let chosenSource = nodeMap.Item source
            chosenSource <! (destinationId, source, 0)
    Thread.Sleep 1000
    printfn "%d requests sent by each node" k
    k <- k+1


Thread.Sleep 1000
printfn "Requests sent"
let mutable hopsDone:double = 0.0
let nodeAvg = nodeHopMap |> Map.toSeq |> Seq.map snd |> Seq.toArray
for hop in nodeAvg do
    hopsDone <- hopsDone + hop.[0]
printfn "Average Hop Size: %A" (hopsDone/(nodeHopMap.Count|>double))
