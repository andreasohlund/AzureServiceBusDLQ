using System.Runtime.InteropServices;
using Spectre.Console.Cli;

public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    protected abstract Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellation);

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        using var cancellationSource = new CancellationTokenSource();

        using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, onSignal);
        using var sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, onSignal);
        using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, onSignal);

        return await ExecuteAsync(context, settings, cancellationSource.Token);;

        void onSignal(PosixSignalContext signalContext)
        {
            signalContext.Cancel = true;
            cancellationSource.Cancel();
        }
    }
}