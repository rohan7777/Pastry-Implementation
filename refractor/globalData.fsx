#r "nuget: Akka.FSharp"
#r "nuget: Akka.TestKit"


module globalData = 
    open Akka.Actor
    open Akka.FSharp
    let mutable nodeMap : Map<string, IActorRef> = Map.empty
    let mutable nodeHopMap: Map<string,list<double>> = Map.empty
