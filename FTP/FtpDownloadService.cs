using System.Net;

namespace FtpDownloader;

public sealed class FtpDownloadService
{
    private readonly string _host;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _useSsl;

    public FtpDownloadService(string host, string username, string password, bool useSsl = false)
    {
        _host = host.Trim().TrimEnd('/');
        _username = username;
        _password = password;
        _useSsl = useSsl;
    }

    public async Task DownloadFileAsync(
        DownloadItem item,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        var request = CreateRequest(item.RemotePath, WebRequestMethods.Ftp.DownloadFile);

        var targetDirectory = Path.GetDirectoryName(item.LocalPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        try
        {
#pragma warning disable SYSLIB0014
            using var response = (FtpWebResponse)await request.GetResponseAsync();
#pragma warning restore SYSLIB0014
            await using var ftpStream = response.GetResponseStream();
            await using var fileStream = new FileStream(item.LocalPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var totalBytes = response.ContentLength;
            var downloadedBytes = 0L;
            var buffer = new byte[81920];

            while (true)
            {
                var bytesRead = await ftpStream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (int)(downloadedBytes * 100 / totalBytes);
                    progress.Report(percent);
                }
            }

            progress.Report(100);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(item.LocalPath))
            {
                File.Delete(item.LocalPath);
            }

            throw;
        }
    }

    public async Task<List<string>> ListDirectoryAsync(string remotePath, CancellationToken cancellationToken)
    {
        var request = CreateRequest(remotePath, WebRequestMethods.Ftp.ListDirectory);
        var files = new List<string>();

#pragma warning disable SYSLIB0014
        using var response = (FtpWebResponse)await request.GetResponseAsync();
#pragma warning restore SYSLIB0014
        await using var stream = response.GetResponseStream();
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                files.Add(line.Trim());
            }
        }

        return files;
    }

    private FtpWebRequest CreateRequest(string remotePath, string method)
    {
        var requestUri = BuildRequestUri(remotePath);

#pragma warning disable SYSLIB0014
        var request = (FtpWebRequest)WebRequest.Create(requestUri);
#pragma warning restore SYSLIB0014

        request.Method = method;
        request.Credentials = new NetworkCredential(_username, _password);
        request.EnableSsl = _useSsl;
        request.UseBinary = true;
        request.UsePassive = true;
        request.KeepAlive = false;
        request.ReadWriteTimeout = 30000;
        request.Timeout = 30000;
        return request;
    }

    private Uri BuildRequestUri(string remotePath)
    {
        var cleanPath = string.IsNullOrWhiteSpace(remotePath) ? "/" : remotePath.Trim();
        if (!cleanPath.StartsWith('/'))
        {
            cleanPath = "/" + cleanPath;
        }

        var baseAddress = _host.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)
            ? _host
            : $"ftp://{_host}";

        return new Uri($"{baseAddress}{cleanPath}");
    }
}
