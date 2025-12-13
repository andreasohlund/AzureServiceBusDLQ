using Spectre.Console.Cli;

public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IAsyncDisposable, IDisposable
{
    public object? Resolve(Type? type)
    {
        return type == null ? null : provider.GetService(type);
    }

    public void Dispose()
    {
        //Hack until spectre support async disposable (add issue/pr link) 
        DisposeAsync().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (provider is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}