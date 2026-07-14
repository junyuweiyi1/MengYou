public class Logger
{
    private string _name;

    public Logger(string name)
    {
        _name = name;
    }

    public void Log(string message)
    {

    }

    public void LogFormat(string format, params object[] args)
    {

    }

    private string Title()
    {
        return $"[${_name}]";
    }
}