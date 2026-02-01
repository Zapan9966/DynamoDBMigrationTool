using McMaster.Extensions.CommandLineUtils;

namespace DynamoDBMigrationTool.Extensions;

internal static class ConsoleExtensions
{
    #region Error

    internal static void WriteLineError<T>(this IConsole console, T message)
        => console.WriteError(message + Environment.NewLine);

    internal static void WriteLineError(this IConsole console, string format, params object?[] args)
        => console.WriteError(string.Format(format, args) + Environment.NewLine);

    internal static void WriteError<T>(this IConsole console, T message)
    {
        ArgumentNullException.ThrowIfNull(message);

        console.ForegroundColor = ConsoleColor.Red;
        console.Write(message);
        console.ResetColor();
    }

    #endregion

    #region Success

    internal static void WriteLineSuccess<T>(this IConsole console, T message)
        => console.WriteSuccess(message + Environment.NewLine);

    internal static void WriteLineSuccess(this IConsole console, string format, params object?[] args)
        => console.WriteSuccess(string.Format(format, args) + Environment.NewLine);

    internal static void WriteSuccess<T>(this IConsole console, T message)
    {
        ArgumentNullException.ThrowIfNull(message);

        console.ForegroundColor = ConsoleColor.Green;
        console.Write(message);
        console.ResetColor();
    }

    #endregion
}
