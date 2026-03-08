using Content.Shared.Humanoid;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.RMCRecords;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCRecordsSystem))]
public sealed partial class RMCGeneralRecordComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Name = "Unknown";

    /// <summary>
    ///     The character's assigned rank/job title.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Rank = "Unassigned";

    /// <summary>
    ///     This is the character's <see cref="Sex"/>, not <see cref="Gender"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Sex Sex = Sex.Male;

    [DataField, AutoNetworkedField]
    public int Age;

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
