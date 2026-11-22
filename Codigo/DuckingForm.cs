namespace BotonMicrofono;

internal sealed class DuckingForm : Form
{
    private readonly CheckBox enabled = new() { Text = "Reducir otras aplicaciones cuando detecte mi voz", AutoSize = true };
    private readonly TrackBar slider = new() { Minimum = 0, Maximum = 100, TickFrequency = 10, Dock = DockStyle.Fill };
    private readonly Label value = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
    public bool DuckingEnabled => enabled.Checked;
    public int DuckingLevel => slider.Value;

    public DuckingForm(bool isEnabled, int initialLevel)
    {
        Text = "Reducción automática de otros sonidos";
        ClientSize = new Size(470, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);
        enabled.Checked = isEnabled;
        slider.Value = Math.Clamp(initialLevel, 0, 100);
        slider.Enabled = enabled.Checked;
        enabled.CheckedChanged += (_, _) => slider.Enabled = enabled.Checked;
        slider.ValueChanged += (_, _) => UpdateLabel();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 90 });
        buttons.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 90 });
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 6 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.Controls.Add(enabled, 0, 0);
        layout.Controls.Add(new Label { Text = "Nivel temporal de las aplicaciones que estaban al 100 %:", Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(slider, 0, 2);
        layout.Controls.Add(value, 0, 3);
        layout.Controls.Add(buttons, 0, 5);
        Controls.Add(layout);
        AcceptButton = (Button)buttons.Controls[0];
        CancelButton = (Button)buttons.Controls[1];
        UpdateLabel();
    }

    private void UpdateLabel() => value.Text = $"Bajar temporalmente al {slider.Value} %";
}
