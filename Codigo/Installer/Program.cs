using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace InstaladorBotonMicrofono;

internal static class Program
{
    internal const string AppName = "Botón Micrófono";
    internal const string Version = "1.6.0";
    internal static string AppExe(string folder) => Path.Combine(folder, "BotonMicrofono.exe");
    internal static string Uninstaller(string folder) => Path.Combine(folder, "Desinstalar.exe");

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase))) Uninstall();
        else Application.Run(new InstallerForm());
    }

    internal static void Install(string folder)
    {
        MigratePreviousInstallation(folder);
        Directory.CreateDirectory(folder);
        using (var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("BotonMicrofono.exe")
            ?? throw new InvalidOperationException("No se encontró el programa incluido."))
        using (var destination = File.Create(AppExe(folder))) source.CopyTo(destination);
        File.Copy(Environment.ProcessPath!, Uninstaller(folder), true);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        var menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), AppName);
        Directory.CreateDirectory(menu);
        CreateShortcut(Path.Combine(desktop, AppName + ".lnk"), AppExe(folder), folder);
        CreateShortcut(Path.Combine(menu, AppName + ".lnk"), AppExe(folder), folder);
        CreateShortcut(Path.Combine(menu, "Desinstalar.lnk"), Uninstaller(folder), folder, "/uninstall");

        using var key = Registry.LocalMachine.CreateSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY", true)!;
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", Version);
        key.SetValue("Publisher", "SISTEMAS LY");
        key.SetValue("DisplayIcon", AppExe(folder));
        key.SetValue("UninstallString", $"\"{Uninstaller(folder)}\" /uninstall");
        key.SetValue("InstallLocation", folder);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void MigratePreviousInstallation(string newFolder)
    {
        foreach (var process in Process.GetProcessesByName("BotonMicrofono"))
        {
            try
            {
                process.CloseMainWindow();
                if (!process.WaitForExit(3000))
                    throw new InvalidOperationException("Cierra Botón Micrófono antes de continuar con la actualización.");
            }
            finally { process.Dispose(); }
        }

        var oldFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Boton Microfono");
        if (!oldFolder.Equals(newFolder, StringComparison.OrdinalIgnoreCase))
            try { if (Directory.Exists(oldFolder)) Directory.Delete(oldFolder, true); } catch { }
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY", false); } catch { }

        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (run?.GetValue("BotonMicrofonoLY") is string current && !string.IsNullOrWhiteSpace(current))
                run.SetValue("BotonMicrofonoLY", $"\"{AppExe(newFolder)}\"");
        }
        catch { }
    }

    private static void CreateShortcut(string shortcutPath, string target, string folder, string arguments = "")
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host no está disponible.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = target;
        shortcut.Arguments = arguments;
        shortcut.WorkingDirectory = folder;
        shortcut.Description = AppName + " · LY derechos reservados";
        shortcut.Save();
    }

    private static void Uninstall()
    {
        var folder = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        if (MessageBox.Show($"¿Quieres desinstalar {AppName}?", "Desinstalar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        foreach (var process in Process.GetProcessesByName("BotonMicrofono"))
            try { process.CloseMainWindow(); process.WaitForExit(2000); } catch { }

        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), AppName + ".lnk"));
        var menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), AppName);
        try { if (Directory.Exists(menu)) Directory.Delete(menu, true); } catch { }
        try { Registry.LocalMachine.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BotonMicrofonoLY", false); } catch { }
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            run?.DeleteValue("BotonMicrofonoLY", false);
        }
        catch { }
        TryDelete(AppExe(folder));
        MessageBox.Show($"{AppName} fue desinstalado.", "Desinstalar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"{folder}\"")
        { CreateNoWindow = true, UseShellExecute = false });
    }

    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
}

internal sealed class InstallerForm : Form
{
    private static readonly string Terms = LoadTerms();

    private readonly TextBox path = new() { Dock = DockStyle.Fill };
    private readonly CheckBox accept = new() { Text = "He leído y acepto los términos y condiciones", AutoSize = true };
    private readonly Button install = new() { Text = "Aceptar e instalar", Width = 145, Height = 36, Enabled = false };
    private readonly Button reject = new() { Text = "Rechazar", Width = 100, Height = 36 };
    private readonly Label status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private string installedExe = "";

    public InstallerForm()
    {
        Text = "Instalar Botón Micrófono 1.6.0";
        ClientSize = new Size(650, 560);
        MinimumSize = new Size(600, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        path.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Boton Microfono");

        var title = new Label { Text = "Botón Micrófono", Font = new Font("Segoe UI Semibold", 22F), AutoSize = true };
        var terms = new RichTextBox
        {
            Text = Terms,
            ReadOnly = true,
            DetectUrls = false,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 6, 0, 10)
        };
        var browse = new Button { Text = "Examinar…", Width = 90, Dock = DockStyle.Right };
        browse.Click += (_, _) => Browse();
        accept.Margin = new Padding(0, 7, 0, 5);
        accept.CheckedChanged += (_, _) => install.Enabled = accept.Checked;
        reject.Click += (_, _) => Close();
        install.Click += InstallClicked;

        var destination = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        destination.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        destination.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        destination.Controls.Add(path, 0, 0); destination.Controls.Add(browse, 1, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(install); buttons.Controls.Add(reject);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26, 22, 26, 20), RowCount = 8 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(new Label { Text = "Lee los términos antes de continuar:", Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(terms, 0, 2);
        layout.Controls.Add(accept, 0, 3);
        layout.Controls.Add(new Label { Text = "Carpeta de instalación:", Dock = DockStyle.Fill }, 0, 4);
        layout.Controls.Add(destination, 0, 5);
        layout.Controls.Add(buttons, 0, 6);
        layout.Controls.Add(status, 0, 7);
        Controls.Add(layout);
    }

    private static string LoadTerms()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Terms.txt")
            ?? throw new InvalidOperationException("No se encontraron los términos y condiciones.");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { Description = "Selecciona la carpeta de instalación", SelectedPath = path.Text, UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) path.Text = Path.Combine(dialog.SelectedPath, "Boton Microfono");
    }

    private async void InstallClicked(object? sender, EventArgs e)
    {
        if (!accept.Checked || string.IsNullOrWhiteSpace(path.Text)) return;
        install.Enabled = reject.Enabled = path.Enabled = false;
        status.Text = "Instalando…";
        try
        {
            var folder = Path.GetFullPath(path.Text.Trim());
            await Task.Run(() => Program.Install(folder));
            installedExe = Program.AppExe(folder);
            status.Text = "Instalación completada correctamente.";
            install.Text = "Abrir programa";
            install.Enabled = true;
            install.Click -= InstallClicked;
            install.Click += (_, _) => { Process.Start(new ProcessStartInfo(installedExe) { UseShellExecute = true }); Close(); };
        }
        catch (Exception ex)
        {
            status.Text = "No se pudo completar la instalación.";
            install.Enabled = reject.Enabled = path.Enabled = true;
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
