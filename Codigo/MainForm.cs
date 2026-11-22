using System.Drawing.Drawing2D;
using NAudio.CoreAudioApi;

namespace BotonMicrofono;

internal sealed class MainForm : Form
{
    private readonly AppSettings settings = AppSettings.Load();
    private readonly AudioRouter router = new();
    private readonly DuckingManager ducking = new();
    private readonly System.Windows.Forms.Timer duckingTimer = new() { Interval = 100 };
    private readonly ToolTip tip = new();
    private Point dragOrigin;
    private bool dragging;
    private bool changingStartup;
    private float audioLevel;
    private DateTime lastVoiceAt = DateTime.MinValue;

    public MainForm()
    {
        Text = "Botón Micrófono";
        // La ventana usa tamaños exactos en píxeles. Si WinForms aplica también el
        // escalado DPI, el icono recibe un segundo escalado y termina recortado.
        AutoScaleMode = AutoScaleMode.None;
        settings.ButtonSize = settings.ButtonSize is 42 or 58 or 74 or 90 ? settings.ButtonSize : 58;
        ClientSize = new Size(settings.ButtonSize, settings.ButtonSize);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = settings.AlwaysOnTop;
        StartPosition = FormStartPosition.Manual;
        Location = ConstrainToWorkingArea(new Point(settings.Left, settings.Top), ClientSize);
        DoubleBuffered = true;
        ApplyAppearance();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Configurar entrada y salida…", null, (_, _) => Configure());
        menu.Items.Add("Volumen y ganancia…", null, (_, _) => ConfigureVolume());
        menu.Items.Add("Reducción de ruido…", null, (_, _) => ConfigureNoiseReduction());
        menu.Items.Add("Procesamiento de voz…", null, (_, _) => ConfigureVoiceProcessing());
        menu.Items.Add("Reducción de otros sonidos…", null, (_, _) => ConfigureDucking());
        menu.Items.Add("Tema y tamaño…", null, (_, _) => ConfigureAppearance());
        var onTop = new ToolStripMenuItem("Siempre visible") { Checked = TopMost, CheckOnClick = true };
        onTop.CheckedChanged += (_, _) => { TopMost = settings.AlwaysOnTop = onTop.Checked; settings.Save(); };
        menu.Items.Add(onTop);
        var startup = new ToolStripMenuItem("Iniciar con Windows") { Checked = StartupManager.IsEnabled, CheckOnClick = true };
        startup.CheckedChanged += (_, _) => ChangeStartup(startup);
        menu.Items.Add(startup);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Acerca de…", null, (_, _) => ShowAbout());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => Close());
        ContextMenuStrip = menu;

        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { dragOrigin = e.Location; dragging = false; } };
        MouseMove += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (Math.Abs(e.X - dragOrigin.X) + Math.Abs(e.Y - dragOrigin.Y) > 3) dragging = true;
            if (dragging) Location = new Point(Left + e.X - dragOrigin.X, Top + e.Y - dragOrigin.Y);
        };
        MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            if (dragging)
            {
                Location = ConstrainToWorkingArea(Location, ClientSize);
                SavePosition();
            }
            else ToggleMute();
        };
        Paint += PaintButton;
        Resize += (_, _) => RefreshWindowRegion();
        DpiChanged += (_, _) =>
        {
            if (IsHandleCreated)
                BeginInvoke(NormalizeWindowGeometry);
        };
        FormClosing += (_, _) => { duckingTimer.Stop(); ducking.Dispose(); SavePosition(); router.Dispose(); };
        router.LevelChanged += level =>
        {
            if (IsDisposed) return;
            try { BeginInvoke(() => { audioLevel = level; if (level >= 0.006f) lastVoiceAt = DateTime.UtcNow; Invalidate(); }); } catch { }
        };
        duckingTimer.Tick += (_, _) => UpdateDucking();
        duckingTimer.Start();
        Shown += (_, _) =>
        {
            NormalizeWindowGeometry();
            InitializeAudio();
        };
    }

    private void InitializeAudio()
    {
        if (string.IsNullOrWhiteSpace(settings.InputDeviceId) || string.IsNullOrWhiteSpace(settings.OutputDeviceId))
        {
            UpdateVisual("Clic derecho para configurar");
            BeginInvoke(Configure);
            return;
        }
        StartRouting();
    }

    private void Configure()
    {
        using var dialog = new SettingsForm(settings.InputDeviceId, settings.OutputDeviceId);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (dialog.SelectedInput is null || dialog.SelectedOutput is null)
        {
            MessageBox.Show(this, "Debes seleccionar una entrada y una salida.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        settings.InputDeviceId = dialog.SelectedInput.Id;
        settings.OutputDeviceId = dialog.SelectedOutput.Id;
        settings.Save();
        StartRouting();
    }

    private void StartRouting()
    {
        try
        {
            using var devices = new MMDeviceEnumerator();
            using var input = devices.GetDevice(settings.InputDeviceId!);
            using var output = devices.GetDevice(settings.OutputDeviceId!);
            router.Muted = settings.Muted;
            router.OutputLevel = settings.OutputLevel;
            router.ConfigureProcessing(settings);
            router.Start(input, output);
            UpdateVisual(settings.Muted ? "Muteado" : "Transmitiendo");
        }
        catch (Exception ex)
        {
            router.Stop();
            UpdateVisual("Audio no disponible");
            MessageBox.Show(this, "No se pudo iniciar el envío de audio. Revisa los dispositivos seleccionados.\n\n" + ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleMute()
    {
        if (!router.Running) { Configure(); return; }
        settings.Muted = !settings.Muted;
        router.Muted = settings.Muted;
        if (settings.Muted) audioLevel = 0;
        settings.Save();
        UpdateVisual(settings.Muted ? "Muteado" : "Transmitiendo");
    }

    private void ConfigureVolume()
    {
        using var dialog = new VolumeForm(settings.OutputLevel);
        dialog.LevelChanged += level =>
        {
            settings.OutputLevel = level;
            router.OutputLevel = level;
            settings.Save();
        };
        dialog.ShowDialog(this);
    }

    private void ConfigureDucking()
    {
        using var dialog = new DuckingForm(settings.DuckingEnabled, settings.DuckingLevel);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        settings.DuckingEnabled = dialog.DuckingEnabled;
        settings.DuckingLevel = dialog.DuckingLevel;
        settings.Save();
        if (!settings.DuckingEnabled) ducking.SetDucking(false, settings.DuckingLevel);
    }

    private void ConfigureVoiceProcessing()
    {
        using var dialog = new VoiceProcessingForm(settings);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        dialog.ApplyTo(settings);
        settings.Save();
        router.ConfigureProcessing(settings);
    }

    private void ConfigureNoiseReduction()
    {
        using var dialog = new NoiseReductionForm(settings);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        dialog.ApplyTo(settings);
        settings.Save();
        router.ConfigureProcessing(settings);
    }

    private void ConfigureAppearance()
    {
        using var dialog = new AppearanceForm(settings.ThemeName, settings.ButtonSize);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        settings.ThemeName = dialog.ThemeName;
        settings.ButtonSize = dialog.ButtonSize;
        ClientSize = new Size(settings.ButtonSize, settings.ButtonSize);
        Location = ConstrainToWorkingArea(Location, ClientSize);
        ApplyAppearance();
        settings.Save();
    }

    private void ApplyAppearance()
    {
        BackColor = settings.ThemeName switch
        {
            "Azul" => Color.FromArgb(25, 55, 90),
            "Oscuro" => Color.FromArgb(28, 28, 32),
            "Ámbar" => Color.FromArgb(90, 60, 15),
            _ => Color.FromArgb(30, 30, 34)
        };
        RefreshWindowRegion();
        Invalidate();
    }

    private void NormalizeWindowGeometry()
    {
        ClientSize = new Size(settings.ButtonSize, settings.ButtonSize);
        RefreshWindowRegion();
        Location = ConstrainToWorkingArea(Location, ClientSize);
        SavePosition();
        Invalidate();
    }

    private void RefreshWindowRegion()
    {
        if (ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0) return;
        var oldRegion = Region;
        Region = RoundedRegion(ClientRectangle, Math.Max(8, Math.Min(ClientSize.Width, ClientSize.Height) / 5));
        oldRegion?.Dispose();
    }

    private void UpdateDucking()
    {
        var speaking = settings.DuckingEnabled && router.Running && !settings.Muted &&
            DateTime.UtcNow - lastVoiceAt < TimeSpan.FromMilliseconds(850);
        ducking.SetDucking(speaking, settings.DuckingLevel);
        ducking.Tick();
    }

    private void ChangeStartup(ToolStripMenuItem item)
    {
        if (changingStartup) return;
        try
        {
            StartupManager.SetEnabled(item.Checked);
        }
        catch (Exception ex)
        {
            changingStartup = true;
            item.Checked = StartupManager.IsEnabled;
            changingStartup = false;
            MessageBox.Show(this, "No se pudo cambiar el inicio con Windows.\n\n" + ex.Message,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(this,
            "Botón Micrófono\nVersión 1.6.0\n\nCreado por SISTEMAS LY\n© 2026 SISTEMAS LY. Todos los derechos reservados.",
            "Acerca de Botón Micrófono", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateVisual(string status) { tip.SetToolTip(this, status + " · clic derecho: configurar"); Invalidate(); }
    private void SavePosition() { settings.Left = Left; settings.Top = Top; settings.Save(); }

    private void PaintButton(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var active = router.Running && !settings.Muted;
        var (mutedColor, activeColor) = settings.ThemeName switch
        {
            "Azul" => (Color.FromArgb(45, 82, 125), Color.FromArgb(25, 145, 210)),
            "Oscuro" => (Color.FromArgb(65, 65, 72), Color.FromArgb(126, 87, 194)),
            "Ámbar" => (Color.FromArgb(145, 74, 20), Color.FromArgb(232, 151, 25)),
            _ => (Color.FromArgb(210, 62, 72), Color.FromArgb(35, 170, 92))
        };
        using var bg = new SolidBrush(active ? activeColor : mutedColor);
        e.Graphics.FillRectangle(bg, ClientRectangle);
        var width = ClientRectangle.Width;
        var height = ClientRectangle.Height;
        using var pen = new Pen(Color.White, Math.Max(2f, width * 0.052f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        var microphone = new RectangleF(width * 0.397f, height * 0.207f, width * 0.207f, height * 0.414f);
        e.Graphics.DrawRoundedRectangle(pen, microphone, width * 0.103f);
        e.Graphics.DrawArc(pen, width * 0.293f, height * 0.362f, width * 0.414f, height * 0.379f, 0, 180);
        e.Graphics.DrawLine(pen, width * 0.5f, height * 0.741f, width * 0.5f, height * 0.828f);
        e.Graphics.DrawLine(pen, width * 0.397f, height * 0.828f, width * 0.603f, height * 0.828f);
        if (!active)
            e.Graphics.DrawLine(pen, width * 0.224f, height * 0.224f, width * 0.776f, height * 0.776f);
        else DrawLevelBars(e.Graphics);
    }

    private void DrawLevelBars(Graphics graphics)
    {
        // Conversión logarítmica aproximada: -60 dB queda vacío y 0 dB llena las barras.
        var db = audioLevel <= 0.0001f ? -60f : 20f * MathF.Log10(audioLevel);
        var normalized = Math.Clamp((db + 60f) / 60f, 0f, 1f);
        var lit = (int)MathF.Ceiling(normalized * 5);
        var width = ClientRectangle.Width;
        var height = ClientRectangle.Height;
        using var bar = new SolidBrush(Color.FromArgb(235, 255, 255, 255));
        for (var i = 0; i < 5; i++)
        {
            var barHeight = height * (0.069f + i * 0.052f);
            var x = width * (0.052f + i * 0.052f);
            if (i < lit) graphics.FillRectangle(bar, x, height * 0.897f - barHeight, Math.Max(1f, width * 0.034f), barHeight);
        }
    }

    private static Region RoundedRegion(Rectangle bounds, int radius)
    {
        using var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return new Region(path);
    }

    private static Point ConstrainToWorkingArea(Point point, Size size)
    {
        var screen = Screen.AllScreens.FirstOrDefault(s => s.WorkingArea.Contains(point))
            ?? Screen.FromPoint(point)
            ?? Screen.PrimaryScreen;
        var area = screen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        var maximumX = Math.Max(area.Left, area.Right - size.Width);
        var maximumY = Math.Max(area.Top, area.Bottom - size.Height);
        return new Point(
            Math.Clamp(point.X, area.Left, maximumX),
            Math.Clamp(point.Y, area.Top, maximumY));
    }
}

internal static class GraphicsExtensions
{
    public static void DrawRoundedRectangle(this Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.DrawPath(pen, path);
    }
}
