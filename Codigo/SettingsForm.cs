using NAudio.CoreAudioApi;

namespace BotonMicrofono;

internal sealed class SettingsForm : Form
{
    private readonly ComboBox input = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox output = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    public DeviceItem? SelectedInput => input.SelectedItem as DeviceItem;
    public DeviceItem? SelectedOutput => output.SelectedItem as DeviceItem;

    public SettingsForm(string? inputId, string? outputId)
    {
        Text = "Dispositivos de audio";
        ClientSize = new Size(480, 170);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 6 };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.Controls.Add(new Label { Text = "Entrada (micrófono)", Dock = DockStyle.Fill }, 0, 0);
        table.Controls.Add(input, 0, 1);
        table.Controls.Add(new Label { Text = "Salida de destino", Dock = DockStyle.Fill }, 0, 2);
        table.Controls.Add(output, 0, 3);
        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        buttons.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 90 });
        buttons.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 90 });
        table.Controls.Add(buttons, 0, 5);
        Controls.Add(table);
        AcceptButton = (Button)buttons.Controls[0];
        CancelButton = (Button)buttons.Controls[1];

        using var devices = new MMDeviceEnumerator();
        foreach (var d in devices.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)) input.Items.Add(new DeviceItem(d));
        foreach (var d in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)) output.Items.Add(new DeviceItem(d));
        Select(input, inputId);
        Select(output, outputId);
    }

    private static void Select(ComboBox box, string? id)
    {
        for (var i = 0; i < box.Items.Count; i++)
            if ((box.Items[i] as DeviceItem)?.Id == id) { box.SelectedIndex = i; return; }
        if (box.Items.Count > 0) box.SelectedIndex = 0;
    }
}
