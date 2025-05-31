namespace LegendaryLang;

public class Path
{
    private readonly List<string> _path = new();

    public Path(IEnumerable<string> path)
    {
        _path.AddRange(path);
    }
}