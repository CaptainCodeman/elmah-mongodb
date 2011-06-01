copy ..\..\src\Elmah.MongoDB\bin\Release\Elmah.MongoDB.dll nuspec\lib\net35 /Y
copy ..\..\src\Elmah.MongoDB\bin\Release\MongoDB.Bson.dll nuspec\lib\net35 /Y
copy ..\..\src\Elmah.MongoDB\bin\Release\MongoDB.Driver.dll nuspec\lib\net35 /Y
Tools\NuGet pack nuspec\Elmah.MongoDB.nuspec
rem Tools\NuGet push  elmah.mongodb.1.2.nupkg