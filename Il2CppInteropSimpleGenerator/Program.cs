using Cpp2IL.Core;
using EasyNetLog;
using HarmonyLib;
using Il2CppInterop.Common;
using Il2CppInterop.Generator;
using Il2CppInterop.Generator.Runners;

namespace Il2CppInteropSimpleGenerator;

internal static class Program
{
    private static readonly EasyNetLogger logger = new(x => $"[<color=green>{DateTime.Now:HH:mm:ss.fff}</color>] {x}", true);

    private static int Main(string[] args)
    {
        var standaloneCli = args.Length == 0;

        var settingsPath = "settings.txt";

        string? gamePath = null;
        string? output = null;
        if (standaloneCli)
        {
            var loadSettings = false;
            if (File.Exists(settingsPath))
            {
                Console.Write("Would you like to use your previous settings? [Y/N] ");
                for (; ; )
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Y)
                    {
                        loadSettings = true;
                        break;
                    }
                    if (key.Key == ConsoleKey.N)
                        break;
                }
                Console.WriteLine();

                if (loadSettings)
                {
                    var lines = File.ReadAllLines(settingsPath);
                    if (lines.Length >= 2)
                    {
                        gamePath = lines[0];
                        output = lines[1];
                    }
                    else
                    {
                        Console.WriteLine("The settings file contains an invalid format. Please enter the paths manually.");
                        loadSettings = false;
                    }
                }
            }

            if (!loadSettings)
            {
                Console.Write("Game Path: ");
                gamePath = Console.ReadLine()?.Trim(' ', '"');

                Console.Write("Output Path: ");
                output = Console.ReadLine()?.Trim(' ', '"');
            }
        }
        else
        {
            if (args.Length < 2)
            {
                Console.WriteLine($"Invalid amount of arguments.");
                Console.WriteLine($"The first argument should be the path to the game directory, and the second argument should be the output path.");
                return -1;
            }

            gamePath = args[0];
            output = args[1];
        }

        if (!Directory.Exists(gamePath))
        {
            Console.WriteLine($"{gamePath} is not a valid directory.");
            if (standaloneCli)
                Console.ReadKey();

            return -1;
        }

        if (!Directory.Exists(output))
        {
            try
            {
                if (output == null)
                    throw new ArgumentNullException();

                Directory.CreateDirectory(output);
            }
            catch
            {
                Console.WriteLine($"{output} is not a valid directory path.");
                if (standaloneCli)
                    Console.ReadKey();

                return -1;
            }
        }
        else
        {
            Log("Removing old assemblies");

            Directory.EnumerateFiles(output, "*.dll").Do(File.Delete);
        }

        Log("Reading game info");

        GameInfo.Read(gamePath);

        if (GameInfo.UnityVersion == null)
        {
            Log("<color=red>Failed to read the unity version.</color>");
            if (standaloneCli)
                Console.ReadKey();

            return -1;
        }

        if (standaloneCli)
        {
            File.WriteAllLines(settingsPath, new string[]
            {
                gamePath,
                output
            });
        }

        Log($"Game Directory: {GameInfo.GameDirectory}");
        Log($"Game Data Directory: {GameInfo.GameDataDirectory}");
        Log($"Unity Version: {GameInfo.UnityVersion}");

        Log("");

        Log("Starting Cpp2IL");

        var metadataPath = Path.Combine(GameInfo.GameDataDirectory, "il2cpp_data", "Metadata", "global-metadata.dat");

        Logger.InfoLog += (message, s) =>
            Log($"[<color=purple>Cpp2IL</color>] [{s}] {message.Trim()}");
        Logger.WarningLog += (message, s) =>
            Log($"[<color=purple>Cpp2IL</color>] [{s}] <color=yellow>{message.Trim()}</color>");
        Logger.ErrorLog += (message, s) =>
            Log($"[<color=purple>Cpp2IL</color>] [{s}] <color=red>{message.Trim()}</color>");

        Cpp2IlApi.InitializeLibCpp2Il(GameInfo.GameAssemblyPath, metadataPath, new int[] { GameInfo.UnityVersion.Major, GameInfo.UnityVersion.Minor, GameInfo.UnityVersion.Build }, false);

        var cpp2ilOutput = Cpp2IlApi.MakeDummyDLLs();

        Cpp2IlApi.DisposeAndCleanupAll();

        Log("");
        Log("Cpp2IL generation finished. Starting Il2CppInterop generator");

        Il2CppInteropGenerator.Create(new()
        {
            Source = cpp2ilOutput,
            GameAssemblyPath = GameInfo.GameAssemblyPath,
            OutputDir = output
        })
            .AddLogger(new CustomLogger("<color=blue>Il2CppInterop</color>", Microsoft.Extensions.Logging.LogLevel.Information))
            .AddInteropAssemblyGenerator()
            .Run();

        Log("Il2CppInterop assembly generation has finished.");
        if (standaloneCli)
            Console.ReadKey();

        return 0;
    }

    internal static void Log(string msg)
    {
        logger.Log(msg);
    }
}