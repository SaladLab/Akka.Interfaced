#I @"packages/FAKE/tools"
#r "FakeLib.dll"
#r "System.Xml.Linq"

open System
open System.IO
open System.Text
open Fake
open Fake.FileUtils
open Fake.MSTest
open Fake.NUnitCommon
open Fake.TaskRunnerHelper
open Fake.ProcessHelper

cd __SOURCE_DIRECTORY__

//--------------------------------------------------------------------------------
// Information about the project for Nuget and Assembly info files
//--------------------------------------------------------------------------------

let product = "Akka.Interfaced"
let authors = [ "Esun Kim" ]
let copyright = "Copyright © 2015 Saladbowl Creative"
let company = "Saladbowl Creative"
let description = "Akka.Interfaced provides the interfaced way for actor messaging in Akka.NET"
let tags = ["akka";"actors";"actor";"model";"Akka";"concurrency";"interfaced";"type-safe"]
let configuration = "Release"
let toolDir = "tools"
let CloudCopyDir = toolDir @@ "CloudCopy"
let AzCopyDir = toolDir @@ "AzCopy"

// Read release notes and version

let parsedRelease =
    File.ReadLines "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let envBuildNumber = System.Environment.GetEnvironmentVariable("BUILD_NUMBER")
let buildNumber = if String.IsNullOrWhiteSpace(envBuildNumber) then "0" else envBuildNumber

let version = parsedRelease.AssemblyVersion + "." + buildNumber
let preReleaseVersion = version + "-beta"

let isUnstableDocs = hasBuildParam "unstable"
let isPreRelease = hasBuildParam "nugetprerelease"
let release = if isPreRelease
              then ReleaseNotesHelper.ReleaseNotes.New(version, version + "-" + (getBuildParam "nugetprerelease"), parsedRelease.Notes)
              else parsedRelease

printfn "Assembly version: %s\nNuget version; %s\n" release.AssemblyVersion release.NugetVersion

//--------------------------------------------------------------------------------
// Directories
//--------------------------------------------------------------------------------

let binDir = "bin"
let outputDir = "output"
let testOutput = "TestResults"

let nugetDir = binDir @@ "nuget"
let workingDir = binDir @@ "build"
let libDir = workingDir @@ @"lib\net45\"
let lib35Dir = workingDir @@ @"lib\net35\"
let nugetExe = FullName @"tools\nuget\NuGet.exe"
let docDir = "bin" @@ "doc"

open Fake.RestorePackageHelper
Target "RestorePackages" (fun _ -> 
     "./Akka.Interfaced.sln"
     |> RestoreMSSolutionPackages (fun p ->
         { p with
             OutputPath = "./packages"
             Retries = 4 })
 )

//--------------------------------------------------------------------------------
// Clean build results
//--------------------------------------------------------------------------------

Target "Clean" <| fun _ ->
    DeleteDir binDir

//--------------------------------------------------------------------------------
// Generate AssemblyInfo files with the version for release notes 
//--------------------------------------------------------------------------------

open AssemblyInfoFile

Target "AssemblyInfo" <| fun _ ->
    CreateCSharpAssemblyInfoWithConfig "./core/SharedAssemblyInfo.cs" [
        Attribute.Company company
        Attribute.Copyright copyright
        Attribute.Trademark ""
        Attribute.Version version
        Attribute.FileVersion version ] <| AssemblyInfoFileConfig(false)

    for file in !! "./core/**/AssemblyInfo.cs" do
        let title =
            file
            |> Path.GetDirectoryName
            |> Path.GetDirectoryName
            |> Path.GetFileName

        CreateCSharpAssemblyInfo file [ 
            Attribute.Title title
            Attribute.Product product
            Attribute.Description description
            Attribute.Copyright copyright
            Attribute.Company company
            Attribute.ComVisible false
            Attribute.CLSCompliant true
            Attribute.Version version
            Attribute.FileVersion version ]


//--------------------------------------------------------------------------------
// Build the solution
//--------------------------------------------------------------------------------

Target "Build" <| fun _ ->

    !!"./Akka.Interfaced.sln"
    |> MSBuildRelease "" "Rebuild"
    |> ignore

Target "BuildMono" <| fun _ ->

    !!"./Akka.Interfaced.sln"
    |> MSBuild "" "Rebuild" [("Configuration","Release Mono")]
    |> ignore

//--------------------------------------------------------------------------------
// Copy the build output to bin directory
//--------------------------------------------------------------------------------

Target "CopyOutput" DoNothing

Target "BuildRelease" DoNothing

//--------------------------------------------------------------------------------
// Tests targets
//--------------------------------------------------------------------------------

//--------------------------------------------------------------------------------
// Clean test output
//--------------------------------------------------------------------------------

Target "CleanTests" <| fun _ ->
    DeleteDir testOutput

//--------------------------------------------------------------------------------
// Run tests
//--------------------------------------------------------------------------------

Target "RunTests" DoNothing

Target "RunTestsMono" DoNothing

Target "MultiNodeTests" DoNothing

//--------------------------------------------------------------------------------
// Nuget targets 
//--------------------------------------------------------------------------------

module Nuget = 
    // add Akka dependency for other projects
    let getAkkaDependency project =
        match project with
        | "Akka.Interfaced" -> [ "Akka.Interfaced-Base", release.NugetVersion ]
        | "Akka.Interfaced-Base" -> []
        | "Akka.Interfaced-ProtobufSerializer" -> [ "Akka.Interfaced", release.NugetVersion ]
        | "Akka.Interfaced-SlimClient" -> [ "Akka.Interfaced-Base", release.NugetVersion ]
        | "Akka.Interfaced-SlimSocketBase" -> []
        | "Akka.Interfaced-SlimSocketClient" -> [ "Akka.Interfaced-SlimSocketBase", release.NugetVersion ]
        | "Akka.Interfaced-SlimSocketServer" -> [ "Akka.Interfaced-SlimSocketBase", release.NugetVersion ]
        | "Akka.Interfaced.Templates" -> [ "Akka.Interfaced", release.NugetVersion ]
        | "Akka.Interfaced.Templates.SlimClient" -> [ "Akka.Interfaced-SlimClient", release.NugetVersion ]
        | _ -> []

    // used to add -pre suffix to pre-release packages
    let getProjectVersion project =
      match project with
      | _ -> release.NugetVersion

open Nuget

//--------------------------------------------------------------------------------
// Clean nuget directory
//--------------------------------------------------------------------------------

Target "CleanNuget" <| fun _ ->
    CleanDir nugetDir

//--------------------------------------------------------------------------------
// Pack nuget for all projects
// Publish to nuget.org if nugetkey is specified
//--------------------------------------------------------------------------------

let createNugetPackages _ =
    let removeDir dir = 
        let del _ = 
            DeleteDir dir
            not (directoryExists dir)
        runWithRetries del 3 |> ignore

    // Workaround of FileHelper.CopyFiles problem
    // FileHelper.CopyFiles may still own a handle of target directory after copy finished,
    // which makes removing target directory to fail.
    // (Exception: System.UnauthorizedAccessException: Access to the path '~\bin\build\lib\net45' is denied.)
    let copyFileToDir target fileName =
        System.IO.File.Copy(fileName, target @@ Path.GetFileName(fileName))

    ensureDirectory nugetDir
    for nuspec in !! "./core/**/*.nuspec" do
        printfn "Creating nuget packages for %s" nuspec
        
        CleanDir workingDir
        
        if nuspec.Contains "Templates" then do
            let project = Path.GetFileNameWithoutExtension nuspec
            let releaseVersion = getProjectVersion project
            let dependencies = getAkkaDependency project
            NuGetHelper.NuGet
                (fun p ->
                    { p with
                        Description = description
                        Authors = authors
                        Copyright = copyright
                        Project =  project
                        Properties = ["Configuration", "Release"]
                        ReleaseNotes = release.Notes |> String.concat "\n"
                        Version = releaseVersion
                        Tags = tags |> String.concat " "
                        OutputPath = nugetDir
                        WorkingDir = workingDir
                        Dependencies = dependencies })
                nuspec
        else do
            let project = Path.GetFileNameWithoutExtension nuspec 
            let projectDir = Path.GetDirectoryName nuspec
            let projectFile = (!! (projectDir @@ project + ".*sproj")) |> Seq.head
            let releaseDir = outputDir @@ project @@ @"bin\Release"
            let release35Dir = outputDir @@ (project + ".Net35") @@ @"bin\Release"
            let packages = projectDir @@ "packages.config"
            let packageDependencies = if (fileExists packages) then (getDependencies packages) else []
            let dependencies = packageDependencies @ getAkkaDependency project
            let releaseVersion = getProjectVersion project

            let pack outputDir symbolPackage =
                NuGetHelper.NuGet
                    (fun p ->
                        { p with
                            Description = description
                            Authors = authors
                            Copyright = copyright
                            Project =  project
                            Properties = ["Configuration", "Release"]
                            ReleaseNotes = release.Notes |> String.concat "\n"
                            Version = releaseVersion
                            Tags = tags |> String.concat " "
                            OutputPath = outputDir
                            WorkingDir = workingDir
                            SymbolPackage = symbolPackage
                            Dependencies = dependencies })
                    nuspec

            // Copy dll, pdb and xml to libdir = workingDir/lib/net45/
            ensureDirectory libDir
            !! (releaseDir @@ project + ".dll")
            ++ (releaseDir @@ project + ".pdb")
            ++ (releaseDir @@ project + ".xml")
            ++ (releaseDir @@ project + ".ExternalAnnotations.xml")
            |> Seq.iter (copyFileToDir libDir) 

            // Copy dll, pdb and xml to lib35dir = workingDir/lib/net35/
            if Directory.Exists(release35Dir) then do
                printfn "Detect .Net35 output: %s" release35Dir
                ensureDirectory lib35Dir
                !! (release35Dir @@ project + ".Net35.dll")
                ++ (release35Dir @@ project + ".Net35.pdb")
                ++ (release35Dir @@ project + ".Net35.xml")
                ++ (release35Dir @@ project + ".Net35.ExternalAnnotations.xml")
                |> Seq.iter (copyFileToDir lib35Dir)

            // Copy all src-files (.cs and .fs files) to workingDir/src
            let nugetSrcDir = workingDir @@ @"src/"
            // CreateDir nugetSrcDir

            let isCs = hasExt ".cs"
            let isFs = hasExt ".fs"
            let isAssemblyInfo f = (filename f).Contains("AssemblyInfo")
            let isSrc f = (isCs f || isFs f) && not (isAssemblyInfo f) 
            CopyDir nugetSrcDir projectDir isSrc
        
            //Remove workingDir/src/obj and workingDir/src/bin
            removeDir (nugetSrcDir @@ "obj")
            removeDir (nugetSrcDir @@ "bin")

            // Create both normal nuget package and symbols nuget package. 
            // Uses the files we copied to workingDir and outputs to nugetdir
            pack nugetDir NugetSymbolPackage.Nuspec

        removeDir workingDir

let publishNugetPackages _ = 
    let rec publishPackage url accessKey trialsLeft packageFile =
        let tracing = enableProcessTracing
        enableProcessTracing <- false
        let args p =
            match p with
            | (pack, key, "") -> sprintf "push \"%s\" %s" pack key
            | (pack, key, url) -> sprintf "push \"%s\" %s -source %s" pack key url

        tracefn "Pushing %s Attempts left: %d" (FullName packageFile) trialsLeft
        try 
            let result = ExecProcess (fun info -> 
                    info.FileName <- nugetExe
                    info.WorkingDirectory <- (Path.GetDirectoryName (FullName packageFile))
                    info.Arguments <- args (packageFile, accessKey,url)) (System.TimeSpan.FromMinutes 10.0)
            enableProcessTracing <- tracing
            if result <> 0 then failwithf "Error during NuGet symbol push. %s %s" nugetExe (args (packageFile, "key omitted",url))
        with exn -> 
            if (trialsLeft > 0) then (publishPackage url accessKey (trialsLeft-1) packageFile)
            else raise exn
    let shouldPushNugetPackages = hasBuildParam "nugetkey"
    let shouldPushSymbolsPackages = (hasBuildParam "symbolspublishurl") && (hasBuildParam "symbolskey")
    
    if (shouldPushNugetPackages || shouldPushSymbolsPackages) then
        printfn "Pushing nuget packages"
        if shouldPushNugetPackages then
            let normalPackages= 
                !! (nugetDir @@ "*.nupkg") 
                -- (nugetDir @@ "*.symbols.nupkg") |> Seq.sortBy(fun x -> x.ToLower())
            for package in normalPackages do
                publishPackage (getBuildParamOrDefault "nugetpublishurl" "") (getBuildParam "nugetkey") 3 package

        if shouldPushSymbolsPackages then
            let symbolPackages= !! (nugetDir @@ "*.symbols.nupkg") |> Seq.sortBy(fun x -> x.ToLower())
            for package in symbolPackages do
                publishPackage (getBuildParam "symbolspublishurl") (getBuildParam "symbolskey") 3 package


Target "Nuget" <| fun _ -> 
    createNugetPackages()
    publishNugetPackages()

Target "CreateNuget" <| fun _ -> 
    createNugetPackages()

Target "PublishNuget" <| fun _ -> 
    publishNugetPackages()

//--------------------------------------------------------------------------------
// Help 
//--------------------------------------------------------------------------------

Target "Help" <| fun _ ->
    List.iter printfn [
      "usage:"
      "build [target]"
      ""
      " Targets for building:"
      " * Build      Builds"
      " * Nuget      Create and optionally publish nugets packages"
      " * RunTests   Runs tests"
      " * MultiNodeTests  Runs the slower multiple node specifications"
      " * All        Builds, run tests, creates and optionally publish nuget packages"
      ""
      " Other Targets"
      " * Help       Display this help" 
      " * HelpNuget  Display help about creating and pushing nuget packages" 
      " * HelpDocs   Display help about creating and pushing API docs"
      " * HelpMultiNodeTests  Display help about running the multiple node specifications"
      ""]

Target "HelpNuget" <| fun _ ->
    List.iter printfn [
      "usage: "
      "build Nuget [nugetkey=<key> [nugetpublishurl=<url>]] "
      "            [symbolskey=<key> symbolspublishurl=<url>] "
      "            [nugetprerelease=<prefix>]"
      ""
      "Arguments for Nuget target:"
      "   nugetprerelease=<prefix>   Creates a pre-release package."
      "                              The version will be version-prefix<date>"
      "                              Example: nugetprerelease=dev =>"
      "                                       0.6.3-dev1408191917"
      ""
      "In order to publish a nuget package, keys must be specified."
      "If a key is not specified the nuget packages will only be created on disk"
      "After a build you can find them in bin/nuget"
      ""
      "For pushing nuget packages to nuget.org and symbols to symbolsource.org"
      "you need to specify nugetkey=<key>"
      "   build Nuget nugetKey=<key for nuget.org>"
      ""
      "For pushing the ordinary nuget packages to another place than nuget.org specify the url"
      "  nugetkey=<key>  nugetpublishurl=<url>  "
      ""
      "For pushing symbols packages specify:"
      "  symbolskey=<key>  symbolspublishurl=<url> "
      ""
      "Examples:"
      "  build Nuget                      Build nuget packages to the bin/nuget folder"
      ""
      "  build Nuget nugetprerelease=dev  Build pre-release nuget packages"
      ""
      "  build Nuget nugetkey=123         Build and publish to nuget.org and symbolsource.org"
      ""
      "  build Nuget nugetprerelease=dev nugetkey=123 nugetpublishurl=http://abc"
      "              symbolskey=456 symbolspublishurl=http://xyz"
      "                                   Build and publish pre-release nuget packages to http://abc"
      "                                   and symbols packages to http://xyz"
      ""]

//--------------------------------------------------------------------------------
//  Target dependencies
//--------------------------------------------------------------------------------

// build dependencies
"Clean" ==> "AssemblyInfo" ==> "RestorePackages" ==> "Build" ==> "CopyOutput" ==> "BuildRelease"

// tests dependencies
"CleanTests" ==> "RunTests"
"BuildRelease" ==> "CleanTests" ==> "MultiNodeTests"

// nuget dependencies
"CleanNuget" ==> "CreateNuget"
"CleanNuget" ==> "BuildRelease" ==> "Nuget"
//"CleanNuget" ==> "Nuget"

//docs dependencies
//"BuildRelease" ==> "Docs" ==> "AzureDocsDeploy" ==> "PublishDocs"

Target "All" DoNothing
"BuildRelease" ==> "All"
"RunTests" ==> "All"
//"MultiNodeTests" ==> "All"
"Nuget" ==> "All"

Target "AllTests" DoNothing //used for Mono builds, due to Mono 4.0 bug with FAKE / NuGet https://github.com/fsharp/fsharp/issues/427
"BuildRelease" ==> "AllTests"
"RunTests" ==> "AllTests"
//"MultiNodeTests" ==> "AllTests"

RunTargetOrDefault "Help"
