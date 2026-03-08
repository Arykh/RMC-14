using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.RMCRecords;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCRecordsSystem))]
public sealed partial class RMCSecurityRecordComponent : Component
{
    /// <summary>
    ///     The criminal status of the crewmember.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCCriminalStatus CriminalStatus = RMCCriminalStatus.None;

    /// <summary>
    ///     A list of recorded incidents/comments on the crewmember's security record.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<RMCSecurityIncident> Incidents = [];
}

[Serializable, NetSerializable, DataRecord]
public sealed record RMCSecurityIncident(TimeSpan Time, string Author, string Details);

[Serializable, NetSerializable]
public enum RMCCriminalStatus : byte
{
    None,
    Suspect,
    Arrested,
    Incarcerated,
    Paroled,
    Released,
}
