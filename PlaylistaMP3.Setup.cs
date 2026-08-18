using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Instalator Playlista Media")]
[assembly: AssemblyDescription("Instalator aplikacji Playlista Media")]
[assembly: AssemblyCompany("Playlista Media")]
[assembly: AssemblyProduct("Playlista Media")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

namespace PlaylistaMP3Setup
{
    internal static class Product
    {
        internal const string Name = "Playlista Media";
        internal const string Version = "2.0.0";
        internal const string PreviousName = "Playlista MP3";
        internal const string RegistryPath =
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\PlaylistaMP3";
        internal const string ApplicationFileName = "PlaylistaMP3.exe";
        internal const string SetupFileName = "PlaylistaMP3.Setup.exe";

        internal static string DataDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PlaylistaMP3");
            }
        }

        internal static string DefaultInstallDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs", "Playlista Media");
            }
        }

        internal static string InstalledDirectory
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                    {
                        object value = key == null ? null : key.GetValue("InstallLocation");
                        string path = value as string;
                        if (!string.IsNullOrWhiteSpace(path))
                            return path;
                    }
                }
                catch
                {
                }
                return DefaultInstallDirectory;
            }
        }

        internal static string QuoteArgument(string value)
        {
            if (value == null)
                return "\"\"";

            StringBuilder quoted = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char character in value)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (character == '"')
                {
                    quoted.Append('\\', backslashes * 2 + 1);
                    quoted.Append('"');
                }
                else
                {
                    quoted.Append('\\', backslashes);
                    quoted.Append(character);
                }
                backslashes = 0;
            }

            quoted.Append('\\', backslashes * 2);
            quoted.Append('"');
            return quoted.ToString();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length >= 4 &&
                string.Equals(args[0], "/cleanup", StringComparison.OrdinalIgnoreCase))
            {
                int parentProcessId;
                if (int.TryParse(args[1], out parentProcessId))
                    CleanupHelper.Run(parentProcessId, args[2], args[3]);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                MessageBox.Show(
                    "Wystąpił nieoczekiwany błąd instalatora:\n\n" + e.Exception.Message,
                    "Playlista Media — błąd instalatora",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            using (Mutex instanceMutex = new Mutex(false, @"Local\PlaylistaMP3.Setup"))
            {
                bool ownsMutex = false;
                try
                {
                    try
                    {
                        ownsMutex = instanceMutex.WaitOne(0, false);
                    }
                    catch (AbandonedMutexException)
                    {
                        ownsMutex = true;
                    }

                    if (!ownsMutex)
                    {
                        MessageBox.Show(
                            "Inne okno instalatora Playlista Media jest już uruchomione.",
                            "Instalator jest uruchomiony",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        return;
                    }

                    bool uninstall = HasArgument(args, "/uninstall");
                    bool toolsOnly = HasArgument(args, "/tools");
                    if (uninstall)
                        Application.Run(new UninstallForm());
                    else
                        Application.Run(new SetupForm(toolsOnly));
                }
                finally
                {
                    if (ownsMutex)
                        instanceMutex.ReleaseMutex();
                }
            }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            foreach (string argument in args)
            {
                if (string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    internal sealed class SetupForm : Form
    {
        private const string ApplicationResourceName = "PlaylistaMP3.Payload.exe";
        private const string ReadmeResourceName = "PlaylistaMP3.Readme.md";
        private const string LicenseResourceName = "PlaylistaMP3.License.txt";
        private const string ThirdPartyResourceName = "PlaylistaMP3.ThirdParty.md";
        private const string YtDlpUrl =
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
        private const string YtDlpChecksumUrl =
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS";
        private const string FfmpegUrl =
            "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
        private const string FfmpegChecksumUrl =
            "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.sha256";
        private const string DenoUrl =
            "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";
        private const string DenoChecksumUrl =
            "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip.sha256sum";

        private readonly Color navy = Color.FromArgb(15, 23, 42);
        private readonly Color blue = Color.FromArgb(37, 99, 235);
        private readonly Color gray = Color.FromArgb(71, 85, 105);
        private readonly bool toolsOnly;

        private Panel pageHost;
        private Panel introPanel;
        private Panel progressPanel;
        private Panel successPanel;
        private TextBox installPathBox;
        private Button browseButton;
        private CheckBox desktopShortcutCheck;
        private ProgressBar overallProgressBar;
        private Label statusLabel;
        private Label detailLabel;
        private RichTextBox logBox;
        private Label successTitleLabel;
        private Label successDescriptionLabel;
        private CheckBox launchCheckBox;
        private Button primaryButton;
        private Button secondaryButton;

        private CancellationTokenSource cancellation;
        private WebClient activeClient;
        private bool installing;
        private bool completed;
        private bool closeWhenCancelled;
        private string installedApplicationPath;

        internal SetupForm(bool toolsOnly)
        {
            this.toolsOnly = toolsOnly;
            BuildWindow();
            FormClosing += OnFormClosing;
        }

        private void BuildWindow()
        {
            Text = toolsOnly
                ? "Aktualizacja narzędzi — Playlista Media"
                : "Instalacja — Playlista Media";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(760, 610);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;

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
            windowLayout.RowCount = 3;
            windowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            windowLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
            windowLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            windowLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            Controls.Add(windowLayout);

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0);
            header.BackColor = navy;
            windowLayout.Controls.Add(header, 0, 0);

            Label productLabel = new Label();
            productLabel.AutoSize = true;
            productLabel.Text = toolsOnly ? "Aktualizacja narzędzi" : "Playlista Media";
            productLabel.ForeColor = Color.White;
            productLabel.Font = new Font("Segoe UI Semibold", 23F, FontStyle.Bold);
            productLabel.Location = new Point(31, 17);
            header.Controls.Add(productLabel);

            Label headerDescription = new Label();
            headerDescription.AutoSize = true;
            headerDescription.Text = toolsOnly
                ? "yt-dlp, FFmpeg i środowisko Deno"
                : "Kreator instalacji • wersja " + Product.Version;
            headerDescription.ForeColor = Color.FromArgb(191, 219, 254);
            headerDescription.Font = new Font("Segoe UI", 10F);
            headerDescription.Location = new Point(34, 67);
            header.Controls.Add(headerDescription);

            Panel footer = new Panel();
            footer.Dock = DockStyle.Fill;
            footer.Margin = new Padding(0);
            footer.BackColor = Color.FromArgb(248, 250, 252);
            footer.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.DrawLine(new Pen(Color.FromArgb(226, 232, 240)), 0, 0, footer.Width, 0);
            };
            windowLayout.Controls.Add(footer, 0, 2);

            primaryButton = MakeButton(toolsOnly ? "AKTUALIZUJ" : "ZAINSTALUJ", blue, Color.White);
            primaryButton.Size = new Size(155, 40);
            primaryButton.Location = new Point(574, 16);
            primaryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            primaryButton.Click += PrimaryClicked;
            footer.Controls.Add(primaryButton);

            secondaryButton = MakeButton("Anuluj", Color.FromArgb(226, 232, 240), navy);
            secondaryButton.Size = new Size(112, 40);
            secondaryButton.Location = new Point(446, 16);
            secondaryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            secondaryButton.Click += SecondaryClicked;
            footer.Controls.Add(secondaryButton);

            Label permissionsLabel = new Label();
            permissionsLabel.AutoSize = true;
            permissionsLabel.Text = "Bez uprawnień administratora";
            permissionsLabel.ForeColor = gray;
            permissionsLabel.Location = new Point(30, 28);
            footer.Controls.Add(permissionsLabel);

            pageHost = new Panel();
            pageHost.Dock = DockStyle.Fill;
            pageHost.Margin = new Padding(0);
            pageHost.BackColor = Color.White;
            windowLayout.Controls.Add(pageHost, 0, 1);

            introPanel = new Panel();
            introPanel.Dock = DockStyle.Fill;
            introPanel.Padding = new Padding(30, 24, 30, 20);
            pageHost.Controls.Add(introPanel);
            introPanel.BringToFront();

            BuildIntroPage();
            BuildProgressPage();
            BuildSuccessPage();
        }

        private void BuildIntroPage()
        {
            Label title = new Label();
            title.AutoSize = true;
            title.Text = toolsOnly
                ? "Zaktualizuj składniki pobierające"
                : "Gotowy do instalacji";
            title.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            title.ForeColor = navy;
            title.Location = new Point(30, 22);
            introPanel.Controls.Add(title);

            Label description = new Label();
            description.AutoSize = false;
            description.Size = new Size(690, toolsOnly ? 78 : 56);
            description.Location = new Point(32, 63);
            description.ForeColor = gray;
            description.Font = new Font("Segoe UI", 10F);
            description.Text = toolsOnly
                ? "Instalator pobierze najnowszy yt-dlp. FFmpeg i Deno zostaną pobrane tylko wtedy, gdy ich brakuje. Duże składniki nie są bez potrzeby pobierane ponownie."
                : "Aplikacja zostanie zainstalowana dla bieżącego użytkownika. Przy pierwszej instalacji wymagane narzędzia są pobierane z internetu.";
            introPanel.Controls.Add(description);

            Label folderLabel = new Label();
            folderLabel.AutoSize = true;
            folderLabel.Text = "Folder instalacji";
            folderLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            folderLabel.ForeColor = navy;
            folderLabel.Location = new Point(32, 137);
            folderLabel.Visible = !toolsOnly;
            introPanel.Controls.Add(folderLabel);

            installPathBox = new TextBox();
            installPathBox.Location = new Point(32, 163);
            installPathBox.Size = new Size(545, 29);
            installPathBox.Font = new Font("Segoe UI", 10F);
            installPathBox.Text = Product.InstalledDirectory;
            installPathBox.Visible = !toolsOnly;
            introPanel.Controls.Add(installPathBox);

            browseButton = MakeButton("Wybierz…", Color.FromArgb(226, 232, 240), navy);
            browseButton.Location = new Point(592, 160);
            browseButton.Size = new Size(128, 35);
            browseButton.Click += BrowseClicked;
            browseButton.Visible = !toolsOnly;
            introPanel.Controls.Add(browseButton);

            desktopShortcutCheck = new CheckBox();
            desktopShortcutCheck.AutoSize = true;
            desktopShortcutCheck.Text = "Utwórz skrót na pulpicie";
            desktopShortcutCheck.Checked = true;
            desktopShortcutCheck.Location = new Point(34, 219);
            desktopShortcutCheck.Visible = !toolsOnly;
            introPanel.Controls.Add(desktopShortcutCheck);

            Label componentsTitle = new Label();
            componentsTitle.AutoSize = true;
            componentsTitle.Text = toolsOnly ? "Aktualizowane składniki" : "Instalowane składniki";
            componentsTitle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            componentsTitle.ForeColor = navy;
            componentsTitle.Location = new Point(32, toolsOnly ? 171 : 264);
            introPanel.Controls.Add(componentsTitle);

            Label components = new Label();
            components.AutoSize = false;
            components.Size = new Size(680, 72);
            components.Location = new Point(32, toolsOnly ? 198 : 291);
            components.ForeColor = gray;
            components.Text = toolsOnly
                ? "• yt-dlp — zawsze do najnowszej wersji\n• FFmpeg — tylko jeśli brakuje\n• Deno — tylko jeśli brakuje"
                : "• Playlista Media 2.0.0\n• Audio: MP3, M4A, Opus, FLAC, WAV\n• Wideo: MP4 i WebM\n• yt-dlp, FFmpeg i Deno";
            introPanel.Controls.Add(components);

            Label dataNote = new Label();
            dataNote.AutoSize = false;
            dataNote.Size = new Size(680, 45);
            dataNote.Location = new Point(32, toolsOnly ? 295 : 363);
            dataNote.ForeColor = Color.FromArgb(30, 64, 175);
            dataNote.Text = toolsOnly
                ? "Aktualizacja nie zmienia ustawień ani pobranych plików."
                : "Pobrane pliki będą zapisywane wyłącznie w folderze wybranym w aplikacji.";
            introPanel.Controls.Add(dataNote);
        }

        private void BuildProgressPage()
        {
            progressPanel = new Panel();
            progressPanel.Dock = DockStyle.Fill;
            progressPanel.Visible = false;
            pageHost.Controls.Add(progressPanel);

            Label title = new Label();
            title.AutoSize = true;
            title.Text = toolsOnly ? "Aktualizowanie…" : "Instalowanie…";
            title.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            title.ForeColor = navy;
            title.Location = new Point(30, 22);
            progressPanel.Controls.Add(title);

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(690, 28);
            statusLabel.Location = new Point(32, 68);
            statusLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            statusLabel.ForeColor = navy;
            statusLabel.Text = "Przygotowywanie…";
            progressPanel.Controls.Add(statusLabel);

            detailLabel = new Label();
            detailLabel.AutoSize = false;
            detailLabel.Size = new Size(690, 24);
            detailLabel.Location = new Point(32, 97);
            detailLabel.ForeColor = gray;
            progressPanel.Controls.Add(detailLabel);

            overallProgressBar = new ProgressBar();
            overallProgressBar.Location = new Point(32, 130);
            overallProgressBar.Size = new Size(688, 23);
            overallProgressBar.Style = ProgressBarStyle.Continuous;
            progressPanel.Controls.Add(overallProgressBar);

            logBox = new RichTextBox();
            logBox.Location = new Point(32, 176);
            logBox.Size = new Size(688, 224);
            logBox.ReadOnly = true;
            logBox.BackColor = Color.FromArgb(248, 250, 252);
            logBox.BorderStyle = BorderStyle.FixedSingle;
            logBox.Font = new Font("Consolas", 8.5F);
            logBox.ForeColor = Color.FromArgb(51, 65, 85);
            progressPanel.Controls.Add(logBox);
        }

        private void BuildSuccessPage()
        {
            successPanel = new Panel();
            successPanel.Dock = DockStyle.Fill;
            successPanel.Visible = false;
            pageHost.Controls.Add(successPanel);

            successTitleLabel = new Label();
            successTitleLabel.AutoSize = true;
            successTitleLabel.Text = "Instalacja zakończona";
            successTitleLabel.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            successTitleLabel.ForeColor = Color.FromArgb(21, 128, 61);
            successTitleLabel.Location = new Point(30, 39);
            successPanel.Controls.Add(successTitleLabel);

            successDescriptionLabel = new Label();
            successDescriptionLabel.AutoSize = false;
            successDescriptionLabel.Size = new Size(680, 92);
            successDescriptionLabel.Location = new Point(33, 91);
            successDescriptionLabel.Font = new Font("Segoe UI", 10F);
            successDescriptionLabel.ForeColor = gray;
            successDescriptionLabel.Text = "Playlista Media jest gotowa do użycia.";
            successPanel.Controls.Add(successDescriptionLabel);

            launchCheckBox = new CheckBox();
            launchCheckBox.AutoSize = true;
            launchCheckBox.Text = "Uruchom Playlista Media";
            launchCheckBox.Checked = true;
            launchCheckBox.Location = new Point(35, 207);
            launchCheckBox.Visible = !toolsOnly;
            successPanel.Controls.Add(launchCheckBox);

            Label hint = new Label();
            hint.AutoSize = false;
            hint.Size = new Size(680, 76);
            hint.Location = new Point(33, 264);
            hint.ForeColor = Color.FromArgb(30, 64, 175);
            hint.Text = toolsOnly
                ? "Możesz zamknąć instalator i wrócić do aplikacji."
                : "Program znajdziesz także w menu Start. Odinstalowanie jest dostępne w Ustawieniach Windows → Aplikacje.";
            successPanel.Controls.Add(hint);
        }

        private Button MakeButton(string text, Color background, Color foreground)
        {
            Button button = new Button();
            button.Text = text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = background;
            button.ForeColor = foreground;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private void BrowseClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Wybierz folder instalacji Playlista Media";
                dialog.SelectedPath = installPathBox.Text.Trim();
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    installPathBox.Text = dialog.SelectedPath;
            }
        }

        private async void PrimaryClicked(object sender, EventArgs e)
        {
            if (completed)
            {
                if (!toolsOnly && launchCheckBox.Checked && File.Exists(installedApplicationPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = installedApplicationPath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message, "Nie można uruchomić aplikacji",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                Close();
                return;
            }

            if (installing)
                return;

            string installDirectory = Product.InstalledDirectory;
            if (!toolsOnly)
            {
                try
                {
                    installDirectory = ValidateInstallDirectory(installPathBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Nieprawidłowy folder",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (IsApplicationRunning())
                {
                    MessageBox.Show(this,
                        "Zamknij uruchomioną aplikację Playlista Media i spróbuj ponownie.",
                        "Aplikacja jest uruchomiona", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            installing = true;
            closeWhenCancelled = false;
            bool applicationWasPresent = toolsOnly || File.Exists(
                Path.Combine(installDirectory, Product.ApplicationFileName));
            cancellation = new CancellationTokenSource();
            installPathBox.Enabled = false;
            browseButton.Enabled = false;
            desktopShortcutCheck.Enabled = false;
            primaryButton.Enabled = false;
            secondaryButton.Text = "Anuluj";
            introPanel.Visible = false;
            successPanel.Visible = false;
            progressPanel.Visible = true;
            progressPanel.BringToFront();
            logBox.Clear();
            overallProgressBar.Value = 0;

            try
            {
                await InstallAsync(installDirectory, cancellation.Token);
                ShowSuccess(installDirectory);
            }
            catch (OperationCanceledException)
            {
                if (!applicationWasPresent)
                    CleanupPartialInstallation(installDirectory);
                if (!closeWhenCancelled)
                    ShowFailure("Operacja została anulowana.", false);
            }
            catch (Exception ex)
            {
                if (!applicationWasPresent)
                    CleanupPartialInstallation(installDirectory);
                AppendLog("BŁĄD: " + ex.Message);
                ShowFailure(ex.Message, true);
            }
            finally
            {
                installing = false;
                if (cancellation != null)
                {
                    cancellation.Dispose();
                    cancellation = null;
                }
                activeClient = null;

                if (closeWhenCancelled && !completed)
                {
                    closeWhenCancelled = false;
                    Close();
                }
            }
        }

        private void SecondaryClicked(object sender, EventArgs e)
        {
            if (installing)
            {
                if (MessageBox.Show(this, "Anulować trwającą operację?", "Anulowanie",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    RequestCancellation();
                return;
            }
            Close();
        }

        private void RequestCancellation()
        {
            if (cancellation == null || cancellation.IsCancellationRequested)
                return;

            statusLabel.Text = "Anulowanie…";
            detailLabel.Text = "Kończenie bieżącej operacji";
            cancellation.Cancel();
            try
            {
                if (activeClient != null)
                    activeClient.CancelAsync();
            }
            catch
            {
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!installing)
                return;

            if (MessageBox.Show(this, "Instalacja nadal trwa. Anulować ją i zamknąć okno?",
                "Instalacja w toku", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                e.Cancel = true;
                closeWhenCancelled = true;
                RequestCancellation();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private async Task InstallAsync(string installDirectory, CancellationToken token)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string temporaryDirectory = Path.Combine(Path.GetTempPath(),
                "PlaylistaMP3-Setup-" + Guid.NewGuid().ToString("N"));
            string dataDirectory = Product.DataDirectory;
            string toolsDirectory = Path.Combine(dataDirectory, "tools");
            string ffmpegDirectory = Path.Combine(toolsDirectory, "ffmpeg");

            Directory.CreateDirectory(temporaryDirectory);
            Directory.CreateDirectory(toolsDirectory);
            Directory.CreateDirectory(ffmpegDirectory);

            try
            {
                if (!toolsOnly)
                {
                    SetProgress(3, "Instalowanie aplikacji", installDirectory);
                    AppendLog("Instalowanie Playlista Media " + Product.Version);
                    InstallApplicationFiles(installDirectory, token);
                    installedApplicationPath = Path.Combine(installDirectory,
                        Product.ApplicationFileName);
                }

                token.ThrowIfCancellationRequested();
                string ytDlpTemporary = Path.Combine(temporaryDirectory, "yt-dlp.exe");
                SetProgress(8, "Pobieranie najnowszego yt-dlp", "Łączenie z serwerem…");
                AppendLog("Pobieranie najnowszego yt-dlp…");
                await DownloadWithRetryAsync(YtDlpUrl, ytDlpTemporary, 8, 18, token);
                string ytDlpChecksum = Path.Combine(temporaryDirectory, "yt-dlp.sha256");
                SetProgress(19, "Weryfikowanie yt-dlp", "Sprawdzanie sumy SHA-256");
                await DownloadWithRetryAsync(YtDlpChecksumUrl, ytDlpChecksum, 19, 20, token, 32);
                await Task.Run(delegate
                {
                    token.ThrowIfCancellationRequested();
                    VerifySha256(ytDlpTemporary, ytDlpChecksum, "yt-dlp.exe");
                }, token);
                AssertPortableExecutable(ytDlpTemporary);
                CopyFileSafely(ytDlpTemporary, Path.Combine(toolsDirectory, "yt-dlp.exe"));
                AppendLog("yt-dlp: gotowe");

                string ffmpegTarget = Path.Combine(ffmpegDirectory, "ffmpeg.exe");
                string ffprobeTarget = Path.Combine(ffmpegDirectory, "ffprobe.exe");
                if (!File.Exists(ffmpegTarget) || !File.Exists(ffprobeTarget))
                {
                    string ffmpegArchive = Path.Combine(temporaryDirectory, "ffmpeg.zip");
                    string ffmpegExpanded = Path.Combine(temporaryDirectory, "ffmpeg-expanded");
                    SetProgress(28, "Pobieranie FFmpeg", "Największy składnik — pobierany jednorazowo");
                    AppendLog("Pobieranie FFmpeg…");
                    await DownloadWithRetryAsync(FfmpegUrl, ffmpegArchive, 28, 52, token);
                    string ffmpegChecksum = Path.Combine(temporaryDirectory, "ffmpeg.sha256");
                    SetProgress(53, "Weryfikowanie FFmpeg", "Sprawdzanie sumy SHA-256");
                    await DownloadWithRetryAsync(FfmpegChecksumUrl, ffmpegChecksum, 53, 54, token, 32);
                    await Task.Run(delegate
                    {
                        token.ThrowIfCancellationRequested();
                        VerifySha256(ffmpegArchive, ffmpegChecksum,
                            "ffmpeg-release-essentials.zip");
                    }, token);

                    SetProgress(56, "Rozpakowywanie FFmpeg", "To może potrwać kilkadziesiąt sekund");
                    AppendLog("Rozpakowywanie FFmpeg…");
                    await Task.Run(delegate
                    {
                        token.ThrowIfCancellationRequested();
                        Directory.CreateDirectory(ffmpegExpanded);
                        ZipFile.ExtractToDirectory(ffmpegArchive, ffmpegExpanded);
                    }, token);

                    string[] ffmpegFiles = Directory.GetFiles(ffmpegExpanded,
                        "ffmpeg.exe", SearchOption.AllDirectories);
                    string[] ffprobeFiles = Directory.GetFiles(ffmpegExpanded,
                        "ffprobe.exe", SearchOption.AllDirectories);
                    if (ffmpegFiles.Length == 0 || ffprobeFiles.Length == 0)
                        throw new InvalidDataException("Archiwum FFmpeg nie zawiera wymaganych plików.");

                    AssertPortableExecutable(ffmpegFiles[0]);
                    AssertPortableExecutable(ffprobeFiles[0]);
                    await Task.Run(delegate
                    {
                        token.ThrowIfCancellationRequested();
                        CopyFileSafely(ffmpegFiles[0], ffmpegTarget);
                        CopyFileSafely(ffprobeFiles[0], ffprobeTarget);
                    }, token);
                    AppendLog("FFmpeg: gotowe");
                }
                else
                {
                    SetProgress(63, "FFmpeg jest już zainstalowany", "Pomijanie dużego pobierania");
                    AppendLog("FFmpeg: już zainstalowany — pominięto pobieranie");
                }

                token.ThrowIfCancellationRequested();
                string denoTarget = Path.Combine(toolsDirectory, "deno.exe");
                if (!File.Exists(denoTarget))
                {
                    string denoArchive = Path.Combine(temporaryDirectory, "deno.zip");
                    string denoExpanded = Path.Combine(temporaryDirectory, "deno-expanded");
                    SetProgress(66, "Pobieranie środowiska Deno", "Składnik wymagany przez aktualne YouTube");
                    AppendLog("Pobieranie Deno…");
                    await DownloadWithRetryAsync(DenoUrl, denoArchive, 66, 83, token);
                    string denoChecksum = Path.Combine(temporaryDirectory, "deno.sha256");
                    SetProgress(84, "Weryfikowanie Deno", "Sprawdzanie sumy SHA-256");
                    await DownloadWithRetryAsync(DenoChecksumUrl, denoChecksum, 84, 85, token, 32);
                    await Task.Run(delegate
                    {
                        token.ThrowIfCancellationRequested();
                        VerifySha256(denoArchive, denoChecksum,
                            "deno-x86_64-pc-windows-msvc.zip");
                    }, token);

                    SetProgress(87, "Rozpakowywanie Deno", string.Empty);
                    await Task.Run(delegate
                    {
                        token.ThrowIfCancellationRequested();
                        Directory.CreateDirectory(denoExpanded);
                        ZipFile.ExtractToDirectory(denoArchive, denoExpanded);
                    }, token);

                    string[] denoFiles = Directory.GetFiles(denoExpanded,
                        "deno.exe", SearchOption.AllDirectories);
                    if (denoFiles.Length == 0)
                        throw new InvalidDataException("Archiwum Deno nie zawiera pliku deno.exe.");
                    AssertPortableExecutable(denoFiles[0]);
                    CopyFileSafely(denoFiles[0], denoTarget);
                    AppendLog("Deno: gotowe");
                }
                else
                {
                    SetProgress(90, "Deno jest już zainstalowane", "Pomijanie pobierania");
                    AppendLog("Deno: już zainstalowane — pominięto pobieranie");
                }

                token.ThrowIfCancellationRequested();
                if (!toolsOnly)
                {
                    SetProgress(94, "Tworzenie skrótów i wpisu odinstalowania", string.Empty);
                    CreateShortcuts(installDirectory, desktopShortcutCheck.Checked);
                    WriteUninstallInformation(installDirectory);
                    AppendLog("Skróty i moduł odinstalowania: gotowe");
                }

                SetProgress(100, toolsOnly ? "Aktualizacja zakończona" : "Instalacja zakończona",
                    "Wszystkie składniki są gotowe");
            }
            finally
            {
                SafeDeleteDirectory(temporaryDirectory);
            }
        }

        private void InstallApplicationFiles(string installDirectory, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(installDirectory);

            WriteEmbeddedResource(ApplicationResourceName,
                Path.Combine(installDirectory, Product.ApplicationFileName));
            WriteEmbeddedResource(ReadmeResourceName,
                Path.Combine(installDirectory, "README.md"));
            WriteEmbeddedResource(LicenseResourceName,
                Path.Combine(installDirectory, "LICENSE.txt"));
            WriteEmbeddedResource(ThirdPartyResourceName,
                Path.Combine(installDirectory, "THIRD_PARTY_NOTICES.md"));

            string setupTarget = Path.Combine(installDirectory, Product.SetupFileName);
            string setupSource = Path.GetFullPath(Application.ExecutablePath);
            if (!string.Equals(setupSource, Path.GetFullPath(setupTarget),
                StringComparison.OrdinalIgnoreCase))
                CopyFileSafely(setupSource, setupTarget);

            File.WriteAllText(Path.Combine(installDirectory, "install-manifest.txt"),
                Product.ApplicationFileName + "\r\n" + Product.SetupFileName +
                "\r\nREADME.md\r\nLICENSE.txt\r\nTHIRD_PARTY_NOTICES.md" +
                "\r\ninstall-manifest.txt\r\n",
                new UTF8Encoding(false));
        }

        private static void WriteEmbeddedResource(string resourceName, string targetPath)
        {
            using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (input == null)
                    throw new InvalidOperationException("Brakuje składnika instalatora: " + resourceName);

                string temporaryTarget = targetPath + ".new";
                using (FileStream output = new FileStream(temporaryTarget,
                    FileMode.Create, FileAccess.Write, FileShare.None))
                    input.CopyTo(output);

                try
                {
                    File.Copy(temporaryTarget, targetPath, true);
                }
                finally
                {
                    SafeDeleteFile(temporaryTarget);
                }
            }
        }

        private async Task DownloadWithRetryAsync(string uri, string target,
            int progressStart, int progressEnd, CancellationToken token,
            int minimumBytes = 1024)
        {
            Exception lastError = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                token.ThrowIfCancellationRequested();
                SafeDeleteFile(target);

                using (WebClient client = new WebClient())
                {
                    activeClient = client;
                    client.Headers.Add(HttpRequestHeader.UserAgent,
                        "PlaylistaMP3-Setup/" + Product.Version);
                    client.DownloadProgressChanged += delegate(object sender,
                        DownloadProgressChangedEventArgs e)
                    {
                        int value = progressStart +
                            ((progressEnd - progressStart) * e.ProgressPercentage / 100);
                        string transfer = FormatBytes(e.BytesReceived);
                        if (e.TotalBytesToReceive > 0)
                            transfer += " / " + FormatBytes(e.TotalBytesToReceive);
                        SetProgress(value, null, e.ProgressPercentage + "% • " + transfer);
                    };

                    try
                    {
                        await client.DownloadFileTaskAsync(new Uri(uri), target);
                        if (new FileInfo(target).Length < minimumBytes)
                            throw new InvalidDataException("Pobrany plik jest pusty albo niekompletny.");
                        activeClient = null;
                        return;
                    }
                    catch (Exception ex)
                    {
                        activeClient = null;
                        if (token.IsCancellationRequested)
                            throw new OperationCanceledException(token);
                        lastError = ex;
                        AppendLog("Próba " + attempt + "/3 nie powiodła się: " + ex.Message);
                    }
                }

                if (attempt < 3)
                    await Task.Delay(1500 * attempt, token);
            }

            throw new InvalidOperationException(
                "Nie udało się pobrać wymaganego składnika po trzech próbach.", lastError);
        }

        private void ShowSuccess(string installDirectory)
        {
            completed = true;
            installedApplicationPath = Path.Combine(installDirectory, Product.ApplicationFileName);
            progressPanel.Visible = false;
            introPanel.Visible = false;
            successPanel.Visible = true;
            successPanel.BringToFront();
            successTitleLabel.Text = toolsOnly
                ? "Aktualizacja zakończona"
                : "Instalacja zakończona";
            successDescriptionLabel.Text = toolsOnly
                ? "Narzędzia używane do pobierania zostały przygotowane."
                : "Playlista Media " + Product.Version + " została zainstalowana w:\n" + installDirectory;
            primaryButton.Enabled = true;
            primaryButton.Text = toolsOnly ? "ZAMKNIJ" : "ZAKOŃCZ";
            secondaryButton.Visible = false;
        }

        private void ShowFailure(string message, bool error)
        {
            progressPanel.Visible = false;
            successPanel.Visible = false;
            introPanel.Visible = true;
            introPanel.BringToFront();
            installPathBox.Enabled = true;
            browseButton.Enabled = true;
            desktopShortcutCheck.Enabled = true;
            primaryButton.Enabled = true;
            primaryButton.Text = toolsOnly ? "SPRÓBUJ PONOWNIE" : "SPRÓBUJ PONOWNIE";
            secondaryButton.Text = "Zamknij";
            MessageBox.Show(this, message,
                error ? "Instalacja nie powiodła się" : "Operacja anulowana",
                MessageBoxButtons.OK, error ? MessageBoxIcon.Error : MessageBoxIcon.Information);
        }

        private void SetProgress(int value, string status, string detail)
        {
            SafeUi(delegate
            {
                overallProgressBar.Value = Math.Max(0, Math.Min(100, value));
                if (status != null)
                    statusLabel.Text = status;
                if (detail != null)
                    detailLabel.Text = detail;
            });
        }

        private void AppendLog(string line)
        {
            SafeUi(delegate
            {
                logBox.AppendText("• " + line + Environment.NewLine);
                logBox.SelectionStart = logBox.TextLength;
                logBox.ScrollToCaret();
            });
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

        private static string ValidateInstallDirectory(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Wybierz folder instalacji.");

            string fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(input.Trim()));
            string root = Path.GetPathRoot(fullPath);
            if (string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar),
                root == null ? null : root.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Nie można instalować aplikacji bezpośrednio w głównym katalogu dysku.");

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (IsInside(fullPath, programFiles) || IsInside(fullPath, programFilesX86))
                throw new ArgumentException(
                    "Ten folder zwykle wymaga uprawnień administratora. Wybierz folder w swoim profilu użytkownika.");

            if (IsInside(fullPath, Product.DataDirectory))
                throw new ArgumentException(
                    "Wybierz inny folder. Ten katalog jest zarezerwowany na narzędzia i ustawienia aplikacji.");

            return fullPath;
        }

        private static bool IsInside(string path, string parent)
        {
            if (string.IsNullOrEmpty(parent))
                return false;
            string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            string normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            return normalizedPath.StartsWith(normalizedParent, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsApplicationRunning()
        {
            Process[] processes = Process.GetProcessesByName("PlaylistaMP3");
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
        }

        private static void AssertPortableExecutable(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.ReadByte() != 0x4D || stream.ReadByte() != 0x5A)
                    throw new InvalidDataException("Pobrany plik nie jest prawidłowym programem Windows.");
            }
        }

        private static void VerifySha256(string filePath, string checksumPath,
            string expectedFileName)
        {
            string expectedHash = null;
            foreach (string rawLine in File.ReadAllLines(checksumPath))
            {
                string line = rawLine.Trim();
                if (line.Length < 64)
                    continue;

                string candidate = line.Substring(0, 64);
                bool hexadecimal = true;
                foreach (char character in candidate)
                {
                    if (!((character >= '0' && character <= '9') ||
                          (character >= 'a' && character <= 'f') ||
                          (character >= 'A' && character <= 'F')))
                    {
                        hexadecimal = false;
                        break;
                    }
                }

                if (hexadecimal &&
                    (line.Length == 64 || line.IndexOf(expectedFileName,
                        StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    expectedHash = candidate.ToLowerInvariant();
                    break;
                }
            }

            if (expectedHash == null)
                throw new InvalidDataException(
                    "Nie udało się odczytać oficjalnej sumy kontrolnej dla " + expectedFileName + ".");

            string actualHash;
            using (SHA256 sha = SHA256.Create())
            using (FileStream input = File.OpenRead(filePath))
                actualHash = BitConverter.ToString(sha.ComputeHash(input))
                    .Replace("-", string.Empty).ToLowerInvariant();

            if (!string.Equals(actualHash, expectedHash, StringComparison.Ordinal))
                throw new InvalidDataException(
                    "Suma kontrolna pobranego pliku " + expectedFileName + " jest nieprawidłowa.");
        }

        private static void CopyFileSafely(string source, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string temporaryDestination = destination + ".new";
            try
            {
                File.Copy(source, temporaryDestination, true);
                File.Copy(temporaryDestination, destination, true);
            }
            finally
            {
                SafeDeleteFile(temporaryDestination);
            }
        }

        private static void CreateShortcuts(string installDirectory, bool desktopShortcut)
        {
            string applicationPath = Path.Combine(installDirectory, Product.ApplicationFileName);
            string setupPath = Path.Combine(installDirectory, Product.SetupFileName);
            string programsDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string desktopDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // Aktualizacja z wersji 1.x: usuń skróty pod poprzednią nazwą produktu.
            SafeDeleteDirectory(Path.Combine(programsDirectory, Product.PreviousName));
            SafeDeleteFile(Path.Combine(desktopDirectory, Product.PreviousName + ".lnk"));

            string startMenuDirectory = Path.Combine(
                programsDirectory, Product.Name);
            Directory.CreateDirectory(startMenuDirectory);

            CreateShortcut(Path.Combine(startMenuDirectory, Product.Name + ".lnk"),
                applicationPath, string.Empty, installDirectory,
                "Pobieranie playlist YouTube do plików audio i wideo");
            CreateShortcut(Path.Combine(startMenuDirectory, "Odinstaluj " + Product.Name + ".lnk"),
                setupPath, "/uninstall", installDirectory,
                "Odinstaluj Playlista Media");

            string desktopLink = Path.Combine(
                desktopDirectory, Product.Name + ".lnk");
            if (desktopShortcut)
                CreateShortcut(desktopLink, applicationPath, string.Empty, installDirectory,
                    "Pobieranie playlist YouTube do plików audio i wideo");
            else
                SafeDeleteFile(desktopLink);
        }

        private static void CleanupPartialInstallation(string installDirectory)
        {
            string[] files =
            {
                Product.ApplicationFileName,
                Product.SetupFileName,
                "README.md",
                "LICENSE.txt",
                "THIRD_PARTY_NOTICES.md",
                "install-manifest.txt"
            };
            foreach (string file in files)
                SafeDeleteFile(Path.Combine(installDirectory, file));

            string desktopLink = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Product.Name + ".lnk");
            SafeDeleteFile(desktopLink);
            SafeDeleteDirectory(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs), Product.Name));
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(Product.RegistryPath, false);
            }
            catch
            {
            }

            try
            {
                if (Directory.Exists(installDirectory) &&
                    Directory.GetFileSystemEntries(installDirectory).Length == 0)
                    Directory.Delete(installDirectory, false);
            }
            catch
            {
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath,
            string arguments, string workingDirectory, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
                throw new InvalidOperationException("System Windows nie udostępnił mechanizmu tworzenia skrótów.");

            object shell = null;
            object shortcut = null;
            try
            {
                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                    null, shell, new object[] { shortcutPath });
                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty,
                    null, shortcut, new object[] { targetPath });
                shortcutType.InvokeMember("Arguments", BindingFlags.SetProperty,
                    null, shortcut, new object[] { arguments });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty,
                    null, shortcut, new object[] { workingDirectory });
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty,
                    null, shortcut, new object[] { description });
                shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty,
                    null, shortcut, new object[] { targetPath + ",0" });
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod,
                    null, shortcut, null);
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                    Marshal.FinalReleaseComObject(shortcut);
                if (shell != null && Marshal.IsComObject(shell))
                    Marshal.FinalReleaseComObject(shell);
            }
        }

        private static void WriteUninstallInformation(string installDirectory)
        {
            string setupPath = Path.Combine(installDirectory, Product.SetupFileName);
            string applicationPath = Path.Combine(installDirectory, Product.ApplicationFileName);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(Product.RegistryPath))
            {
                if (key == null)
                    throw new InvalidOperationException("Nie udało się zapisać informacji o instalacji.");
                key.SetValue("DisplayName", Product.Name, RegistryValueKind.String);
                key.SetValue("DisplayVersion", Product.Version, RegistryValueKind.String);
                key.SetValue("Publisher", "Playlista Media", RegistryValueKind.String);
                key.SetValue("InstallLocation", installDirectory, RegistryValueKind.String);
                key.SetValue("DisplayIcon", applicationPath + ",0", RegistryValueKind.String);
                key.SetValue("UninstallString", Product.QuoteArgument(setupPath) + " /uninstall",
                    RegistryValueKind.String);
                key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"), RegistryValueKind.String);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

                long size = DirectorySize(installDirectory) + DirectorySize(Product.DataDirectory);
                key.SetValue("EstimatedSize", (int)Math.Min(int.MaxValue, size / 1024),
                    RegistryValueKind.DWord);
            }
        }

        private static long DirectorySize(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return 0;
                long total = 0;
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        total += new FileInfo(file).Length;
                    }
                    catch
                    {
                    }
                }
                return total;
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";
            if (bytes < 1024L * 1024L)
                return (bytes / 1024D).ToString("0.0") + " KB";
            if (bytes < 1024L * 1024L * 1024L)
                return (bytes / 1024D / 1024D).ToString("0.0") + " MB";
            return (bytes / 1024D / 1024D / 1024D).ToString("0.00") + " GB";
        }

        internal static void SafeDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        internal static void SafeDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
            }
        }
    }

    internal sealed class UninstallForm : Form
    {
        private readonly Color navy = Color.FromArgb(15, 23, 42);
        private CheckBox removeDataCheck;
        private Button uninstallButton;
        private Button cancelButton;
        private Label statusLabel;
        private ProgressBar progressBar;

        internal UninstallForm()
        {
            Text = "Odinstaluj Playlista Media";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(650, 395);
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 96;
            header.BackColor = navy;
            Controls.Add(header);

            Label title = new Label();
            title.AutoSize = true;
            title.Text = "Odinstaluj Playlista Media";
            title.ForeColor = Color.White;
            title.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            title.Location = new Point(28, 21);
            header.Controls.Add(title);

            Label question = new Label();
            question.AutoSize = false;
            question.Size = new Size(585, 52);
            question.Location = new Point(30, 124);
            question.Font = new Font("Segoe UI", 10F);
            question.Text = "Program, skróty i wpis z listy aplikacji zostaną usunięte. Pobrane przez Ciebie pliki pozostaną bez zmian.";
            Controls.Add(question);

            removeDataCheck = new CheckBox();
            removeDataCheck.AutoSize = true;
            removeDataCheck.Checked = true;
            removeDataCheck.Text = "Usuń także narzędzia, historię i ustawienia aplikacji";
            removeDataCheck.Location = new Point(33, 194);
            Controls.Add(removeDataCheck);

            statusLabel = new Label();
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(580, 25);
            statusLabel.Location = new Point(32, 235);
            statusLabel.ForeColor = Color.FromArgb(71, 85, 105);
            Controls.Add(statusLabel);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(32, 265);
            progressBar.Size = new Size(586, 20);
            progressBar.Visible = false;
            Controls.Add(progressBar);

            uninstallButton = new Button();
            uninstallButton.Text = "ODINSTALUJ";
            uninstallButton.FlatStyle = FlatStyle.Flat;
            uninstallButton.FlatAppearance.BorderSize = 0;
            uninstallButton.BackColor = Color.FromArgb(220, 38, 38);
            uninstallButton.ForeColor = Color.White;
            uninstallButton.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            uninstallButton.Size = new Size(145, 39);
            uninstallButton.Location = new Point(473, 326);
            uninstallButton.Click += UninstallClicked;
            Controls.Add(uninstallButton);

            cancelButton = new Button();
            cancelButton.Text = "Anuluj";
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.BackColor = Color.FromArgb(226, 232, 240);
            cancelButton.ForeColor = navy;
            cancelButton.Size = new Size(110, 39);
            cancelButton.Location = new Point(347, 326);
            cancelButton.Click += delegate { Close(); };
            Controls.Add(cancelButton);
        }

        private void UninstallClicked(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                "Czy na pewno chcesz odinstalować Playlista Media?",
                "Potwierdź odinstalowanie", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (IsApplicationRunning())
            {
                MessageBox.Show(this, "Najpierw zamknij aplikację Playlista Media.",
                    "Aplikacja jest uruchomiona", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            uninstallButton.Enabled = false;
            cancelButton.Enabled = false;
            removeDataCheck.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            statusLabel.Text = "Usuwanie aplikacji…";

            try
            {
                string installDirectory = Product.InstalledDirectory;
                DeleteShortcuts();
                try
                {
                    Registry.CurrentUser.DeleteSubKeyTree(Product.RegistryPath, false);
                }
                catch
                {
                }

                string[] installedFiles =
                {
                    Product.ApplicationFileName,
                    "README.md",
                    "LICENSE.txt",
                    "THIRD_PARTY_NOTICES.md",
                    "install-manifest.txt"
                };
                foreach (string file in installedFiles)
                    SetupForm.SafeDeleteFile(Path.Combine(installDirectory, file));

                if (removeDataCheck.Checked && IsExpectedDataDirectory(Product.DataDirectory))
                    SetupForm.SafeDeleteDirectory(Product.DataDirectory);

                statusLabel.Text = "Odinstalowanie zakończone.";
                MessageBox.Show(this,
                    "Playlista Media została odinstalowana. Pobrane pliki nie zostały usunięte.",
                    "Gotowe", MessageBoxButtons.OK, MessageBoxIcon.Information);
                StartCleanupHelper(installDirectory);
                Close();
            }
            catch (Exception ex)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 0;
                statusLabel.Text = "Odinstalowanie nie powiodło się.";
                uninstallButton.Enabled = true;
                cancelButton.Enabled = true;
                removeDataCheck.Enabled = true;
                MessageBox.Show(this, ex.Message, "Błąd odinstalowania",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void StartCleanupHelper(string installDirectory)
        {
            string helperPath = Path.Combine(Path.GetTempPath(),
                "PlaylistaMP3-Cleanup-" + Guid.NewGuid().ToString("N") + ".exe");
            File.Copy(Application.ExecutablePath, helperPath, true);
            string installedSetupPath = Path.Combine(installDirectory, Product.SetupFileName);
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = helperPath;
            info.Arguments = "/cleanup " + Process.GetCurrentProcess().Id + " " +
                Product.QuoteArgument(installedSetupPath) + " " +
                Product.QuoteArgument(installDirectory);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            Process.Start(info);
        }

        private static void DeleteShortcuts()
        {
            string desktopDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string programsDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            SetupForm.SafeDeleteFile(Path.Combine(desktopDirectory, Product.Name + ".lnk"));
            SetupForm.SafeDeleteFile(Path.Combine(desktopDirectory, Product.PreviousName + ".lnk"));
            SetupForm.SafeDeleteDirectory(Path.Combine(programsDirectory, Product.Name));
            SetupForm.SafeDeleteDirectory(Path.Combine(programsDirectory, Product.PreviousName));
        }

        private static bool IsExpectedDataDirectory(string path)
        {
            try
            {
                return string.Equals(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(Product.DataDirectory).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsApplicationRunning()
        {
            Process[] processes = Process.GetProcessesByName("PlaylistaMP3");
            try
            {
                return processes.Length > 0;
            }
            finally
            {
                foreach (Process process in processes)
                    process.Dispose();
            }
        }
    }

    internal static class CleanupHelper
    {
        private const int MoveFileDelayUntilReboot = 0x4;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existingFileName,
            string newFileName, int flags);

        internal static void Run(int parentProcessId, string installedSetupPath,
            string installDirectory)
        {
            try
            {
                using (Process parent = Process.GetProcessById(parentProcessId))
                    parent.WaitForExit(60000);
            }
            catch
            {
            }

            SetupForm.SafeDeleteFile(installedSetupPath);
            try
            {
                if (Directory.Exists(installDirectory) &&
                    Directory.GetFileSystemEntries(installDirectory).Length == 0)
                    Directory.Delete(installDirectory, false);
            }
            catch
            {
            }

            try
            {
                MoveFileEx(Application.ExecutablePath, null, MoveFileDelayUntilReboot);
            }
            catch
            {
            }
        }
    }
}
