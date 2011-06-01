pkg\nuget\Tools\NuGet i src\Elmah.MongoDB\packages.config -o src\packages
%windir%\Microsoft.NET\Framework\v3.5\msbuild src\Elmah-MongoDB.sln /t:Rebuild /p:Configuration=Release
cd pkg\nuget\
pack.cmd