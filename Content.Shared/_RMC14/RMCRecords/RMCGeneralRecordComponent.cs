using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.RMCRecords;

/// <summary>
///     General record for a crewmember. Contains basic identifying information.
///     Attached directly to the mob entity on spawn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCRecordsSystem))]
public sealed partial class RMCGeneralRecordComponent : Component
{
    /// <summary>
    ///     The character's full name.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Name = "Unknown";

    /// <summary>
    ///     The character's assigned rank/job title.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Rank = "Unassigned";

    /// <summary>
    ///     The character's sex.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Sex Sex = Sex.Male;

    /// <summary>
    ///     The character's age.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Age;

    /// <summary>
    ///     The character's species.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Species = "Human";

    /// <summary>
    ///     The character's physical status (Active, Unconscious, Dead, etc.).
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCPhysicalStatus PhysicalStatus = RMCPhysicalStatus.Active;

    /// <summary>
    ///     The character's mental status (Stable, Unstable, etc.).
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCMentalStatus MentalStatus = RMCMentalStatus.Stable;
}

[Serializable, NetSerializable]
public enum RMCPhysicalStatus : byte
{
    Active,
    Unconscious,
    Dead,
}

[Serializable, NetSerializable]
public enum RMCMentalStatus : byte
{
    Stable,
    Unstable,
    Insane,
}
