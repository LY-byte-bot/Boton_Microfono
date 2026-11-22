namespace BotonMicrofono;

internal sealed class VoiceProcessingForm : Form
{
    private readonly CheckBox compressor = new() { Text = "Activar compresor", AutoSize = true };
    private readonly NumericUpDown threshold = Number(-40, -3, -18, " dB");
    private readonly NumericUpDown ratio = Number(1, 10, 3, ":1", 1);
    private readonly NumericUpDown makeup = Number(0, 18, 6, " dB");
    private readonly CheckBox limiter = new() { Text = "Activar limitador de picos", AutoSize = true };
    private readonly NumericUpDown ceiling = Number(-12, 0, -1, " dB");
    private readonly CheckBox equalizer = new() { Text = "Activar ecualizador", AutoSize = true };
    private readonly NumericUpDown bass = Number(-12, 12, 0, " dB");
    private readonly NumericUpDown mid = Number(-12, 12, 0, " dB");
    private readonly NumericUpDown treble = Number(-12, 12, 0, " dB");

    public VoiceProcessingForm(AppSettings settings)
    {
        Text = "Procesamiento de voz";
        ClientSize = new Size(490, 455);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);
        compressor.Checked = settings.CompressorEnabled;
        threshold.Value = (decimal)settings.CompressorThresholdDb;
        ratio.Value = (decimal)settings.CompressorRatio;
        makeup.Value = (decimal)settings.CompressorMakeupDb;
        limiter.Checked = settings.LimiterEnabled;
        ceiling.Value = (decimal)settings.LimiterCeilingDb;
        equalizer.Checked = settings.EqualizerEnabled;
        bass.Value = (decimal)settings.BassDb;
        mid.Value = (decimal)settings.MidDb;
        treble.Value = (decimal)settings.TrebleDb;

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 12 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        AddSection(table, compressor, 0);
        AddRow(table, "Umbral", threshold, 1);
        AddRow(table, "Relación", ratio, 2);
        AddRow(table, "Ganancia de compensación", makeup, 3);
        AddSection(table, limiter, 4);
        AddRow(table, "Techo máximo", ceiling, 5);
        AddSection(table, equalizer, 6);
        AddRow(table, "Graves (120 Hz)", bass, 7);
        AddRow(table, "Medios (1.2 kHz)", mid, 8);
        AddRow(table, "Agudos (6 kHz)", treble, 9);
        var advice = new Label { Text = "Consejo: el compresor nivela la voz y el limitador evita picos digitales.", Dock = DockStyle.Fill, AutoSize = false };
        table.Controls.Add(advice, 0, 10);
        table.SetColumnSpan(advice, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 90 });
        buttons.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 90 });
        table.Controls.Add(buttons, 0, 11); table.SetColumnSpan(buttons, 2);
        Controls.Add(table);
        AcceptButton = (Button)buttons.Controls[0]; CancelButton = (Button)buttons.Controls[1];
    }

    public void ApplyTo(AppSettings settings)
    {
        settings.CompressorEnabled = compressor.Checked;
        settings.CompressorThresholdDb = (float)threshold.Value;
        settings.CompressorRatio = (float)ratio.Value;
        settings.CompressorMakeupDb = (float)makeup.Value;
        settings.LimiterEnabled = limiter.Checked;
        settings.LimiterCeilingDb = (float)ceiling.Value;
        settings.EqualizerEnabled = equalizer.Checked;
        settings.BassDb = (float)bass.Value;
        settings.MidDb = (float)mid.Value;
        settings.TrebleDb = (float)treble.Value;
    }

    private static NumericUpDown Number(decimal min, decimal max, decimal value, string suffix, int decimals = 0) =>
        new() { Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals, Increment = decimals == 0 ? 1 : 0.5m, Dock = DockStyle.Fill };
    private static void AddSection(TableLayoutPanel table, CheckBox box, int row) { table.Controls.Add(box, 0, row); table.SetColumnSpan(box, 2); }
    private static void AddRow(TableLayoutPanel table, string name, Control control, int row)
    {
        table.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        table.Controls.Add(control, 1, row);
    }
}
