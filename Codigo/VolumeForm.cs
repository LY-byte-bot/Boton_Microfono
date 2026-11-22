namespace BotonMicrofono;

internal sealed class VolumeForm : Form
{
    private readonly TrackBar slider = new() { Minimum = 0, Maximum = 200, TickFrequency = 25, Dock = DockStyle.Fill };
    private readonly Label value = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
    public event Action<int>? LevelChanged;

    public VolumeForm(int initial)
    {
        Text = "Volumen de salida y ganancia";
        ClientSize = new Size(410, 150);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);
        slider.Value = Math.Clamp(initial, 0, 200);
        slider.ValueChanged += (_, _) => { UpdateLabel(); LevelChanged?.Invoke(slider.Value); };

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 4 };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = "0–100 %: volumen · 101–200 %: ganancia hasta +20 dB", Dock = DockStyle.Fill }, 0, 0);
        table.Controls.Add(slider, 0, 1);
        table.Controls.Add(value, 0, 2);
        Controls.Add(table);
        UpdateLabel();
    }

    private void UpdateLabel() => value.Text = slider.Value <= 100
        ? $"Volumen: {slider.Value} %"
        : $"Ganancia: {slider.Value} %  (+{(slider.Value - 100) / 5.0:0.#} dB)";
}
