namespace FtpDownloader;

public sealed class MainForm : Form
{
    private GroupBox _connectionGroup = null!;
    private Label _hostLabel = null!;
    private Label _userLabel = null!;
    private Label _passwordLabel = null!;
    private TextBox _hostTextBox = null!;
    private TextBox _userTextBox = null!;
    private TextBox _passwordTextBox = null!;
    private CheckBox _sslCheckBox = null!;

    private GroupBox _settingsGroup = null!;
    private Label _delayLabel = null!;
    private Label _delayValueLabel = null!;
    private Label _saveToLabel = null!;
    private TrackBar _delayTrackBar = null!;
    private TextBox _saveToTextBox = null!;
    private Button _saveFolderButton = null!;

    private GroupBox _filesGroup = null!;
    private TextBox _remotePathTextBox = null!;
    private Button _addButton = null!;
    private Button _removeButton = null!;
    private Button _clearButton = null!;
    private Button _browseFtpButton = null!;
    private ListView _filesListView = null!;

    private Button _startButton = null!;
    private Button _stopButton = null!;
    private ProgressBar _overallProgressBar = null!;
    private Label _statusLabel = null!;

    private readonly List<DownloadItem> _items = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public MainForm()
    {
        InitializeComponents();
        ApplyTheme();
        UpdateDelayLabel();
    }

    private void InitializeComponents()
    {
        Text = "FTP Downloader";
        Size = new Size(780, 720);
        MinimumSize = new Size(700, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        _connectionGroup = CreateGroupBox("Connection", 8, 8, 756, 110);
        _hostLabel = CreateLabel("FTP Host:", 12, 28);
        _hostTextBox = CreateTextBox(90, 26, 400, "ftp.example.com");
        _userLabel = CreateLabel("Username:", 12, 62);
        _userTextBox = CreateTextBox(90, 60, 180, "anonymous");
        _passwordLabel = CreateLabel("Password:", 290, 62);
        _passwordTextBox = CreateTextBox(370, 60, 180, string.Empty);
        _passwordTextBox.PasswordChar = '*';
        _sslCheckBox = new CheckBox { Text = "Use SSL", Left = 570, Top = 62, AutoSize = true };
        _connectionGroup.Controls.AddRange([_hostLabel, _hostTextBox, _userLabel, _userTextBox, _passwordLabel, _passwordTextBox, _sslCheckBox]);

        _settingsGroup = CreateGroupBox("Download Settings", 8, 126, 756, 110);
        _delayLabel = CreateLabel("Delay between files:", 12, 28);
        _delayTrackBar = new TrackBar
        {
            Left = 155,
            Top = 20,
            Width = 480,
            Minimum = 0,
            Maximum = 30,
            Value = 3,
            TickFrequency = 5,
            SmallChange = 1,
            LargeChange = 5
        };
        _delayTrackBar.Scroll += (_, _) => UpdateDelayLabel();
        _delayValueLabel = new Label
        {
            Left = 645,
            Top = 28,
            Width = 90,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        _saveToLabel = CreateLabel("Save to:", 12, 70);
        _saveToTextBox = CreateTextBox(90, 68, 560, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FtpDownloads"));
        _saveFolderButton = CreateButton("...", 658, 66, 70);
        _saveFolderButton.Click += BrowseSaveFolder;
        _settingsGroup.Controls.AddRange([_delayLabel, _delayTrackBar, _delayValueLabel, _saveToLabel, _saveToTextBox, _saveFolderButton]);

        _filesGroup = CreateGroupBox("Files Queue", 8, 244, 756, 340);
        _remotePathTextBox = CreateTextBox(10, 26, 540, "/path/to/file.zip");
        _addButton = CreateButton("Add", 558, 24, 60);
        _addButton.Click += AddRemotePath;
        _browseFtpButton = CreateButton("Browse FTP", 624, 24, 100);
        _browseFtpButton.Click += BrowseFtpDirectoryAsync;
        _removeButton = CreateButton("Remove", 10, 56, 80);
        _removeButton.Click += RemoveSelectedItems;
        _clearButton = CreateButton("Clear All", 96, 56, 80);
        _clearButton.Click += ClearItems;
        _filesListView = new ListView
        {
            Left = 10,
            Top = 86,
            Width = 728,
            Height = 235,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = true
        };
        _filesListView.Columns.Add("File Name", 220);
        _filesListView.Columns.Add("Remote Path", 300);
        _filesListView.Columns.Add("Status", 180);
        _filesGroup.Controls.AddRange([_remotePathTextBox, _addButton, _browseFtpButton, _removeButton, _clearButton, _filesListView]);

        _startButton = new Button
        {
            Text = "Start Download",
            Left = 8,
            Top = 592,
            Width = 160,
            Height = 38,
            BackColor = Color.FromArgb(34, 139, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        _startButton.Click += StartDownloadAsync;

        _stopButton = new Button
        {
            Text = "Stop",
            Left = 176,
            Top = 592,
            Width = 100,
            Height = 38,
            Enabled = false,
            BackColor = Color.FromArgb(180, 40, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        _stopButton.Click += StopDownload;

        _overallProgressBar = new ProgressBar
        {
            Left = 284,
            Top = 596,
            Width = 480,
            Height = 30,
            Style = ProgressBarStyle.Continuous
        };

        _statusLabel = new Label
        {
            Left = 8,
            Top = 638,
            Width = 756,
            Height = 22,
            Text = "Ready",
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9f)
        };

        Controls.AddRange([_connectionGroup, _settingsGroup, _filesGroup, _startButton, _stopButton, _overallProgressBar, _statusLabel]);
    }

    private void ApplyTheme()
    {
        BackColor = Color.FromArgb(245, 245, 248);
    }

    private static GroupBox CreateGroupBox(string text, int x, int y, int width, int height)
        => new() { Text = text, Left = x, Top = y, Width = width, Height = height };

    private static Label CreateLabel(string text, int x, int y)
        => new() { Text = text, Left = x, Top = y + 3, AutoSize = true };

    private static TextBox CreateTextBox(int x, int y, int width, string text)
        => new() { Left = x, Top = y, Width = width, Text = text };

    private static Button CreateButton(string text, int x, int y, int width)
        => new() { Text = text, Left = x, Top = y, Width = width, Height = 26, FlatStyle = FlatStyle.System };

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Enter && _remotePathTextBox.Focused)
        {
            AddRemotePath(this, EventArgs.Empty);
            return true;
        }

        return base.ProcessCmdKey(ref message, keyData);
    }

    private void UpdateDelayLabel()
    {
        var value = _delayTrackBar.Value;
        _delayValueLabel.Text = value == 0 ? "No delay" : $"{value} second{(value == 1 ? string.Empty : "s")}";
    }

    private void BrowseSaveFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Select a folder for downloaded files" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _saveToTextBox.Text = dialog.SelectedPath;
        }
    }

    private void AddRemotePath(object? sender, EventArgs e)
    {
        var remotePath = NormalizeRemotePath(_remotePathTextBox.Text);
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return;
        }

        if (_items.Any(item => string.Equals(item.RemotePath, remotePath, StringComparison.OrdinalIgnoreCase)))
        {
            SetStatus("That file is already in the queue.");
            return;
        }

        var item = new DownloadItem
        {
            RemotePath = remotePath,
            LocalPath = Path.Combine(_saveToTextBox.Text, Path.GetFileName(remotePath))
        };

        _items.Add(item);

        var listItem = new ListViewItem(item.FileName);
        listItem.SubItems.Add(item.RemotePath);
        listItem.SubItems.Add(item.StatusText);
        listItem.Tag = item;
        _filesListView.Items.Add(listItem);

        _remotePathTextBox.Clear();
        SetStatus($"Added {item.FileName} to the queue.");
    }

    private async void BrowseFtpDirectoryAsync(object? sender, EventArgs e)
    {
        if (!ValidateConnectionInputs())
        {
            return;
        }

        var remotePath = NormalizeRemotePath(_remotePathTextBox.Text);
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            remotePath = "/";
        }

        try
        {
            SetStatus("Loading FTP directory...");
            var service = BuildService();
            var files = await service.ListDirectoryAsync(remotePath, CancellationToken.None);

            using var dialog = new FtpBrowseDialog(files, remotePath);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in dialog.SelectedFiles)
                {
                    AddRemotePathFromBrowser(file);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to list the FTP directory.\n\n{ex.Message}", "FTP error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Listing failed.");
        }
        finally
        {
            if (_cancellationTokenSource is null)
            {
                SetStatus("Ready");
            }
        }
    }

    private async void StartDownloadAsync(object? sender, EventArgs e)
    {
        if (!ValidateDownloadInputs())
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;
        ToggleRunningState(true);

        _overallProgressBar.Value = 0;
        _overallProgressBar.Maximum = _items.Count;
        var completedCount = 0;
        var delaySeconds = _delayTrackBar.Value;
        var service = BuildService();
        var cancelled = false;

        try
        {
            foreach (var item in _items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                item.Status = DownloadStatus.Downloading;
                item.Progress = 0;
                item.ErrorMessage = string.Empty;
                RefreshItem(item);
                SetStatus($"Downloading {item.FileName} ({completedCount + 1}/{_items.Count})");

                var progress = new Progress<int>(value =>
                {
                    item.Progress = value;
                    RefreshItem(item);
                });

                try
                {
                    await service.DownloadFileAsync(item, progress, cancellationToken);
                    item.Status = DownloadStatus.Done;
                }
                catch (OperationCanceledException)
                {
                    item.Status = DownloadStatus.Waiting;
                    cancelled = true;
                    RefreshItem(item);
                    break;
                }
                catch (Exception ex)
                {
                    item.Status = DownloadStatus.Failed;
                    item.ErrorMessage = ex.Message;
                }

                RefreshItem(item);
                completedCount++;
                _overallProgressBar.Value = completedCount;

                if (completedCount < _items.Count && delaySeconds > 0)
                {
                    SetStatus($"Waiting {delaySeconds} second{(delaySeconds == 1 ? string.Empty : "s")} before the next file...");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                        break;
                    }
                }
            }

            SetStatus(cancelled
                ? $"Download stopped. {completedCount} of {_items.Count} files completed."
                : $"Finished. {completedCount} of {_items.Count} files processed.");
        }
        finally
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            ToggleRunningState(false);
        }
    }

    private void StopDownload(object? sender, EventArgs e)
    {
        _stopButton.Enabled = false;
        _cancellationTokenSource?.Cancel();
        SetStatus("Stopping download...");
    }

    private void RemoveSelectedItems(object? sender, EventArgs e)
    {
        foreach (ListViewItem listItem in _filesListView.SelectedItems)
        {
            if (listItem.Tag is DownloadItem downloadItem)
            {
                _items.Remove(downloadItem);
            }

            _filesListView.Items.Remove(listItem);
        }

        SetStatus("Removed selected items.");
    }

    private void ClearItems(object? sender, EventArgs e)
    {
        _items.Clear();
        _filesListView.Items.Clear();
        SetStatus("Queue cleared.");
    }

    private void AddRemotePathFromBrowser(string remotePath)
    {
        _remotePathTextBox.Text = remotePath;
        AddRemotePath(this, EventArgs.Empty);
    }

    private bool ValidateConnectionInputs()
    {
        if (!string.IsNullOrWhiteSpace(_hostTextBox.Text))
        {
            return true;
        }

        MessageBox.Show("Enter the FTP host before browsing the server.", "Missing host", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private bool ValidateDownloadInputs()
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Add at least one file to the queue.", "Empty queue", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (!ValidateConnectionInputs())
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_saveToTextBox.Text))
        {
            MessageBox.Show("Choose a local folder for downloaded files.", "Missing folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        Directory.CreateDirectory(_saveToTextBox.Text);
        return true;
    }

    private FtpDownloadService BuildService()
        => new(_hostTextBox.Text.Trim(), _userTextBox.Text.Trim(), _passwordTextBox.Text, _sslCheckBox.Checked);

    private void RefreshItem(DownloadItem item)
    {
        if (InvokeRequired)
        {
            Invoke(() => RefreshItem(item));
            return;
        }

        foreach (ListViewItem listItem in _filesListView.Items)
        {
            if (!ReferenceEquals(listItem.Tag, item))
            {
                continue;
            }

            listItem.SubItems[2].Text = item.StatusText;
            listItem.ForeColor = item.Status switch
            {
                DownloadStatus.Done => Color.DarkGreen,
                DownloadStatus.Failed => Color.DarkRed,
                DownloadStatus.Downloading => Color.DarkBlue,
                _ => SystemColors.WindowText
            };
            break;
        }
    }

    private void SetStatus(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => SetStatus(message));
            return;
        }

        _statusLabel.Text = message;
    }

    private void ToggleRunningState(bool isRunning)
    {
        _startButton.Enabled = !isRunning;
        _stopButton.Enabled = isRunning;
        _browseFtpButton.Enabled = !isRunning;
        _addButton.Enabled = !isRunning;
        _removeButton.Enabled = !isRunning;
        _clearButton.Enabled = !isRunning;
    }

    private static string NormalizeRemotePath(string remotePath)
    {
        var value = remotePath.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.StartsWith('/') ? value : "/" + value;
    }
}
