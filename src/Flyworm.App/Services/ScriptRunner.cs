using System.Diagnostics;
using System.IO;
using System.Text;

namespace Flyworm.Services;

public sealed class ScriptRunner
{
    public async Task<int> RunPowerShellAsync(
        string scriptPath,
        IEnumerable<string> arguments,
        string workingDirectory,
        Action<string>? onOutput = null,
        CancellationToken cancellationToken = default)
    {
        var start = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-ExecutionPolicy");
        start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(scriptPath);
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"Script not found: {scriptPath}", scriptPath);
        }

        using var process = new Process { StartInfo = start, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                onOutput?.Invoke(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                onOutput?.Invoke(e.Data);
            }
        };
        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);
        if (!process.Start())
        {
            throw new InvalidOperationException("PowerShell başlatılamadı.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // ignored
            }
        });

        return await tcs.Task.WaitAsync(cancellationToken);
    }
}
