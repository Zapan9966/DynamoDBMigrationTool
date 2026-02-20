using DynamoDBMigrationLib.Migrations.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace DynamoDBMigrationTool.Services.Interface;

internal interface IAssemblyService
{
    string AssemblyPath(string? csprojPath);
    IInternalMigrationRunner CreateRunner(Assembly? assembly, IConfiguration configuration, string? assemblyPath);
    Assembly LoadAssembly(string? assemblyPath);
}
