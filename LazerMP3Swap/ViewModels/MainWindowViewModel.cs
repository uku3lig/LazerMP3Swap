using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Realms;

namespace LazerMP3Swap.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private string _osuDirectory = GetOsuPath() ?? "";
    private string _idTextInput = "";
    private string _replacementPath = "";
    private string _oldFilePathText = "";

    private IObservable<Realm?> RealmObs => this.WhenAnyValue(x => x.OsuDirectory).Select(GetRealm);

    public string OsuDirectory
    {
        get => _osuDirectory;
        set => this.RaiseAndSetIfChanged(ref _osuDirectory, value);
    }

    public string IdTextInput
    {
        get => _idTextInput;
        set => this.RaiseAndSetIfChanged(ref _idTextInput, value);
    }

    public string ReplacementPath
    {
        get => _replacementPath;
        set => this.RaiseAndSetIfChanged(ref _replacementPath, value);
    }

    public string OldFilePathText
    {
        get => _oldFilePathText;
        private set => this.RaiseAndSetIfChanged(ref _oldFilePathText, value);
    }

    private IObservable<Beatmap?> Beatmap => this.WhenAnyValue(x => x.IdTextInput)
        .Select(s => Observable.FromAsync(() => GetMap(s))).Merge(1);

    public IObservable<string> BeatmapText =>
        this.WhenAnyObservable(x => x.Beatmap).Select(b => b == null
            ? "beatmap not found :("
            : $"Found {b.Metadata.Artist} - {b.Metadata.Title} [{b.DifficultyName}]");

    public IObservable<bool> IsOsuDirectoryValid =>
        this.WhenAnyValue(x => x.OsuDirectory, IsOsuDirValid);

    private IObservable<bool> IsValidBeatmap => this.WhenAnyObservable(x => x.Beatmap).Select(b => b != null);

    public IObservable<bool> IsValidReplacement => this.WhenAnyValue(x => x.ReplacementPath, Path.Exists);

    public IObservable<bool> IsEverythingValid => this.WhenAnyObservable(x => x.IsOsuDirectoryValid,
        x => x.IsValidBeatmap, x => x.IsValidReplacement, (o, b, r) => o && b && r);

    public ReactiveCommand<Unit, Unit> ReplaceCommand => ReactiveCommand.Create(ReplaceFile, IsEverythingValid);

    // Utility methods

    private static Realm? GetRealm(string osuDir)
    {
        if (!IsOsuDirValid(osuDir)) return null;

        var config = new RealmConfiguration(Path.Combine(osuDir, "client.realm"))
        {
            SchemaVersion = 41,
            IsReadOnly = true,
        };

        return Realm.GetInstance(config);
    }

    private async Task<Beatmap?> GetMap(string input)
    {
        var realm = await RealmObs.FirstAsync();

        return realm != null && int.TryParse(input, out var id)
            ? realm.All<Beatmap>().FirstOrDefault(b => b.OnlineID == id)
            : null;
    }

    private string GetFilePath(Beatmap b)
    {
        var filename = b.Metadata.AudioFile;
        var file = b.BeatmapSet.Files.First(f => f.Filename == filename);
        var hash = file.File.Hash;

        var first = hash[..1];
        var two = hash[..2];
        return Path.Combine(OsuDirectory, "files", first, two, hash);
    }

    private async void ReplaceFile()
    {
        var beatmap = await Beatmap.FirstAsync();
        if (beatmap == null) return;

        var filePath = GetFilePath(beatmap);
        var backupPath = ReplacementPath + ".old";

        File.Replace(ReplacementPath, filePath, backupPath);

        ReplacementPath = "";
        OldFilePathText = $"Successful! Backed up original file at {backupPath}";
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
        }
        else if (OperatingSystem.IsLinux())
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
        return Directory.Exists(path) && Directory.Exists(Path.Combine(path, "files")) &&
               Path.Exists(Path.Combine(path, "client.realm"));
    }
}