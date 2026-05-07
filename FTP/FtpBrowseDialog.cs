namespace FtpDownloader;

public sealed class FtpBrowseDialog : Form
{
    private readonly string _basePath;
    private CheckedListBox _filesList = null!;

    public List<string> SelectedFiles { get; private set; } = new();

    public FtpBrowseDialog(List<string> files, string basePath)
    {
        _basePath = string.IsNullOrWhiteSpace(basePath) ? "/" : basePath.TrimEnd('/');
        InitializeComponents(files);
    }

    private void InitializeComponents(List<string> files)
    {
        Text = "Browse FTP Directory";
        Size = new Size(560, 460);
        MinimumSize = new Size(420, 340);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5f);

        var infoLabel = new Label
        {
            Left = 10,
            Top = 10,
            Width = 520,
            Height = 20,
            ForeColor = Color.DimGray,
            Text = $"Directory: {_basePath} ({files.Count} items)"
        };

        _filesList = new CheckedListBox
        {
            Left = 10,
            Top = 36,
            Width = 524,
            Height = 330,
            CheckOnClick = true
        };

        foreach (var file in files)
        {
            var fullPath = file.StartsWith('/') ? file : $"{_basePath}/{file}";
            _filesList.Items.Add(fullPath.Replace("//", "/"));
        }

        var selectAllButton = new Button { Text = "Select All", Left = 10, Top = 376, Width = 90, Height = 28 };
        var clearButton = new Button { Text = "Clear", Left = 106, Top = 376, Width = 70, Height = 28 };
        var addButton = new Button
        {
            Text = "Add Selected",
            Left = 350,
            Top = 376,
            Width = 100,
            Height = 28,
            BackColor = Color.FromArgb(34, 139, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        var cancelButton = new Button { Text = "Cancel", Left = 456, Top = 376, Width = 78, Height = 28 };

        selectAllButton.Click += (_, _) => SetAllChecked(true);
        clearButton.Click += (_, _) => SetAllChecked(false);
        addButton.Click += (_, _) => ConfirmSelection();
        cancelButton.Click += (_, _) => DialogResult = DialogResult.Cancel;

        Controls.AddRange(
        [
            infoLabel,
            _filesList,
            selectAllButton,
            clearButton,
            addButton,
            cancelButton
        ]);
    }

    private void SetAllChecked(bool value)
    {
        for (var index = 0; index < _filesList.Items.Count; index++)
        {
            _filesList.SetItemChecked(index, value);
        }
    }

    private void ConfirmSelection()
    {
        SelectedFiles = _filesList.CheckedItems.Cast<string>().ToList();
        if (SelectedFiles.Count == 0)
        {
            MessageBox.Show("Select at least one file.", "No selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult = DialogResult.OK;
    }
}
