using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Playlista Media")]
[assembly: AssemblyDescription("Pobieranie playlist YouTube w formatach audio i wideo")]
[assembly: AssemblyCompany("Playlista Media")]
[assembly: AssemblyProduct("Playlista Media")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyVersion("2.0.1.0")]
[assembly: AssemblyFileVersion("2.0.1.0")]

namespace PlaylistaMP3
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            bool selfTest = HasArgument(args, "/self-test");
            string logPath = GetStartupLogPath();
            try
            {
                WriteStartupLog(logPath, "START", null);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    WriteStartupLog(logPath, "BŁĄD WĄTKU INTERFEJSU", e.Exception);
                    MessageBox.Show(
                        "Wystąpił nieoczekiwany błąd:\n\n" + e.Exception.Message +
                        "\n\nSzczegóły zapisano w:\n" + logPath,
                        "Playlista Media — błąd",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender,
                    UnhandledExceptionEventArgs e)
                {
                    WriteStartupLog(logPath, "BŁĄD NIEOBSŁUŻONY", e.ExceptionObject as Exception);
                };

                if (selfTest)
                {
                    using (MainForm form = new MainForm())
                    {
                        if (string.IsNullOrWhiteSpace(form.Text))
                            throw new InvalidOperationException("Okno aplikacji nie zostało zainicjalizowane.");
                    }
                    WriteStartupLog(logPath, "SELF-TEST OK", null);
                    return 0;
                }

                Application.Run(new MainForm());
                WriteStartupLog(logPath, "ZAMKNIĘCIE", null);
                return 0;
            }
            catch (Exception ex)
            {
                WriteStartupLog(logPath, selfTest ? "SELF-TEST BŁĄD" : "BŁĄD STARTU", ex);
                if (!selfTest)
                {
                    MessageBox.Show(
                        "Nie udało się uruchomić aplikacji.\n\n" + ex.Message +
                        "\n\nSzczegóły zapisano w:\n" + logPath,
                        "Playlista Media — błąd uruchamiania",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return 1;
            }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            if (args == null)
                return false;
            foreach (string argument in args)
            {
                if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string GetStartupLogPath()
        {
            try
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PlaylistaMP3", "logs", "startup.log");
            }
            catch
            {
                return Path.Combine(Path.GetTempPath(), "PlaylistaMP3-startup.log");
            }
        }

        private static void WriteStartupLog(string path, string eventName, Exception exception)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                if (File.Exists(path) && new FileInfo(path).Length > 512 * 1024)
                    File.Delete(path);

                StringBuilder line = new StringBuilder();
                line.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                line.Append(" | ").Append(eventName);
                line.Append(" | wersja 2.0.1");
                line.Append(" | CLR ").Append(Environment.Version);
                line.Append(" | ").Append(Environment.OSVersion);
                if (exception != null)
                {
                    line.AppendLine();
                    line.Append(exception.ToString());
                }
                line.AppendLine();
                File.AppendAllText(path, line.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }

    internal sealed partial class MainForm : Form
    {
        private readonly Color navy = Color.FromArgb(15, 23, 42);
        private readonly Color blue = Color.FromArgb(37, 99, 235);
        private readonly Color paleBlue = Color.FromArgb(239, 246, 255);
        private readonly Color gray = Color.FromArgb(71, 85, 105);
        private readonly Color lightBorder = Color.FromArgb(203, 213, 225);

        private TextBox urlBox;
        private TextBox folderBox;
        private Button pasteButton;
        private Button browseButton;
        private Button startButton;
        private Button cancelButton;
        private Button openFolderButton;
        private Button updateButton;
        private CheckBox playlistFolderCheck;
        private CheckBox coverCheck;
        private CheckBox archiveCheck;
        private ComboBox browserCombo;
        private ModernProgressBar progressBar;
        private ModernProgressBar trackProgressBar;
        private ComboBox mediaTypeCombo;
        private ComboBox formatCombo;
        private ComboBox qualityCombo;
        private Label qualityHintLabel;
        private Label statusLabel;
        private Label currentLabel;
        private Label overallPercentLabel;
        private Label trackPercentLabel;
        private Label statsLabel;
        private Label toolsStatusLabel;
        private RichTextBox logBox;
        private System.Windows.Forms.Timer elapsedTimer;

        private Process downloadProcess;
        private bool cancellationRequested;
        private int errorCount;
        private int completedCount;
        private string outputFolder;
        private DateTime downloadStartedAt;
        private bool updatingMediaOptions;

        private readonly string dataDirectory;
        private readonly string toolsDirectory;
        private readonly string ytDlpPath;
        private readonly string ffmpegDirectory;
        private readonly string denoPath;
        private readonly string settingsPath;

        public MainForm()
        {
            dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PlaylistaMP3");
            toolsDirectory = Path.Combine(dataDirectory, "tools");
            ytDlpPath = Path.Combine(toolsDirectory, "yt-dlp.exe");
            ffmpegDirectory = Path.Combine(toolsDirectory, "ffmpeg");
            denoPath = Path.Combine(toolsDirectory, "deno.exe");
            settingsPath = Path.Combine(dataDirectory, "ustawienia.txt");

            InitializeModernWindow();
            LoadSettings();
            RefreshToolStatus();
        }

#if LEGACY_UI
        private void InitializeWindow()
        {
            Text = "Playlista Media — 2.0.1";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 880);
            MinimumSize = new Size(780, 840);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            TableLayoutPanel windowLayout = new TableLayoutPanel();
            windowLayout.Dock = DockStyle.Fill;
            windowLayout.Margin = new Padding(0);
            windowLayout.Padding = new Padding(0);
            windowLayout.ColumnCount = 1;
            windowLayout.RowCount = 2;
            windowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            windowLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
            windowLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(windowLayout);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0);
            header.BackColor = navy;
            windowLayout.Controls.Add(header, 0, 0);

            Label title = new Label();
            title.AutoSize = true;
            title.Text = "Playlista  →  MP3";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold, GraphicsUnit.Point);
            title.Location = new Point(28, 15);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.AutoSize = true;
            subtitle.Text = "Pobieranie całej playlisty lub pojedynczego filmu do plików audio i wideo";
            subtitle.ForeColor = Color.FromArgb(191, 219, 254);
            subtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            subtitle.Location = new Point(31, 61);
            header.Controls.Add(subtitle);

            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(0);
            content.BackColor = Color.White;
            windowLayout.Controls.Add(content, 0, 1);

            Label urlLabel = MakeLabel("Link do playlisty YouTube lub filmu", true);
            urlLabel.Location = new Point(30, 22);
            content.Controls.Add(urlLabel);

            urlBox = new TextBox();
            urlBox.Location = new Point(30, 48);
            urlBox.Size = new Size(680, 29);
            urlBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            urlBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            content.Controls.Add(urlBox);

            pasteButton = MakeButton("Wklej link", Color.FromArgb(219, 234, 254), Color.FromArgb(30, 64, 175));
            pasteButton.Location = new Point(725, 45);
            pasteButton.Size = new Size(145, 35);
            pasteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pasteButton.Click += PasteLinkClicked;
            content.Controls.Add(pasteButton);

            Label folderLabel = MakeLabel("Folder docelowy", true);
            folderLabel.Location = new Point(30, 94);
            content.Controls.Add(folderLabel);

            folderBox = new TextBox();
            folderBox.Location = new Point(30, 120);
            folderBox.Size = new Size(680, 29);
            folderBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            folderBox.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            content.Controls.Add(folderBox);

            browseButton = MakeButton("Wybierz folder…", Color.FromArgb(241, 245, 249), navy);
            browseButton.Location = new Point(725, 117);
            browseButton.Size = new Size(145, 35);
            browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browseButton.Click += BrowseClicked;
            content.Controls.Add(browseButton);

            GroupBox options = new GroupBox();
            options.Text = "Ustawienia";
            options.Location = new Point(30, 171);
            options.Size = new Size(840, 142);
            options.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            options.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
            content.Controls.Add(options);

            playlistFolderCheck = new CheckBox();
            playlistFolderCheck.Text = "Utwórz podfolder o nazwie playlisty";
            playlistFolderCheck.Checked = true;
            playlistFolderCheck.AutoSize = true;
            playlistFolderCheck.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            playlistFolderCheck.Location = new Point(18, 29);
            options.Controls.Add(playlistFolderCheck);

            coverCheck = new CheckBox();
            coverCheck.Text = "Osadź miniaturę i metadane w MP3";
            coverCheck.Checked = true;
            coverCheck.AutoSize = true;
            coverCheck.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            coverCheck.Location = new Point(18, 61);
            options.Controls.Add(coverCheck);

            archiveCheck = new CheckBox();
            archiveCheck.Text = "Pomijaj elementy już pobrane w tym profilu";
            archiveCheck.Checked = true;
            archiveCheck.AutoSize = true;
            archiveCheck.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            archiveCheck.Location = new Point(18, 93);
            options.Controls.Add(archiveCheck);

            Label browserLabel = MakeLabel("Logowanie z przeglądarki:", false);
            browserLabel.Location = new Point(454, 31);
            browserLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            options.Controls.Add(browserLabel);

            browserCombo = new ComboBox();
            browserCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            browserCombo.Items.AddRange(new object[]
            {
                "Bez logowania",
                "Microsoft Edge",
                "Google Chrome",
                "Mozilla Firefox"
            });
            browserCombo.SelectedIndex = 0;
            browserCombo.Location = new Point(617, 27);
            browserCombo.Size = new Size(202, 28);
            browserCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browserCombo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            options.Controls.Add(browserCombo);

            Label qualityLabel = MakeLabel("Wybierz format i jakość w panelu wyjściowym", false);
            qualityLabel.ForeColor = gray;
            qualityLabel.Location = new Point(454, 67);
            qualityLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            options.Controls.Add(qualityLabel);

            toolsStatusLabel = MakeLabel("Sprawdzanie narzędzi…", false);
            toolsStatusLabel.Location = new Point(454, 99);
            toolsStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            options.Controls.Add(toolsStatusLabel);

            Label notice = new Label();
            notice.Text = "320 kb/s określa plik wynikowy — konwersja nie poprawi jakości źródła z YouTube. " +
                          "Pobieraj wyłącznie treści, do których masz prawo lub zgodę.";
            notice.ForeColor = gray;
            notice.BackColor = paleBlue;
            notice.BorderStyle = BorderStyle.FixedSingle;
            notice.Location = new Point(30, 326);
            notice.Size = new Size(840, 48);
            notice.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            notice.Padding = new Padding(10, 7, 10, 6);
            content.Controls.Add(notice);

            startButton = MakeButton("POBIERZ MEDIA", blue, Color.White);
            startButton.Location = new Point(30, 394);
            startButton.Size = new Size(205, 43);
            startButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            startButton.Click += StartClicked;
            content.Controls.Add(startButton);

            cancelButton = MakeButton("Anuluj", Color.FromArgb(254, 226, 226), Color.FromArgb(153, 27, 27));
            cancelButton.Location = new Point(246, 394);
            cancelButton.Size = new Size(112, 43);
            cancelButton.Enabled = false;
            cancelButton.Click += CancelClicked;
            content.Controls.Add(cancelButton);

            openFolderButton = MakeButton("Otwórz folder", Color.FromArgb(241, 245, 249), navy);
            openFolderButton.Location = new Point(369, 394);
            openFolderButton.Size = new Size(148, 43);
            openFolderButton.Click += OpenFolderClicked;
            content.Controls.Add(openFolderButton);

            updateButton = MakeButton("Aktualizuj narzędzia", Color.FromArgb(241, 245, 249), navy);
            updateButton.Location = new Point(681, 394);
            updateButton.Size = new Size(189, 43);
            updateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            updateButton.Click += UpdateClicked;
            content.Controls.Add(updateButton);

            Panel progressPanel = new Panel();
            progressPanel.Location = new Point(30, 452);
            progressPanel.Size = new Size(840, 154);
            progressPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressPanel.BackColor = Color.FromArgb(248, 250, 252);
            progressPanel.BorderStyle = BorderStyle.FixedSingle;
            content.Controls.Add(progressPanel);

            statusLabel = MakeLabel("Gotowe do pracy", true);
            statusLabel.AutoSize = false;
            statusLabel.Location = new Point(15, 9);
            statusLabel.Size = new Size(330, 22);
            statusLabel.ForeColor = blue;
            statusLabel.AutoEllipsis = true;
            progressPanel.Controls.Add(statusLabel);

            statsLabel = MakeLabel("Gotowe: 0  •  Błędy: 0  •  Czas: 00:00:00", false);
            statsLabel.AutoSize = false;
            statsLabel.Location = new Point(474, 9);
            statsLabel.Size = new Size(348, 22);
            statsLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            statsLabel.TextAlign = ContentAlignment.MiddleRight;
            statsLabel.ForeColor = gray;
            progressPanel.Controls.Add(statsLabel);

            currentLabel = MakeLabel("Wklej link, wybierz folder i rozpocznij pobieranie.", false);
            currentLabel.AutoSize = false;
            currentLabel.ForeColor = gray;
            currentLabel.Location = new Point(15, 34);
            currentLabel.Size = new Size(807, 22);
            currentLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            currentLabel.AutoEllipsis = true;
            progressPanel.Controls.Add(currentLabel);

            Label overallCaption = MakeLabel("CAŁA PLAYLISTA", true);
            overallCaption.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold, GraphicsUnit.Point);
            overallCaption.Location = new Point(15, 60);
            overallCaption.ForeColor = gray;
            progressPanel.Controls.Add(overallCaption);

            overallPercentLabel = MakeLabel("0%", true);
            overallPercentLabel.AutoSize = false;
            overallPercentLabel.Location = new Point(750, 58);
            overallPercentLabel.Size = new Size(72, 20);
            overallPercentLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            overallPercentLabel.TextAlign = ContentAlignment.MiddleRight;
            progressPanel.Controls.Add(overallPercentLabel);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(15, 80);
            progressBar.Size = new Size(807, 18);
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressPanel.Controls.Add(progressBar);

            Label trackCaption = MakeLabel("AKTUALNY UTWÓR", true);
            trackCaption.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold, GraphicsUnit.Point);
            trackCaption.Location = new Point(15, 106);
            trackCaption.ForeColor = gray;
            progressPanel.Controls.Add(trackCaption);

            trackPercentLabel = MakeLabel("0%", true);
            trackPercentLabel.AutoSize = false;
            trackPercentLabel.Location = new Point(722, 104);
            trackPercentLabel.Size = new Size(100, 20);
            trackPercentLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            trackPercentLabel.TextAlign = ContentAlignment.MiddleRight;
            progressPanel.Controls.Add(trackPercentLabel);

            trackProgressBar = new ProgressBar();
            trackProgressBar.Location = new Point(15, 126);
            trackProgressBar.Size = new Size(807, 14);
            trackProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackProgressBar.Style = ProgressBarStyle.Continuous;
            progressPanel.Controls.Add(trackProgressBar);

            Label logLabel = MakeLabel("Dziennik", true);
            logLabel.Location = new Point(30, 624);
            content.Controls.Add(logLabel);

            logBox = new RichTextBox();
            logBox.Location = new Point(30, 650);
            logBox.Size = new Size(840, 108);
            logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logBox.ReadOnly = true;
            logBox.BackColor = Color.FromArgb(248, 250, 252);
            logBox.BorderStyle = BorderStyle.FixedSingle;
            logBox.Font = new Font("Consolas", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            logBox.DetectUrls = false;
            content.Controls.Add(logBox);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(browserCombo,
                "Wybierz przeglądarkę tylko dla prywatnych lub ograniczonych materiałów. " +
                "W razie błędu zamknij przeglądarkę i spróbuj ponownie.");
            tip.SetToolTip(archiveCheck,
                "Aplikacja zapisuje historię osobno dla każdego profilu pobierania.");

            elapsedTimer = new System.Windows.Forms.Timer();
            elapsedTimer.Interval = 1000;
            elapsedTimer.Tick += delegate { UpdateStatsLabel(); };

            FormClosing += WindowClosing;
        }

        private Label MakeLabel(string text, bool bold)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.ForeColor = navy;
            label.Font = new Font(
                bold ? "Segoe UI Semibold" : "Segoe UI",
                9F,
                bold ? FontStyle.Bold : FontStyle.Regular,
                GraphicsUnit.Point);
            return label;
        }

        private Button MakeButton(string text, Color background, Color foreground)
        {
            Button button = new Button();
            button.Text = text;
            button.BackColor = background;
            button.ForeColor = foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = lightBorder;
            button.FlatAppearance.BorderSize = background == blue ? 0 : 1;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            return button;
        }
#endif

        private void BrowseClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Wybierz folder, w którym mają znaleźć się pobrane pliki";
                dialog.ShowNewFolderButton = true;
                if (Directory.Exists(folderBox.Text))
                    dialog.SelectedPath = folderBox.Text;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    folderBox.Text = dialog.SelectedPath;
            }
        }

        private void PasteLinkClicked(object sender, EventArgs e)
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    MessageBox.Show(this, "Schowek nie zawiera tekstu.", "Brak linku",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                urlBox.Text = Clipboard.GetText().Trim();
                urlBox.Focus();
                urlBox.SelectionStart = urlBox.TextLength;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Nie udało się odczytać schowka:\n" + ex.Message,
                    "Błąd schowka", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartClicked(object sender, EventArgs e)
        {
            if (downloadProcess != null)
                return;

            string url = urlBox.Text.Trim();
            string folder = folderBox.Text.Trim();

            if (!IsYouTubeUrl(url))
            {
                MessageBox.Show(
                    this,
                    "Wklej prawidłowy link z youtube.com albo youtu.be.",
                    "Nieprawidłowy link",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                urlBox.Focus();
                return;
            }

            if (folder.Length == 0)
            {
                MessageBox.Show(this, "Wybierz folder docelowy.", "Brak folderu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Nie można utworzyć folderu:\n" + ex.Message,
                    "Błąd folderu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!ToolsAreReady())
            {
                RefreshToolStatus();
                MessageBox.Show(
                    this,
                    "Brakuje któregoś z wymaganych narzędzi. Kliknij „Aktualizuj narzędzia” " +
                    "albo uruchom ponownie instalator aplikacji.",
                    "Brak narzędzi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            List<string> arguments;
            try
            {
                arguments = BuildArguments(url, folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Nie udało się przygotować nazw plików wynikowych:\n" + ex.Message,
                    "Błąd ustawień", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveSettings();
            outputFolder = folder;
            cancellationRequested = false;
            errorCount = 0;
            completedCount = 0;
            downloadStartedAt = DateTime.Now;
            logBox.Clear();
            progressBar.Value = 0;
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.MarqueeAnimationSpeed = 25;
            trackProgressBar.Style = ProgressBarStyle.Continuous;
            trackProgressBar.Value = 0;
            overallPercentLabel.Text = "analiza…";
            trackPercentLabel.Text = "0%";
            statusLabel.Text = "Odczytywanie playlisty…";
            currentLabel.Text = "YouTube może potrzebować chwili na przygotowanie listy elementów.";
            UpdateStatsLabel();
            elapsedTimer.Start();
            SetDownloadingState(true);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = ytDlpPath;
            startInfo.Arguments = JoinArguments(arguments);
            startInfo.WorkingDirectory = folder;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = new UTF8Encoding(false);
            startInfo.StandardErrorEncoding = new UTF8Encoding(false);
            startInfo.EnvironmentVariables["PATH"] =
                ffmpegDirectory + ";" + toolsDirectory + ";" +
                startInfo.EnvironmentVariables["PATH"];

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += delegate(object outputSender, DataReceivedEventArgs outputEvent)
            {
                if (outputEvent.Data != null)
                    SafeUi(delegate { HandleOutput(outputEvent.Data, false); });
            };
            process.ErrorDataReceived += delegate(object errorSender, DataReceivedEventArgs errorEvent)
            {
                if (errorEvent.Data != null)
                    SafeUi(delegate { HandleOutput(errorEvent.Data, true); });
            };
            process.Exited += delegate
            {
                int exitCode = -1;
                try
                {
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                    Thread.Sleep(120);
                }
                catch
                {
                }

                SafeUi(delegate
                {
                    if (downloadProcess == process)
                        FinishDownload(process, exitCode);
                });
            };

            try
            {
                downloadProcess = process;
                AppendLog("Start pobierania…", false);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                downloadProcess = null;
                process.Dispose();
                SetDownloadingState(false);
                elapsedTimer.Stop();
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                trackProgressBar.Style = ProgressBarStyle.Continuous;
                trackProgressBar.Value = 0;
                overallPercentLabel.Text = "0%";
                trackPercentLabel.Text = "0%";
                UpdateStatsLabel();
                statusLabel.Text = "Nie udało się uruchomić pobierania";
                AppendLog(ex.Message, true);
                MessageBox.Show(this, ex.Message, "Błąd uruchomienia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<string> BuildArguments(string url, string folder)
        {
            string mode = SelectedOptionValue(mediaTypeCombo);
            string format = SelectedOptionValue(formatCombo);
            string quality = SelectedOptionValue(qualityCombo);
            if (mode != "video")
                mode = "audio";

            string fileTemplate = "%(playlist_index,autonumber)03d - %(title)s.%(ext)s";
            string outputTemplate;
            if (playlistFolderCheck.Checked)
            {
                outputTemplate = JoinTemplatePath(
                    folder,
                    "%(playlist_title,playlist_id|Playlista)s",
                    fileTemplate);
            }
            else
            {
                outputTemplate = JoinTemplatePath(folder, fileTemplate);
            }

            List<string> args = new List<string>();
            args.Add("--ignore-config");
            args.Add("--color");
            args.Add("never");
            args.Add("--encoding");
            args.Add("utf-8");
            args.Add("--newline");
            args.Add("--progress");
            args.Add("--progress-delta");
            args.Add("0.3");
            args.Add("--progress-template");
            args.Add("download:__DL__|%(progress._percent_str)s|%(progress._speed_str)s|%(progress._eta_str)s|%(info.playlist_index)s|%(info.playlist_count)s|%(info.title)s");
            args.Add("--progress-template");
            args.Add("postprocess:__PP__|%(info.playlist_index)s|%(info.playlist_count)s|%(info.title)s");
            args.Add("--print");
            args.Add("after_move:__DONE__|%(playlist_index,autonumber)s|%(playlist_count,n_entries|?)s|%(title)s|%(filepath)s");
            args.Add("--no-simulate");
            args.Add("--yes-playlist");
            args.Add("--no-abort-on-error");
            args.Add("--windows-filenames");
            args.Add("--trim-filenames");
            args.Add("180");
            args.Add("--concurrent-fragments");
            args.Add("4");
            args.Add("--retries");
            args.Add("10");
            args.Add("--fragment-retries");
            args.Add("10");
            args.Add("--retry-sleep");
            args.Add("exp=1:10");
            args.Add("--socket-timeout");
            args.Add("30");
            args.Add("--ffmpeg-location");
            args.Add(ffmpegDirectory);
            args.Add("--js-runtimes");
            args.Add("deno:" + denoPath);

            if (mode == "video")
            {
                AddVideoArguments(args, format, quality);
                if (coverCheck.Checked)
                    args.Add("--embed-metadata");
            }
            else
            {
                AddAudioArguments(args, format, quality);
                if (coverCheck.Checked && format != "wav" && format != "original")
                {
                    args.Add("--embed-metadata");
                    args.Add("--embed-thumbnail");
                    args.Add("--convert-thumbnails");
                    args.Add("jpg");
                }
            }

            if (archiveCheck.Checked)
            {
                args.Add("--download-archive");
                string archiveName = "historia_" + mode + "_" + format + "_" + quality + ".txt";
                args.Add(Path.Combine(folder, archiveName));
            }

            string browser = SelectedBrowserArgument();
            if (browser != null)
            {
                args.Add("--cookies-from-browser");
                args.Add(browser);
            }

            args.Add("--output");
            args.Add(outputTemplate);
            args.Add(url);
            return args;
        }

        private static void AddAudioArguments(List<string> args, string format, string quality)
        {
            args.Add("-f");
            args.Add("bestaudio/best");

            if (format == "original")
                return;

            if (format != "m4a" && format != "opus" && format != "flac" &&
                format != "wav")
                format = "mp3";

            args.Add("-x");
            args.Add("--audio-format");
            args.Add(format);

            if (format != "flac" && format != "wav")
            {
                if (quality != "256" && quality != "192" && quality != "128")
                    quality = "320";
                args.Add("--audio-quality");
                args.Add(quality + "K");
            }

            if (format == "mp3")
            {
                args.Add("--postprocessor-args");
                args.Add("ExtractAudio+ffmpeg_o:-id3v2_version 3");
            }
        }

        private static void AddVideoArguments(List<string> args, string format, string quality)
        {
            if (format == "webm")
            {
                args.Add("-f");
                args.Add("bv*[ext=webm]+ba[ext=webm]/b[ext=webm]/bv*+ba/b");
                args.Add("--merge-output-format");
                args.Add("webm");
                args.Add("--remux-video");
                args.Add("webm");
            }
            else
            {
                format = "mp4";
                args.Add("-f");
                args.Add("bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]/bv*+ba/b");
                args.Add("--merge-output-format");
                args.Add("mp4");
                args.Add("--remux-video");
                args.Add("mp4");
            }

            if (quality != "best")
            {
                if (quality != "2160" && quality != "1440" && quality != "1080" &&
                    quality != "720" && quality != "480" && quality != "360")
                    quality = "1080";
                args.Add("--format-sort");
                args.Add("res:" + quality);
            }
        }

        // Szablony yt-dlp mogą zawierać znaki takie jak |, które są poprawne
        // w składni szablonu, ale Path.Combine w .NET Framework odrzuca je,
        // zanim yt-dlp zdąży zamienić pola na bezpieczną nazwę pliku.
        private static string JoinTemplatePath(string basePath, params string[] templateParts)
        {
            string result = basePath.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            foreach (string part in templateParts)
            {
                result += Path.DirectorySeparatorChar;
                result += part;
            }
            return result;
        }

        private string SelectedBrowserArgument()
        {
            switch (browserCombo.SelectedIndex)
            {
                case 1: return "edge";
                case 2: return "chrome";
                case 3: return "firefox";
                default: return null;
            }
        }

        private void HandleOutput(string rawLine, bool fromErrorStream)
        {
            string line = rawLine.TrimEnd();
            if (line.Length == 0)
                return;

            if (line.StartsWith("__DL__|", StringComparison.Ordinal))
            {
                HandleProgress(line);
                return;
            }

            if (line.StartsWith("__PP__|", StringComparison.Ordinal))
            {
                string[] parts = line.Split(new[] { '|' }, 4);
                trackProgressBar.Style = ProgressBarStyle.Marquee;
                trackProgressBar.MarqueeAnimationSpeed = 20;
                trackPercentLabel.Text = "konwersja…";
                statusLabel.Text = "Przetwarzanie i zapisywanie pliku…";
                if (parts.Length >= 4)
                    currentLabel.Text = parts[3];
                return;
            }

            if (line.StartsWith("__DONE__|", StringComparison.Ordinal))
            {
                string[] parts = line.Split(new[] { '|' }, 5);
                completedCount++;
                trackProgressBar.Style = ProgressBarStyle.Continuous;
                trackProgressBar.Value = 100;
                trackPercentLabel.Text = "100%";
                UpdateStatsLabel();
                if (parts.Length >= 4)
                    AppendLog("Gotowe: " + parts[3], false);
                return;
            }

            bool isError = fromErrorStream &&
                           line.IndexOf("ERROR:", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isError)
            {
                errorCount++;
                UpdateStatsLabel();
            }

            if (line.IndexOf("Downloading item", StringComparison.OrdinalIgnoreCase) >= 0)
                statusLabel.Text = "Przygotowywanie kolejnego elementu…";

            AppendLog(line, isError);
        }

        private void HandleProgress(string line)
        {
            string[] parts = line.Split(new[] { '|' }, 7);
            if (parts.Length < 7)
                return;

            double percent = ParsePercent(parts[1]);
            int index = ParseInteger(parts[4]);
            int count = ParseInteger(parts[5]);

            double totalPercent = percent;
            if (index > 0 && count > 0)
                totalPercent = ((index - 1) + percent / 100.0) / count * 100.0;

            int progress = (int)Math.Round(Math.Max(0.0, Math.Min(100.0, totalPercent)));
            if (progressBar.Style != ProgressBarStyle.Continuous)
                progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Value = progress;
            overallPercentLabel.Text = progress.ToString(CultureInfo.InvariantCulture) + "%";

            int trackProgress = (int)Math.Round(Math.Max(0.0, Math.Min(100.0, percent)));
            if (trackProgressBar.Style != ProgressBarStyle.Continuous)
                trackProgressBar.Style = ProgressBarStyle.Continuous;
            trackProgressBar.Value = trackProgress;
            trackPercentLabel.Text = percent.ToString("0.0", CultureInfo.InvariantCulture) + "%";

            string item = index > 0 ? index.ToString(CultureInfo.InvariantCulture) : "?";
            string total = count > 0 ? count.ToString(CultureInfo.InvariantCulture) : "?";
            string speed = NormalizeProgressValue(parts[2]);
            string eta = NormalizeProgressValue(parts[3]);

            statusLabel.Text = "Pobieranie elementu " + item + "/" + total +
                               (speed.Length > 0 ? "  •  " + speed : "") +
                               (eta.Length > 0 ? "  •  pozostało " + eta : "");
            currentLabel.Text = parts[6];
        }

        private static string NormalizeProgressValue(string value)
        {
            string result = value.Trim();
            if (result == "NA" || result == "N/A" || result == "Unknown")
                return string.Empty;
            return result;
        }

        private static double ParsePercent(string value)
        {
            Match match = Regex.Match(value.Replace(',', '.'), @"[0-9]+(?:\.[0-9]+)?");
            double parsed;
            if (match.Success && double.TryParse(match.Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out parsed))
                return parsed;
            return 0.0;
        }

        private static int ParseInteger(string value)
        {
            int parsed;
            return int.TryParse(value.Trim(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
        }

        private void UpdateStatsLabel()
        {
            TimeSpan elapsed = downloadStartedAt == DateTime.MinValue
                ? TimeSpan.Zero
                : DateTime.Now - downloadStartedAt;
            int hours = (int)elapsed.TotalHours;
            string elapsedText = hours.ToString("00", CultureInfo.InvariantCulture) + ":" +
                                 elapsed.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" +
                                 elapsed.Seconds.ToString("00", CultureInfo.InvariantCulture);

            statsLabel.Text = "Gotowe: " + completedCount +
                              "  •  Błędy: " + errorCount +
                              "  •  Czas: " + elapsedText;
        }

        private void FinishDownload(Process process, int exitCode)
        {
            downloadProcess = null;
            SetDownloadingState(false);
            elapsedTimer.Stop();
            UpdateStatsLabel();
            progressBar.Style = ProgressBarStyle.Continuous;
            trackProgressBar.Style = ProgressBarStyle.Continuous;

            if (cancellationRequested)
            {
                statusLabel.Text = "Pobieranie anulowane";
                trackPercentLabel.Text = "anulowano";
                currentLabel.Text = "Nieukończone pliki .part mogą zostać wznowione przy kolejnej próbie.";
                AppendLog("Pobieranie zostało anulowane.", true);
            }
            else if (exitCode == 0 && errorCount == 0)
            {
                progressBar.Value = 100;
                trackProgressBar.Value = 100;
                overallPercentLabel.Text = "100%";
                trackPercentLabel.Text = "100%";
                statusLabel.Text = "Zakończono — zapisano " + completedCount + " plików";
                currentLabel.Text = "Pliki znajdują się w wybranym folderze.";
                AppendLog("Pobieranie zakończone pomyślnie.", false);
                System.Media.SystemSounds.Asterisk.Play();
            }
            else
            {
                statusLabel.Text = "Zakończono z błędami — zapisano " + completedCount + " plików";
                currentLabel.Text = "Część filmów mogła być prywatna, usunięta albo zablokowana. Sprawdź dziennik.";
                AppendLog("Proces zakończył się kodem " + exitCode + ".", true);
                System.Media.SystemSounds.Exclamation.Play();
            }

            try
            {
                process.Dispose();
            }
            catch
            {
            }
        }

        private void CancelClicked(object sender, EventArgs e)
        {
            Process process = downloadProcess;
            if (process == null)
                return;

            cancellationRequested = true;
            cancelButton.Enabled = false;
            statusLabel.Text = "Anulowanie…";
            trackPercentLabel.Text = "anulowanie…";

            try
            {
                int processId = process.Id;
                ProcessStartInfo killInfo = new ProcessStartInfo();
                killInfo.FileName = "taskkill.exe";
                killInfo.Arguments = "/PID " + processId + " /T /F";
                killInfo.UseShellExecute = false;
                killInfo.CreateNoWindow = true;
                Process killer = Process.Start(killInfo);
                if (killer != null)
                    killer.WaitForExit(3000);
            }
            catch
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch
                {
                }
            }
        }

        private void OpenFolderClicked(object sender, EventArgs e)
        {
            string folder = outputFolder;
            if (string.IsNullOrWhiteSpace(folder))
                folder = folderBox.Text.Trim();

            if (!Directory.Exists(folder))
            {
                MessageBox.Show(this, "Wybrany folder jeszcze nie istnieje.", "Brak folderu",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = folder;
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Nie można otworzyć folderu",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateClicked(object sender, EventArgs e)
        {
            if (downloadProcess != null)
            {
                MessageBox.Show(this, "Najpierw zakończ lub anuluj pobieranie.", "Pobieranie trwa",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string updater = Path.Combine(applicationDirectory, "PlaylistaMP3.Setup.exe");
            string updaterArguments = "/tools";

            // Zgodność ze starszą, przenośną paczką ZIP.
            if (!File.Exists(updater))
            {
                updater = Path.Combine(applicationDirectory, "AKTUALIZUJ_NARZEDZIA.cmd");
                updaterArguments = string.Empty;
            }

            if (!File.Exists(updater))
            {
                MessageBox.Show(this,
                    "Nie znaleziono modułu aktualizacji. Uruchom ponownie instalator Playlista Media.",
                    "Brak aktualizatora", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = updater;
                info.Arguments = updaterArguments;
                info.UseShellExecute = true;
                Process updaterProcess = Process.Start(info);
                startButton.Enabled = false;
                updateButton.Enabled = false;
                statusLabel.Text = "Aktualizowanie narzędzi…";
                MessageBox.Show(this,
                    "Uruchomiono moduł aktualizacji. Po zakończeniu wróć do aplikacji.",
                    "Aktualizacja", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (updaterProcess == null)
                {
                    RefreshToolStatus();
                    startButton.Enabled = true;
                    updateButton.Enabled = true;
                    return;
                }

                updaterProcess.EnableRaisingEvents = true;
                updaterProcess.Exited += delegate
                {
                    SafeUi(delegate
                    {
                        RefreshToolStatus();
                        startButton.Enabled = true;
                        updateButton.Enabled = true;
                        statusLabel.Text = ToolsAreReady()
                            ? "Aktualizacja zakończona — gotowe"
                            : "Aktualizacja nie powiodła się";
                        updaterProcess.Dispose();
                    });
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Nie można uruchomić aktualizacji",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetDownloadingState(bool downloading)
        {
            startButton.Enabled = !downloading;
            cancelButton.Enabled = downloading;
            pasteButton.Enabled = !downloading;
            browseButton.Enabled = !downloading;
            updateButton.Enabled = !downloading;
            urlBox.ReadOnly = downloading;
            folderBox.ReadOnly = downloading;
            playlistFolderCheck.Enabled = !downloading;
            coverCheck.Enabled = !downloading;
            archiveCheck.Enabled = !downloading;
            browserCombo.Enabled = !downloading;
            mediaTypeCombo.Enabled = !downloading;
            formatCombo.Enabled = !downloading;
            qualityCombo.Enabled = !downloading;
            if (!downloading)
                UpdateOutputUi();
        }

        private bool ToolsAreReady()
        {
            return File.Exists(ytDlpPath) &&
                   File.Exists(Path.Combine(ffmpegDirectory, "ffmpeg.exe")) &&
                   File.Exists(Path.Combine(ffmpegDirectory, "ffprobe.exe")) &&
                   File.Exists(denoPath);
        }

        private void RefreshToolStatus()
        {
            if (ToolsAreReady())
            {
                toolsStatusLabel.Text = "● Narzędzia gotowe";
                toolsStatusLabel.ForeColor = Color.FromArgb(52, 211, 153);
            }
            else
            {
                toolsStatusLabel.Text = "● Brakuje narzędzi — kliknij aktualizację";
                toolsStatusLabel.ForeColor = Color.FromArgb(251, 113, 133);
            }
        }

        private static bool IsYouTubeUrl(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
                return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            string host = uri.Host.ToLowerInvariant();
            return host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal) ||
                   host == "youtu.be" || host.EndsWith(".youtu.be", StringComparison.Ordinal);
        }

        private void AppendLog(string text, bool error)
        {
            if (logBox.TextLength > 60000)
            {
                logBox.Select(0, 15000);
                logBox.SelectedText = string.Empty;
            }

            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = error
                ? Color.FromArgb(251, 113, 133)
                : Color.FromArgb(181, 194, 219);
            logBox.AppendText(text + Environment.NewLine);
            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }

        private void SafeUi(MethodInvoker action)
        {
            if (IsDisposed || Disposing)
                return;

            try
            {
                if (InvokeRequired)
                    BeginInvoke(action);
                else
                    action();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static string JoinArguments(IEnumerable<string> arguments)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (builder.Length > 0)
                    builder.Append(' ');
                builder.Append(QuoteArgument(argument));
            }
            return builder.ToString();
        }

        // Quoting zgodny z regułami CommandLineToArgvW używanymi przez aplikacje Windows.
        private static string QuoteArgument(string argument)
        {
            if (argument == null || argument.Length == 0)
                return "\"\"";
            if (!Regex.IsMatch(argument, "[\\s\\\"]"))
                return argument;

            StringBuilder result = new StringBuilder();
            result.Append('"');
            int backslashes = 0;
            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (character == '"')
                {
                    result.Append('\\', backslashes * 2 + 1);
                    result.Append('"');
                    backslashes = 0;
                    continue;
                }

                result.Append('\\', backslashes);
                backslashes = 0;
                result.Append(character);
            }
            result.Append('\\', backslashes * 2);
            result.Append('"');
            return result.ToString();
        }

        private void LoadSettings()
        {
            string defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (string.IsNullOrWhiteSpace(defaultFolder))
                defaultFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            folderBox.Text = defaultFolder;

            try
            {
                if (!File.Exists(settingsPath))
                    return;

                string[] lines = File.ReadAllLines(settingsPath, Encoding.UTF8);
                if (lines.Length > 0)
                {
                    string savedFolder = DecodeSetting(lines[0]);
                    if (savedFolder.Length > 0)
                        folderBox.Text = savedFolder;
                }
                if (lines.Length > 1)
                    playlistFolderCheck.Checked = lines[1] == "1";
                if (lines.Length > 2)
                    coverCheck.Checked = lines[2] == "1";
                if (lines.Length > 3)
                    archiveCheck.Checked = lines[3] == "1";
                if (lines.Length > 4)
                {
                    int browserIndex = ParseInteger(lines[4]);
                    if (browserIndex >= 0 && browserIndex < browserCombo.Items.Count)
                        browserCombo.SelectedIndex = browserIndex;
                }
                string savedMode = lines.Length > 5 ? lines[5] : "audio";
                string savedFormat = lines.Length > 6 ? lines[6] : null;
                string savedQuality = lines.Length > 7 ? lines[7] : null;
                SelectOption(mediaTypeCombo, savedMode);
                UpdateFormatOptions(savedFormat, savedQuality);
            }
            catch
            {
            }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(dataDirectory);
                string[] lines = new[]
                {
                    EncodeSetting(folderBox.Text.Trim()),
                    playlistFolderCheck.Checked ? "1" : "0",
                    coverCheck.Checked ? "1" : "0",
                    archiveCheck.Checked ? "1" : "0",
                    browserCombo.SelectedIndex.ToString(CultureInfo.InvariantCulture),
                    SelectedOptionValue(mediaTypeCombo),
                    SelectedOptionValue(formatCombo),
                    SelectedOptionValue(qualityCombo)
                };
                File.WriteAllLines(settingsPath, lines, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static string EncodeSetting(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string DecodeSetting(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private void WindowClosing(object sender, FormClosingEventArgs e)
        {
            if (downloadProcess == null)
            {
                SaveSettings();
                return;
            }

            DialogResult result = MessageBox.Show(
                this,
                "Pobieranie nadal trwa. Anulować je i zamknąć aplikację?",
                "Pobieranie w toku",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            CancelClicked(this, EventArgs.Empty);
        }
    }
}
