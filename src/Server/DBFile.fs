module ServerCode.DBFile

open System.IO
open Newtonsoft.Json
open ServerCode.Domain
open Suave.Logging
open Suave.Logging.Message

let logger = Log.create "FableSample"      



//get path for upload download and others
module Path =
    let uploadDirStudent ctx = "temp/db/modules/"+(Query.useModuleId ctx)+"/assignments/"+
                                     (Query.useAssignmentId ctx)+"/students/"+(Query.useUserName ctx)+"/"
    let uploadDirTB ctx = "temp/db/modules/"+(Query.useModuleId ctx)+"/assignments/"+
                                     (Query.useAssignmentId ctx)+"/TB/"
//get file name path
module JSONFileName =

    let modules = "./temp/db/modules.json"

    let assignment moduleId = sprintf "./temp/db/modules/%s/assignments.json" moduleId

    let users = "./temp/db/users.json"

    let courseworkStudent ctx = (Path.uploadDirStudent ctx)+"courseworkStudent.json"

    let courseworkTeacher ctx = (Path.uploadDirTB ctx)+"courseworkTeacher.json"

module Users =
    
    let read ()=
        let fi = FileInfo(JSONFileName.users)
        if not fi.Exists then
            DBDefault.userList
        else
            File.ReadAllText(fi.FullName)
            |> JsonConvert.DeserializeObject<UserTable>

    let addModuleId (userNames:string list) (moduleId:ID) =
        try
            let users = read()
            let fi = FileInfo(JSONFileName.users)
            if not fi.Directory.Exists then
                fi.Directory.Create()

            let updateUser (user:UserRow) moduleId =
                { user with Data = { user.Data with ModulesID = moduleId::user.Data.ModulesID } }

            let usersupdated:UserTable =
                List.map (fun user -> 
                    if (List.contains user.Data.UserName userNames) then 
                        (updateUser user moduleId)     
                    else 
                        user
                    ) users
                    
            File.WriteAllText(fi.FullName,JsonConvert.SerializeObject usersupdated)
        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)


/// Query the database for Modules
module Modules =

    let readAll () : ModuleTable =
        let fi = FileInfo(JSONFileName.modules)
        if not fi.Exists then
            DBDefault.modules
        else
            File.ReadAllText(fi.FullName)
            |> JsonConvert.DeserializeObject<ModuleTable>

    let readForUser userName : ModuleTable =
        let users = Users.read()

        let modules = readAll()

        let user = List.tryFind (fun user -> user.Data.UserName = userName ) users

        if user = None then
            []
        else
            let userModules : ModuleTable = 
                List.filter (fun (x:ModuleRow) -> List.contains x.ID user.Value.Data.ModulesID )  modules
            logger.debug (eventX ( "userModules = "+(JsonConvert.SerializeObject userModules) ))
            userModules
            


    let write (modules:ModuleTable) =
        try
            let fi = FileInfo(JSONFileName.modules)
            if not fi.Directory.Exists then
                fi.Directory.Create()
            File.WriteAllText(fi.FullName,JsonConvert.SerializeObject modules)
        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)

/// Query the database
module Assignments =
    let readForModule moduleId =
        let fi = FileInfo(JSONFileName.assignment moduleId)
        if not fi.Exists then
            DBDefault.assignments moduleId
        else
            File.ReadAllText(fi.FullName)
            |> JsonConvert.DeserializeObject<AssignmentTable>

    let write moduleId (assignments:AssignmentTable) =
        try
            let fi = FileInfo(JSONFileName.assignment moduleId)
            if not fi.Directory.Exists then
                fi.Directory.Create()
            File.WriteAllText(fi.FullName,JsonConvert.SerializeObject assignments)
        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)

module Upload =
    let coursework fileText ctx=
        try
            
            let filename = "StudentsAnswers"

            let filePath = "./"+(Path.uploadDirStudent ctx)+filename

            let fi = FileInfo(filePath)

            if not fi.Directory.Exists then
                fi.Directory.Create()
            File.WriteAllText(filePath+".fs",fileText)

            (*File.WriteAllText(filePath+".cmd","@echo off \n"+
                "cls \n \n"+
                @"..\..\..\..\..\..\..\..\..\..\packages\FSharp.Compiler.Tools\Tools\fsc.exe "+
                filename+".fs"+" --standalone -o "+filename+".exe \n"+
                filename)*)

        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)

    let testbench fileText filePath =
        try
            
            let fi = FileInfo(filePath)

            if not fi.Directory.Exists then
                fi.Directory.Create()
            File.WriteAllText(filePath,fileText)

        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)

module StudentCoursework =
    
    let read ctx=
        let fi = FileInfo(JSONFileName.courseworkStudent ctx)
        if not fi.Exists then
            DBDefault.courseworkStudent
        else
            File.ReadAllText(fi.FullName)
            |> JsonConvert.DeserializeObject<StudentCoursework>

    let write (coursework:StudentCoursework) ctx =
        try
            let fi = FileInfo(JSONFileName.courseworkStudent ctx)
            if not fi.Directory.Exists then
                fi.Directory.Create()
            File.WriteAllText(fi.FullName,JsonConvert.SerializeObject coursework)
        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)

module TeacherCoursework =
    
    let read ctx=
        let fi = FileInfo(JSONFileName.courseworkTeacher ctx)
        if not fi.Exists then
            DBDefault.courseworkTeacher
        else
            File.ReadAllText(fi.FullName)
            |> JsonConvert.DeserializeObject<TeacherCoursework>

    let write (coursework:TeacherCoursework) ctx =
        try
            let fi = FileInfo(JSONFileName.courseworkTeacher ctx)
            if not fi.Directory.Exists then
                fi.Directory.Create()
            File.WriteAllText(fi.FullName,JsonConvert.SerializeObject coursework)
        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)

module Coursework =
    
    let createRunBox ctx=
        try
            
            let dirname = "RunBox/"

            let projname = "DotNet.fsproj"
             
            let refname = "paket.references"

            let dirPath = "./"+(Path.uploadDirStudent ctx)+dirname

            let fiproj = FileInfo(dirPath+projname)
            let firef = FileInfo(dirPath+refname)
            if not fiproj.Directory.Exists then
                fiproj.Directory.Create()
            File.WriteAllText(firef.FullName,"group Test \nFSharp.Core")
            File.WriteAllText(fiproj.FullName,"<Project Sdk=\"FSharp.NET.Sdk;Microsoft.NET.Sdk\">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>


  <ItemGroup>
    <Compile Include=\"..\..\..\TB\ModelAnswers.fs\" />
    <Compile Include=\"..\StudentsAnswers.fs\" />
    <Compile Include=\"..\..\..\TB\TestBench.fs\" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=\"FSharp.NET.Sdk\" Version=\"1.0.0-beta-060000\" PrivateAssets=\"All\" />
    <DotNetCliToolReference Include=\"dotnet-compile-fsc\" Version=\"1.0.0-preview2-020000\" />
    <EmbeddedResource Include=\"**\*.resx\" />
    <DotNetCliToolReference Include=\"Microsoft.DotNet.Watcher.Tools\" Version=\"1.0.0\" />
  </ItemGroup>
  <Import Project=\"..\..\..\..\..\..\..\..\..\..\..\.paket\Paket.Restore.targets\" />


</Project>")


        with exn ->
            logger.error (eventX "Save failed with exception" >> addExn exn)
