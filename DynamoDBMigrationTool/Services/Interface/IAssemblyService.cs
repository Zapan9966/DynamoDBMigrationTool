using DynamoDBMigrationLib.Migrations.Interfaces;
using System.Reflection;

namespace DynamoDBMigrationTool.Services.Interface;

internal interface IAssemblyService
{
    string AssemblyPath(string? csprojPath);
    IMigrationRunner CreateRunner(Assembly? assembly, string? assemblyPath);
    Assembly LoadAssembly(string? assemblyPath);
}
