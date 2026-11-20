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
            if (args.Any(IsHelpRequest))
            {
                Console.Out.WriteLine(Usage.Text);
                return 0;
            }

            if (args.Length == 0)
            {
                Console.Error.WriteLine(Usage.Text);
                return 2;
            }

            LoggingSetup.Configure(
                LoggingOptions.Default with { FilePrefix = "areaimport" },
                AppContext.BaseDirectory);

            Logger.Warn("Area import is not implemented yet (T011). No files were written.");
            Console.Error.WriteLine("vibrom-areaimport: import pipeline is not implemented yet (T011).");
            return 2;
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception, "Unhandled importer error");
            return 1;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    private static bool IsHelpRequest(string argument) =>
        argument is "--help" or "-h" or "-?" or "/?";
}
