namespace MiniMapLibrary.Config
{
    public interface IMutableConfigEntry<T> : IConfigEntry<T>
    {
        new T Value { get; set; }
    }
}
