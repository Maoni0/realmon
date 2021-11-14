How to make a global .NET CLI tool to monitor garbage collection in real time?
-----------------------------------------------------------------------------------------------------
Such a tool is simply a console application stored in a nuget package with some specific properties.
The dotnet-gcmon.csproj file contains the corresponding settings and link to the implementation of the GCRealTimeMon console application.

When building this dotnet-gcmon C# project, a nuget package (dotnet-gcmon.(version x.y.z).nupkg) is generated under the nupkg folder.
Ensure that the configuration is "Release" to publish a new version.

It is also possible to manually generate the package in Release, by using the following command in the .csproj folder:
   dotnet pack -c Release

To publish a new version, upload the new dotnet-gcmon.(version x.y.z).nupkg file to https://www.nuget.org/packages/manage/upload.
After a while, it should appear under https://www.nuget.org/packages/dotnet-gcmon.
At that point, use the following command to install it on a machine:
   dotnet tool install -g dotnet-gcmon

NOTE: to prepare a new version, update the following property
    <PackageVersion>x.y.z</PackageVersion>
in the .csproj file before building/generating the nuget package.

NOTE: when new files are added to GCRealTimeMon project, don't forget to add them as links in the dotnet-gcmon.csproj file.

Read https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create for more details about creating a global .NET CLI tool.