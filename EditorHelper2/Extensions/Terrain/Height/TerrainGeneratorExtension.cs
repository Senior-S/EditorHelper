using System;
using System.Collections.Generic;
using DanielWillett.UITools.API.Extensions;
using EditorHelper2.common.API.Attributes;
using EditorHelper2.common.API.Interfaces;
using EditorHelper2.UI.Builders;
using SDG.Framework.Devkit;
using SDG.Framework.Landscapes;
using SDG.Unturned;
using UnityEngine;

namespace EditorHelper2.Extensions.Terrain.Height;

[UIExtension(typeof(EditorTerrainHeightUI))]
[EHExtension("Terrain Generator Extension", "Senior S")]
public class TerrainGeneratorExtension : UIExtension, IExtension
{
    private enum EGeneratorAlgorithm
    {
        Perlin = 0,
        Fractal = 1,
        Ridged = 2,
        CellularAutomataCave = 3,
        DiamondSquare = 4,
        SimplexNoise = 5
    }

    private enum EGenerationScope
    {
        WholeMap = 0,
        CurrentTile = 1
    }

    private readonly EditorTerrainHeightUI? _currentUIInstance;

    private readonly SleekButtonState _scopeButton;
    private readonly SleekButtonState _algorithmButton;
    private readonly ISleekInt32Field _seedField;
    private readonly ISleekFloat32Field _baseHeightField;
    private readonly ISleekFloat32Field _amplitudeField;
    private readonly ISleekFloat32Field _frequencyField;
    private readonly ISleekInt32Field _octavesField;
    private readonly ISleekInt32Field _tilePaddingField;
    private readonly ISleekInt32Field _singleTileSeamBlendField;
    private readonly ISleekToggle _createMissingTilesToggle;
    private readonly ISleekButton _generateButton;
    private readonly ISleekLabel _statusLabel;

    public TerrainGeneratorExtension(EditorTerrainHeightUI instance)
    {
        _currentUIInstance = instance;

        UIBuilder builder = new(240f, 30f);
        builder.SetAnchorHorizontal(1f)
            .SetAnchorVertical(0f)
            .SetOffsetHorizontal(-260f)
            .SetOffsetVertical(40f)
            .SetSizeHorizontal(240f)
            .SetSizeVertical(30f);

        _scopeButton = builder.BuildButtonState(
            new GUIContent("Whole Map", "Generate across the entire level bounds"),
            new GUIContent("Current Tile", "Only generate one tile")
        );
        _scopeButton.AddLabel("Scope", ESleekSide.LEFT);
        _scopeButton.onSwappedState += OnSwappedGenerationScope;

        builder.SetOffsetVertical(76f);
        _algorithmButton = builder.BuildButtonState(
            new GUIContent("Perlin", "Smooth rolling terrain"),
            new GUIContent("Fractal", "Layered hills with more detail"),
            new GUIContent("Ridged", "Sharper mountain-like terrain"),
            new GUIContent("Cellular Cave", "Cave-like cellular pattern mapped into terrain"),
            new GUIContent("Diamond Square", "Classic fractal heightfield"),
            new GUIContent("Simplex", "Simplex fractal noise terrain")
        );
        _algorithmButton.AddLabel("Algorithm", ESleekSide.LEFT);
        _algorithmButton.onSwappedState += OnSwappedAlgorithm;

        builder.SetOffsetVertical(112f)
            .SetText("Seed");
        _seedField = builder.BuildInt32Field("Noise seed");
        _seedField.Value = 1337;

        builder.SetOffsetVertical(148f)
            .SetText("Base Height");
        _baseHeightField = builder.BuildFloatInput();
        _baseHeightField.Value = 0f;

        builder.SetOffsetVertical(184f)
            .SetText("Amplitude");
        _amplitudeField = builder.BuildFloatInput();
        _amplitudeField.Value = 220f;

        builder.SetOffsetVertical(220f)
            .SetText("Frequency");
        _frequencyField = builder.BuildFloatInput();
        _frequencyField.Value = 0.0018f;

        builder.SetOffsetVertical(256f)
            .SetText("Octaves");
        _octavesField = builder.BuildInt32Field("Used by Fractal, Ridged, Diamond and Simplex");
        _octavesField.Value = 4;

        builder.SetOffsetVertical(292f)
            .SetText("Tile Padding");
        _tilePaddingField = builder.BuildInt32Field("Expand tile bounds by N tiles");
        _tilePaddingField.Value = 0;

        builder.SetOffsetVertical(328f)
            .SetText("Seam Blend");
        _singleTileSeamBlendField = builder.BuildInt32Field("Current-tile edge blend in heightmap samples");
        _singleTileSeamBlendField.Value = 24;

        builder.SetOffsetVertical(364f)
            .SetSizeHorizontal(32f)
            .SetText("Create Missing Tiles");
        _createMissingTilesToggle = builder.BuildToggle("Create terrain tiles first if they do not exist", ESleekSide.LEFT);
        _createMissingTilesToggle.Value = true;

        builder.SetOffsetVertical(400f)
            .SetSizeHorizontal(240f)
            .SetText("Generate Terrain");
        _generateButton = builder.BuildButton("Generate terrain");
        _generateButton.OnClicked += OnGenerateClicked;

        builder.SetOffsetHorizontal(-420f)
            .SetOffsetVertical(436f)
            .SetSizeHorizontal(400f)
            .SetText("Ready");
        _statusLabel = builder.BuildLabel(TextAnchor.MiddleLeft);
        _statusLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;

        Initialize();
    }

    public void Initialize()
    {
        if (_currentUIInstance == null) return;

        _currentUIInstance.AddChild(_scopeButton);
        _currentUIInstance.AddChild(_algorithmButton);
        _currentUIInstance.AddChild(_seedField);
        _currentUIInstance.AddChild(_baseHeightField);
        _currentUIInstance.AddChild(_amplitudeField);
        _currentUIInstance.AddChild(_frequencyField);
        _currentUIInstance.AddChild(_octavesField);
        _currentUIInstance.AddChild(_tilePaddingField);
        _currentUIInstance.AddChild(_singleTileSeamBlendField);
        _currentUIInstance.AddChild(_createMissingTilesToggle);
        _currentUIInstance.AddChild(_generateButton);
        _currentUIInstance.AddChild(_statusLabel);
    }

    private void OnSwappedGenerationScope(SleekButtonState button, int index)
    {
        _statusLabel.TextColor = Color.white;
        _statusLabel.Text = $"Scope: {(EGenerationScope)index}";
    }

    private void OnSwappedAlgorithm(SleekButtonState button, int index)
    {
        _statusLabel.TextColor = Color.white;
        _statusLabel.Text = $"Algorithm: {(EGeneratorAlgorithm)index}";
    }

    private void OnGenerateClicked(ISleekElement button)
    {
        int tilePadding = Mathf.Max(0, _tilePaddingField.Value);
        int octaves = Mathf.Clamp(_octavesField.Value, 1, 8);
        float amplitude = Mathf.Max(0f, _amplitudeField.Value);
        float frequency = Mathf.Max(0.00001f, _frequencyField.Value);
        float baseHeight = Mathf.Clamp(_baseHeightField.Value, -Landscape.TILE_HEIGHT / 2f, Landscape.TILE_HEIGHT / 2f);
        int seamBlendSamples = Mathf.Clamp(_singleTileSeamBlendField.Value, 0, Landscape.HEIGHTMAP_RESOLUTION / 2);
        int seed = _seedField.Value;
        EGeneratorAlgorithm algorithm = (EGeneratorAlgorithm)_algorithmButton.state;
        EGenerationScope scope = (EGenerationScope)_scopeButton.state;

        TerrainTileBounds tileBounds = ResolveTileBounds(scope, tilePadding, out string scopeText);
        if (tileBounds.IsEmpty)
        {
            _statusLabel.TextColor = Color.red;
            _statusLabel.Text = "Computed tile bounds are empty.";
            return;
        }

        int createdTiles = 0;
        if (_createMissingTilesToggle.Value)
        {
            createdTiles = EnsureTiles(tileBounds);
        }

        int existingTileCount = CountExistingTiles(tileBounds);
        if (existingTileCount == 0)
        {
            _statusLabel.TextColor = Color.red;
            _statusLabel.Text = "No terrain tiles in scope. Enable Create Missing Tiles.";
            return;
        }

        TerrainGeneratorConfig config = new(algorithm, seed, baseHeight, amplitude, frequency, octaves);
        Bounds worldBounds = tileBounds.GetWorldBounds();
        Func<Vector3, float> sampler = BuildHeightSampler(config, worldBounds);

        if (scope == EGenerationScope.CurrentTile && tileBounds.TryGetSingleTileCoord(out LandscapeCoord singleCoord))
        {
            GenerateSingleTileWithSeamBlend(singleCoord, sampler, seamBlendSamples);
        }
        else
        {
            Landscape.writeHeightmap(worldBounds, (tileCoord, heightmapCoord, worldPosition, currentHeight) =>
                sampler(worldPosition));
        }

        Landscape.applyLOD();
        LevelHierarchy.MarkDirty();

        _statusLabel.TextColor = Color.green;
        _statusLabel.Text = $"Generated {existingTileCount} tile(s) [{algorithm}] in {scopeText}";
        if (createdTiles > 0)
        {
            _statusLabel.Text += $", created {createdTiles}";
        }
    }

    private static TerrainTileBounds ResolveTileBounds(EGenerationScope scope, int tilePadding, out string scopeText)
    {
        TerrainTileBounds levelBounds = TerrainTileBounds.FromLevel(tilePadding);

        if (scope == EGenerationScope.WholeMap)
        {
            scopeText = "whole map";
            return levelBounds;
        }

        if (scope == EGenerationScope.CurrentTile)
        {
            LandscapeCoord currentCoord = TerrainEditor.selectedTile != null
                ? TerrainEditor.selectedTile.coord
                : new LandscapeCoord(GetCameraPosition());

            scopeText = $"tile ({currentCoord.x}, {currentCoord.y})";
            return TerrainTileBounds.FromSingleTile(currentCoord, tilePadding).Clamp(levelBounds);
        }

        scopeText = "whole map";
        return levelBounds;
    }

    private static Vector3 GetCameraPosition()
    {
        Camera? camera = Camera.main;
        if (camera != null)
        {
            return camera.transform.position;
        }

        return Vector3.zero;
    }

    private static void GenerateSingleTileWithSeamBlend(LandscapeCoord tileCoord, Func<Vector3, float> sampler, int seamBlendSamples)
    {
        LandscapeTile? tile = Landscape.getTile(tileCoord);
        if (tile == null)
        {
            return;
        }

        int max = Landscape.HEIGHTMAP_RESOLUTION_MINUS_ONE;
        int band = Mathf.Clamp(seamBlendSamples, 0, max / 2);

        LandscapeTile? leftTile = Landscape.getTile(new LandscapeCoord(tileCoord.x - 1, tileCoord.y));
        LandscapeTile? rightTile = Landscape.getTile(new LandscapeCoord(tileCoord.x + 1, tileCoord.y));
        LandscapeTile? bottomTile = Landscape.getTile(new LandscapeCoord(tileCoord.x, tileCoord.y - 1));
        LandscapeTile? topTile = Landscape.getTile(new LandscapeCoord(tileCoord.x, tileCoord.y + 1));

        for (int x = 0; x <= max; x++)
        {
            for (int y = 0; y <= max; y++)
            {
                float currentHeight = tile.heightmap[x, y];
                Vector3 worldPosition = Landscape.getWorldPosition(tileCoord, new HeightmapCoord(x, y), currentHeight);
                float generatedHeight = sampler(worldPosition);

                if (band <= 0)
                {
                    tile.heightmap[x, y] = generatedHeight;
                    continue;
                }

                float sum = generatedHeight;
                float weight = 1f;

                if (leftTile != null)
                {
                    float edgeWeight = GetEdgeBlendWeight(y, band);
                    if (edgeWeight > 0f)
                    {
                        sum += leftTile.heightmap[x, max] * edgeWeight;
                        weight += edgeWeight;
                    }
                }

                if (rightTile != null)
                {
                    float edgeWeight = GetEdgeBlendWeight(max - y, band);
                    if (edgeWeight > 0f)
                    {
                        sum += rightTile.heightmap[x, 0] * edgeWeight;
                        weight += edgeWeight;
                    }
                }

                if (bottomTile != null)
                {
                    float edgeWeight = GetEdgeBlendWeight(x, band);
                    if (edgeWeight > 0f)
                    {
                        sum += bottomTile.heightmap[max, y] * edgeWeight;
                        weight += edgeWeight;
                    }
                }

                if (topTile != null)
                {
                    float edgeWeight = GetEdgeBlendWeight(max - x, band);
                    if (edgeWeight > 0f)
                    {
                        sum += topTile.heightmap[0, y] * edgeWeight;
                        weight += edgeWeight;
                    }
                }

                tile.heightmap[x, y] = Mathf.Clamp01(sum / weight);
            }
        }

        Landscape.reconcileNeighbors(tile);
    }

    private static float GetEdgeBlendWeight(int distanceFromEdge, int blendBand)
    {
        if (blendBand <= 0 || distanceFromEdge >= blendBand)
        {
            return 0f;
        }

        float t = 1f - distanceFromEdge / (float)blendBand;
        return t * t * 3f;
    }

    private static Func<Vector3, float> BuildHeightSampler(TerrainGeneratorConfig config, Bounds worldBounds)
    {
        if (config.Algorithm == EGeneratorAlgorithm.DiamondSquare)
        {
            GeneratedHeightField field = CreateDiamondSquareField(worldBounds, config.Seed, config.Octaves);
            return worldPosition => MapToHeight01(field.Sample(worldPosition.x, worldPosition.z), config);
        }

        if (config.Algorithm == EGeneratorAlgorithm.CellularAutomataCave)
        {
            GeneratedHeightField field = CreateCellularField(worldBounds, config.Seed, config.Octaves);
            return worldPosition => MapToHeight01(field.Sample(worldPosition.x, worldPosition.z), config);
        }

        if (config.Algorithm == EGeneratorAlgorithm.SimplexNoise)
        {
            SimplexNoise2D simplex = new(config.Seed);
            return worldPosition =>
            {
                float value = SampleSimplex(worldPosition, config, simplex);
                return MapToHeight01(value, config);
            };
        }

        return worldPosition =>
        {
            float value = SampleClassicNoise(worldPosition, config);
            return MapToHeight01(value, config);
        };
    }

    private static float SampleClassicNoise(Vector3 worldPosition, TerrainGeneratorConfig config)
    {
        float sampleX = (worldPosition.x + config.OffsetX) * config.Frequency;
        float sampleZ = (worldPosition.z + config.OffsetZ) * config.Frequency;

        if (config.Algorithm == EGeneratorAlgorithm.Perlin)
        {
            return Mathf.PerlinNoise(sampleX, sampleZ);
        }

        if (config.Algorithm == EGeneratorAlgorithm.Fractal)
        {
            return SampleFractalPerlin(sampleX, sampleZ, config.Octaves);
        }

        return SampleRidgedPerlin(sampleX, sampleZ, config.Octaves);
    }

    private static float SampleFractalPerlin(float x, float z, int octaves)
    {
        float value = 0f;
        float totalWeight = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            value += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            totalWeight += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        if (totalWeight <= 0f) return 0.5f;
        return value / totalWeight;
    }

    private static float SampleRidgedPerlin(float x, float z, int octaves)
    {
        float value = 0f;
        float totalWeight = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            float n = Mathf.PerlinNoise(x * frequency, z * frequency);
            float ridge = 1f - Mathf.Abs(n * 2f - 1f);
            ridge *= ridge;

            value += ridge * amplitude;
            totalWeight += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        if (totalWeight <= 0f) return 0.5f;
        return Mathf.Clamp01(value / totalWeight);
    }

    private static float SampleSimplex(Vector3 worldPosition, TerrainGeneratorConfig config, SimplexNoise2D simplex)
    {
        float sampleX = (worldPosition.x + config.OffsetX) * config.Frequency;
        float sampleZ = (worldPosition.z + config.OffsetZ) * config.Frequency;

        float value = 0f;
        float totalWeight = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < config.Octaves; i++)
        {
            float n = simplex.Evaluate(sampleX * frequency, sampleZ * frequency);
            value += n * amplitude;
            totalWeight += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        if (totalWeight <= 0f) return 0.5f;

        float normalized = value / totalWeight;
        return Mathf.Clamp01(normalized * 0.5f + 0.5f);
    }

    private static float MapToHeight01(float normalizedHeight, TerrainGeneratorConfig config)
    {
        float worldHeight = config.BaseHeight + (normalizedHeight * 2f - 1f) * config.Amplitude;
        return Mathf.Clamp01(worldHeight / Landscape.TILE_HEIGHT + 0.5f);
    }

    private static int EnsureTiles(TerrainTileBounds bounds)
    {
        if (bounds.IsEmpty) return 0;

        List<LandscapeTile> createdTiles = [];

        for (int tileX = bounds.MinX; tileX < bounds.MaxXExclusive; tileX++)
        {
            for (int tileY = bounds.MinY; tileY < bounds.MaxYExclusive; tileY++)
            {
                LandscapeCoord coord = new(tileX, tileY);
                if (Landscape.getTile(coord) != null) continue;

                LandscapeTile? tile = Landscape.addTile(coord);
                if (tile == null) continue;

                tile.updatePrototypes();
                createdTiles.Add(tile);
            }
        }

        if (createdTiles.Count > 0)
        {
            Landscape.linkNeighbors();
            foreach (LandscapeTile tile in createdTiles)
            {
                Landscape.reconcileNeighbors(tile);
            }
            Landscape.applyLOD();
        }

        return createdTiles.Count;
    }

    private static int CountExistingTiles(TerrainTileBounds bounds)
    {
        if (bounds.IsEmpty) return 0;

        int count = 0;
        for (int tileX = bounds.MinX; tileX < bounds.MaxXExclusive; tileX++)
        {
            for (int tileY = bounds.MinY; tileY < bounds.MaxYExclusive; tileY++)
            {
                if (Landscape.getTile(new LandscapeCoord(tileX, tileY)) != null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static GeneratedHeightField CreateDiamondSquareField(Bounds worldBounds, int seed, int octaves)
    {
        int targetResolution = Mathf.Max(
            65,
            Mathf.RoundToInt(Mathf.Max(worldBounds.size.x, worldBounds.size.z) / Landscape.HEIGHTMAP_WORLD_UNIT) + 1
        );

        int size = Mathf.NextPowerOfTwo(Mathf.Max(2, targetResolution - 1)) + 1;
        size = Mathf.Min(size, 4097);

        float[,] values = new float[size, size];
        System.Random random = new(seed);

        int max = size - 1;
        values[0, 0] = (float)random.NextDouble();
        values[0, max] = (float)random.NextDouble();
        values[max, 0] = (float)random.NextDouble();
        values[max, max] = (float)random.NextDouble();

        int step = max;
        float displacement = 0.55f;
        float roughness = Mathf.Lerp(0.35f, 0.65f, Mathf.Clamp01(octaves / 8f));

        while (step > 1)
        {
            int half = step / 2;

            for (int x = half; x < max; x += step)
            {
                for (int y = half; y < max; y += step)
                {
                    float avg = (values[x - half, y - half] + values[x - half, y + half] + values[x + half, y - half] + values[x + half, y + half]) * 0.25f;
                    values[x, y] = Mathf.Clamp01(avg + RandomOffset(random, displacement));
                }
            }

            for (int x = 0; x < size; x += half)
            {
                int yStart = (x + half) % step;
                for (int y = yStart; y < size; y += step)
                {
                    float sum = 0f;
                    int count = 0;

                    if (x - half >= 0)
                    {
                        sum += values[x - half, y];
                        count++;
                    }
                    if (x + half < size)
                    {
                        sum += values[x + half, y];
                        count++;
                    }
                    if (y - half >= 0)
                    {
                        sum += values[x, y - half];
                        count++;
                    }
                    if (y + half < size)
                    {
                        sum += values[x, y + half];
                        count++;
                    }

                    float avg = count > 0 ? sum / count : 0.5f;
                    values[x, y] = Mathf.Clamp01(avg + RandomOffset(random, displacement));
                }
            }

            step /= 2;
            displacement *= roughness;
        }

        Normalize01(values, size, size);
        return new GeneratedHeightField(values, size, size, worldBounds.min.x, worldBounds.min.z, worldBounds.size.x, worldBounds.size.z);
    }

    private static GeneratedHeightField CreateCellularField(Bounds worldBounds, int seed, int octaves)
    {
        int width = Mathf.Clamp(Mathf.RoundToInt(worldBounds.size.x / (Landscape.HEIGHTMAP_WORLD_UNIT * 2f)) + 1, 64, 1025);
        int height = Mathf.Clamp(Mathf.RoundToInt(worldBounds.size.z / (Landscape.HEIGHTMAP_WORLD_UNIT * 2f)) + 1, 64, 1025);

        bool[,] current = new bool[width, height];
        bool[,] next = new bool[width, height];
        System.Random random = new(seed);

        const float fillChance = 0.46f;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                current[x, y] = border || random.NextDouble() < fillChance;
            }
        }

        int iterations = Mathf.Clamp(octaves + 2, 4, 10);
        for (int i = 0; i < iterations; i++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    if (border)
                    {
                        next[x, y] = true;
                        continue;
                    }

                    int walls = CountWallNeighbors(current, width, height, x, y);
                    next[x, y] = current[x, y] ? walls >= 4 : walls >= 5;
                }
            }

            bool[,] temp = current;
            current = next;
            next = temp;
        }

        float[,] values = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int walls = 0;
                int count = 0;
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int sx = x + ox;
                        int sy = y + oy;
                        if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
                        count++;
                        if (current[sx, sy]) walls++;
                    }
                }

                float density = count > 0 ? walls / (float)count : 0f;
                float baseValue = current[x, y] ? 0.85f : 0.2f;
                values[x, y] = Mathf.Clamp01(Mathf.Lerp(baseValue, density, 0.6f));
            }
        }

        BlurField(values, width, height, 1);
        return new GeneratedHeightField(values, width, height, worldBounds.min.x, worldBounds.min.z, worldBounds.size.x, worldBounds.size.z);
    }

    private static void BlurField(float[,] values, int width, int height, int passes)
    {
        float[,] temp = new float[width, height];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            int sx = x + ox;
                            int sy = y + oy;
                            if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
                            sum += values[sx, sy];
                            count++;
                        }
                    }

                    temp[x, y] = count > 0 ? sum / count : values[x, y];
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    values[x, y] = temp[x, y];
                }
            }
        }
    }

    private static int CountWallNeighbors(bool[,] field, int width, int height, int x, int y)
    {
        int walls = 0;
        for (int ox = -1; ox <= 1; ox++)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                if (ox == 0 && oy == 0) continue;

                int sx = x + ox;
                int sy = y + oy;
                if (sx < 0 || sy < 0 || sx >= width || sy >= height)
                {
                    walls++;
                }
                else if (field[sx, sy])
                {
                    walls++;
                }
            }
        }

        return walls;
    }

    private static float RandomOffset(System.Random random, float magnitude)
    {
        return ((float)random.NextDouble() * 2f - 1f) * magnitude;
    }

    private static void Normalize01(float[,] values, int width, int height)
    {
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float value = values[x, y];
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        float range = max - min;
        if (range <= 0.0001f)
        {
            return;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                values[x, y] = (values[x, y] - min) / range;
            }
        }
    }

    private readonly struct TerrainGeneratorConfig
    {
        public EGeneratorAlgorithm Algorithm { get; }
        public int Seed { get; }
        public float OffsetX { get; }
        public float OffsetZ { get; }
        public float BaseHeight { get; }
        public float Amplitude { get; }
        public float Frequency { get; }
        public int Octaves { get; }

        public TerrainGeneratorConfig(EGeneratorAlgorithm algorithm, int seed, float baseHeight, float amplitude, float frequency, int octaves)
        {
            Algorithm = algorithm;
            Seed = seed;
            OffsetX = seed * 0.173f + 37.1f;
            OffsetZ = seed * -0.127f + 11.9f;
            BaseHeight = baseHeight;
            Amplitude = amplitude;
            Frequency = frequency;
            Octaves = octaves;
        }
    }

    private readonly struct TerrainTileBounds
    {
        public int MinX { get; }
        public int MinY { get; }
        public int MaxXExclusive { get; }
        public int MaxYExclusive { get; }

        public bool IsEmpty => MaxXExclusive <= MinX || MaxYExclusive <= MinY;
        public bool IsSingleTile => !IsEmpty && MaxXExclusive - MinX == 1 && MaxYExclusive - MinY == 1;

        private TerrainTileBounds(int minX, int minY, int maxXExclusive, int maxYExclusive)
        {
            MinX = minX;
            MinY = minY;
            MaxXExclusive = maxXExclusive;
            MaxYExclusive = maxYExclusive;
        }

        public static TerrainTileBounds FromLevel(int tilePadding)
        {
            float halfSize = SDG.Unturned.Level.size * 0.5f;
            int min = Mathf.FloorToInt(-halfSize / Landscape.TILE_SIZE) - tilePadding;
            int max = Mathf.CeilToInt(halfSize / Landscape.TILE_SIZE) + tilePadding;
            return new TerrainTileBounds(min, min, max, max);
        }

        public static TerrainTileBounds FromSingleTile(LandscapeCoord coord, int tilePadding)
        {
            int minX = coord.x - tilePadding;
            int minY = coord.y - tilePadding;
            int maxX = coord.x + tilePadding + 1;
            int maxY = coord.y + tilePadding + 1;
            return new TerrainTileBounds(minX, minY, maxX, maxY);
        }

        public static TerrainTileBounds FromWorldBounds(Bounds worldBounds, int tilePadding)
        {
            int minX = Mathf.FloorToInt(worldBounds.min.x / Landscape.TILE_SIZE) - tilePadding;
            int minY = Mathf.FloorToInt(worldBounds.min.z / Landscape.TILE_SIZE) - tilePadding;
            int maxX = Mathf.CeilToInt(worldBounds.max.x / Landscape.TILE_SIZE) + tilePadding;
            int maxY = Mathf.CeilToInt(worldBounds.max.z / Landscape.TILE_SIZE) + tilePadding;
            return new TerrainTileBounds(minX, minY, maxX, maxY);
        }

        public TerrainTileBounds Clamp(TerrainTileBounds bounds)
        {
            int minX = Mathf.Max(MinX, bounds.MinX);
            int minY = Mathf.Max(MinY, bounds.MinY);
            int maxX = Mathf.Min(MaxXExclusive, bounds.MaxXExclusive);
            int maxY = Mathf.Min(MaxYExclusive, bounds.MaxYExclusive);
            return new TerrainTileBounds(minX, minY, maxX, maxY);
        }

        public bool TryGetSingleTileCoord(out LandscapeCoord coord)
        {
            if (IsSingleTile)
            {
                coord = new LandscapeCoord(MinX, MinY);
                return true;
            }

            coord = default;
            return false;
        }

        public Bounds GetWorldBounds()
        {
            if (IsEmpty)
            {
                return default;
            }

            Vector3 min = new(
                MinX * Landscape.TILE_SIZE,
                -Landscape.TILE_HEIGHT * 0.5f,
                MinY * Landscape.TILE_SIZE
            );
            Vector3 max = new(
                MaxXExclusive * Landscape.TILE_SIZE,
                Landscape.TILE_HEIGHT * 0.5f,
                MaxYExclusive * Landscape.TILE_SIZE
            );

            Bounds bounds = default;
            bounds.SetMinMax(min, max);
            return bounds;
        }
    }

    private sealed class GeneratedHeightField
    {
        private readonly float[,] _values;
        private readonly int _width;
        private readonly int _height;
        private readonly float _minX;
        private readonly float _minZ;
        private readonly float _sizeX;
        private readonly float _sizeZ;

        public GeneratedHeightField(float[,] values, int width, int height, float minX, float minZ, float sizeX, float sizeZ)
        {
            _values = values;
            _width = width;
            _height = height;
            _minX = minX;
            _minZ = minZ;
            _sizeX = Mathf.Max(1f, sizeX);
            _sizeZ = Mathf.Max(1f, sizeZ);
        }

        public float Sample(float worldX, float worldZ)
        {
            if (_width < 2 || _height < 2)
            {
                return _values[0, 0];
            }

            float u = Mathf.Clamp01((worldX - _minX) / _sizeX);
            float v = Mathf.Clamp01((worldZ - _minZ) / _sizeZ);

            float fx = u * (_width - 1);
            float fy = v * (_height - 1);

            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            int x1 = Mathf.Min(x0 + 1, _width - 1);
            int y1 = Mathf.Min(y0 + 1, _height - 1);

            float tx = fx - x0;
            float ty = fy - y0;

            float a = Mathf.Lerp(_values[x0, y0], _values[x1, y0], tx);
            float b = Mathf.Lerp(_values[x0, y1], _values[x1, y1], tx);
            return Mathf.Lerp(a, b, ty);
        }
    }

    private sealed class SimplexNoise2D
    {
        private static readonly int[,] Gradients =
        {
            { 1, 1 }, { -1, 1 }, { 1, -1 }, { -1, -1 },
            { 1, 0 }, { -1, 0 }, { 1, 0 }, { -1, 0 },
            { 0, 1 }, { 0, -1 }, { 0, 1 }, { 0, -1 }
        };

        private readonly int[] _perm = new int[512];

        public SimplexNoise2D(int seed)
        {
            int[] p = new int[256];
            for (int i = 0; i < 256; i++)
            {
                p[i] = i;
            }

            System.Random random = new(seed);
            for (int i = 255; i >= 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (p[i], p[swapIndex]) = (p[swapIndex], p[i]);
            }

            for (int i = 0; i < 512; i++)
            {
                _perm[i] = p[i & 255];
            }
        }

        public float Evaluate(float xin, float yin)
        {
            const float f2 = 0.366025403f;
            const float g2 = 0.211324865f;

            float s = (xin + yin) * f2;
            int i = Mathf.FloorToInt(xin + s);
            int j = Mathf.FloorToInt(yin + s);

            float t = (i + j) * g2;
            float x0 = xin - (i - t);
            float y0 = yin - (j - t);

            int i1;
            int j1;
            if (x0 > y0)
            {
                i1 = 1;
                j1 = 0;
            }
            else
            {
                i1 = 0;
                j1 = 1;
            }

            float x1 = x0 - i1 + g2;
            float y1 = y0 - j1 + g2;
            float x2 = x0 - 1f + 2f * g2;
            float y2 = y0 - 1f + 2f * g2;

            int ii = i & 255;
            int jj = j & 255;
            int gi0 = _perm[ii + _perm[jj]] % 12;
            int gi1 = _perm[ii + i1 + _perm[jj + j1]] % 12;
            int gi2 = _perm[ii + 1 + _perm[jj + 1]] % 12;

            float n0 = CornerContribution(gi0, x0, y0);
            float n1 = CornerContribution(gi1, x1, y1);
            float n2 = CornerContribution(gi2, x2, y2);

            return 70f * (n0 + n1 + n2);
        }

        private static float CornerContribution(int gradientIndex, float x, float y)
        {
            float t = 0.5f - x * x - y * y;
            if (t < 0f)
            {
                return 0f;
            }

            t *= t;
            int gx = Gradients[gradientIndex, 0];
            int gy = Gradients[gradientIndex, 1];
            return t * t * (gx * x + gy * y);
        }
    }

    public void Dispose()
    {
        _scopeButton.onSwappedState -= OnSwappedGenerationScope;
        _algorithmButton.onSwappedState -= OnSwappedAlgorithm;
        _generateButton.OnClicked -= OnGenerateClicked;

        if (_currentUIInstance == null) return;

        _currentUIInstance.RemoveChild(_scopeButton);
        _currentUIInstance.RemoveChild(_algorithmButton);
        _currentUIInstance.RemoveChild(_seedField);
        _currentUIInstance.RemoveChild(_baseHeightField);
        _currentUIInstance.RemoveChild(_amplitudeField);
        _currentUIInstance.RemoveChild(_frequencyField);
        _currentUIInstance.RemoveChild(_octavesField);
        _currentUIInstance.RemoveChild(_tilePaddingField);
        _currentUIInstance.RemoveChild(_singleTileSeamBlendField);
        _currentUIInstance.RemoveChild(_createMissingTilesToggle);
        _currentUIInstance.RemoveChild(_generateButton);
        _currentUIInstance.RemoveChild(_statusLabel);
    }
}