namespace VibeROM.Common.Configuration;

/// <summary>Raised when configuration is missing, malformed or fails validation.</summary>
public sealed class GameConfigurationException : Exception
{
    public GameConfigurationException(string message)
        : base(message)
    {
    }

    public GameConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
