using System.ComponentModel;

namespace AccountEditorVxaos.UI;

public partial class GroupDialog : Form
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedGroup { get; private set; }

    private readonly Color _primaryColor = Color.FromArgb(0, 120, 215);
    private readonly Color _backgroundColor = Color.FromArgb(245, 245, 245);

    private RadioButton _defaultRadio;
    private RadioButton _monitorRadio;
    private RadioButton _adminRadio;
    private Button _okButton;
    private Button _cancelButton;

    public GroupDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.Text = "Alterar Grupo";
        this.Size = new Size(320, 220);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = _backgroundColor;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var label = new Label
        {
            Text = "Selecione o grupo:",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(20, 20)
        };
        this.Controls.Add(label);

        _defaultRadio = new RadioButton
        {
            Text = "Padrão (0)",
            Font = new Font("Segoe UI", 10),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(40, 55),
            Checked = true
        };
        this.Controls.Add(_defaultRadio);

        _monitorRadio = new RadioButton
        {
            Text = "Monitor (1)",
            Font = new Font("Segoe UI", 10),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(40, 80)
        };
        this.Controls.Add(_monitorRadio);

        _adminRadio = new RadioButton
        {
            Text = "Admin (2)",
            Font = new Font("Segoe UI", 10),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(40, 105)
        };
        this.Controls.Add(_adminRadio);

        _okButton = new Button
        {
            Text = "OK",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(100, 35), 
            Location = new Point(50, 140),
            BackColor = _primaryColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };
        _okButton.FlatAppearance.BorderSize = 0;
        _okButton.Click += OkButton_Click;
        this.Controls.Add(_okButton);

        _cancelButton = new Button
        {
            Text = "Cancelar",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(100, 35),
            Location = new Point(160, 140),
            BackColor = Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel
        };
        _cancelButton.FlatAppearance.BorderSize = 0;
        this.Controls.Add(_cancelButton);

        this.AcceptButton = _okButton;
        this.CancelButton = _cancelButton;

        this.ResumeLayout(false);
    }

    private void OkButton_Click(object sender, EventArgs e)
    {
        if (_defaultRadio.Checked) SelectedGroup = 0;
        else if (_monitorRadio.Checked) SelectedGroup = 1;
        else if (_adminRadio.Checked) SelectedGroup = 2;
    }
}