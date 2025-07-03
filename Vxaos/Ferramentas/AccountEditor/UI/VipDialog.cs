using System.ComponentModel;

namespace AccountEditorVxaos.UI;

public partial class VipDialog : Form
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Days { get; private set; }

    private readonly Color _primaryColor = Color.FromArgb(0, 120, 215);
    private readonly Color _backgroundColor = Color.WhiteSmoke;

    private NumericUpDown _daysNumericUpDown;
    private Button _okButton;
    private Button _cancelButton;

    public VipDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.Text = "Alterar VIP";
        this.Size = new Size(320, 160);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = _backgroundColor;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var label = new Label
        {
            Text = "Dias de VIP a adicionar:",
            Font = new Font("Segoe UI", 10),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(20, 15)
        };
        this.Controls.Add(label);

        _daysNumericUpDown = new NumericUpDown
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(100, 25),
            Location = new Point(20, 45),
            Minimum = 1,
            Maximum = 3650,
            Value = 30
        };
        this.Controls.Add(_daysNumericUpDown);

        _okButton = new Button
        {
            Text = "OK",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(90, 35),
            Location = new Point(100, 80),
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
            Size = new Size(90, 35),
            Location = new Point(200, 80),
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
        Days = (int)_daysNumericUpDown.Value;
    }
}