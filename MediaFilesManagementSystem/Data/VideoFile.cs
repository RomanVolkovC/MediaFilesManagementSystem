using MediaInfoLib;

using Microsoft.EntityFrameworkCore;

namespace MediaFilesManagementSystem.Data;

[Index(nameof(Path), IsUnique = true)]
public class VideoFile
{
    public VideoFile() { }
    public VideoFile(MediaInfo mi, User user)
    {
        Path = mi.Get(StreamKind.General, 0, "CompleteName");
        AddedById = user.Id;
        AddedBy = user;
        State = user.Role == Role.Administrator ? VideoFileState.None : VideoFileState.Adding;

        VideoCodec = mi.Get(StreamKind.Video, 0, "Format");
        Width = int.Parse(mi.Get(StreamKind.Video, 0, "Width"));
        Height = int.Parse(mi.Get(StreamKind.Video, 0, "Height"));
        Duration = TimeSpan.FromMilliseconds(int.Parse(mi.Get(StreamKind.Video, 0, "Duration")));
        VideoBitrate = int.Parse(mi.Get(StreamKind.Video, 0, "BitRate"));
        AspectRatioMode = mi.Get(StreamKind.Video, 0, "AspectRatio/String");
        AspectRatio = double.Parse(mi.Get(StreamKind.Video, 0, "AspectRatio").Replace('.', ','));
        FrameRateMode = mi.Get(StreamKind.Video, 0, "FrameRate_Mode");
        FrameRate = double.Parse(mi.Get(StreamKind.Video, 0, "FrameRate").Replace('.', ','));
        ScanType = mi.Get(StreamKind.Video, 0, "ScanType");

        AudioCodec = mi.Get(StreamKind.Audio, 0, "Format");
        int.TryParse(mi.Get(StreamKind.Audio, 0, "Duration"), out int audioDuration);
        AudioDuration = TimeSpan.FromMilliseconds(audioDuration);
        BitrateMode = mi.Get(StreamKind.Audio, 0, "BitRate_Mode");
        int.TryParse(mi.Get(StreamKind.Audio, 0, "BitRate"), out int audioBitrate);
        AudioBitrate = audioBitrate;
        CompressionMode = mi.Get(StreamKind.Audio, 0, "Compression_Mode");
        ChannelPositions = mi.Get(StreamKind.Audio, 0, "ChannelPositions");
        int.TryParse(mi.Get(StreamKind.Audio, 0, "SamplingRate"), out int samplingRate);
        SamplingRate = samplingRate;
    }

    public int Id { get; init; }
    public string Path { get; set; }
    public VideoFileState State { get; set; }
    public int AddedById { get; init; }
    public User AddedBy { get; init; }
    public int? BeingReplacedById { get; set; }
    public User? BeingReplacedBy { get; set; }
    public int? BeingReplacedOnId { get; set; }
    public VideoFile? BeingReplacedOn { get; set; }
    public int? DeletingById { get; set; }
    public User? DeletingBy { get; set; }

    public string VideoCodec { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public TimeSpan Duration { get; init; }
    public int VideoBitrate { get; init; }
    public string AspectRatioMode { get; init; }
    public double AspectRatio { get; init; }
    public string FrameRateMode { get; init; }
    public double FrameRate { get; init; }
    public string ScanType { get; init; }

    public string AudioCodec { get; init; }
    public TimeSpan AudioDuration { get; init; }
    public string BitrateMode { get; init; }
    public int AudioBitrate { get; init; }
    public string CompressionMode { get; init; }
    public string ChannelPositions { get; init; }
    public int SamplingRate { get; init; }
}
