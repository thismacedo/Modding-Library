using Npgsql;
using System.Security.Cryptography;
using System.Text;
using AccountEditorVxaos.Models;

namespace AccountEditorVxaos.Services;

public class Database : IDisposable
{
    private readonly NpgsqlConnection _conn;

    public Database(Config config)
    {
        var connStr = $"Host={config.Host};Username={config.User};Password={config.Pass};Database={config.Name};Port={config.Port}";
        _conn = new NpgsqlConnection(connStr);
        _conn.Open();
    }

    public bool AccountExists(string username)
    {
        using var cmd = new NpgsqlCommand("SELECT id FROM accounts WHERE username = @username", _conn);
        cmd.Parameters.AddWithValue("username", username);
        using var reader = cmd.ExecuteReader();
        return reader.HasRows;
    }

    public void ChangeVipTime(string username, int seconds)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var getCmd = new NpgsqlCommand("SELECT vip_time FROM accounts WHERE username = @username", _conn);
        getCmd.Parameters.AddWithValue("username", username);
        var current = getCmd.ExecuteScalar();
        long vipTime = current != DBNull.Value ? Convert.ToInt64(current) : 0;
        long newVip = Math.Max(vipTime, now) + seconds;

        using var updateCmd = new NpgsqlCommand("UPDATE accounts SET vip_time = @vip WHERE username = @username", _conn);
        updateCmd.Parameters.AddWithValue("vip", newVip);
        updateCmd.Parameters.AddWithValue("username", username);
        updateCmd.ExecuteNonQuery();
    }

    public void ChangeGroup(string username, int group)
    {
        using var cmd = new NpgsqlCommand("UPDATE accounts SET \"group\" = @group WHERE username = @username", _conn);
        cmd.Parameters.AddWithValue("group", group);
        cmd.Parameters.AddWithValue("username", username);
        cmd.ExecuteNonQuery();
    }

    public void ChangePassword(string username, string password)
    {
        using var md5 = MD5.Create();
        var hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "").ToLower();

        using var cmd = new NpgsqlCommand("UPDATE accounts SET password = @pwd WHERE username = @username", _conn);
        cmd.Parameters.AddWithValue("pwd", hash);
        cmd.Parameters.AddWithValue("username", username);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _conn.Close();
}