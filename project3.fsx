#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System.Diagnostics
open System

let system = ActorSystem.Create("FSharp")

let mutable actorMap : Map<string, IActorRef> = Map.empty
let mutable actorHopsMap: Map<string,list<double>> = Map.empty
let mutable srcdst : Map<string, string> = Map.empty

let numNodes = 1000
let numRequests = 10
let numDigits = Math.Ceiling(Math.Log(numNodes|>float)/Math.Log(16.0))

let mutable nodeId: string = ""
let mutable hexNum: string = ""
let mutable len: int = 0
// let mutable actor:IActorRef = null



// Spawn actor of peer here\

type InitOrJoin = string * int * int    // Message type to trigger inin & join. First 2 are parameters for init and join ops, if third int == 0 - init else join 
type RouteMsg = int * int * int * int


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





let Peer (mailbox: Actor<_>) =
    let mutable id: string = ""
    let mutable rows:int = 0
    let mutable cols: int = 16
    let mutable prefix: string = ""
    let mutable suffix: string = ""
    let mutable routingTable: string [,] = Array2D.zeroCreate 0 0
    let mutable leafSet: Set<string> = Set.empty
    let mutable commonPrefixLength: int = 0
    let mutable currentRow: int = 0 



    let rec loop () =
        actor {

            let! receivedMessage = mailbox.Receive()

            match box receivedMessage with
            | :? (string*int*int) as msg ->
                let (n1,n2,operation) = msg
                if(operation = 0) then
                    // Initilize operation
                    id <-  n1
                    rows <- n2
                    routingTable <- Array2D.zeroCreate rows cols
                    let mutable counter:int = 0
                    // let number = Int32.Parse "32" System.Globalization.NumberStyles.HexNumber
                    // Convert.ToString

                
                    ()
                else
                    // Join operation
                    let key = n1
                    let currentIndex = n2

                    ()

            | :? (int*int*int*int) as msg ->
                //Route operation
                ()
            |  _ -> return! loop ()

            
            return! loop()
        }
    loop ()



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


let actor = spawn system ("0") Peer
nodeId <- String.replicate (numDigits|>int) "0"


let msg:InitOrJoin = (nodeId, numDigits|>int, 0)








