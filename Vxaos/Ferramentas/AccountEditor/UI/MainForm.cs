using AccountEditorVxaos.Services;

namespace AccountEditorVxaos.UI;

public partial class MainForm : Form
{
    private readonly Database _db;

    private readonly Color _primaryColor = Color.FromArgb(0, 120, 215);
    private readonly Color _backgroundColor = Color.WhiteSmoke;
    private readonly Color _lightAccent = Color.FromArgb(224, 240, 255);

    private TextBox _usernameTextBox;
    private Button _searchButton;
    private Label _statusLabel;
    private GroupBox _actionsGroupBox;
    private Button _changeVipButton;
    private Button _changeGroupButton;
    private Button _changePasswordButton;
    private string _currentUser = string.Empty;

    public MainForm(Database db)
    {
        _db = db;
        InitializeComponent();
        SetupUI();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        this.Text = "AccountEditor Vxaos";
        this.Size = new Size(520, 430);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = _backgroundColor;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = true;

        this.ResumeLayout(false);
    }

    private void SetupUI()
    {
        this.SuspendLayout();

        var titleLabel = new Label
        {
            Text = "Account Editor Vxaos",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(120, 20)
        };
        this.Controls.Add(titleLabel);

        var usernameLabel = new Label
        {
            Text = "Nome da conta:",
            Font = new Font("Segoe UI", 10),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(30, 80)
        };
        this.Controls.Add(usernameLabel);

        _usernameTextBox = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(260, 27),
            Location = new Point(30, 105),
            BorderStyle = BorderStyle.FixedSingle
        };
        _usernameTextBox.KeyDown += UsernameTextBox_KeyDown;
        this.Controls.Add(_usernameTextBox);

        _searchButton = new Button
        {
            Text = "Buscar",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(90, 30),
            Location = new Point(300, 102),
            BackColor = _primaryColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _searchButton.FlatAppearance.BorderSize = 0;
        _searchButton.Click += SearchButton_Click;
        this.Controls.Add(_searchButton);

        _statusLabel = new Label
        {
            Text = "Digite o nome de uma conta e clique em Buscar",
            Font = new Font("Segoe UI", 9),
            ForeColor = _primaryColor,
            AutoSize = true,
            Location = new Point(30, 145)
        };
        this.Controls.Add(_statusLabel);

        _actionsGroupBox = new GroupBox
        {
            Text = "Ações",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = _primaryColor,
            Size = new Size(440, 190),
            Location = new Point(30, 175),
            Enabled = false
        };

        _changeVipButton = new Button
        {
            Text = "Alterar VIP",
            Font = new Font("Segoe UI", 10),
            Size = new Size(125, 40),
            Location = new Point(20, 40),
            BackColor = _lightAccent,
            ForeColor = _primaryColor,
            FlatStyle = FlatStyle.Flat
        };
        _changeVipButton.FlatAppearance.BorderColor = _primaryColor;
        _changeVipButton.Click += ChangeVipButton_Click;
        _actionsGroupBox.Controls.Add(_changeVipButton);

        _changeGroupButton = new Button
        {
            Text = "Alterar Grupo",
            Font = new Font("Segoe UI", 10),
            Size = new Size(125, 40),
            Location = new Point(155, 40),
            BackColor = _lightAccent,
            ForeColor = _primaryColor,
            FlatStyle = FlatStyle.Flat
        };
        _changeGroupButton.FlatAppearance.BorderColor = _primaryColor;
        _changeGroupButton.Click += ChangeGroupButton_Click;
        _actionsGroupBox.Controls.Add(_changeGroupButton);

        _changePasswordButton = new Button
        {
            Text = "Alterar Senha",
            Font = new Font("Segoe UI", 10),
            Size = new Size(125, 40),
            Location = new Point(290, 40),
            BackColor = _lightAccent,
            ForeColor = _primaryColor,
            FlatStyle = FlatStyle.Flat
        };
        _changePasswordButton.FlatAppearance.BorderColor = _primaryColor;
        _changePasswordButton.Click += ChangePasswordButton_Click;
        _actionsGroupBox.Controls.Add(_changePasswordButton);

        this.Controls.Add(_actionsGroupBox);

        var footerLabel = new Label
        {
            Text = "AccountEditor Vxaos v1.0",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(30, 370)
        };
        this.Controls.Add(footerLabel);

        this.ResumeLayout(false);
    }

    private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            SearchButton_Click(sender, e);
        }
    }

    private void SearchButton_Click(object sender, EventArgs e)
    {
        var username = _usernameTextBox.Text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            MessageBox.Show("Digite um nome de usuário!", "Erro",
                          MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_db.AccountExists(username))
        {
            _currentUser = username;
            _statusLabel.Text = $"Conta encontrada: {username}";
            _statusLabel.ForeColor = Color.Green;
            _actionsGroupBox.Enabled = true;
        }
        else
        {
            _currentUser = string.Empty;
            _statusLabel.Text = "Conta não encontrada!";
            _statusLabel.ForeColor = Color.Red;
            _actionsGroupBox.Enabled = false;
        }
    }

    private void ChangeVipButton_Click(object sender, EventArgs e)
    {
        using var dialog = new VipDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                _db.ChangeVipTime(_currentUser, dialog.Days * 86400);
                MessageBox.Show($"VIP atualizado com sucesso! {dialog.Days} dias adicionados.",
                              "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar VIP: {ex.Message}",
                              "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ChangeGroupButton_Click(object sender, EventArgs e)
    {
        using var dialog = new GroupDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                _db.ChangeGroup(_currentUser, dialog.SelectedGroup);
                string groupName = dialog.SelectedGroup switch
                {
                    0 => "Padrão",
                    1 => "Monitor",
                    2 => "Admin",
                    _ => "Desconhecido"
                };
                MessageBox.Show($"Grupo alterado com sucesso para: {groupName}",
                              "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar grupo: {ex.Message}",
                              "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void ChangePasswordButton_Click(object sender, EventArgs e)
    {
        using var dialog = new PasswordDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                _db.ChangePassword(_currentUser, dialog.NewPassword);
                MessageBox.Show("Senha alterada com sucesso!",
                              "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao alterar senha: {ex.Message}",
                              "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}