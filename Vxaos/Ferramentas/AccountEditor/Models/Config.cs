namespace AccountEditorVxaos.Models;

public class Config
{
    public string Host = "", User = "", Pass = "", Name = "";
    public int Port;

    public static Config Load(string path)
    {
        var config = new Config();
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Contains("DATABASE_HOST")) config.Host = GetValue(line);
            else if (line.Contains("DATABASE_USER")) config.User = GetValue(line);
            else if (line.Contains("DATABASE_PASS")) config.Pass = GetValue(line);
            else if (line.Contains("DATABASE_NAME")) config.Name = GetValue(line);
            else if (line.Contains("DATABASE_PORT")) config.Port = int.Parse(GetValue(line));
        }
        return config;
    }

    private static string GetValue(string line) =>
        line.Split('=').Last().Trim().Replace("\"", "").Replace("'", "");
}