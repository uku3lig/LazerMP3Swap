using System;
using System.IO;
using System.Linq;
using Realms;

namespace LazerMP3Swap;

public static class Program
{
    public static void Main(string[] args)
    {
        string? osuDirectory = GetOsuPath();

        if (osuDirectory == null)
        {
            Console.WriteLine($"Invalid osu!lazer directory: {osuDirectory}");
            return;
        }

        var config = new RealmConfiguration(Path.Combine(osuDirectory, "client.realm"))
        {
            SchemaVersion = 41,
            IsReadOnly = true,
        };

        var realm = Realm.GetInstance(config);
        
        Console.Write("Beatmap ID: ");
        int mapId = int.Parse(Console.ReadLine() ?? "");
        
        Beatmap beatmap = realm.All<Beatmap>().First(b => b.OnlineID == mapId);

        Console.WriteLine($"Found map {beatmap.Metadata.Artist} - {beatmap.Metadata.Title}");

        string filename = beatmap.Metadata.AudioFile;
        RealmNamedFileUsage file = beatmap.BeatmapSet.Files.First(f => f.Filename == filename);
        string hash = file.File.Hash;

        Console.WriteLine($"Found audio file with hash {hash}");

        string first = hash[..1];
        string two = hash[..2];
        string filePath = Path.Combine(osuDirectory, "files", first, two, hash);

        if (!Path.Exists(filePath))
        {
            Console.WriteLine($"Could not find file at {filePath}");
            return;
        }
        
        Console.WriteLine($"Found path {filePath}");

        Console.Write("Path to replacement audio file: ");
        string replacementPath = Console.ReadLine() ?? "";

        if (!Path.Exists(replacementPath))
        {
            Console.WriteLine($"Replacement audio file does not exist at path {replacementPath}");
            return;
        }

        string backupPath = replacementPath + ".old";
        
        File.Replace(replacementPath, filePath, backupPath);

        Console.WriteLine($"Copied backup file to {backupPath}");
        Console.WriteLine($"Copied file to {filePath}");
    }

    private static string? GetOsuPath()
    {
        if (OperatingSystem.IsWindows())
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string osuFolder = Path.Combine(appData, "osu");

            if (IsOsuDirValid(osuFolder))
            {
                return osuFolder;
            }
        } else if (OperatingSystem.IsLinux())
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string osuFolder = Path.Combine(homeDir, ".local", "share", "osu");

            if (IsOsuDirValid(osuFolder))
            {
                return osuFolder;
            }
        }
        
        Console.Write("osu!lazer directory not found, please input manually: ");
        string? path = Console.ReadLine();

        if (path != null && IsOsuDirValid(path))
        {
            return path;
        }

        return null;
    }

    private static bool IsOsuDirValid(string path)
    {
        return Directory.Exists(path) && Directory.Exists(Path.Combine(path, "files")) && Path.Exists(Path.Combine(path, "client.realm"));
    }
}