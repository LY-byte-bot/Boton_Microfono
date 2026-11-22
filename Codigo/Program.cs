using System.Threading;

namespace BotonMicrofono;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, "BotonMicrofono_UnicaInstancia", out var first);
        if (!first)
        {
            MessageBox.Show("Botón Micrófono ya está abierto.", "Botón Micrófono",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
