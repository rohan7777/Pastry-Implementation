#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"
#load "globalData.fsx"
module node = 
    open Akka.Actor
    open Akka.FSharp
    open System
    open GlobalData.globalData

    let getRow i (arr: 'T[,]) = arr.[i..i, *] |> Seq.cast<'T> |> Seq.toArray 

    let Node (mailbox: Actor<_>) =
        let mutable nodeId: string = ""
        let mutable rows:int = 0
        let mutable columns: int = 16
        let mutable routingTable: string [,] = Array2D.zeroCreate 0 0
        let mutable leafNodeSet: Set<string> = Set.empty
        let mutable commonPrefixLength: int = 0
        let mutable rowPtr: int = 0 

        let rec loop () =
            actor {
                let! receivedMessage = mailbox.Receive()
                match box receivedMessage with
                | :? (string*int*int) as msg ->
                    let (n1,n2,operation) = msg
                    if(operation = 0) then
                        // Initilize operation
                        nodeId <- n1
                        rows <- n2
                        routingTable <- Array2D.zeroCreate rows columns
                        let mutable counter:int = 0
                        let number = Int32.Parse (nodeId, Globalization.NumberStyles.HexNumber)
                        let mutable left = number
                        let mutable right = number
                        while counter < 8 do
                            if left = 0 then
                                left <- nodeMap.Count - 1
                            leafNodeSet <- leafNodeSet.Add (left.ToString())
                            counter <- counter+1
                            left <- left-1

                        while counter < 16 do
                            if right = nodeMap.Count-1 then
                                right <- 0
                            leafNodeSet <- leafNodeSet.Add (right.ToString())
                            counter <- counter+1
                            right <- right+1
                        ()
                    else
                        // Join operation
                        let key = n1
                        let currentIndex = n2
                        let mutable i = 0
                        let mutable k = currentIndex
                        while key.[i] = nodeId.[i] do
                            i <- i+1
                        commonPrefixLength <- i
                        let mutable nextHopRow: string [] = Array.zeroCreate 0
                        while (k<=commonPrefixLength) do
                            nextHopRow <- getRow k routingTable 
                            nextHopRow.[Int32.Parse(nodeId.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)] <- nodeId

                            let found = nodeMap.TryFind key
                            match found with
                            | Some target -> 
                                // Update routing table of target
                                target <! nextHopRow
                                ()
                            | None -> printfn "Value not found."
                            k <- k+1
                        let targetRow = commonPrefixLength
                        let targetCol = Int32.Parse (key.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)
                        if (isNull routingTable.[targetRow, targetCol]) then
                            routingTable.[targetRow, targetCol] <- key
                        else 
                            let value = routingTable.[targetRow, targetCol]
                            let found = nodeMap.TryFind value
                            match found with
                            | Some target -> 
                                // Call join on selected node
                                target <! (key, k, 1)
                                ()
                            | None -> printfn "Value not found."
                        ()
                | :? (string[]) as newRow ->
                    // Update routing table
                    routingTable.[rowPtr, *] <- newRow
                    rowPtr <- rowPtr+1
                    ()
                | :? (string*string*int) as route ->
                    //Route operation
                    let (key,source,hops) = route
                    if (nodeId = key) then
                        if(nodeHopMap.ContainsKey(source)) then
                            let found = nodeHopMap.TryFind source
                            match found with
                            | Some hopMap -> 
                                let total = hopMap.[1]
                                let avgHops = hopMap.[0]
                                let entry = [((avgHops*total)+(hops|>double))/(total+1.0); total+1.0]
                                nodeHopMap <- nodeHopMap.Add(source, entry)
                                ()
                            | None -> printfn "Value not found."
                        else
                            let entry = [hops|>double; 1.0]
                            nodeHopMap <- nodeHopMap.Add(source, entry)
                    elif leafNodeSet.Contains(key) then
                        let found = nodeMap.TryFind key
                        match found with
                        | Some nextDest -> 
                            // Call join on selected node
                            nextDest <! (key, source, hops+1)
                            ()
                        | None -> printfn "Value not found."
                    else
                        let mutable i = 0
                        while (key.[i] = nodeId.[i]) do
                            i <- i+1
                        commonPrefixLength <- i
                        let nextHopRow = commonPrefixLength
                        let mutable nextHopCol = Int32.Parse(key.[commonPrefixLength].ToString(), Globalization.NumberStyles.HexNumber)
                        if(isNull routingTable.[nextHopRow, nextHopCol]) then
                            nextHopCol <- 0

                        let found = nodeMap.TryFind routingTable.[nextHopRow, nextHopCol]
                        match found with
                        | Some target -> 
                            // Call join on selected node
                            target <! (key, source, hops+1)
                            ()
                        | None -> printfn "Value not found."
                    ()
                |  _ -> return! loop ()
                return! loop()
            }
        loop ()