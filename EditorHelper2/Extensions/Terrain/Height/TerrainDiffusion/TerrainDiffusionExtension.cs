using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.common.Helpers;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit;
using SDG.Framework.Landscapes;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Terrain.Height.TerrainDiffusion;

/// <summary>
/// Generates Unturned terrain from the Terrain Diffusion Java/ONNX sidecar.
/// </summary>
[UIExtension(typeof(EditorTerrainHeightUI))]
[EHExtension("Terrain Diffusion Generator", "Senior S")]
public sealed class TerrainDiffusionExtension : UIExtension, IExtension
{
    private enum EGenerationScope
    {
        WholeMap = 0,
        CurrentTile = 1
    }

    private readonly EditorTerrainHeightUI? _currentUIInstance;
    private readonly SleekButtonState _scopeButton;
    private readonly ISleekField _workerJarField;
    private readonly ISleekField _modelDirectoryField;
    private readonly ISleekField _seedField;
    private readonly ISleekInt32Field _worldScaleField;
    private readonly ISleekFloat32Field _verticalScaleField;
    private readonly ISleekFloat32Field _seaOffsetField;
    private readonly ISleekButton _generateButton;
    private readonly ISleekButton _cancelButton;
    private readonly ISleekLabel _statusLabel;

    private CancellationTokenSource? _generationCancellation;
    private Task? _generationTask;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Creates the Terrain Diffusion controls for the terrain-height editor page.
    /// </summary>
    /// <param name="instance">The terrain-height UI instance receiving the controls.</param>
    public TerrainDiffusionExtension(EditorTerrainHeightUI instance)
    {
        _currentUIInstance = instance;

        UIBuilder builder = new(340f, 30f);
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(-360f)
            .SetOffsetVertical(40f)
            .SetSizeHorizontal(340f)
            .SetSizeVertical(30f);

        _scopeButton = builder.BuildButtonState(
            new GUIContent("Whole Map", "Generate the complete level terrain bounds"),
            new GUIContent("Current Tile", "Generate only the selected terrain tile"));
        _scopeButton.AddLabel("Scope", ESleekSide.LEFT);
        _scopeButton.onSwappedState += OnScopeChanged;

        builder.SetOffsetVertical(76f)
            .SetText(@"C:\Tools\terrain-diffusion-worker.jar");
        _workerJarField = builder.BuildStringField();
        _workerJarField.TooltipText = "Path to the Java worker JAR";

        builder.SetOffsetVertical(112f)
            .SetText(@"C:\Tools\terrain-diffusion-models");
        _modelDirectoryField = builder.BuildStringField();
        _modelDirectoryField.TooltipText = "Directory containing the diffusion model assets";

        builder.SetOffsetVertical(148f)
            .SetText("1337");
        _seedField = builder.BuildStringField();
        _seedField.Text = "1337";
        _seedField.TooltipText = "Deterministic 64-bit Terrain Diffusion seed";

        builder.SetOffsetVertical(184f)
            .SetText("World Scale");
        _worldScaleField = builder.BuildInt32Field("World units represented by one native model sample");
        _worldScaleField.Value = 1;

        builder.SetOffsetVertical(220f)
            .SetText("Vertical Scale");
        _verticalScaleField = builder.BuildFloatInput();
        _verticalScaleField.Value = 1f;

        builder.SetOffsetVertical(256f)
            .SetText("Sea Offset");
        _seaOffsetField = builder.BuildFloatInput();
        _seaOffsetField.Value = 0f;

        builder.SetOffsetVertical(292f)
            .SetText("Generate Terrain");
        _generateButton = builder.BuildButton("Run the Terrain Diffusion worker");
        _generateButton.OnClicked += OnGenerateClicked;

        builder.SetOffsetHorizontal(-180f)
            .SetOffsetVertical(292f)
            .SetSizeHorizontal(160f)
            .SetText("Cancel");
        _cancelButton = builder.BuildButton("Cancel the active worker process");
        _cancelButton.IsClickable = false;
        _cancelButton.OnClicked += OnCancelClicked;

        builder.SetOffsetHorizontal(-360f)
            .SetOffsetVertical(328f)
            .SetSizeHorizontal(340f)
            .SetText("Ready");
        _statusLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _statusLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        Initialize();
    }

    /// <summary>
    /// Adds the controls to the terrain-height editor page.
    /// </summary>
    public void Initialize()
    {
        if (_currentUIInstance == null)
        {
            return;
        }

        _currentUIInstance.AddChild(_scopeButton);
        _currentUIInstance.AddChild(_workerJarField);
        _currentUIInstance.AddChild(_modelDirectoryField);
        _currentUIInstance.AddChild(_seedField);
        _currentUIInstance.AddChild(_worldScaleField);
        _currentUIInstance.AddChild(_verticalScaleField);
        _currentUIInstance.AddChild(_seaOffsetField);
        _currentUIInstance.AddChild(_generateButton);
        _currentUIInstance.AddChild(_cancelButton);
        _currentUIInstance.AddChild(_statusLabel);
    }

    private void OnScopeChanged(SleekButtonState button, int index)
    {
        if (_isRunning)
        {
            return;
        }

        SetStatus($"Scope: {(EGenerationScope)index}", Color.white);
    }

    private void OnGenerateClicked(ISleekElement button)
    {
        if (_isRunning || _disposed)
        {
            return;
        }

        if (!TryReadRequest(out TerrainDiffusionWorkerRequest request, out GenerationRegion region, out string error))
        {
            SetStatus(error, Color.red);
            return;
        }

        CancellationTokenSource cancellation = new();
        _generationCancellation = cancellation;
        _isRunning = true;
        _generateButton.IsClickable = false;
        _cancelButton.IsClickable = true;
        SetStatus($"Generating {region.TileCount} tile(s) with Terrain Diffusion...", Color.white);

        _generationTask = RunGenerationAsync(request, region, cancellation);
    }

    private void OnCancelClicked(ISleekElement button)
    {
        if (!_isRunning)
        {
            return;
        }

        _generationCancellation?.Cancel();
        SetStatus("Cancelling Terrain Diffusion worker...", Color.yellow);
    }

    private async Task RunGenerationAsync(
        TerrainDiffusionWorkerRequest request,
        GenerationRegion region,
        CancellationTokenSource cancellation)
    {
        try
        {
            TerrainDiffusionHeightmap heightmap = await TerrainDiffusionWorker.RunAsync(
                request,
                message => QueueStatus(message, Color.white),
                cancellation.Token).ConfigureAwait(false);

            cancellation.Token.ThrowIfCancellationRequested();
            await TaskDispatcher.QueueOnMainThreadAsync(
                () => ApplyHeightmap(heightmap, request, region),
                cancellation.Token).ConfigureAwait(false);

            QueueCompletion(() => SetStatus(
                $"Generated {region.TileCount} tile(s), {heightmap.Width}x{heightmap.Height} samples.",
                Color.green));
        }
        catch (OperationCanceledException)
        {
            QueueCompletion(() => SetStatus("Terrain Diffusion generation cancelled.", Color.yellow));
        }
        catch (Exception ex)
        {
            UnturnedLog.error($"Terrain Diffusion generation failed: {ex}");
            QueueCompletion(() => SetStatus($"Terrain Diffusion failed: {ex.Message}", Color.red));
        }
        finally
        {
            TryDelete(request.OutputPath);
            QueueCompletion(() =>
            {
                _isRunning = false;
                _generateButton.IsClickable = true;
                _cancelButton.IsClickable = false;
                if (ReferenceEquals(_generationCancellation, cancellation))
                {
                    _generationCancellation = null;
                    _generationTask = null;
                }
            });

            cancellation.Dispose();
        }
    }

    private void ApplyHeightmap(TerrainDiffusionHeightmap heightmap, TerrainDiffusionWorkerRequest request, GenerationRegion region)
    {
        if (_disposed)
        {
            return;
        }

        if (heightmap.Width != request.J2 - request.J1 || heightmap.Height != request.I2 - request.I1)
        {
            throw new InvalidDataException(
                $"Worker output dimensions {heightmap.Width}x{heightmap.Height} do not match requested " +
                $"region {(request.J2 - request.J1)}x{(request.I2 - request.I1)}.");
        }

        float verticalScale = region.VerticalScale;
        float seaOffset = region.SeaOffset;
        float worldScale = region.WorldScale;

        Landscape.writeHeightmap(region.WorldBounds, (_, _, worldPosition, currentHeight) =>
        {
            float row = worldPosition.z / worldScale - request.I1;
            float column = worldPosition.x / worldScale - request.J1;
            float elevation = heightmap.Sample(row, column);
            float worldHeight = elevation * verticalScale + seaOffset;
            if (float.IsNaN(worldHeight) || float.IsInfinity(worldHeight))
            {
                return currentHeight;
            }

            return Mathf.Clamp01(worldHeight / Landscape.TILE_HEIGHT + 0.5f);
        });

        Landscape.applyLOD();
        LevelHierarchy.MarkDirty();
    }

    private bool TryReadRequest(out TerrainDiffusionWorkerRequest request, out GenerationRegion region, out string error)
    {
        request = default;
        region = default;
        error = string.Empty;

        string workerPath = NormalizePath(_workerJarField.Text);
        if (workerPath.Length == 0 || !File.Exists(workerPath))
        {
            error = "Worker JAR was not found. Set an absolute worker path.";
            return false;
        }

        string modelDirectory = NormalizePath(_modelDirectoryField.Text);
        if (modelDirectory.Length == 0 || !Directory.Exists(modelDirectory))
        {
            error = "Model directory was not found. Set an absolute model directory path.";
            return false;
        }

        if (!long.TryParse((_seedField.Text ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long seed))
        {
            error = "Seed must be a valid 64-bit integer.";
            return false;
        }

        int worldScale = Mathf.Clamp(_worldScaleField.Value, 1, 128);
        float verticalScale = Mathf.Clamp(_verticalScaleField.Value, -1000f, 1000f);
        float seaOffset = Mathf.Clamp(_seaOffsetField.Value, -Landscape.TILE_HEIGHT * 4f, Landscape.TILE_HEIGHT * 4f);
        if (Mathf.Abs(verticalScale) < 0.0001f)
        {
            error = "Vertical Scale must not be zero.";
            return false;
        }

        if (!TryResolveRegion((EGenerationScope)_scopeButton.state, worldScale, verticalScale, seaOffset, out region, out error))
        {
            return false;
        }

        string outputDirectory = Path.Combine(Path.GetTempPath(), "EditorHelper2", "TerrainDiffusion");
        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory, $"terrain-{Guid.NewGuid():N}.tdhm");

        request = new TerrainDiffusionWorkerRequest(
            workerPath,
            modelDirectory,
            seed,
            region.I1,
            region.J1,
            region.I2,
            region.J2,
            outputPath);
        return true;
    }

    private static bool TryResolveRegion(
        EGenerationScope scope,
        int worldScale,
        float verticalScale,
        float seaOffset,
        out GenerationRegion region,
        out string error)
    {
        region = default;
        error = string.Empty;
        Bounds worldBounds;

        if (scope == EGenerationScope.CurrentTile)
        {
            LandscapeCoord coord = TerrainEditor.selectedTile != null
                ? TerrainEditor.selectedTile.coord
                : new LandscapeCoord(Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            LandscapeTile? tile = Landscape.getTile(coord);
            if (tile == null)
            {
                error = "Select an existing terrain tile before generating Current Tile.";
                return false;
            }

            worldBounds = tile.worldBounds;
        }
        else
        {
            float halfSize = SDG.Unturned.Level.size * 0.5f;
            worldBounds = default;
            worldBounds.SetMinMax(
                new Vector3(-halfSize, -Landscape.TILE_HEIGHT * 0.5f, -halfSize),
                new Vector3(halfSize, Landscape.TILE_HEIGHT * 0.5f, halfSize));
        }

        int i1 = Mathf.FloorToInt(worldBounds.min.z / worldScale);
        int j1 = Mathf.FloorToInt(worldBounds.min.x / worldScale);
        int i2 = Mathf.CeilToInt(worldBounds.max.z / worldScale);
        int j2 = Mathf.CeilToInt(worldBounds.max.x / worldScale);
        int tileCount = CountExistingTiles(worldBounds);
        if (i2 <= i1 || j2 <= j1)
        {
            error = "The selected generation scope is empty.";
            return false;
        }

        if (tileCount == 0)
        {
            error = "No existing terrain tiles were found in the selected scope.";
            return false;
        }

        long sampleCount = (long)(i2 - i1) * (j2 - j1);
        if (sampleCount > 100_000_000)
        {
            error = "The selected scope is too large. Increase World Scale or use Current Tile.";
            return false;
        }

        region = new GenerationRegion(worldBounds, i1, j1, i2, j2, tileCount, worldScale, verticalScale, seaOffset);
        return true;
    }

    private static int CountExistingTiles(Bounds bounds)
    {
        int minX = Mathf.FloorToInt(bounds.min.x / Landscape.TILE_SIZE);
        int minY = Mathf.FloorToInt(bounds.min.z / Landscape.TILE_SIZE);
        int maxX = Mathf.CeilToInt(bounds.max.x / Landscape.TILE_SIZE);
        int maxY = Mathf.CeilToInt(bounds.max.z / Landscape.TILE_SIZE);
        int count = 0;
        for (int x = minX; x < maxX; x++)
        {
            for (int y = minY; y < maxY; y++)
            {
                if (Landscape.getTile(new LandscapeCoord(x, y)) != null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static string NormalizePath(string? input)
    {
        string path = System.Environment.ExpandEnvironmentVariables((input ?? string.Empty).Trim().Trim('"'));
        if (path.Length == 0)
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private void QueueStatus(string text, Color color)
    {
        QueueCompletion(() => SetStatus(text, color));
    }

    private void QueueCompletion(System.Action action)
    {
        if (_disposed)
        {
            return;
        }

        TaskDispatcher.QueueOnMainThread(action);
    }

    private void SetStatus(string text, Color color)
    {
        if (_disposed)
        {
            return;
        }

        _statusLabel.TextColor = color;
        _statusLabel.Text = text;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A stale temp file is harmless and can be cleaned up by the next run.
        }
        catch (UnauthorizedAccessException)
        {
            // A stale temp file is harmless and can be cleaned up by the next run.
        }
    }

    /// <summary>
    /// Removes the controls and cancels any active Java worker.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _generationCancellation?.Cancel();
        _scopeButton.onSwappedState -= OnScopeChanged;
        _generateButton.OnClicked -= OnGenerateClicked;
        _cancelButton.OnClicked -= OnCancelClicked;

        if (_currentUIInstance == null)
        {
            return;
        }

        _currentUIInstance.RemoveChild(_scopeButton);
        _currentUIInstance.RemoveChild(_workerJarField);
        _currentUIInstance.RemoveChild(_modelDirectoryField);
        _currentUIInstance.RemoveChild(_seedField);
        _currentUIInstance.RemoveChild(_worldScaleField);
        _currentUIInstance.RemoveChild(_verticalScaleField);
        _currentUIInstance.RemoveChild(_seaOffsetField);
        _currentUIInstance.RemoveChild(_generateButton);
        _currentUIInstance.RemoveChild(_cancelButton);
        _currentUIInstance.RemoveChild(_statusLabel);
    }

    private readonly struct GenerationRegion
    {
        public Bounds WorldBounds { get; }
        public int I1 { get; }
        public int J1 { get; }
        public int I2 { get; }
        public int J2 { get; }
        public int TileCount { get; }
        public float WorldScale { get; }
        public float VerticalScale { get; }
        public float SeaOffset { get; }

        public GenerationRegion(
            Bounds worldBounds,
            int i1,
            int j1,
            int i2,
            int j2,
            int tileCount,
            float worldScale,
            float verticalScale,
            float seaOffset)
        {
            WorldBounds = worldBounds;
            I1 = i1;
            J1 = j1;
            I2 = i2;
            J2 = j2;
            TileCount = tileCount;
            WorldScale = worldScale;
            VerticalScale = verticalScale;
            SeaOffset = seaOffset;
        }
    }
}
