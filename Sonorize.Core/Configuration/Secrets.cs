namespace Sonorize.Core.Configuration;

/// <summary>
/// Manages application secrets using a partial method strategy.
/// </summary>
/// <remarks>
/// <strong>For Contributors:</strong>
/// This file works in tandem with <c>Secrets.Private.cs</c>, which is git-ignored.
/// <para>
/// The <c>InjectLastFmKeys</c> partial method is an implementation "hook".
/// If <c>Secrets.Private.cs</c> exists, the compiler includes the method body containing the real keys.
/// If it does not exist (e.g., in a fresh clone), the compiler removes the method call entirely,
/// safely resulting in empty strings without breaking the build or requiring dummy files.
/// </para>
/// </remarks>
public static partial class Secrets
{
    /// <summary>
    /// Retrieves the API keys.
    /// If Secrets.Private.cs is present, it returns the real keys.
    /// Otherwise, it returns placeholders/empty strings.
    /// </summary>
    public static (string ApiKey, string ApiSecret) GetLastFmKeys()
    {
        string key = "";
        string secret = "";

        // If the private partial implementation exists, this runs.
        // If it does not exist, the compiler removes this call entirely.
        InjectLastFmKeys(ref key, ref secret);

        return (key, secret);
    }

    // The "Hook" - implemented in Secrets.Private.cs (which is git-ignored)
    static partial void InjectLastFmKeys(ref string apiKey, ref string apiSecret);
}