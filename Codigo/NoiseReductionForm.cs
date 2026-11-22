namespace BotonMicrofono;

internal sealed class NoiseReductionForm : Form
{
    private readonly CheckBox enabled = new() { Text = "Activar reducción de ruido", AutoSize = true };
    private readonly TrackBar threshold = new() { Minimum = -70, Maximum = -20, TickFrequency = 5, Dock = DockStyle.Fill };
    private readonly TrackBar reduction = new() { Minimum = 0, Maximum = 48, TickFrequency = 6, Dock = DockStyle.Fill };
    private readonly Label thresholdValue = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
    private readonly Label reductionValue = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

    public NoiseReductionForm(AppSettings settings)
    {
        Text = "Reducción de ruido";
        ClientSize = new Size(500, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);
        enabled.Checked = settings.NoiseSuppressionEnabled;
        threshold.Value = (int)Math.Clamp(settings.NoiseThresholdDb, -70f, -20f);
        reduction.Value = (int)Math.Clamp(settings.NoiseReductionDb, 0f, 48f);
        enabled.CheckedChanged += (_, _) => UpdateEnabled();
        threshold.ValueChanged += (_, _) => UpdateLabels();
        reduction.ValueChanged += (_, _) => UpdateLabels();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 90 });
        buttons.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 90 });
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 9 };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        table.Controls.Add(enabled, 0, 0);
        table.Controls.Add(new Label { Text = "Umbral: súbelo si todavía se oye ruido; bájalo si corta palabras.", Dock = DockStyle.Fill }, 0, 1);
        table.Controls.Add(threshold, 0, 2);
        table.Controls.Add(thresholdValue, 0, 3);
        table.Controls.Add(new Label { Text = "Intensidad: cuánto se atenúa el ruido cuando no hay voz.", Dock = DockStyle.Fill }, 0, 4);
        table.Controls.Add(reduction, 0, 5);
        table.Controls.Add(reductionValue, 0, 6);
        table.Controls.Add(new Label { Text = "Incluye un filtro de 80 Hz para vibraciones y zumbidos graves.", Dock = DockStyle.Fill }, 0, 7);
        table.Controls.Add(buttons, 0, 8);
        Controls.Add(table);
        AcceptButton = (Button)buttons.Controls[0];
        CancelButton = (Button)buttons.Controls[1];
        UpdateEnabled();
        UpdateLabels();
    }

    public void ApplyTo(AppSettings settings)
    {
        settings.NoiseSuppressionEnabled = enabled.Checked;
        settings.NoiseThresholdDb = threshold.Value;
        settings.NoiseReductionDb = reduction.Value;
    }

    private void UpdateEnabled() => threshold.Enabled = reduction.Enabled = enabled.Checked;
    private void UpdateLabels()
    {
        thresholdValue.Text = $"Umbral: {threshold.Value} dB";
        reductionValue.Text = $"Reducción: {reduction.Value} dB";
    }
}
