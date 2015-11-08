#I @"packages/FAKE/tools"
#r "FakeLib.dll"

open Fake
open System.IO
open Fake.Testing.XUnit2
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper

// ------------------------------------------------------------------------------ Project

let buildSolutionFile = "./Akka.Interfaced.sln"
let buildConfiguration = "Release"

type Project = { 
    Name: string;
    Folder: string;
    Template: bool;
    AssemblyVersion: string;
    PackageVersion: string;
    Releases: ReleaseNotes list;
    Dependencies: (string * string) list;
}

let emptyProject = { Name=""; Folder=""; Template=false; AssemblyVersion="";
                     PackageVersion=""; Releases=[]; Dependencies=[] }

let decoratePrerelease v =
    let couldParse, parsedInt = System.Int32.TryParse(v)
    if couldParse then "build" + (sprintf "%04d" parsedInt) else v

let decoratePackageVersion v =
    if hasBuildParam "nugetprerelease" then
        v + "-" + decoratePrerelease((getBuildParam "nugetprerelease"))
    else
        v

let projects = ([
    // Core Libraries
    {   emptyProject with
        Name="Akka.Interfaced";
        Folder="./core/Akka.Interfaced";
        Dependencies=[("Akka", "1.0.4");
                      ("Akka.Interfaced-Base", "")];
    };
    {   emptyProject with
        Name="Akka.Interfaced-Base";
        Folder="./core/Akka.Interfaced-Base";
    };
    {   emptyProject with
        Name="Akka.Interfaced-Persistence";
        Folder="./core/Akka.Interfaced-Persistence";
        Dependencies=[("Akka.Interfaced", "");
                      ("Akka.Persistence", "1.0.4.12-beta")];
    };
    {   emptyProject with
        Name="Akka.Interfaced-ProtobufSerializer";
        Folder="./core/Akka.Interfaced-ProtobufSerializer";
        Dependencies=[("Akka.Interfaced", "");
                      ("protobuf-net", "2.0.0.668");
                      ("TypeAlias", "1.0.1");];
    };
    {   emptyProject with
        Name="Akka.Interfaced-SlimClient";
        Folder="./core/Akka.Interfaced-SlimClient";
        Dependencies=[("Akka.Interfaced-Base", "")];
    };
    // CodeGenerator-Templates
    {   emptyProject with
        Name="Akka.Interfaced-Templates";
        Folder="./core/CodeGenerator-Templates";
        Template=true;
        Dependencies=[("Akka.Interfaced", "")];
    };
    {   emptyProject with
        Name="Akka.Interfaced-Templates-Protobuf";
        Folder="./core/CodeGenerator-Templates";
        Template=true;
        Dependencies=[("Akka.Interfaced", "");
                      ("protobuf-net", "2.0.0.668");
                      ("TypeAlias", "1.0.1");];
    };
    {   emptyProject with
        Name="Akka.Interfaced-Templates-SlimClient";
        Folder="./core/CodeGenerator-Templates";
        Template=true;
        Dependencies=[("Akka.Interfaced-SlimClient", "")];
    };
    {   emptyProject with
        Name="Akka.Interfaced-Templates-SlimClient-Protobuf";
        Folder="./core/CodeGenerator-Templates";
        Template=true;
        Dependencies=[("Akka.Interfaced-SlimClient", "");
                      ("protobuf-net", "2.0.0.668");
                      ("TypeAlias", "1.0.1");];
    };]
    |> List.map (fun p -> 
        let parsedReleases =
            File.ReadLines (p.Folder @@ (p.Name + ".Release.md"))
            |> ReleaseNotesHelper.parseAllReleaseNotes
        let latest = List.head parsedReleases
        { p with AssemblyVersion = latest.AssemblyVersion;
                 PackageVersion = decoratePackageVersion(latest.AssemblyVersion);
                 Releases = parsedReleases }
    ))

let project name =
    List.filter (fun p -> p.Name = name) projects |> List.head

let dependencies p =
    p.Dependencies |>
    List.map (fun d -> match d with 
                       | (id, "") -> (id, (project id).PackageVersion)
                       | (id, ver) -> (id, ver))
    
// ---------------------------------------------------------------------------- Variables

let binDir = "bin"
let testDir = binDir @@ "test"
let nugetDir = binDir @@ "nuget"
let nugetWorkDir = nugetDir @@ "work"

// ------------------------------------------------------------------------------ Targets

Target "Clean" (fun _ -> 
    CleanDirs [binDir]
)

Target "AssemblyInfo" (fun _ ->
    projects
    |> List.filter (fun p -> not p.Template)
    |> List.iter (fun p -> 
        CreateCSharpAssemblyInfo (p.Folder @@ "Properties" @@ "AssemblyInfoGenerated.cs")
          [ Attribute.Version p.AssemblyVersion
            Attribute.FileVersion p.AssemblyVersion
            Attribute.InformationalVersion p.PackageVersion ]
        )
)

Target "Build" (fun _ ->
    !! buildSolutionFile
    |> MSBuild "" "Rebuild" [ "Configuration", buildConfiguration ]
    |> Log "Build-Output: "
)

Target "Test" (fun _ ->  
    let xunitToolPath = findToolInSubPath "xunit.console.exe" "./packages/FAKE/xunit.runner.console*/tools"
    ensureDirectory testDir
    !! ("./core/**/bin/" + buildConfiguration + "/*.Tests.dll")
    |> xUnit2 (fun p -> 
        {p with 
            ToolPath = xunitToolPath;
            ShadowCopy = false;
            XmlOutputPath = Some (testDir @@ "TestResult.xml") })
)

let createNugetPackages _ =
    projects
    |> List.iter (fun project -> 
        let nugetFile = project.Folder @@ project.Name + ".nuspec";
        let workDir = nugetWorkDir @@ project.Name;

        let dllFileName = project.Folder @@ "bin/Release" @@ project.Name;
        let dllFiles = (!! (dllFileName + ".dll")
                        ++ (dllFileName + ".pdb")
                        ++ (dllFileName + ".xml"))
        dllFiles |> CopyFiles (workDir @@ "lib" @@ "net45")

        let dllFileNameNet35 = (project.Folder + ".Net35") @@ "bin/Release" @@ project.Name;
        let dllFilesNet35 = (!! (dllFileNameNet35 + ".dll")
                             ++ (dllFileNameNet35 + ".pdb")
                             ++ (dllFileNameNet35 + ".xml"))
        if (Seq.length dllFilesNet35 > 0) then (
            dllFilesNet35 |> CopyFiles (workDir @@ "lib" @@ "net35"))
         
        let isAssemblyInfo f = (filename f).Contains("AssemblyInfo")
        let isSrc f = (hasExt ".cs" f) && not (isAssemblyInfo f)
        CopyDir (workDir @@ "src") project.Folder isSrc

        NuGet (fun p -> 
            {p with
                Project = project.Name
                OutputPath = nugetDir
                WorkingDir = workDir
                Dependencies = dependencies project
                SymbolPackage = (if (project.Name.Contains("Templates")) then NugetSymbolPackage.None else NugetSymbolPackage.Nuspec)
                Version = project.PackageVersion 
                ReleaseNotes = (List.head project.Releases).Notes |> String.concat "\n"
            }) nugetFile
    )

let publishNugetPackages _ =
    projects
    |> List.iter (fun project -> 
        NuGetPublish (fun p -> 
            {p with
                Project = project.Name
                OutputPath = nugetDir
                WorkingDir = nugetDir
                AccessKey = getBuildParamOrDefault "nugetkey" ""
                PublishUrl = getBuildParamOrDefault "nugetpublishurl" ""
                Version = project.PackageVersion })

        if not project.Template && hasBuildParam "nugetpublishurl" then (
            // current FAKE doesn't support publishing symbol package with NuGetPublish.
            // To workaround thid limitation, let's tweak Version to cheat nuget read symbol package
            NuGetPublish (fun p -> 
                {p with
                    Project = project.Name
                    OutputPath = nugetDir
                    WorkingDir = nugetDir
                    AccessKey = getBuildParamOrDefault "nugetkey" ""
                    PublishUrl = getBuildParamOrDefault "nugetpublishurl" ""
                    Version = project.PackageVersion + ".symbols" })
        )
    )

Target "Nuget" <| fun _ ->
    createNugetPackages()
    publishNugetPackages()

Target "CreateNuget" <| fun _ ->
    createNugetPackages()

Target "PublishNuget" <| fun _ ->
    publishNugetPackages()

Target "Help" (fun _ ->  
    List.iter printfn [
      "usage:"
      "build [target]"
      ""
      " Targets for building:"
      " * Build        Build"
      " * Test         Build and Test"
      " * Nuget        Create and publish nugets packages"
      " * CreateNuget  Create nuget packages"
      "                [nugetprerelease={VERSION_PRERELEASE}] "
      " * PublishNuget Publish nugets packages"
      "                [nugetkey={API_KEY}] [nugetpublishurl={PUBLISH_URL}]"
      ""]
)

// --------------------------------------------------------------------------- Dependency

// Build order
"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "Test"

"Build"
  ==> "Nuget"

"Build"
  ==> "CreateNuget"
  
RunTargetOrDefault "Help"
