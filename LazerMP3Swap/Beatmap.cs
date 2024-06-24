using System;
using System.Collections.Generic;
using Realms;

namespace LazerMP3Swap;

public class Beatmap : RealmObject
{
    [PrimaryKey] public Guid ID { get; set; } = new();

    public int OnlineID { get; set; } = 0;

    public BeatmapMetadata Metadata { get; set; } = null!;

    public BeatmapSet BeatmapSet { get; set; } = null!;
}

public class BeatmapSet : RealmObject
{
    [PrimaryKey] public Guid ID { get; set; } = new();

    public IList<RealmNamedFileUsage> Files { get; } = null!;
}

public class BeatmapMetadata : RealmObject
{
    public string Title { get; set; } = null!;

    public string Artist { get; set; } = null!;

    public string AudioFile { get; set; } = null!;
}

public class RealmNamedFileUsage : EmbeddedObject
{
    public string Filename { get; set; } = null!;

    public RealmFile File { get; set; } = null!;
}

[MapTo("File")]
public class RealmFile : RealmObject
{
    [PrimaryKey] public string Hash { get; set; } = string.Empty;
}