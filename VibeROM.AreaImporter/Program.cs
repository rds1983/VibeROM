using NLog;
using VibeROM.Common.Configuration;
using VibeROM.Common.Logging;

namespace VibeROM.AreaImporter;

public static class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static int Main(string[] args)
    {
        try
        {
            var commandLine = CommandLine.Parse(args);
            if (commandLine.HelpRequested)
            {
                Console.Out.WriteLine(Usage.Text);
                return 0;
            }

            if (commandLine.Errors.Count > 0 || commandLine.Request is null)
            {
                foreach (var error in commandLine.Errors)
                {
                    Console.Error.WriteLine(error);
                }

                Console.Error.WriteLine(Usage.Text);
                return 2;
            }

            LoggingSetup.Configure(
                LoggingOptions.Default with { FilePrefix = "areaimport" },
                AppContext.BaseDirectory);

            var result = AreaImportRunner.Run(commandLine.Request);
            foreach (var message in result.Messages)
            {
                Console.Out.WriteLine(message);
            }

            foreach (var warning in result.Report.Warnings)
            {
                Console.Error.WriteLine(Format("warning", warning));
            }

            foreach (var error in result.Report.Errors)
            {
                Console.Error.WriteLine(Format("error", error));
            }

            if (result.Report.Errors.Count > 0)
            {
                Logger.Error("Import finished with {ErrorCount} error(s).", result.Report.Errors.Count);
            }

            return result.ExitCode;
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception, "Unhandled importer error");
            Console.Error.WriteLine($"Unhandled importer error: {exception.Message}");
            return 1;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    private static string Format(string level, ImportMessage message)
    {
        var location = message.File;
        if (message.Line > 0)
        {
            location += $":{message.Line}:{message.Column}";
        }

        return location.Length == 0
            ? $"{level}: {message.Message}"
            : $"{level}: {location}: {message.Message}";
    }
}
