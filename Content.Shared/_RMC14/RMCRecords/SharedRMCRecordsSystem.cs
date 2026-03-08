using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.HUD.Events;
using Content.Shared._RMC14.Synth;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.RMCRecords;

public abstract class SharedRMCRecordsSystem : EntitySystem
{
    [Dependency] private readonly SkillsSystem _skills = default!;

    private const int MedicalSkillRequired = 2;

    private static readonly EntProtoId<SkillDefinitionComponent> MedicalSkillType = "RMCSkillMedical";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMedicalRecordComponent, ExaminedEvent>(OnMedicalRecordExamined);
    }

    private void OnMedicalRecordExamined(Entity<RMCMedicalRecordComponent> ent, ref ExaminedEvent args)
    {
        // Synths can always see records; otherwise require medical HUD + skill
        if (!HasComp<SynthComponent>(args.Examiner))
        {
            if (!_skills.HasSkill(args.Examiner, MedicalSkillType, MedicalSkillRequired))
                return;

            var scanEvent = new HolocardScanEvent(false, SlotFlags.EYES | SlotFlags.HEAD);
            RaiseLocalEvent(args.Examiner, ref scanEvent);
            if (!scanEvent.CanScan)
                return;
        }

        using (args.PushGroup(nameof(SharedRMCRecordsSystem), -2))
        {
            if (ent.Comp.LastScanTime is not { } scanTime)
                args.PushMarkup(Loc.GetString("rmc-records-examine-no-scan"));
            else
            {
                var timeStr = scanTime.ToString(@"hh\:mm\:ss");
                args.PushMarkup(Loc.GetString("rmc-records-examine-scan-time", ("time", timeStr)));
                args.PushMarkup(Loc.GetString("rmc-records-examine-scan-result", ("result", ent.Comp.LastScanResult)));
            }
        }
    }

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
