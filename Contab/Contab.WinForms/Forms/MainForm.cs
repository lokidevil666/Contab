using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using Contab.WinForms.Models;
using Contab.WinForms.Services;

namespace Contab.WinForms.Forms;

public sealed class MainForm : Form
{
    private readonly LegacyConfigReader _configReader = new();
    private readonly StructureDefinitionReader _structureReader = new();
    private readonly LegacyDataRepository _repository = new();
    private readonly InputFileTransactionReader _fileReader = new();
    private readonly ProcessingService _processing = new();
    private readonly FixedWidthExporter _exporter = new();

    private AppConfig? _loadedConfig;
    private StructureDefinition? _loadedStructure;
    private IReadOnlyList<LegacyTransaction> _transactions = Array.Empty<LegacyTransaction>();

    private readonly TextBox _txtIniPath = new();
    private readonly TextBox _txtStrPath = new();
    private readonly TextBox _txtInputPath = new();
    private readonly TextBox _txtOutputPath = new();
    private readonly TextBox _txtSqlServer = new();
    private readonly TextBox _txtSqlDatabase = new();
    private readonly TextBox _txtSqlUser = new();
    private readonly TextBox _txtSqlPassword = new();
    private readonly TextBox _txtCompanyFilter = new();
    private readonly NumericUpDown _numTake = new();

    private readonly Button _btnLoadConfig = new();
    private readonly Button _btnLoadStructure = new();
    private readonly Button _btnLoadFromDb = new();
    private readonly Button _btnLoadFromFile = new();
    private readonly Button _btnProcess = new();
    private readonly Button _btnTestConnection = new();
    private readonly Button _btnAbout = new();

    private readonly DataGridView _grid = new();
    private readonly RichTextBox _log = new();

    public MainForm()
    {
        Text = "Sage Contab - C# WinForms";
        Width = 1400;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;

        BuildUi();
        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadDefaultPaths();
        AppendLog("Application started.");
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 270));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var settings = BuildSettingsPanel();
        root.Controls.Add(settings, 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 470
        };
        root.Controls.Add(split, 0, 1);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        split.Panel1.Controls.Add(_grid);

        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.Font = new Font("Consolas", 9);
        split.Panel2.Controls.Add(_log);
    }

    private Control BuildSettingsPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 7,
            Padding = new Padding(10)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        for (var i = 0; i < panel.RowCount; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        }

        AddLabeledControl(panel, 0, "INI Path", _txtIniPath, "Structure Path", _txtStrPath);
        AddLabeledControl(panel, 1, "Input File", _txtInputPath, "Output Folder", _txtOutputPath);
        AddLabeledControl(panel, 2, "SQL Server", _txtSqlServer, "Database", _txtSqlDatabase);
        AddLabeledControl(panel, 3, "SQL User", _txtSqlUser, "SQL Password", _txtSqlPassword, true);
        AddLabeledControl(panel, 4, "Company Filter", _txtCompanyFilter, "Take", _numTake);

        _numTake.Minimum = 1;
        _numTake.Maximum = 100000;
        _numTake.Value = 1000;
        _numTake.DecimalPlaces = 0;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        ConfigureButton(_btnLoadConfig, "Load Config");
        ConfigureButton(_btnLoadStructure, "Load Structure");
        ConfigureButton(_btnTestConnection, "Test DB");
        ConfigureButton(_btnLoadFromDb, "Load from DB");
        ConfigureButton(_btnLoadFromFile, "Load from File");
        ConfigureButton(_btnProcess, "Process + Export");
        ConfigureButton(_btnAbout, "About");

        buttons.Controls.AddRange([
            _btnLoadConfig,
            _btnLoadStructure,
            _btnTestConnection,
            _btnLoadFromDb,
            _btnLoadFromFile,
            _btnProcess,
            _btnAbout
        ]);

        panel.Controls.Add(buttons, 0, 6);
        panel.SetColumnSpan(buttons, 6);

        return panel;
    }

    private static void ConfigureButton(Button button, string text)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Height = 28;
        button.Margin = new Padding(0, 0, 8, 0);
    }

    private static void AddLabeledControl(
        TableLayoutPanel panel,
        int row,
        string leftLabel,
        Control leftControl,
        string rightLabel,
        Control rightControl,
        bool rightControlPassword = false)
    {
        var left = new Label
        {
            Text = leftLabel,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left
        };
        panel.Controls.Add(left, 0, row);

        leftControl.Dock = DockStyle.Fill;
        panel.Controls.Add(leftControl, 1, row);

        var right = new Label
        {
            Text = rightLabel,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Anchor = AnchorStyles.Left
        };
        panel.Controls.Add(right, 2, row);

        if (rightControl is TextBox rightTextBox && rightControlPassword)
        {
            rightTextBox.UseSystemPasswordChar = true;
        }

        rightControl.Dock = DockStyle.Fill;
        panel.Controls.Add(rightControl, 3, row);
        panel.SetColumnSpan(rightControl, 3);
    }

    private void WireEvents()
    {
        _btnLoadConfig.Click += (_, _) => LoadConfig();
        _btnLoadStructure.Click += (_, _) => LoadStructure();
        _btnAbout.Click += (_, _) => new AboutForm().ShowDialog(this);
        _btnLoadFromFile.Click += (_, _) => LoadFromFile();
        _btnLoadFromDb.Click += async (_, _) => await LoadFromDatabaseAsync();
        _btnTestConnection.Click += async (_, _) => await TestDatabaseConnectionAsync();
        _btnProcess.Click += (_, _) => ProcessAndExport();
    }

    private void LoadDefaultPaths()
    {
        _txtIniPath.Text = FindCandidate("Contab", "contab.ini") ?? Path.Combine(Environment.CurrentDirectory, "contab.ini");
        _txtStrPath.Text = FindCandidate("Contab", "contab.str") ?? Path.Combine(Environment.CurrentDirectory, "contab.str");
        _txtInputPath.Text = string.Empty;
        _txtOutputPath.Text = Path.Combine(Environment.CurrentDirectory, "out");
    }

    private void LoadConfig()
    {
        try
        {
            _loadedConfig = _configReader.Read(_txtIniPath.Text.Trim());
            _txtSqlServer.Text = _loadedConfig.SqlServer;
            _txtSqlDatabase.Text = _loadedConfig.SqlDatabase;
            _txtSqlUser.Text = _loadedConfig.SqlUser;
            _txtSqlPassword.Text = _loadedConfig.SqlPassword;
            _txtCompanyFilter.Text = _loadedConfig.SocietyView;

            if (!string.IsNullOrWhiteSpace(_loadedConfig.OutputDirectory))
            {
                _txtOutputPath.Text = _loadedConfig.OutputDirectory;
            }

            if (!string.IsNullOrWhiteSpace(_loadedConfig.InputPathOrFilter) &&
                File.Exists(_loadedConfig.InputPathOrFilter))
            {
                _txtInputPath.Text = _loadedConfig.InputPathOrFilter;
            }

            AppendLog($"Config loaded from {_loadedConfig.SourcePath}.");
            AppendLog($"Output format: {_loadedConfig.OutputFormat} | Aggregator: {_loadedConfig.Aggregator}");
        }
        catch (Exception ex)
        {
            ShowError("Failed to load config.", ex);
        }
    }

    private void LoadStructure()
    {
        try
        {
            _loadedStructure = _structureReader.Read(_txtStrPath.Text.Trim());
            AppendLog($"Structure loaded. IN={_loadedStructure.InputFields.Count}, OUT={_loadedStructure.OutputFields.Count}, HDR={_loadedStructure.HeaderFields.Count}");
        }
        catch (Exception ex)
        {
            ShowError("Failed to load structure.", ex);
        }
    }

    private void LoadFromFile()
    {
        try
        {
            if (_loadedStructure is null)
            {
                throw new InvalidOperationException("Load contab.str first.");
            }

            if (string.IsNullOrWhiteSpace(_txtInputPath.Text))
            {
                throw new InvalidOperationException("Input file path is empty.");
            }

            _transactions = _fileReader.Read(_txtInputPath.Text.Trim(), _loadedStructure.InputFields);
            _grid.DataSource = _transactions.ToList();
            AppendLog($"Loaded {_transactions.Count} transactions from file.");
        }
        catch (Exception ex)
        {
            ShowError("Failed to load input file.", ex);
        }
    }

    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            SetBusy(true);
            var config = BuildConfigFromCurrentValues();
            var connectionString = config.BuildConnectionString();

            _transactions = await _repository.LoadPendingTransactionsAsync(
                connectionString,
                (int)_numTake.Value,
                config.UseCashLedger,
                _txtCompanyFilter.Text,
                CancellationToken.None);

            _grid.DataSource = _transactions.ToList();
            AppendLog($"Loaded {_transactions.Count} transactions from database.");
        }
        catch (Exception ex)
        {
            ShowError("Failed to load transactions from database.", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task TestDatabaseConnectionAsync()
    {
        try
        {
            SetBusy(true);
            var config = BuildConfigFromCurrentValues();
            await using var connection = new SqlConnection(config.BuildConnectionString());
            await connection.OpenAsync();
            AppendLog("Database connection successful.");
        }
        catch (Exception ex)
        {
            ShowError("Database connection failed.", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ProcessAndExport()
    {
        try
        {
            if (_loadedStructure is null)
            {
                throw new InvalidOperationException("Load contab.str first.");
            }

            if (_transactions.Count == 0)
            {
                throw new InvalidOperationException("No transactions available. Load from DB or file first.");
            }

            var config = BuildConfigFromCurrentValues();
            var rows = _processing.BuildRows(_transactions, config);
            var outputDirectory = _txtOutputPath.Text.Trim();

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException("Output folder is empty.");
            }

            var outputPath = Path.Combine(outputDirectory, $"CONTAB_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var result = _exporter.Write(outputPath, _loadedStructure, rows);

            _grid.DataSource = rows.ToList();
            AppendLog($"Export completed: {result.FilePath}");
            AppendLog($"Lines={result.LineCount} | Records={result.RecordCount}");
        }
        catch (Exception ex)
        {
            ShowError("Processing failed.", ex);
        }
    }

    private AppConfig BuildConfigFromCurrentValues()
    {
        var baseConfig = _loadedConfig ?? new AppConfig();
        return new AppConfig
        {
            SqlServer = _txtSqlServer.Text.Trim(),
            SqlDatabase = _txtSqlDatabase.Text.Trim(),
            SqlUser = _txtSqlUser.Text.Trim(),
            SqlPassword = _txtSqlPassword.Text,
            SapApplicationServer = baseConfig.SapApplicationServer,
            SapMessageServer = baseConfig.SapMessageServer,
            SapUser = baseConfig.SapUser,
            SapPassword = baseConfig.SapPassword,
            SapLanguage = baseConfig.SapLanguage,
            SapSystemNumber = baseConfig.SapSystemNumber,
            SapSystem = baseConfig.SapSystem,
            SapMandante = baseConfig.SapMandante,
            MarkAsExported = baseConfig.MarkAsExported,
            OutputFormat = baseConfig.OutputFormat,
            OutputDirectory = _txtOutputPath.Text.Trim(),
            UseCashLedger = baseConfig.UseCashLedger,
            ValidateReconciledTable = baseConfig.ValidateReconciledTable,
            InputPathOrFilter = _txtInputPath.Text.Trim(),
            AdministratorsMode = baseConfig.AdministratorsMode,
            UserNameOverride = baseConfig.UserNameOverride,
            CompanyFilter = baseConfig.CompanyFilter,
            SqlSpecificCommand = baseConfig.SqlSpecificCommand,
            ContabException = baseConfig.ContabException,
            Language = baseConfig.Language,
            Layout = baseConfig.Layout,
            KeepSqlForAllRoutines = baseConfig.KeepSqlForAllRoutines,
            Aggregator = baseConfig.Aggregator,
            MaxDaysWithoutRuleUse = baseConfig.MaxDaysWithoutRuleUse,
            DebugEnabled = baseConfig.DebugEnabled,
            HoldingFlag = baseConfig.HoldingFlag,
            SocietyView = _txtCompanyFilter.Text.Trim(),
            ErpDescriptionMode = baseConfig.ErpDescriptionMode,
            SourcePath = baseConfig.SourcePath
        };
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        _btnLoadFromDb.Enabled = !busy;
        _btnTestConnection.Enabled = !busy;
        _btnProcess.Enabled = !busy;
    }

    private void AppendLog(string message)
    {
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _log.ScrollToCaret();
    }

    private void ShowError(string context, Exception ex)
    {
        AppendLog($"{context} {ex.Message}");
        MessageBox.Show($"{context}\n\n{ex.Message}", "Sage Contab", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static string? FindCandidate(string folderName, string fileName)
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        for (var i = 0; i < 8 && current is not null; i++)
        {
            var withFolder = Path.Combine(current.FullName, folderName, fileName);
            if (File.Exists(withFolder))
            {
                return withFolder;
            }

            var direct = Path.Combine(current.FullName, fileName);
            if (File.Exists(direct))
            {
                return direct;
            }

            current = current.Parent;
        }

        return null;
    }
}
