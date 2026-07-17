public class Logger
{
    private readonly string _name;

    public Logger(string name)
    {
        _name = name;
    }

    public void Log(string message)
    {
        System.Diagnostics.Trace.WriteLine($"{DateTimeOffset.Now:O} {Title()} {message}");
    }

    public void LogFormat(string format, params object[] args)
    {
        Log(string.Format(format, args));
    }

    private string Title()
    {
        return $"[{_name}]";
    }
}
