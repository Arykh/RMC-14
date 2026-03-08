using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.RMCRecords;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCRecordsSystem))]
public sealed partial class RMCMedicalRecordComponent : Component
{
    // TODO RMC-14 actual blood types — update when blood type system is implemented
    [DataField, AutoNetworkedField]
    public string BloodType = "O-";

    [DataField, AutoNetworkedField]
    public string MinorDisability = "None";

    [DataField, AutoNetworkedField]
    public string MinorDisabilityDetails = "No minor disabilities have been declared.";

    [DataField, AutoNetworkedField]
    public string MajorDisability = "None";

    [DataField, AutoNetworkedField]
    public string MajorDisabilityDetails = "No major disabilities have been diagnosed.";

    [DataField, AutoNetworkedField]
    public string Allergies = "None";

    [DataField, AutoNetworkedField]
    public string AllergiesDetails = "No allergies have been detected in this patient.";

    [DataField, AutoNetworkedField]
    public string Diseases = "None";

    [DataField, AutoNetworkedField]
    public string DiseasesDetails = "No diseases have been diagnosed at the moment.";

    /// <summary>
    ///     The time of the last body scan, or null if never scanned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? LastScanTime;

    /// <summary>
    ///     A human-readable summary of the last scan result.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string LastScanResult = "No scan data on record";

    /// <summary>
    ///     A list of autodoc surgery data entries logged by the autodoc system.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<RMCAutodocRecord> AutodocData = [];
}

[Serializable, NetSerializable, DataRecord]
public sealed record RMCAutodocRecord(TimeSpan Time, string Procedure, string Details);
