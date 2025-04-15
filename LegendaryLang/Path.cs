namespace LegendaryLang;

public class Path
{
    private List<string> _path = new List<string>();

    public Path(IEnumerable<String> path)
    {
        _path.AddRange(path);
    }
}