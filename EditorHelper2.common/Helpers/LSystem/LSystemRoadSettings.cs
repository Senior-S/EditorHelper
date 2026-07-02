using System;

namespace EditorHelper2.common.Helpers.LSystem;

/// <summary>
/// Settings for bounded L-System road growth.
/// </summary>
public sealed class LSystemRoadSettings
{
    public static readonly Guid DefaultStraightRoadGuid = Guid.Parse("b832729132a546a29eaedac06b84a46f");
    public static readonly Guid DefaultTeeRoadGuid = Guid.Parse("8221c9c6360c4629a7dc1a197fac0196");
    public static readonly Guid DefaultQuadRoadGuid = Guid.Parse("bec22e8664b54b06aaee361c062980e3");
    public static readonly Guid DefaultCornerRoadGuid = Guid.Parse("329682e42ea141ea8d033278e88d763c");
    public static readonly Guid DefaultEndRoadGuid = Guid.Parse("27501590239d4699a8dc001efa4dee37");

    /// <summary>
    /// Gets a new settings instance with the default road growth values.
    /// </summary>
    public static LSystemRoadSettings Default => new();

    /// <summary>
    /// Maximum number of accepted road segments to place.
    /// </summary>
    public int SegmentLimit { get; set; } = 64;

    /// <summary>
    /// Random seed used for repeatable road growth.
    /// </summary>
    public int Seed { get; set; } = 4148;

    /// <summary>
    /// Initial segment length in road-piece units.
    /// </summary>
    public int InitialLength { get; set; } = 8;

    /// <summary>
    /// Minimum segment length in road-piece units.
    /// </summary>
    public int MinLength { get; set; } = 2;

    /// <summary>
    /// Chance that a segment endpoint creates a side branch.
    /// </summary>
    public float BranchChance { get; set; } = 0.35f;

    /// <summary>
    /// Chance that the continuing road turns at a segment endpoint.
    /// </summary>
    public float TurnChance { get; set; } = 0.25f;

    /// <summary>
    /// Candidate angles used for branches and turns.
    /// </summary>
    public float[] BranchAngles { get; set; } = { -90f, 90f };

    /// <summary>
    /// Maximum XZ distance from the start object. Values less than or equal to zero disable the radius check.
    /// </summary>
    public float MaxRadiusFromStart { get; set; } = 650f;

    /// <summary>
    /// Maximum XZ distance for snapping a new endpoint to an existing generated road point.
    /// </summary>
    public float SnapDistance { get; set; } = 10f;

    /// <summary>
    /// Minimum allowed XZ distance from unrelated generated road segments.
    /// </summary>
    public float CollisionDistance { get; set; } = 18f;

    /// <summary>
    /// Object asset used for straight road pieces.
    /// </summary>
    public Guid StraightRoadGuid { get; set; } = DefaultStraightRoadGuid;

    /// <summary>
    /// Object asset used for three-way road junctions.
    /// </summary>
    public Guid TeeRoadGuid { get; set; } = DefaultTeeRoadGuid;

    /// <summary>
    /// Object asset used for four-way road junctions.
    /// </summary>
    public Guid QuadRoadGuid { get; set; } = DefaultQuadRoadGuid;

    /// <summary>
    /// Object asset used for corner road pieces.
    /// </summary>
    public Guid CornerRoadGuid { get; set; } = DefaultCornerRoadGuid;

    /// <summary>
    /// Object asset used for dead-end road pieces.
    /// </summary>
    public Guid EndRoadGuid { get; set; } = DefaultEndRoadGuid;
}