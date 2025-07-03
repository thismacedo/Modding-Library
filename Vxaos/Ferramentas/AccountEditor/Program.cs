using AccountEditorVxaos.Models;
using AccountEditorVxaos.Services;
using AccountEditorVxaos.UI;

namespace AccountEditorVxaos;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var config = Config.Load("configs.ini");
            using var db = new Database(config);
            
            Application.Run(new MainForm(db));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao inicializar aplicação: {ex.Message}", 
                          "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}