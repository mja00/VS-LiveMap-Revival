namespace livemap.registry;

public abstract class Registry<T>(string id) : Vintagestory.API.Datastructures.OrderedDictionary<string, T>, Keyed
    where T : Keyed {
    public string Id { get; } = $"{LiveMap.Api.ModId}:{id}";

    public virtual int Register(T value) => Register(value.Id, value);

    public virtual int Register(string id, T value) => Add(id, value);

    public virtual bool Unregister(T value) => Unregister(value.Id);

    public virtual bool Unregister(string id) => Remove(id);

    public virtual void Dispose() => Clear();
}
