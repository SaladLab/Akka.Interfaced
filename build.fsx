#I @"packages/FAKE/tools"
#I @"packages/FAKE.BuildLib/lib/net451"
#r "FakeLib.dll"
#r "BuildLib.dll"

open Fake
open BuildLib

let solution = 
    initSolution
        "./Akka.Interfaced.sln" "Release"
        [ { // Core Libraries
            emptyProject with Name = "Akka.Interfaced"
                              Folder = "./core/Akka.Interfaced"
                              Dependencies = 
                                  [ ("Akka.Interfaced-Base", "")
                                    ("Akka", "") ] }
          { emptyProject with Name = "Akka.Interfaced-Base"
                              Folder = "./core/Akka.Interfaced-Base" }
          { // CodeGenerator-Templates
            emptyProject with Name = "Akka.Interfaced.Templates"
                              Folder = "./core/CodeGenerator-Templates"
                              Template = true
                              Dependencies = [ ("Akka.Interfaced", "") ] }
          { emptyProject with Name = "Akka.Interfaced.Templates-Protobuf"
                              Folder = "./core/CodeGenerator-Templates"
                              Template = true
                              PackagePrerelease = "beta"
                              Dependencies = 
                                  [ ("Akka.Interfaced", "")
                                    ("protobuf-net", "2.1.0-alpha-1")
                                    ("TypeAlias", "1.1.2") ] }
          { emptyProject with Name = "Akka.Interfaced-SlimClient.Templates"
                              Folder = "./core/CodeGenerator-Templates"
                              Template = true
                              Dependencies = [ ("Akka.Interfaced-Base", "") ] }
          { emptyProject with Name = "Akka.Interfaced-SlimClient.Templates-Protobuf"
                              Folder = "./core/CodeGenerator-Templates"
                              Template = true
                              PackagePrerelease = "beta"
                              Dependencies = 
                                  [ ("Akka.Interfaced-Base", "")
                                    ("protobuf-net", "2.1.0-alpha-1")
                                    ("TypeAlias", "1.1.2") ] }
          { // CodeVerifier
            emptyProject with Name = "Akka.Interfaced.CodeVerifier"
                              Folder = "./core/CodeVerifier"
                              Template = true
                              Dependencies = [ ] }
          { // Plugins
            emptyProject with Name = "Akka.Interfaced.LogFilter"
                              Folder = "./plugins/Akka.Interfaced.LogFilter"
                              Dependencies = 
                                  [ ("Akka.Interfaced", "")
                                    ("Akka", "")
                                    ("Newtonsoft.Json", "") ] }
          { emptyProject with Name = "Akka.Interfaced.Persistence"
                              Folder = "./plugins/Akka.Interfaced.Persistence"
                              PackagePrerelease = "beta"
                              Dependencies = 
                                  [ ("Akka.Interfaced", "")
                                    ("Akka.Persistence", "") ] }
          { emptyProject with Name = "Akka.Interfaced.ProtobufSerializer"
                              Folder = "./plugins/Akka.Interfaced.ProtobufSerializer"
                              PackagePrerelease = "beta"
                              Dependencies = 
                                  [ ("Akka.Interfaced", "")
                                    ("protobuf-net", "")
                                    ("TypeAlias", "") ] }
          { emptyProject with Name = "Akka.Interfaced.TestKit"
                              Folder = "./plugins/Akka.Interfaced.TestKit"
                              Dependencies = [ ("Akka.Interfaced", "") ] } ]

Target "Clean" <| fun _ -> cleanBin

Target "AssemblyInfo" <| fun _ -> generateAssemblyInfo solution

Target "Restore" <| fun _ -> restoreNugetPackages solution

Target "Build" <| fun _ -> buildSolution solution

Target "Test" <| fun _ -> testSolution solution

Target "Cover" <| fun _ ->
     coverSolutionWithParams 
        (fun p -> { p with Filter = "+[Akka.Interfaced*]* -[*.Tests]*" })
        solution

Target "Coverity" <| fun _ -> coveritySolution solution "SaladLab/Akka.Interfaced"

Target "PackNuget" <| fun _ -> createNugetPackages solution

Target "PackUnity" <| fun _ ->
    packUnityPackage "./core/UnityPackage/AkkaInterfaced.unitypackage.json"

Target "Pack" <| fun _ -> ()

Target "PublishNuget" <| fun _ -> publishNugetPackages solution

Target "PublishUnity" <| fun _ -> ()

Target "Publish" <| fun _ -> ()

Target "CI" <| fun _ -> ()

Target "Help" <| fun _ -> 
    showUsage solution (fun _ -> None)

"Clean"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"
  ==> "Test"

"Build" ==> "Cover"
"Restore" ==> "Coverity"

let isPublishOnly = getBuildParam "publishonly"

"Build" ==> "PackNuget" =?> ("PublishNuget", isPublishOnly = "")
"Build" ==> "PackUnity" =?> ("PublishUnity", isPublishOnly = "")
"PackNuget" ==> "Pack"
"PackUnity" ==> "Pack"
"PublishNuget" ==> "Publish"
"PublishUnity" ==> "Publish"

"Test" ==> "CI"
"Cover" ==> "CI"
"Publish" ==> "CI"

RunTargetOrDefault "Help"
