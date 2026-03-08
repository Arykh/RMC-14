using Content.Shared.Mobs;

namespace Content.Shared._RMC14.RMCRecords;

public abstract class SharedRMCRecordsSystem : EntitySystem
{
    /// <summary>
    ///     Attempts to retrieve the general record for a given entity.
    /// </summary>
    public bool TryGetGeneralRecord(EntityUid uid, out RMCGeneralRecordComponent record)
    {
        return TryComp(uid, out record!);
    }

    /// <summary>
    ///     Attempts to retrieve the medical record for a given entity.
    /// </summary>
    public bool TryGetMedicalRecord(EntityUid uid, out RMCMedicalRecordComponent record)
    {
        return TryComp(uid, out record!);
    }

    /// <summary>
    ///     Attempts to retrieve the security record for a given entity.
    /// </summary>
    public bool TryGetSecurityRecord(EntityUid uid, out RMCSecurityRecordComponent record)
    {
        return TryComp(uid, out record!);
    }

    /// <summary>
    ///     Maps a <see cref="MobState"/> to the corresponding <see cref="RMCPhysicalStatus"/>.
    /// </summary>
    public static RMCPhysicalStatus MobStateToPhysicalStatus(MobState state)
    {
        return state switch
        {
            MobState.Alive => RMCPhysicalStatus.Active,
            MobState.Critical => RMCPhysicalStatus.Unconscious,
            MobState.Dead => RMCPhysicalStatus.Dead,
            _ => RMCPhysicalStatus.Active,
        };
    }
}
