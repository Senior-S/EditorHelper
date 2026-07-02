using System;
using System.IO;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit;
using SDG.Framework.Landscapes;
using SDG.Unturned;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorHelper2.Extensions.Terrain.Height;

[UIExtension(typeof(EditorTerrainHeightUI))]
[EHExtension("Heightmap Importer", "Senior S")]
public class HeightmapImporterExtension : UIExtension, IExtension
{
    private enum EImportScope
    {
        WholeMap = 0,
        CurrentTile = 1
    }

    private readonly EditorTerrainHeightUI? _currentUIInstance;

    private readonly SleekButtonState _scopeButton;
    private readonly ISleekField _pathField;
    private readonly ISleekFloat32Field _minHeightField;
    private readonly ISleekFloat32Field _maxHeightField;
    private readonly ISleekInt32Field _smoothPassesField;
    private readonly ISleekToggle _flipYToggle;
    private readonly ISleekButton _importButton;
    private readonly ISleekLabel _statusLabel;

    public HeightmapImporterExtension(EditorTerrainHeightUI instance)
    {
        _currentUIInstance = instance;

        UIBuilder builder = new(220f, 30f);
        builder.SetAnchorHorizontal(0f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(10f)
            .SetOffsetVertical(40f)
            .SetSizeHorizontal(220f)
            .SetSizeVertical(30f);

        _scopeButton = builder.BuildButtonState(
            new GUIContent("Whole Map", "Import across the existing map terrain bounds"),
            new GUIContent("Current Tile", "Import into the selected terrain tile")
        );
        _scopeButton.AddLabel("Import", ESleekSide.RIGHT);

        builder.SetOffsetVertical(76f)
            .SetText(@"C:\heightmap.png");
        _pathField = builder.BuildStringField();
        _pathField.TooltipText = "PNG/JPG image, square 16-bit raw, or Unturned .heightmap file";

        builder.SetOffsetVertical(112f)
            .SetText("Min Height");
        _minHeightField = builder.BuildFloatInput(ESleekSide.RIGHT);
        _minHeightField.Value = -Landscape.TILE_HEIGHT / 2f;

        builder.SetOffsetVertical(148f)
            .SetText("Max Height");
        _maxHeightField = builder.BuildFloatInput(ESleekSide.RIGHT);
        _maxHeightField.Value = Landscape.TILE_HEIGHT / 2f;

        builder.SetOffsetVertical(184f)
            .SetText("Smoothing");
        _smoothPassesField = builder.BuildInt32Field("Box blur passes applied before import", ESleekSide.RIGHT);
        _smoothPassesField.Value = 0;

        builder.SetOffsetVertical(220f)
            .SetSizeHorizontal(32f)
            .SetText("Flip Y");
        _flipYToggle = builder.BuildToggle("Flip the image vertically before sampling", ESleekSide.RIGHT);
        _flipYToggle.Value = true;

        builder.SetOffsetVertical(256f)
            .SetSizeHorizontal(220f)
            .SetText("Import Heightmap");
        _importButton = builder.BuildButton("Import heightmap into terrain");
        _importButton.OnClicked += OnImportClicked;

        builder.SetOffsetHorizontal(20f)
            .SetOffsetVertical(292f)
            .SetSizeHorizontal(330f)
            .SetText("Ready");
        _statusLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _statusLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        Initialize();
    }

    public void Initialize()
    {
        if (_currentUIInstance == null) return;

        _currentUIInstance.AddChild(_scopeButton);
        _currentUIInstance.AddChild(_pathField);
        _currentUIInstance.AddChild(_minHeightField);
        _currentUIInstance.AddChild(_maxHeightField);
        _currentUIInstance.AddChild(_smoothPassesField);
        _currentUIInstance.AddChild(_flipYToggle);
        _currentUIInstance.AddChild(_importButton);
        _currentUIInstance.AddChild(_statusLabel);
    }

    private void OnImportClicked(ISleekElement button)
    {
        string path = System.Environment.ExpandEnvironmentVariables((_pathField.Text ?? string.Empty).Trim().Trim('"'));
        if (!File.Exists(path))
        {
            SetStatus($"File not found: {path}", Color.red);
            return;
        }

        try
        {
            HeightmapData source = LoadHeightmap(path);
            int smoothPasses = Mathf.Clamp(_smoothPassesField.Value, 0, 8);
            source.Smooth(smoothPasses);

            Bounds bounds = ResolveWorldBounds((EImportScope)_scopeButton.state);
            if (bounds.size.x <= 0f || bounds.size.z <= 0f)
            {
                SetStatus("No terrain tile found for import scope.", Color.red);
                return;
            }

            int tileCount = CountExistingTiles(bounds);
            if (tileCount == 0)
            {
                SetStatus("No terrain tiles in import scope.", Color.red);
                return;
            }

            float minHeight = Mathf.Clamp(_minHeightField.Value, -Landscape.TILE_HEIGHT / 2f, Landscape.TILE_HEIGHT / 2f);
            float maxHeight = Mathf.Clamp(_maxHeightField.Value, -Landscape.TILE_HEIGHT / 2f, Landscape.TILE_HEIGHT / 2f);
            if (maxHeight < minHeight)
            {
                (minHeight, maxHeight) = (maxHeight, minHeight);
            }

            bool flipY = _flipYToggle.Value;
            Landscape.writeHeightmap(bounds, (_, _, worldPosition, _) =>
            {
                float u = Mathf.Clamp01((worldPosition.x - bounds.min.x) / bounds.size.x);
                float v = Mathf.Clamp01((worldPosition.z - bounds.min.z) / bounds.size.z);
                float height = Mathf.Lerp(minHeight, maxHeight, source.Sample(u, flipY ? 1f - v : v));
                return height / Landscape.TILE_HEIGHT + 0.5f;
            });
            Landscape.applyLOD();
            LevelHierarchy.MarkDirty();

            SetStatus($"Imported {source.Width}x{source.Height} heightmap into {tileCount} tile(s).", Color.green);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, Color.red);
        }
    }

    private static Bounds ResolveWorldBounds(EImportScope scope)
    {
        if (scope == EImportScope.CurrentTile)
        {
            LandscapeCoord coord = TerrainEditor.selectedTile != null
                ? TerrainEditor.selectedTile.coord
                : new LandscapeCoord(Camera.main != null ? Camera.main.transform.position : Vector3.zero);

            LandscapeTile? tile = Landscape.getTile(coord);
            return tile?.worldBounds ?? default;
        }

        float halfSize = SDG.Unturned.Level.size * 0.5f;
        Bounds bounds = default;
        bounds.SetMinMax(
            new Vector3(-halfSize, -Landscape.TILE_HEIGHT / 2f, -halfSize),
            new Vector3(halfSize, Landscape.TILE_HEIGHT / 2f, halfSize)
        );
        return bounds;
    }

    private static HeightmapData LoadHeightmap(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        string extension = Path.GetExtension(path);
        if (string.Equals(extension, ".heightmap", StringComparison.OrdinalIgnoreCase))
        {
            return LoadRaw16(bytes, Landscape.HEIGHTMAP_RESOLUTION, Landscape.HEIGHTMAP_RESOLUTION);
        }

        Texture2D texture = new(2, 2);
        try
        {
            if (texture.LoadImage(bytes))
            {
                Color32[] pixels = texture.GetPixels32();
                float[] values = new float[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    values[i] = (pixel.r + pixel.g + pixel.b) / 765f;
                }
                return new HeightmapData(values, texture.width, texture.height);
            }
        }
        finally
        {
            Object.Destroy(texture);
        }

        int samples = bytes.Length / 2;
        int size = Mathf.RoundToInt(Mathf.Sqrt(samples));
        if (size * size * 2 != bytes.Length)
        {
            throw new InvalidDataException("Unsupported heightmap format.");
        }

        // ponytail: raw files are assumed big-endian 16-bit like Unturned; add byte-order UI only if someone needs it.
        return LoadRaw16(bytes, size, size);
    }

    private static int CountExistingTiles(Bounds bounds)
    {
        int minX = Mathf.FloorToInt(bounds.min.x / Landscape.TILE_SIZE);
        int minY = Mathf.FloorToInt(bounds.min.z / Landscape.TILE_SIZE);
        int maxX = Mathf.CeilToInt(bounds.max.x / Landscape.TILE_SIZE);
        int maxY = Mathf.CeilToInt(bounds.max.z / Landscape.TILE_SIZE);
        int count = 0;

        for (int tileX = minX; tileX < maxX; tileX++)
        {
            for (int tileY = minY; tileY < maxY; tileY++)
            {
                if (Landscape.getTile(new LandscapeCoord(tileX, tileY)) != null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static HeightmapData LoadRaw16(byte[] bytes, int width, int height)
    {
        if (bytes.Length != width * height * 2)
        {
            throw new InvalidDataException($"Expected {width * height * 2} bytes, got {bytes.Length}.");
        }

        float[] values = new float[width * height];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = ((bytes[i * 2] << 8) | bytes[i * 2 + 1]) / 65535f;
        }

        return new HeightmapData(values, width, height);
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.TextColor = color;
        _statusLabel.Text = text;
    }

    public void Dispose()
    {
        _importButton.OnClicked -= OnImportClicked;

        if (_currentUIInstance == null) return;

        _currentUIInstance.RemoveChild(_scopeButton);
        _currentUIInstance.RemoveChild(_pathField);
        _currentUIInstance.RemoveChild(_minHeightField);
        _currentUIInstance.RemoveChild(_maxHeightField);
        _currentUIInstance.RemoveChild(_smoothPassesField);
        _currentUIInstance.RemoveChild(_flipYToggle);
        _currentUIInstance.RemoveChild(_importButton);
        _currentUIInstance.RemoveChild(_statusLabel);
    }

    private sealed class HeightmapData
    {
        private readonly float[] _values;

        public int Width { get; }
        public int Height { get; }

        public HeightmapData(float[] values, int width, int height)
        {
            _values = values;
            Width = width;
            Height = height;
        }

        public void Smooth(int passes)
        {
            if (passes <= 0 || Width < 2 || Height < 2) return;

            float[] temp = new float[_values.Length];
            for (int pass = 0; pass < passes; pass++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        float sum = 0f;
                        int count = 0;
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            int sy = y + oy;
                            if (sy < 0 || sy >= Height) continue;

                            for (int ox = -1; ox <= 1; ox++)
                            {
                                int sx = x + ox;
                                if (sx < 0 || sx >= Width) continue;

                                sum += _values[sy * Width + sx];
                                count++;
                            }
                        }

                        temp[y * Width + x] = sum / count;
                    }
                }

                Array.Copy(temp, _values, _values.Length);
            }
        }

        public float Sample(float u, float v)
        {
            if (Width <= 1 || Height <= 1)
            {
                return _values[0];
            }

            float x = Mathf.Clamp01(u) * (Width - 1);
            float y = Mathf.Clamp01(v) * (Height - 1);
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, Width - 1);
            int y1 = Mathf.Min(y0 + 1, Height - 1);
            float tx = x - x0;
            float ty = y - y0;
            float a = Mathf.Lerp(_values[y0 * Width + x0], _values[y0 * Width + x1], tx);
            float b = Mathf.Lerp(_values[y1 * Width + x0], _values[y1 * Width + x1], tx);
            return Mathf.Lerp(a, b, ty);
        }
    }
}