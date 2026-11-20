using NLog;
using VibeROM.Common;

namespace VibeROM.Server;

public static class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static int Main()
    {
        try
        {
            Logger.Info("{Description}", GameInfo.Describe());

            Logger.Info("Scaffold only: server bootstrap is implemented in T020 and T024.");
            return 0;
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception, "Unhandled startup error");
            return 1;
        }
        finally
        {
            LogManager.Shutdown();
        }
    }
}
