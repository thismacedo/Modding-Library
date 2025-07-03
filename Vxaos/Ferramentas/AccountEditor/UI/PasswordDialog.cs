using System.ComponentModel;

namespace AccountEditorVxaos.UI;

public partial class PasswordDialog : Form
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string NewPassword { get; private set; } = string.Empty;

    private readonly Color _primaryColor = Color.FromArgb(0, 120, 215);
    private readonly Color _backgroundColor = Color.WhiteSmoke;

    private TextBox _passwordTextBox;
    private TextBox _confirmPasswordTextBox;
    private Button _okButton;
    private Button _cancelButton;

    public PasswordDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.Text = "Alterar Senha";
        this.Size = new Size(360, 200);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = _backgroundColor;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        var passwordLabel = new Label
        {
            Text = "Nova senha:",
            Font = new Font("Segoe UI", 10),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(20, 5)
        };
        this.Controls.Add(passwordLabel);

        _passwordTextBox = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(280, 25),
            Location = new Point(20, 30),
            UseSystemPasswordChar = true
        };
        this.Controls.Add(_passwordTextBox);

        var confirmLabel = new Label
        {
            Text = "Confirmar senha:",
            Font = new Font("Segoe UI", 10),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(20, 65)
        };
        this.Controls.Add(confirmLabel);

        _confirmPasswordTextBox = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(280, 25),
            Location = new Point(20, 90),
            UseSystemPasswordChar = true
        };
        this.Controls.Add(_confirmPasswordTextBox);

        _okButton = new Button
        {
            Text = "OK",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(90, 35),
            Location = new Point(130, 120),
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
            Location = new Point(230, 120),
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
        var password = _passwordTextBox.Text;
        var confirmPassword = _confirmPasswordTextBox.Text;

        if (string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("A senha não pode estar vazia!", "Erro",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }

        if (password != confirmPassword)
        {
            MessageBox.Show("As senhas não coincidem!", "Erro",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
            return;
        }

        NewPassword = password;
    }
}