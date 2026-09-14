using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

internal sealed class FakeBaseStore : IBaseStore
{
    public List<string> Destinations { get; } = new();

    public Task<TitleDirectory> StageAsync(BaseTitle @base, string destination, CancellationToken cancellationToken = default)
    {
        Destinations.Add(destination);
        return Task.FromResult(TestTitle.Populate(destination));
    }
}

internal sealed class FakeBootSoundConverter : IBootSoundConverter
{
    public List<(string Source, string Destination)> Calls { get; } = new();

    public Task ConvertAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        Calls.Add((sourcePath, destinationPath));
        File.WriteAllBytes(destinationPath, new byte[8]);
        return Task.CompletedTask;
    }
}

internal sealed class FakeImageConverter : IImageConverter
{
    public List<(string Source, ImageSlot Slot, string Destination)> Calls { get; } = new();

    public Task ConvertAsync(string sourcePath, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default)
    {
        Calls.Add((sourcePath, slot, destinationPath));
        File.WriteAllBytes(destinationPath, new byte[18]);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRomInjector : IRomInjector
{
    private readonly Func<Injection, TitleDirectory, Task>? _action;

    public FakeRomInjector(SourceConsole console, Func<Injection, TitleDirectory, Task>? action = null)
    {
        Console = console;
        _action = action;
    }

    public SourceConsole Console { get; }
    public List<TitleDirectory> Titles { get; } = new();

    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Titles.Add(title);
        progress?.Report("injecting");
        return _action?.Invoke(injection, title) ?? Task.CompletedTask;
    }
}

internal sealed class FakeTitlePacker : ITitlePacker
{
    public List<(TitleDirectory Title, string Output, string[] MetaFiles)> Calls { get; } = new();

    public Task PackAsync(TitleDirectory title, string outputDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        Calls.Add((title, outputDirectory, Directory.GetFiles(title.Meta).Select(f => Path.GetFileName(f)).ToArray()));
        File.WriteAllText(Path.Combine(outputDirectory, "title.tmd"), "packed");
        return Task.CompletedTask;
    }
}
