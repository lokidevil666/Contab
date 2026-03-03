using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using Contab.WinForms.Models;
using Contab.WinForms.Services;

namespace Contab.WinForms.Forms;

public sealed class MainForm : Form
{
    // Services used by the form.
    private readonly LegacyConfigReader _configReader = new LegacyConfigReader();
    private readonly StructureDefinitionReader _structureReader = new StructureDefinitionReader();
    private readonly LegacyDataRepository _repository = new LegacyDataRepository();
    private readonly InputFileTransactionReader _fileReader = new InputFileTransactionReader();
    private readonly ProcessingService _processing = new ProcessingService();
    private readonly FixedWidthExporter _exporter = new FixedWidthExporter();

    // In-memory state.
    private AppConfig? _loadedConfig;
    private StructureDefinition? _loadedStructure;
    private List<LegacyTransaction> _transactions = new List<LegacyTransaction>();

    // Input controls.
    private readonly TextBox _txtIniPath = new TextBox();
    private readonly TextBox _txtStrPath = new TextBox();
    private readonly TextBox _txtInputPath = new TextBox();
    private readonly TextBox _txtOutputPath = new TextBox();
    private readonly TextBox _txtSqlServer = new TextBox();
    private readonly TextBox _txtSqlDatabase = new TextBox();
    private readonly TextBox _txtSqlUser = new TextBox();
    private readonly TextBox _txtSqlPassword = new TextBox();
    private readonly TextBox _txtCompanyFilter = new TextBox();
    private readonly NumericUpDown _numTake = new NumericUpDown();

    // Action buttons.
    private readonly Button _btnLoadConfig = new Button();
    private readonly Button _btnLoadStructure = new Button();
    private readonly Button _btnLoadFromDb = new Button();
    private readonly Button _btnLoadFromFile = new Button();
    private readonly Button _btnProcess = new Button();
    private readonly Button _btnTestConnection = new Button();
    private readonly Button _btnAbout = new Button();

    // Output controls.
    private readonly DataGridView _grid = new DataGridView();
    private readonly RichTextBox _log = new RichTextBox();

    public MainForm()
    {
        Text = "Sage Contab - C# WinForms";
        Width = 1380;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;

        BuildSimpleLayout();
        WireEvents();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        SetDefaultPaths();
        Log("Application started.");
    }

    private void BuildSimpleLayout()
    {
        Panel topPanel = BuildTopPanel();
        Controls.Add(topPanel);

        SplitContainer split = new SplitContainer();
        split.Dock = DockStyle.Fill;
        split.Orientation = Orientation.Horizontal;
        split.SplitterDistance = 470;
        Controls.Add(split);

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
        split.Panel1.Controls.Add(_grid);

        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.Font = new Font("Consolas", 9F);
        split.Panel2.Controls.Add(_log);
    }

    private Panel BuildTopPanel()
    {
        Panel panel = new Panel();
        panel.Dock = DockStyle.Top;
        panel.Height = 235;

        int y = 15;
        AddPair(panel, "INI Path", _txtIniPath, "Structure Path", _txtStrPath, y);
        y += 34;
        AddPair(panel, "Input File", _txtInputPath, "Output Folder", _txtOutputPath, y);
        y += 34;
        AddPair(panel, "SQL Server", _txtSqlServer, "Database", _txtSqlDatabase, y);
        y += 34;
        AddPair(panel, "SQL User", _txtSqlUser, "SQL Password", _txtSqlPassword, y);
        _txtSqlPassword.UseSystemPasswordChar = true;
        y += 34;
        AddPair(panel, "Company Filter", _txtCompanyFilter, "Take", _numTake, y);

        _numTake.Minimum = 1;
        _numTake.Maximum = 100000;
        _numTake.Value = 1000;
        _numTake.DecimalPlaces = 0;

        FlowLayoutPanel buttons = new FlowLayoutPanel();
        buttons.Left = 12;
        buttons.Top = 186;
        buttons.Width = 1320;
        buttons.Height = 34;
        buttons.WrapContents = false;

        SetupButton(_btnLoadConfig, "Load Config");
        SetupButton(_btnLoadStructure, "Load Structure");
        SetupButton(_btnTestConnection, "Test DB");
        SetupButton(_btnLoadFromDb, "Load from DB");
        SetupButton(_btnLoadFromFile, "Load from File");
        SetupButton(_btnProcess, "Process + Export");
        SetupButton(_btnAbout, "About");

        buttons.Controls.Add(_btnLoadConfig);
        buttons.Controls.Add(_btnLoadStructure);
        buttons.Controls.Add(_btnTestConnection);
        buttons.Controls.Add(_btnLoadFromDb);
        buttons.Controls.Add(_btnLoadFromFile);
        buttons.Controls.Add(_btnProcess);
        buttons.Controls.Add(_btnAbout);

        panel.Controls.Add(buttons);
        return panel;
    }

    private static void SetupButton(Button button, string text)
    {
        button.Text = text;
        button.Height = 30;
        button.AutoSize = true;
        button.Margin = new Padding(0, 0, 8, 0);
    }

    private static void AddPair(
        Panel panel,
        string leftLabel,
        Control leftControl,
        string rightLabel,
        Control rightControl,
        int y)
    {
        Label lblLeft = new Label();
        lblLeft.Text = leftLabel;
        lblLeft.Left = 12;
        lblLeft.Top = y + 4;
        lblLeft.Width = 95;
        panel.Controls.Add(lblLeft);

        leftControl.Left = 112;
        leftControl.Top = y;
        leftControl.Width = 500;
        panel.Controls.Add(leftControl);

        Label lblRight = new Label();
        lblRight.Text = rightLabel;
        lblRight.Left = 640;
        lblRight.Top = y + 4;
        lblRight.Width = 100;
        panel.Controls.Add(lblRight);

        rightControl.Left = 742;
        rightControl.Top = y;
        rightControl.Width = 300;
        panel.Controls.Add(rightControl);
    }

    private void WireEvents()
    {
        _btnLoadConfig.Click += delegate { LoadConfig(); };
        _btnLoadStructure.Click += delegate { LoadStructure(); };
        _btnLoadFromFile.Click += delegate { LoadTransactionsFromFile(); };
        _btnProcess.Click += delegate { ProcessAndExport(); };
        _btnAbout.Click += delegate { new AboutForm().ShowDialog(this); };

        _btnLoadFromDb.Click += async delegate
        {
            await LoadTransactionsFromDatabaseAsync();
        };

        _btnTestConnection.Click += async delegate
        {
            await TestDatabaseConnectionAsync();
        };
    }

    private void SetDefaultPaths()
    {
        string? ini = FindFileNearCurrentDirectory("Contab", "contab.ini");
        string? str = FindFileNearCurrentDirectory("Contab", "contab.str");

        _txtIniPath.Text = ini ?? Path.Combine(Environment.CurrentDirectory, "contab.ini");
        _txtStrPath.Text = str ?? Path.Combine(Environment.CurrentDirectory, "contab.str");
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

            Log("Config loaded from: " + _loadedConfig.SourcePath);
            Log("Output format: " + _loadedConfig.OutputFormat);
            Log("Aggregator: " + _loadedConfig.Aggregator);
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
            Log("Structure loaded.");
            Log("IN fields: " + _loadedStructure.InputFields.Count);
            Log("OUT fields: " + _loadedStructure.OutputFields.Count);
            Log("HDR fields: " + _loadedStructure.HeaderFields.Count);
        }
        catch (Exception ex)
        {
            ShowError("Failed to load structure.", ex);
        }
    }

    private void LoadTransactionsFromFile()
    {
        try
        {
            if (_loadedStructure == null)
            {
                throw new InvalidOperationException("Load contab.str first.");
            }

            if (string.IsNullOrWhiteSpace(_txtInputPath.Text))
            {
                throw new InvalidOperationException("Input file path is empty.");
            }

            IReadOnlyList<LegacyTransaction> fromFile = _fileReader.Read(
                _txtInputPath.Text.Trim(),
                _loadedStructure.InputFields);

            _transactions = new List<LegacyTransaction>(fromFile);
            _grid.DataSource = _transactions.ToList();
            Log("Loaded " + _transactions.Count + " transactions from file.");
        }
        catch (Exception ex)
        {
            ShowError("Failed to load input file.", ex);
        }
    }

    private async Task LoadTransactionsFromDatabaseAsync()
    {
        try
        {
            SetBusy(true);

            AppConfig config = BuildConfigFromScreen();
            string connectionString = config.BuildConnectionString();

            IReadOnlyList<LegacyTransaction> fromDb = await _repository.LoadPendingTransactionsAsync(
                connectionString,
                (int)_numTake.Value,
                config.UseCashLedger,
                _txtCompanyFilter.Text.Trim(),
                CancellationToken.None);

            _transactions = new List<LegacyTransaction>(fromDb);
            _grid.DataSource = _transactions.ToList();
            Log("Loaded " + _transactions.Count + " transactions from database.");
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
            AppConfig config = BuildConfigFromScreen();

            await using SqlConnection connection = new SqlConnection(config.BuildConnectionString());
            await connection.OpenAsync();
            Log("Database connection successful.");
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
            if (_loadedStructure == null)
            {
                throw new InvalidOperationException("Load contab.str first.");
            }

            if (_transactions.Count == 0)
            {
                throw new InvalidOperationException("Load transactions first.");
            }

            AppConfig config = BuildConfigFromScreen();
            IReadOnlyList<AccountingRow> rows = _processing.BuildRows(_transactions, config);

            string outputFolder = _txtOutputPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                throw new InvalidOperationException("Output folder is empty.");
            }

            string outputFile = Path.Combine(outputFolder, "CONTAB_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
            ExportResult result = _exporter.Write(outputFile, _loadedStructure, rows);

            _grid.DataSource = rows.ToList();
            Log("Export completed: " + result.FilePath);
            Log("Lines: " + result.LineCount + " | Records: " + result.RecordCount);
        }
        catch (Exception ex)
        {
            ShowError("Processing failed.", ex);
        }
    }

    private AppConfig BuildConfigFromScreen()
    {
        AppConfig baseConfig = _loadedConfig ?? new AppConfig();

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

    private void Log(string message)
    {
        _log.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        _log.ScrollToCaret();
    }

    private void ShowError(string context, Exception ex)
    {
        Log(context + " " + ex.Message);
        MessageBox.Show(
            context + Environment.NewLine + Environment.NewLine + ex.Message,
            "Sage Contab",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static string? FindFileNearCurrentDirectory(string folderName, string fileName)
    {
        DirectoryInfo? current = new DirectoryInfo(Environment.CurrentDirectory);

        for (int i = 0; i < 8 && current != null; i++)
        {
            string fileInsideFolder = Path.Combine(current.FullName, folderName, fileName);
            if (File.Exists(fileInsideFolder))
            {
                return fileInsideFolder;
            }

            string fileDirectly = Path.Combine(current.FullName, fileName);
            if (File.Exists(fileDirectly))
            {
                return fileDirectly;
            }

            current = current.Parent;
        }

        return null;
    }
}
