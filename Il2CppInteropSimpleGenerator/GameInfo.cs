using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Il2CppInteropSimpleGenerator;

public static class GameInfo
{
    [NotNull] public static string? GameDirectory { get; private set; }
    [NotNull] public static string? GameDataDirectory { get; private set; }
    [NotNull] public static string? GameExePath { get; private set; }
    [NotNull] public static string? GameAssemblyPath { get; private set; }
    public static Version? UnityVersion { get; private set; }

    internal static void Read(string gameDirectory)
    {
        GameDirectory = gameDirectory;

        GameDataDirectory = Directory.EnumerateDirectories(gameDirectory, "*_Data").FirstOrDefault();

        if (GameDataDirectory == null)
            throw new ArgumentException($"{gameDirectory} is not a valid game path.");

        GameExePath = GameDataDirectory[..^5] + ".exe";

        GameAssemblyPath = Path.Combine(gameDirectory, "GameAssembly.dll");
        if (!File.Exists(GameAssemblyPath))
            throw new ArgumentException($"{gameDirectory} is not a valid unity il2cpp game path.");

        var unityPlayer = Path.Combine(gameDirectory, "UnityPlayer.dll");

        UnityVersion = Version.TryParse(FileVersionInfo.GetVersionInfo(unityPlayer).FileVersion, out var unityVersion)
            ? new(unityVersion.Major, unityVersion.Minor, unityVersion.Build) : null;
    }
}
