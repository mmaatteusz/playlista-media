using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PlaylistaMP3
{
    internal sealed partial class MainForm
    {
        private static readonly Color UiCanvas = Color.FromArgb(8, 13, 26);
        private static readonly Color UiCard = Color.FromArgb(17, 25, 43);
        private static readonly Color UiCardAlt = Color.FromArgb(13, 21, 38);
        private static readonly Color UiBorder = Color.FromArgb(39, 50, 75);
        private static readonly Color UiText = Color.FromArgb(238, 242, 255);
        private static readonly Color UiMuted = Color.FromArgb(148, 163, 184);
        private static readonly Color UiAccent = Color.FromArgb(99, 102, 241);
        private static readonly Color UiAccentLight = Color.FromArgb(56, 189, 248);

        private void InitializeModernWindow()
        {
            Text = "Playlista Media — 2.0.0";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1140, 880);
            MinimumSize = new Size(1040, 850);
            BackColor = UiCanvas;
            ForeColor = UiText;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            DoubleBuffered = true;

            try
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
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
            windowLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            windowLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(windowLayout);

            GradientPanel header = new GradientPanel();
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0);
            header.StartColor = Color.FromArgb(20, 28, 58);
            header.EndColor = Color.FromArgb(39, 31, 82);
            windowLayout.Controls.Add(header, 0, 0);

            MediaLogo logo = new MediaLogo();
            logo.Location = new Point(28, 27);
            logo.Size = new Size(48, 48);
            header.Controls.Add(logo);

            Label title = UiLabel("Playlista Media", 24F, FontStyle.Bold, UiText);
            title.Location = new Point(91, 17);
            header.Controls.Add(title);

            Label versionBadge = UiLabel("2.0", 8.5F, FontStyle.Bold, Color.FromArgb(224, 231, 255));
            versionBadge.BackColor = Color.FromArgb(76, 70, 170);
            versionBadge.Padding = new Padding(9, 4, 9, 4);
            versionBadge.Location = new Point(310, 27);
            header.Controls.Add(versionBadge);

            Label subtitle = UiLabel(
                "Playlisty i pojedyncze materiały → audio lub wideo w wybranej jakości",
                10F, FontStyle.Regular, Color.FromArgb(196, 206, 230));
            subtitle.Location = new Point(94, 65);
            header.Controls.Add(subtitle);

            toolsStatusLabel = UiLabel("Sprawdzanie narzędzi…", 9F,
                FontStyle.Bold, Color.FromArgb(165, 180, 252));
            toolsStatusLabel.AutoSize = false;
            toolsStatusLabel.Size = new Size(330, 25);
            toolsStatusLabel.Location = new Point(770, 21);
            toolsStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            toolsStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            header.Controls.Add(toolsStatusLabel);

            updateButton = UiButton("Aktualizuj narzędzia", ButtonKind.Ghost);
            updateButton.Location = new Point(926, 56);
            updateButton.Size = new Size(174, 36);
            updateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            updateButton.Click += UpdateClicked;
            header.Controls.Add(updateButton);

            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.Margin = new Padding(0);
            content.BackColor = UiCanvas;
            content.AutoScroll = true;
            windowLayout.Controls.Add(content, 0, 1);

            RoundedPanel sourceCard = UiCardPanel();
            sourceCard.Location = new Point(24, 22);
            sourceCard.Size = new Size(700, 208);
            sourceCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            content.Controls.Add(sourceCard);

            sourceCard.Controls.Add(SectionTitle("ŹRÓDŁO I ZAPIS", 20, 17));
            Label urlLabel = UiLabel("Link do playlisty lub filmu YouTube", 9F,
                FontStyle.Bold, UiMuted);
            urlLabel.Location = new Point(20, 49);
            sourceCard.Controls.Add(urlLabel);

            RoundedPanel urlShell = InputShell(new Point(20, 72), new Size(520, 40));
            urlShell.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            sourceCard.Controls.Add(urlShell);
            urlBox = InputTextBox(urlShell);

            pasteButton = UiButton("Wklej link", ButtonKind.Secondary);
            pasteButton.Location = new Point(552, 72);
            pasteButton.Size = new Size(128, 40);
            pasteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pasteButton.Click += PasteLinkClicked;
            sourceCard.Controls.Add(pasteButton);

            Label folderLabel = UiLabel("Folder docelowy", 9F, FontStyle.Bold, UiMuted);
            folderLabel.Location = new Point(20, 126);
            sourceCard.Controls.Add(folderLabel);

            RoundedPanel folderShell = InputShell(new Point(20, 149), new Size(520, 40));
            folderShell.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            sourceCard.Controls.Add(folderShell);
            folderBox = InputTextBox(folderShell);

            browseButton = UiButton("Wybierz…", ButtonKind.Secondary);
            browseButton.Location = new Point(552, 149);
            browseButton.Size = new Size(128, 40);
            browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browseButton.Click += BrowseClicked;
            sourceCard.Controls.Add(browseButton);

            RoundedPanel formatCard = UiCardPanel();
            formatCard.Location = new Point(744, 22);
            formatCard.Size = new Size(372, 208);
            formatCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            content.Controls.Add(formatCard);
            formatCard.Controls.Add(SectionTitle("FORMAT WYJŚCIOWY", 20, 17));

            Label mediaLabel = UiLabel("Typ", 8.5F, FontStyle.Bold, UiMuted);
            mediaLabel.Location = new Point(20, 50);
            formatCard.Controls.Add(mediaLabel);
            Label formatLabel = UiLabel("Format", 8.5F, FontStyle.Bold, UiMuted);
            formatLabel.Location = new Point(186, 50);
            formatCard.Controls.Add(formatLabel);

            mediaTypeCombo = UiCombo(new Point(20, 72), new Size(152, 33));
            mediaTypeCombo.Items.Add(new OptionItem("Audio", "audio"));
            mediaTypeCombo.Items.Add(new OptionItem("Wideo", "video"));
            mediaTypeCombo.SelectedIndex = 0;
            formatCard.Controls.Add(mediaTypeCombo);

            formatCombo = UiCombo(new Point(186, 72), new Size(166, 33));
            formatCard.Controls.Add(formatCombo);

            Label qualityLabel = UiLabel("Jakość", 8.5F, FontStyle.Bold, UiMuted);
            qualityLabel.Location = new Point(20, 113);
            formatCard.Controls.Add(qualityLabel);
            qualityCombo = UiCombo(new Point(20, 135), new Size(332, 33));
            formatCard.Controls.Add(qualityCombo);

            qualityHintLabel = UiLabel(string.Empty, 8F, FontStyle.Regular, UiMuted);
            qualityHintLabel.AutoSize = false;
            qualityHintLabel.Size = new Size(332, 30);
            qualityHintLabel.Location = new Point(20, 173);
            formatCard.Controls.Add(qualityHintLabel);

            RoundedPanel optionsCard = UiCardPanel();
            optionsCard.Location = new Point(24, 246);
            optionsCard.Size = new Size(700, 154);
            optionsCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            content.Controls.Add(optionsCard);
            optionsCard.Controls.Add(SectionTitle("OPCJE POBIERANIA", 20, 16));

            playlistFolderCheck = UiCheckBox("Podfolder o nazwie playlisty", 20, 45);
            playlistFolderCheck.Checked = true;
            optionsCard.Controls.Add(playlistFolderCheck);

            coverCheck = UiCheckBox("Metadane i okładka, gdy obsługiwane", 20, 79);
            coverCheck.Checked = true;
            optionsCard.Controls.Add(coverCheck);

            archiveCheck = UiCheckBox("Pomijaj pliki pobrane wcześniej", 20, 113);
            archiveCheck.Checked = true;
            optionsCard.Controls.Add(archiveCheck);

            Label browserLabel = UiLabel("Dostęp przez przeglądarkę", 8.5F,
                FontStyle.Bold, UiMuted);
            browserLabel.Location = new Point(395, 48);
            browserLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            optionsCard.Controls.Add(browserLabel);

            browserCombo = UiCombo(new Point(395, 71), new Size(285, 33));
            browserCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browserCombo.Items.AddRange(new object[]
            {
                "Bez logowania",
                "Microsoft Edge",
                "Google Chrome",
                "Mozilla Firefox"
            });
            browserCombo.SelectedIndex = 0;
            optionsCard.Controls.Add(browserCombo);

            Label browserHint = UiLabel("Wybierz tylko dla prywatnych lub ograniczonych materiałów.",
                8F, FontStyle.Regular, UiMuted);
            browserHint.AutoSize = false;
            browserHint.Size = new Size(285, 32);
            browserHint.Location = new Point(395, 108);
            browserHint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            optionsCard.Controls.Add(browserHint);

            RoundedPanel actionCard = UiCardPanel();
            actionCard.Location = new Point(744, 246);
            actionCard.Size = new Size(372, 154);
            actionCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            content.Controls.Add(actionCard);
            actionCard.Controls.Add(SectionTitle("GOTOWE DO POBRANIA", 20, 16));

            startButton = UiButton("POBIERZ AUDIO", ButtonKind.Primary);
            startButton.Location = new Point(20, 45);
            startButton.Size = new Size(332, 48);
            startButton.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            startButton.Click += StartClicked;
            actionCard.Controls.Add(startButton);

            cancelButton = UiButton("Anuluj", ButtonKind.Danger);
            cancelButton.Location = new Point(20, 105);
            cancelButton.Size = new Size(156, 36);
            cancelButton.Enabled = false;
            cancelButton.Click += CancelClicked;
            actionCard.Controls.Add(cancelButton);

            openFolderButton = UiButton("Otwórz folder", ButtonKind.Secondary);
            openFolderButton.Location = new Point(196, 105);
            openFolderButton.Size = new Size(156, 36);
            openFolderButton.Click += OpenFolderClicked;
            actionCard.Controls.Add(openFolderButton);

            RoundedPanel progressCard = UiCardPanel();
            progressCard.Location = new Point(24, 416);
            progressCard.Size = new Size(1092, 172);
            progressCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            content.Controls.Add(progressCard);

            statusLabel = UiLabel("Gotowe do pracy", 10F, FontStyle.Bold, UiAccentLight);
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(470, 24);
            statusLabel.Location = new Point(20, 14);
            statusLabel.AutoEllipsis = true;
            progressCard.Controls.Add(statusLabel);

            statsLabel = UiLabel("Gotowe: 0  •  Błędy: 0  •  Czas: 00:00:00",
                8.5F, FontStyle.Regular, UiMuted);
            statsLabel.AutoSize = false;
            statsLabel.Size = new Size(420, 24);
            statsLabel.Location = new Point(652, 14);
            statsLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            statsLabel.TextAlign = ContentAlignment.MiddleRight;
            progressCard.Controls.Add(statsLabel);

            currentLabel = UiLabel("Wklej link, wybierz format i rozpocznij pobieranie.",
                8.5F, FontStyle.Regular, UiMuted);
            currentLabel.AutoSize = false;
            currentLabel.Size = new Size(1052, 21);
            currentLabel.Location = new Point(20, 42);
            currentLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            currentLabel.AutoEllipsis = true;
            progressCard.Controls.Add(currentLabel);

            Label overallCaption = UiLabel("CAŁA PLAYLISTA", 7.5F, FontStyle.Bold, UiMuted);
            overallCaption.Location = new Point(20, 70);
            progressCard.Controls.Add(overallCaption);

            overallPercentLabel = UiLabel("0%", 8F, FontStyle.Bold, UiText);
            overallPercentLabel.AutoSize = false;
            overallPercentLabel.Size = new Size(80, 20);
            overallPercentLabel.Location = new Point(992, 67);
            overallPercentLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            overallPercentLabel.TextAlign = ContentAlignment.MiddleRight;
            progressCard.Controls.Add(overallPercentLabel);

            progressBar = new ModernProgressBar();
            progressBar.Location = new Point(20, 91);
            progressBar.Size = new Size(1052, 12);
            progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressCard.Controls.Add(progressBar);

            Label trackCaption = UiLabel("AKTUALNY ELEMENT", 7.5F, FontStyle.Bold, UiMuted);
            trackCaption.Location = new Point(20, 116);
            progressCard.Controls.Add(trackCaption);

            trackPercentLabel = UiLabel("0%", 8F, FontStyle.Bold, UiText);
            trackPercentLabel.AutoSize = false;
            trackPercentLabel.Size = new Size(110, 20);
            trackPercentLabel.Location = new Point(962, 113);
            trackPercentLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            trackPercentLabel.TextAlign = ContentAlignment.MiddleRight;
            progressCard.Controls.Add(trackPercentLabel);

            trackProgressBar = new ModernProgressBar();
            trackProgressBar.Location = new Point(20, 139);
            trackProgressBar.Size = new Size(1052, 10);
            trackProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            trackProgressBar.Secondary = true;
            progressCard.Controls.Add(trackProgressBar);

            RoundedPanel logCard = UiCardPanel();
            logCard.Location = new Point(24, 604);
            logCard.Size = new Size(1092, 142);
            logCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            content.Controls.Add(logCard);
            logCard.Controls.Add(SectionTitle("DZIENNIK", 20, 14));

            Label rightsHint = UiLabel("Tylko treści, do których masz prawa lub zgodę.",
                8F, FontStyle.Regular, UiMuted);
            rightsHint.AutoSize = false;
            rightsHint.Size = new Size(360, 20);
            rightsHint.Location = new Point(712, 12);
            rightsHint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            rightsHint.TextAlign = ContentAlignment.MiddleRight;
            logCard.Controls.Add(rightsHint);

            logBox = new RichTextBox();
            logBox.Location = new Point(20, 39);
            logBox.Size = new Size(1052, 83);
            logBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            logBox.ReadOnly = true;
            logBox.BackColor = UiCardAlt;
            logBox.ForeColor = Color.FromArgb(181, 194, 219);
            logBox.BorderStyle = BorderStyle.None;
            logBox.Font = new Font("Cascadia Mono", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            logBox.DetectUrls = false;
            logCard.Controls.Add(logBox);

            ToolTip tip = new ToolTip();
            tip.BackColor = Color.FromArgb(30, 41, 59);
            tip.ForeColor = UiText;
            tip.SetToolTip(browserCombo,
                "Dla własnych prywatnych materiałów. W razie błędu zamknij przeglądarkę.");
            tip.SetToolTip(archiveCheck,
                "Historia jest prowadzona osobno dla każdego formatu i jakości.");

            mediaTypeCombo.SelectedIndexChanged += MediaTypeChanged;
            formatCombo.SelectedIndexChanged += FormatChanged;
            qualityCombo.SelectedIndexChanged += QualityChanged;
            UpdateFormatOptions(null, null);

            elapsedTimer = new System.Windows.Forms.Timer();
            elapsedTimer.Interval = 1000;
            elapsedTimer.Tick += delegate { UpdateStatsLabel(); };
            FormClosing += WindowClosing;
        }

        private void MediaTypeChanged(object sender, EventArgs e)
        {
            UpdateFormatOptions(null, null);
        }

        private void FormatChanged(object sender, EventArgs e)
        {
            if (updatingMediaOptions)
                return;
            UpdateQualityOptions(null);
        }

        private void QualityChanged(object sender, EventArgs e)
        {
            if (updatingMediaOptions)
                return;
            UpdateOutputUi();
        }

        private void UpdateFormatOptions(string preferredFormat, string preferredQuality)
        {
            if (updatingMediaOptions)
                return;
            updatingMediaOptions = true;
            try
            {
                string mode = SelectedOptionValue(mediaTypeCombo);
                formatCombo.Items.Clear();
                if (mode == "video")
                {
                    formatCombo.Items.Add(new OptionItem("MP4", "mp4"));
                    formatCombo.Items.Add(new OptionItem("WebM", "webm"));
                    SelectOption(formatCombo, preferredFormat ?? "mp4");
                }
                else
                {
                    formatCombo.Items.Add(new OptionItem("MP3", "mp3"));
                    formatCombo.Items.Add(new OptionItem("M4A", "m4a"));
                    formatCombo.Items.Add(new OptionItem("Opus", "opus"));
                    formatCombo.Items.Add(new OptionItem("FLAC", "flac"));
                    formatCombo.Items.Add(new OptionItem("WAV", "wav"));
                    formatCombo.Items.Add(new OptionItem("Oryginalny", "original"));
                    SelectOption(formatCombo, preferredFormat ?? "mp3");
                }
                UpdateQualityOptions(preferredQuality);
            }
            finally
            {
                updatingMediaOptions = false;
            }
            UpdateOutputUi();
        }

        private void UpdateQualityOptions(string preferredQuality)
        {
            string mode = SelectedOptionValue(mediaTypeCombo);
            string format = SelectedOptionValue(formatCombo);
            qualityCombo.Items.Clear();

            if (mode == "video")
            {
                qualityCombo.Items.Add(new OptionItem("Najlepsza dostępna", "best"));
                qualityCombo.Items.Add(new OptionItem("Do 2160p (4K)", "2160"));
                qualityCombo.Items.Add(new OptionItem("Do 1440p", "1440"));
                qualityCombo.Items.Add(new OptionItem("Do 1080p", "1080"));
                qualityCombo.Items.Add(new OptionItem("Do 720p", "720"));
                qualityCombo.Items.Add(new OptionItem("Do 480p", "480"));
                qualityCombo.Items.Add(new OptionItem("Do 360p", "360"));
                SelectOption(qualityCombo, preferredQuality ?? "1080");
            }
            else if (format == "flac" || format == "wav")
            {
                qualityCombo.Items.Add(new OptionItem("Bezstratny plik wyjściowy", "lossless"));
                qualityCombo.SelectedIndex = 0;
            }
            else if (format == "original")
            {
                qualityCombo.Items.Add(new OptionItem("Najlepszy strumień źródłowy", "source"));
                qualityCombo.SelectedIndex = 0;
            }
            else
            {
                qualityCombo.Items.Add(new OptionItem("320 kb/s", "320"));
                qualityCombo.Items.Add(new OptionItem("256 kb/s", "256"));
                qualityCombo.Items.Add(new OptionItem("192 kb/s", "192"));
                qualityCombo.Items.Add(new OptionItem("128 kb/s", "128"));
                SelectOption(qualityCombo, preferredQuality ?? "320");
            }
            UpdateOutputUi();
        }

        private void UpdateOutputUi()
        {
            if (qualityHintLabel == null || startButton == null)
                return;

            string mode = SelectedOptionValue(mediaTypeCombo);
            string format = SelectedOptionValue(formatCombo);
            string quality = SelectedOptionValue(qualityCombo);
            if (mode == "video")
            {
                startButton.Text = "POBIERZ WIDEO";
                coverCheck.Text = "Dodaj metadane do pliku wideo";
                coverCheck.Enabled = true;
                qualityHintLabel.Text = quality == "best"
                    ? "Najlepszy dostępny obraz i dźwięk."
                    : "Limit " + quality + "p; gdy brak, zostanie wybrana niższa jakość.";
            }
            else
            {
                startButton.Text = "POBIERZ AUDIO";
                if (format == "wav")
                    coverCheck.Text = "Okładka niedostępna dla formatu WAV";
                else if (format == "original")
                    coverCheck.Text = "Oryginalny strumień bez modyfikacji";
                else
                    coverCheck.Text = "Metadane i okładka, gdy obsługiwane";
                coverCheck.Enabled = format != "wav" && format != "original";
                if (format == "flac" || format == "wav")
                    qualityHintLabel.Text = "Bezstratny kontener nie odzyska jakości utraconej w YouTube.";
                else if (format == "original")
                    qualityHintLabel.Text = "Bez ponownej konwersji; rozszerzenie zależy od źródła.";
                else
                    qualityHintLabel.Text = quality + " kb/s dotyczy pliku wynikowego, nie jakości źródła.";
            }
        }

        private static string SelectedOptionValue(ComboBox combo)
        {
            OptionItem item = combo == null ? null : combo.SelectedItem as OptionItem;
            return item == null ? string.Empty : item.Value;
        }

        private static void SelectOption(ComboBox combo, string value)
        {
            if (combo == null || combo.Items.Count == 0)
                return;
            for (int index = 0; index < combo.Items.Count; index++)
            {
                OptionItem item = combo.Items[index] as OptionItem;
                if (item != null && string.Equals(item.Value, value,
                    StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = index;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private static Label UiLabel(string text, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Text = text;
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.Font = new Font("Segoe UI", size, style, GraphicsUnit.Point);
            return label;
        }

        private static Label SectionTitle(string text, int x, int y)
        {
            Label label = UiLabel(text, 8F, FontStyle.Bold, Color.FromArgb(129, 140, 248));
            label.Location = new Point(x, y);
            return label;
        }

        private static RoundedPanel UiCardPanel()
        {
            RoundedPanel panel = new RoundedPanel();
            panel.FillColor = UiCard;
            panel.BorderColor = UiBorder;
            panel.BorderWidth = 1;
            panel.CornerRadius = 16;
            return panel;
        }

        private static RoundedPanel InputShell(Point location, Size size)
        {
            RoundedPanel panel = new RoundedPanel();
            panel.Location = location;
            panel.Size = size;
            panel.FillColor = UiCardAlt;
            panel.BorderColor = UiBorder;
            panel.BorderWidth = 1;
            panel.CornerRadius = 10;
            return panel;
        }

        private static TextBox InputTextBox(RoundedPanel shell)
        {
            TextBox box = new TextBox();
            box.BorderStyle = BorderStyle.None;
            box.BackColor = UiCardAlt;
            box.ForeColor = UiText;
            box.Font = new Font("Segoe UI", 10F);
            box.Location = new Point(13, 10);
            box.Size = new Size(shell.Width - 26, 24);
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            shell.Controls.Add(box);
            return box;
        }

        private static ModernButton UiButton(string text, ButtonKind kind)
        {
            ModernButton button = new ModernButton();
            button.Text = text;
            button.Kind = kind;
            return button;
        }

        private static ModernComboBox UiCombo(Point location, Size size)
        {
            ModernComboBox combo = new ModernComboBox();
            combo.Location = location;
            combo.Size = size;
            return combo;
        }

        private static ModernCheckBox UiCheckBox(string text, int x, int y)
        {
            ModernCheckBox checkBox = new ModernCheckBox();
            checkBox.Text = text;
            checkBox.Location = new Point(x, y);
            checkBox.Size = new Size(340, 27);
            return checkBox;
        }
    }

    internal sealed class OptionItem
    {
        internal readonly string Text;
        internal readonly string Value;

        internal OptionItem(string text, string value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    internal enum ButtonKind
    {
        Primary,
        Secondary,
        Ghost,
        Danger
    }

    internal sealed class GradientPanel : Panel
    {
        internal Color StartColor = Color.FromArgb(20, 28, 58);
        internal Color EndColor = Color.FromArgb(39, 31, 82);

        internal GradientPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(
                ClientRectangle, StartColor, EndColor, 0F))
                e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    internal sealed class RoundedPanel : Panel
    {
        internal Color FillColor = Color.FromArgb(17, 25, 43);
        internal Color BorderColor = Color.FromArgb(39, 50, 75);
        internal int BorderWidth = 1;
        internal int CornerRadius = 16;

        internal RoundedPanel()
        {
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            if (Width <= 0 || Height <= 0)
                return;
            using (GraphicsPath path = UiGeometry.RoundRectangle(
                new Rectangle(0, 0, Width, Height), CornerRadius))
            {
                Region previous = Region;
                Region = new Region(path);
                if (previous != null)
                    previous.Dispose();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rectangle = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UiGeometry.RoundRectangle(rectangle, CornerRadius))
            using (SolidBrush fill = new SolidBrush(FillColor))
                e.Graphics.FillPath(fill, path);

            if (BorderWidth > 0)
            {
                using (GraphicsPath path = UiGeometry.RoundRectangle(rectangle, CornerRadius))
                using (Pen pen = new Pen(BorderColor, BorderWidth))
                    e.Graphics.DrawPath(pen, path);
            }
        }
    }

    internal sealed class ModernButton : Button
    {
        private ButtonKind kind;
        private bool hovered;
        private bool pressed;

        internal ButtonKind Kind
        {
            get { return kind; }
            set { kind = value; Invalidate(); }
        }

        internal ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color start;
            Color end;
            Color text;
            Color border;
            if (!Enabled)
            {
                start = end = Color.FromArgb(45, 55, 75);
                text = Color.FromArgb(120, 132, 151);
                border = Color.FromArgb(55, 66, 88);
            }
            else if (kind == ButtonKind.Primary)
            {
                start = Color.FromArgb(99, 102, 241);
                end = Color.FromArgb(37, 99, 235);
                text = Color.White;
                border = Color.Transparent;
            }
            else if (kind == ButtonKind.Danger)
            {
                start = end = Color.FromArgb(75, 31, 45);
                text = Color.FromArgb(254, 202, 202);
                border = Color.FromArgb(127, 45, 62);
            }
            else if (kind == ButtonKind.Ghost)
            {
                start = end = Color.FromArgb(39, 45, 83);
                text = Color.FromArgb(199, 210, 254);
                border = Color.FromArgb(81, 83, 150);
            }
            else
            {
                start = end = Color.FromArgb(28, 38, 60);
                text = Color.FromArgb(226, 232, 240);
                border = Color.FromArgb(52, 65, 91);
            }

            if (hovered && Enabled)
            {
                start = ControlPaint.Light(start, 0.08F);
                end = ControlPaint.Light(end, 0.08F);
            }
            if (pressed && Enabled)
            {
                start = ControlPaint.Dark(start, 0.08F);
                end = ControlPaint.Dark(end, 0.08F);
            }

            Rectangle rectangle = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UiGeometry.RoundRectangle(rectangle, 10))
            using (LinearGradientBrush fill = new LinearGradientBrush(rectangle, start, end, 0F))
            {
                e.Graphics.FillPath(fill, path);
                if (border != Color.Transparent)
                {
                    using (Pen pen = new Pen(border))
                        e.Graphics.DrawPath(pen, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, rectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);
        }
    }

    internal sealed class ModernComboBox : ComboBox
    {
        internal ModernComboBox()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 24;
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(13, 21, 38);
            ForeColor = Color.FromArgb(238, 242, 255);
            Font = new Font("Segoe UI", 9F);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;
            Color background = (e.State & DrawItemState.Selected) != 0
                ? Color.FromArgb(67, 70, 139)
                : Color.FromArgb(13, 21, 38);
            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);
            string text = GetItemText(Items[e.Index]);
            TextRenderer.DrawText(e.Graphics, text, Font,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height),
                Color.FromArgb(238, 242, 255),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        }
    }

    internal sealed class ModernCheckBox : CheckBox
    {
        private bool hovered;

        internal ModernCheckBox()
        {
            ForeColor = Color.FromArgb(220, 226, 240);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9F);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(1, 4, 18, 18);
            Color fill = Checked
                ? Color.FromArgb(99, 102, 241)
                : Color.FromArgb(13, 21, 38);
            Color border = hovered
                ? Color.FromArgb(129, 140, 248)
                : Color.FromArgb(61, 76, 104);
            using (GraphicsPath path = UiGeometry.RoundRectangle(box, 5))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }
            if (Checked)
            {
                using (Pen pen = new Pen(Color.White, 2F))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    e.Graphics.DrawLines(pen, new[]
                    {
                        new Point(5, 13), new Point(9, 17), new Point(16, 9)
                    });
                }
            }
            TextRenderer.DrawText(e.Graphics, Text, Font,
                new Rectangle(29, 0, Width - 29, Height),
                Enabled ? ForeColor : Color.FromArgb(100, 116, 139),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    internal sealed class ModernProgressBar : Control
    {
        private readonly Timer marqueeTimer;
        private int value;
        private int marqueeOffset;
        private ProgressBarStyle style;
        private int marqueeAnimationSpeed = 25;

        internal bool Secondary { get; set; }

        public int Value
        {
            get { return value; }
            set
            {
                this.value = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        public ProgressBarStyle Style
        {
            get { return style; }
            set
            {
                style = value;
                marqueeTimer.Enabled = style == ProgressBarStyle.Marquee;
                Invalidate();
            }
        }

        public int MarqueeAnimationSpeed
        {
            get { return marqueeAnimationSpeed; }
            set
            {
                marqueeAnimationSpeed = Math.Max(10, value);
                marqueeTimer.Interval = marqueeAnimationSpeed;
            }
        }

        internal ModernProgressBar()
        {
            style = ProgressBarStyle.Continuous;
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            marqueeTimer = new Timer();
            marqueeTimer.Interval = marqueeAnimationSpeed;
            marqueeTimer.Tick += delegate
            {
                marqueeOffset = (marqueeOffset + 5) % Math.Max(1, Width + 80);
                Invalidate();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                marqueeTimer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            int radius = Math.Max(2, Height / 2);
            using (GraphicsPath trackPath = UiGeometry.RoundRectangle(track, radius))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(31, 42, 66)))
                e.Graphics.FillPath(trackBrush, trackPath);

            Rectangle fill;
            if (style == ProgressBarStyle.Marquee)
            {
                int segment = Math.Max(45, Width / 4);
                int x = marqueeOffset - segment;
                fill = new Rectangle(x, 0, segment, Math.Max(1, Height - 1));
            }
            else
            {
                int width = (int)Math.Round((Width - 1) * value / 100D);
                if (width <= 0)
                    return;
                fill = new Rectangle(0, 0, width, Math.Max(1, Height - 1));
            }

            GraphicsState graphicsState = e.Graphics.Save();
            using (GraphicsPath trackPath = UiGeometry.RoundRectangle(track, radius))
            {
                e.Graphics.SetClip(trackPath);
                Color start = Secondary
                    ? Color.FromArgb(45, 212, 191)
                    : Color.FromArgb(129, 140, 248);
                Color end = Secondary
                    ? Color.FromArgb(14, 165, 233)
                    : Color.FromArgb(59, 130, 246);
                using (LinearGradientBrush brush = new LinearGradientBrush(fill, start, end, 0F))
                    e.Graphics.FillRectangle(brush, fill);
            }
            e.Graphics.Restore(graphicsState);
        }
    }

    internal sealed class MediaLogo : Control
    {
        internal MediaLogo()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle box = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = UiGeometry.RoundRectangle(box, 14))
            using (LinearGradientBrush brush = new LinearGradientBrush(box,
                Color.FromArgb(99, 102, 241), Color.FromArgb(14, 165, 233), 45F))
                e.Graphics.FillPath(brush, path);

            Point[] play =
            {
                new Point(17, 12),
                new Point(17, 35),
                new Point(35, 24)
            };
            e.Graphics.FillPolygon(Brushes.White, play);
        }
    }

    internal static class UiGeometry
    {
        internal static GraphicsPath RoundRectangle(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(1, radius * 2);
            if (diameter > rectangle.Width)
                diameter = rectangle.Width;
            if (diameter > rectangle.Height)
                diameter = rectangle.Height;
            Rectangle arc = new Rectangle(rectangle.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
