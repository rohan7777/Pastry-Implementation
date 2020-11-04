#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"

open Akka.Actor
open Akka.FSharp
open System
open System.Threading
let system = ActorSystem.Create("FSharp")

let mutable actorMap : Map<string, IActorRef> = Map.empty
let mutable actorHopsMap: Map<string,list<double>> = Map.empty
// let mutable srcdst : Map<string, string> = Map.empty
let mutable deadSet: Set<string> = Set.empty

let numNodes = 1000
let numRequests = 5
let numDigits = Math.Ceiling(Math.Log(numNodes|>float)/Math.Log(16.0)) |> int
let failureNodes = 100

let mutable nodeId: string = ""
let mutable hexNum: string = ""
let mutable len: int = 0
// let mutable actor:IActorRef = null
let getRow i (arr: 'T[,]) = arr.[i..i, *] |> Seq.cast<'T> |> Seq.toArray 

let rand = Random()


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
            // printfn "msg here %A" receivedMessage
            match box receivedMessage with
            | :? (string*int*int) as msg ->
                let (n1,n2,operation) = msg
                if(operation = 0) then
                    // Initilize operation
                    id <- n1
                    rows <- n2
                    routingTable <- Array2D.zeroCreate rows cols
                    // printfn "initilizing %s" id
                    let mutable counter:int = 0
                    // let number = Int32.Parse "32" System.Globalization.NumberStyles.HexNumber
                    // Convert.ToString
                    let number = Int32.Parse (id, Globalization.NumberStyles.HexNumber)
                    let mutable left = number
                    let mutable right = number
                    // printfn "num: %A" number
                    while counter < 8 do
                        if left = 0 then
                            left <- actorMap.Count - 1
                        leafSet <- leafSet.Add (left.ToString())
                        // printfn "adding left into leafset:%A" left
                        counter <- counter+1
                        left <- left-1

                    while counter < 16 do
                        if right = actorMap.Count-1 then
                            right <- 0
                        leafSet <- leafSet.Add (right.ToString())
                        // printfn "adding right into leafset:%A" right
                        counter <- counter+1
                        right <- right+1
                    ()
                else
                    // Join operation
                    let key = n1
                    let currentIndex = n2
                    let mutable i = 0
                    let mutable j = 0
                    let mutable k = currentIndex
                    while key.[i] = id.[i] do
                        i <- i+1
                    commonPrefixLength <- i
                    // printfn "comminprefix %A at id %A" commonPrefixLength id
                    let mutable routingRow: string [] = Array.zeroCreate 0
                    while (k<=commonPrefixLength) do
                        routingRow <- getRow k routingTable 
                        routingRow.[Int32.Parse(id.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)] <- id

                        let found = actorMap.TryFind key
                        match found with
                        | Some x -> 
                            // Update routing table of x
                            x <! routingRow
                            ()
                        | None -> printfn "Did not find the specified value."
                        k <- k+1
                    let rtrow = commonPrefixLength
                    let rtcol = Int32.Parse (key.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)
                    if (isNull routingTable.[rtrow, rtcol]) then
                        routingTable.[rtrow, rtcol] <- key
                    else 
                        let value = routingTable.[rtrow, rtcol]
                        let found = actorMap.TryFind value
                        match found with
                        | Some x -> 
                            // Call join on selected node
                            x <! (key, k, 1)
                            ()
                        | None -> printfn "Did not find the specified value."

                    ()
            | :? (string[]) as newRow ->
                // Update routing table
                routingTable.[currentRow, *] <- newRow
                currentRow <- currentRow+1
                ()
            | :? (string*string*int) as route ->
                //Route operation
                let (key,source,hops) = route
                if (id = key) then
                    if(actorHopsMap.ContainsKey(source)) then
                        // let item = actorHopsMap
                        let found = actorHopsMap.TryFind source
                        match found with
                        | Some hopMap -> 
                            let total = hopMap.[1]
                            let avgHops = hopMap.[0]
                            let entry = [((avgHops*total)+(hops|>double))/(total+1.0); total+1.0]
                            actorHopsMap <- actorHopsMap.Add(source, entry)
                            ()
                        | None -> printfn "Did not find the specified value."
                    else 
                        let entry = [hops|>double; 1.0]
                        actorHopsMap <- actorHopsMap.Add(source, entry)
                elif leafSet.Contains(key) then
                    let act = actorMap.Item (key)
                    act <! (key, source, hops+1)
                else
                    let mutable i = 0
                    let mutable j = 0
                    while (key.Length <> 0 && key.[i] = id.[i]) do
                        i <- i+1
                    commonPrefixLength <- i
                    let rtrow = commonPrefixLength
                    let mutable rtcol = Int32.Parse(key.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)

                    if (deadSet.Contains routingTable.[rtrow, rtcol]) && (rtcol <> 0) then
                        rtcol <- rtcol-1

                    if(isNull routingTable.[rtrow, rtcol]) then
                        rtcol <- 0

                    if id = "00" && rtcol = 0 then
                        rtcol <- rtcol+1
                    actorMap.Item(routingTable.[rtrow, rtcol]) <! (key, source, hops+1)
                ()


            | :? string as printCMD ->
                // printfn "Routing table of node: %s" id
                printfn "Routing table of node: %s \n%A" id routingTable
            |  _ -> return! loop ()

            
            return! loop()
        }
    loop ()



////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


nodeId <- String.replicate (numDigits|>int) "0"
let mutable actor = spawn system nodeId Peer

// let msg:InitOrJoin = (nodeId, numDigits|>int, 0)
actor <! (nodeId, numDigits, 0)
actorMap <- actorMap.Add(nodeId, actor)

for i in [1 .. numNodes-1] do
    hexNum <- i.ToString("X")
    len <- hexNum.Length
    nodeId <- (String.replicate ((numDigits|>int)-len) "0") + hexNum
    actor <- spawn system nodeId Peer
    actor <! (nodeId, numDigits, 0)
    actorMap <- actorMap.Add(nodeId, actor)
    let zeroth = String.replicate (numDigits|>int) "0"
    let temp = actorMap.Item zeroth
    temp <! (nodeId, 0, 1)
    Thread.Sleep 5

Thread.Sleep 1000
printfn "Network is built"

let actorsArray = actorMap |> Map.toSeq |> Seq.map fst |> Seq.toArray
let mutable ctr = 0
let mutable deadNode = String.replicate (numDigits|>int) "0"
while ctr < failureNodes do
    while deadNode = String.replicate (numDigits|>int) "0" || deadSet.Contains deadNode do
        deadNode <- actorsArray.[rand.Next actorsArray.Length]
    Thread.Sleep 5
    deadSet <- deadSet.Add deadNode
    ctr <- ctr+1 // TODO: add dealy 5 

// printfn "Deadset size: %A" deadSet.Count
// printfn "Deadset is: %A" deadSet




printfn "Processing requests"
let mutable k=1
let mutable destinationId=""
let mutable counter = 0
while(k<=numRequests) do
    for source in actorsArray do
        if not (deadSet.Contains source) then
            counter <- counter + 1
            destinationId <- source
            while(destinationId = source || (deadSet.Contains destinationId)) do
                destinationId <- actorsArray.[rand.Next actorsArray.Length]
            let chosenSource = actorMap.Item source
            chosenSource <! (destinationId, source, 0)
            Thread.Sleep 5
    printfn "Each peer has performed %d requests" k
    k <- k+1


Thread.Sleep 1000
printfn "Requests processed"
let mutable totalHopSize:double = 0.0
printfn "Computing average hop size"
let nodeAvg = actorHopsMap |> Map.toSeq |> Seq.map snd |> Seq.toArray
for hop in nodeAvg do
    totalHopSize <- totalHopSize + hop.[0]
// printfn "HOP map: %A" actorHopsMap
printfn "Avg Hop Size %A" (totalHopSize/(actorHopsMap.Count|>double))
