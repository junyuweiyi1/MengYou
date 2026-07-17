using System.Diagnostics;

public class Logger
{
    private string _name;

    public Logger(string name)
    {
        _name = name;
    }

    public void Log(string message)
    {
        Trace.WriteLine($"{Title()} {message}");
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
