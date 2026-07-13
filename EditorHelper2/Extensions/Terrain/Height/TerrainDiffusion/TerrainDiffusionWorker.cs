using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EditorHelper2.Extensions.Terrain.Height.TerrainDiffusion;

/// <summary>
/// Describes one invocation of the Terrain Diffusion Java sidecar.
/// </summary>
internal readonly struct TerrainDiffusionWorkerRequest
{
    public string WorkerJarPath { get; }
    public string ModelDirectory { get; }
    public long Seed { get; }
    public int I1 { get; }
    public int J1 { get; }
    public int I2 { get; }
    public int J2 { get; }
    public string OutputPath { get; }

    public TerrainDiffusionWorkerRequest(
        string workerJarPath,
        string modelDirectory,
        long seed,
        int i1,
        int j1,
        int i2,
        int j2,
        string outputPath)
    {
        WorkerJarPath = workerJarPath;
        ModelDirectory = modelDirectory;
        Seed = seed;
        I1 = i1;
        J1 = j1;
        I2 = i2;
        J2 = j2;
        OutputPath = outputPath;
    }
}

/// <summary>
/// Runs the Java sidecar and reads its compact Terrain Diffusion heightmap format.
/// </summary>
internal static class TerrainDiffusionWorker
{
    private const int MaxDimension = 16385;
    private const long MaxSampleCount = 268_000_000;

    public static async Task<TerrainDiffusionHeightmap> RunAsync(
        TerrainDiffusionWorkerRequest request,
        Action<string>? reportProgress,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        ProcessStartInfo startInfo = new()
        {
            FileName = "java",
            Arguments = BuildArguments(request),
            WorkingDirectory = Path.GetDirectoryName(request.WorkerJarPath) ?? System.Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using Process process = new() { StartInfo = startInfo, EnableRaisingEvents = false };
        reportProgress?.Invoke("Starting Java diffusion worker...");

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Java process could not be started.");
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException(
                "Unable to start Java. Install a Java runtime and ensure 'java' is on PATH.", ex);
        }

        Task stdoutTask = PumpOutputAsync(process.StandardOutput, reportProgress, false);
        Task stderrTask = PumpOutputAsync(process.StandardError, reportProgress, true);
        Task exitTask = Task.Run(() => process.WaitForExit());

        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(() => TryKill(process));
        try
        {
            while (!exitTask.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100).ConfigureAwait(false);
            }

            await exitTask.ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            try
            {
                await exitTask.ConfigureAwait(false);
                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
            }
            catch
            {
                // Cancellation remains the operation's authoritative result.
            }

            throw;
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Terrain Diffusion worker exited with code {process.ExitCode}.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        reportProgress?.Invoke("Worker finished; loading heightmap...");
        return await Task.Run(() => TerrainDiffusionHeightmap.Load(request.OutputPath), cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task PumpOutputAsync(StreamReader reader, Action<string>? reportProgress, bool isError)
    {
        try
        {
            while (true)
            {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    return;
                }

                string text = line.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                if (text.Length > 300)
                {
                    text = text.Substring(0, 300) + "...";
                }

                reportProgress?.Invoke(isError ? $"Worker: {text}" : text);
            }
        }
        catch (ObjectDisposedException)
        {
            // Process shutdown can close redirected streams before the pump observes EOF.
        }
        catch (IOException)
        {
            // Treat stream closure during cancellation as benign.
        }
    }

    private static string BuildArguments(TerrainDiffusionWorkerRequest request)
    {
        return string.Join(" ",
            "-jar",
            Quote(request.WorkerJarPath),
            "--model-dir",
            Quote(request.ModelDirectory),
            "--seed",
            request.Seed.ToString(CultureInfo.InvariantCulture),
            "--i1",
            request.I1.ToString(CultureInfo.InvariantCulture),
            "--j1",
            request.J1.ToString(CultureInfo.InvariantCulture),
            "--i2",
            request.I2.ToString(CultureInfo.InvariantCulture),
            "--j2",
            request.J2.ToString(CultureInfo.InvariantCulture),
            "--output",
            Quote(request.OutputPath));
    }

    private static string Quote(string value)
    {
        if (value.IndexOf('"') >= 0)
        {
            throw new ArgumentException("Paths must not contain quote characters.", nameof(value));
        }

        // Escape trailing backslashes so a Windows command-line parser does not
        // consume the closing quote (important for a model directory such as C:\\).
        int trailingBackslashes = 0;
        for (int i = value.Length - 1; i >= 0 && value[i] == '\\'; i--)
        {
            trailingBackslashes++;
        }

        if (trailingBackslashes == 0)
        {
            return $"\"{value}\"";
        }

        return $"\"{value}{new string('\\', trailingBackslashes)}\"";
    }

    private static void ValidateRequest(TerrainDiffusionWorkerRequest request)
    {
        if (!File.Exists(request.WorkerJarPath))
        {
            throw new FileNotFoundException("Terrain Diffusion worker JAR was not found.", request.WorkerJarPath);
        }

        if (!Directory.Exists(request.ModelDirectory))
        {
            throw new DirectoryNotFoundException($"Terrain Diffusion model directory was not found: {request.ModelDirectory}");
        }

        if (string.IsNullOrWhiteSpace(request.OutputPath))
        {
            throw new ArgumentException("An output path is required.", nameof(request));
        }

        if (request.I2 <= request.I1 || request.J2 <= request.J1)
        {
            throw new ArgumentException("Terrain Diffusion coordinates must describe a non-empty region.", nameof(request));
        }

        long sampleCount = (long)(request.I2 - request.I1) * (request.J2 - request.J1);
        if (request.I2 - request.I1 > MaxDimension || request.J2 - request.J1 > MaxDimension || sampleCount > MaxSampleCount)
        {
            throw new ArgumentException("The requested Terrain Diffusion region is too large.", nameof(request));
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited or was never started.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The process can disappear between HasExited and Kill.
        }
    }
}

/// <summary>
/// Binary little-endian Terrain Diffusion heightmap.
/// </summary>
internal sealed class TerrainDiffusionHeightmap
{
    private const int HeaderSize = 16;
    private readonly float[] _values;

    public int Width { get; }
    public int Height { get; }

    private TerrainDiffusionHeightmap(int width, int height, float[] values)
    {
        Width = width;
        Height = height;
        _values = values;
    }

    public static TerrainDiffusionHeightmap Load(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: false);

        if (stream.Length < HeaderSize)
        {
            throw new InvalidDataException("Terrain Diffusion output is too short.");
        }

        byte[] magic = reader.ReadBytes(4);
        if (magic.Length != 4 || magic[0] != (byte)'T' || magic[1] != (byte)'D' || magic[2] != (byte)'H' || magic[3] != (byte)'M')
        {
            throw new InvalidDataException("Terrain Diffusion output has an invalid TDHM header.");
        }

        int version = reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported Terrain Diffusion output version: {version}.");
        }

        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        if (width < 1 || height < 1 || width > 16385 || height > 16385)
        {
            throw new InvalidDataException($"Invalid Terrain Diffusion dimensions: {width}x{height}.");
        }

        long sampleCount = (long)width * height;
        long expectedLength = HeaderSize + sampleCount * sizeof(float);
        if (sampleCount > 268_000_000 || stream.Length != expectedLength)
        {
            throw new InvalidDataException($"Terrain Diffusion output length does not match {width}x{height} samples.");
        }

        float[] values = new float[sampleCount];
        for (int index = 0; index < values.Length; index++)
        {
            float value = reader.ReadSingle();
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidDataException("Terrain Diffusion output contains a non-finite height.");
            }

            values[index] = value;
        }

        return new TerrainDiffusionHeightmap(width, height, values);
    }

    public float Sample(float row, float column)
    {
        if (Width == 1 && Height == 1)
        {
            return _values[0];
        }

        float x = Math.Clamp(column, 0f, Width - 1f);
        float y = Math.Clamp(row, 0f, Height - 1f);
        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        int x1 = Math.Min(x0 + 1, Width - 1);
        int y1 = Math.Min(y0 + 1, Height - 1);
        float tx = x - x0;
        float ty = y - y0;

        float a = Lerp(_values[y0 * Width + x0], _values[y0 * Width + x1], tx);
        float b = Lerp(_values[y1 * Width + x0], _values[y1 * Width + x1], tx);
        return Lerp(a, b, ty);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
