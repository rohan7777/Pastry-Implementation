open System
#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"


module utilities = 
    open Akka.Actor
    open Akka.FSharp
    let mutable nodeMap : Map<string, IActorRef> = Map.empty
    let mutable nodeHopMap: Map<string,list<double>> = Map.empty
    let mutable deadNodeSet: Set<string> = Set.empty

    let getRow i (arr: 'T[,]) = arr.[i..i, *] |> Seq.cast<'T> |> Seq.toArray 
    let randomNumberGenerator = Random()

