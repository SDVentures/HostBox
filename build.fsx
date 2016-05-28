#r "packages/FAKE/tools/FakeLib.dll" // include Fake lib
open Fake
open Fake.AssemblyInfoFile

open System
open System.IO

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let project = "HostBox"
let authors = ["SDVentures Team"]
let summary = "The host of plugin applications."
let description = """
  The host of plugin applications can be executed as a console application or as a windows service."""
let license = "MIT License"
let tags = "rabbitmq client servicebus"

let release = ReleaseNotesHelper.parseReleaseNotes (File.ReadLines "RELEASE_NOTES.md")

let buildDir = @"build\"
let nugetDir = @"nuget\"

let projects =
    !! "Sources/**/*.csproj"

let isAppVeyorBuild = environVar "APPVEYOR" <> null
let appVeyorBuildNumber = environVar "APPVEYOR_BUILD_NUMBER"
let appVeyorRepoCommit = environVar "APPVEYOR_REPO_COMMIT"


Target "CleanUp" (fun _ ->
    CleanDirs [ buildDir ]
)

Target "BuildVersion" (fun _ ->
    let buildVersion = sprintf "%s-build%s" release.NugetVersion appVeyorBuildNumber
    Shell.Exec("appveyor", sprintf "UpdateBuild -Version \"%s\"" buildVersion) |> ignore
)

Target "AssemblyInfo" (fun _ ->
    printfn "%A" release
    let info =
        [ Attribute.Title project
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion
          Attribute.InformationalVersion release.NugetVersion
          Attribute.Copyright license
          Attribute.InternalsVisibleTo "DynamicProxyGenAssembly2"
          Attribute.InternalsVisibleTo "HostBox.Common.Tests"
          Attribute.InternalsVisibleTo "HostBox.RabbitMq.Tests"
          Attribute.InternalsVisibleTo "HostBox.Configurator.Tests" ]
    CreateCSharpAssemblyInfo <| "./Sources/" @@ project @@ "/Properties/AssemblyInfo.cs" <| info
)

Target "Build" (fun () ->
    MSBuildRelease buildDir "Build" projects |> Log "Build Target Output: "
)

Target "Deploy" (fun () ->
    NuGet (fun p ->
        { p with
            Authors = authors
            Project = project
            Summary = summary
            Description = description
            Version = release.NugetVersion
            ReleaseNotes = toLines release.Notes
            Tags = tags
            OutputPath = buildDir
            ToolPath = "./packages/NuGet.CommandLine/tools/Nuget.exe"
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies =
                [ "Common.Logging", GetPackageVersion "packages" "Common.Logging";
                  "Common.Logging.NLog32", GetPackageVersion "packages" "Common.Logging.NLog32";
                  "NLog", GetPackageVersion "packages" "NLog";
                  "Topshelf", GetPackageVersion "packages" "Topshelf";
                  "Topshelf.Common.Logging", GetPackageVersion "packages" "Topshelf.Common.Logging" ]
            Files = [ (@"..\" +  buildDir + "*.*", Some "tools", None ) ] })


        <| (nugetDir + project + ".nuspec")
)

"CleanUp"
    =?> ("BuildVersion", isAppVeyorBuild)
    ==> "AssemblyInfo"
    ==> "Build"
    ==> "Deploy"

RunTargetOrDefault "Deploy"
