namespace FtpDownloader;

public enum DownloadStatus
{
    Waiting,
    Downloading,
    Done,
    Failed
}

public sealed class DownloadItem
{
    public string RemotePath { get; set; } = string.Empty;

    public string LocalPath { get; set; } = string.Empty;

    public DownloadStatus Status { get; set; } = DownloadStatus.Waiting;

    public string ErrorMessage { get; set; } = string.Empty;

    public int Progress { get; set; }

    public string StatusText => Status switch
    {
        DownloadStatus.Waiting => "Queued",
        DownloadStatus.Downloading => $"Downloading {Progress}%",
        DownloadStatus.Done => "Completed",
        DownloadStatus.Failed => $"Failed: {ErrorMessage}",
        _ => string.Empty
    };

    public string FileName => Path.GetFileName(RemotePath.TrimEnd('/'));
}
