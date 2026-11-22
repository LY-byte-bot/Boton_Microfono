namespace BotonMicrofono;

internal sealed class AppearanceForm : Form
{
    private readonly ComboBox theme = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox size = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    public string ThemeName => theme.SelectedItem?.ToString() ?? "Clásico";
    public int ButtonSize => int.TryParse(size.SelectedItem?.ToString()?.Split(' ')[0], out var value) ? value : 58;

    public AppearanceForm(string currentTheme, int currentSize)
    {
        Text = "Apariencia";
        ClientSize = new Size(390, 185);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);
        theme.Items.AddRange(["Clásico", "Azul", "Oscuro", "Ámbar"]);
        theme.SelectedItem = theme.Items.Cast<string>().Contains(currentTheme) ? currentTheme : "Clásico";
        size.Items.AddRange(["42 px · Pequeño", "58 px · Normal", "74 px · Grande", "90 px · Muy grande"]);
        size.SelectedIndex = currentSize switch { 42 => 0, 74 => 2, 90 => 3, _ => 1 };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 90 });
        buttons.Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 90 });
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 4 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = "Tema", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        table.Controls.Add(theme, 1, 0);
        table.Controls.Add(new Label { Text = "Tamaño", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        table.Controls.Add(size, 1, 1);
        table.Controls.Add(buttons, 0, 3); table.SetColumnSpan(buttons, 2);
        Controls.Add(table);
        AcceptButton = (Button)buttons.Controls[0]; CancelButton = (Button)buttons.Controls[1];
    }
}
