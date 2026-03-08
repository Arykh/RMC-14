using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.HUD.Events;
using Content.Shared._RMC14.Synth;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.RMCRecords;

public abstract class SharedRMCRecordsSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    private const int MedicalSkillRequired = 2;
    private static readonly EntProtoId<SkillDefinitionComponent> MedicalSkillType = "RMCSkillMedical";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMedicalRecordComponent, GetVerbsEvent<ExamineVerb>>(OnMedicalRecordExamineVerb);
    }

    private void OnMedicalRecordExamineVerb(Entity<RMCMedicalRecordComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract)
            return;

        // Synths can always see records; otherwise require medical HUD + skill
        if (!HasComp<SynthComponent>(args.User))
        {
            if (!_skills.HasSkill(args.User, MedicalSkillType, MedicalSkillRequired))
                return;

            var scanEvent = new HolocardScanEvent(false, SlotFlags.EYES | SlotFlags.HEAD);
            RaiseLocalEvent(args.User, ref scanEvent);
            if (!scanEvent.CanScan)
                return;
        }

        var msg = new FormattedMessage();
        if (ent.Comp.LastScanTime is not { } scanTime)
        {
            msg.AddMarkupOrThrow(Loc.GetString("rmc-records-examine-no-scan"));
        }
        else
        {
            var timeStr = scanTime.ToString(@"hh\:mm\:ss");
            msg.AddMarkupOrThrow(Loc.GetString("rmc-records-examine-scan-time", ("time", timeStr)));
            msg.PushNewline();
            msg.AddMarkupOrThrow(Loc.GetString("rmc-records-examine-scan-result", ("result", ent.Comp.LastScanResult)));
        }

        _examine.AddDetailedExamineVerb(
            args,
            ent.Comp,
            msg,
            Loc.GetString("rmc-records-examine-verb-text"),
            "/Textures/_RMC14/Interface/VerbIcons/ambulance.png",
            Loc.GetString("rmc-records-examine-verb-message"));
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
