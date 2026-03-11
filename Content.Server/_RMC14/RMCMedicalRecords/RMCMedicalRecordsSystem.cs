using Content.Server.StationRecords.Systems;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Medical.Surgery.Steps.Parts;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.RMCMedicalRecords;
using Content.Shared._RMC14.Temperature;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.RMCMedicalRecords;

/// <summary>
///     Creates <see cref="RMCMedicalRecord"/> entries in the station record set when a general record is created.
///     Also manages entity-bound scan data on <see cref="RMCMedicalRecordComponent"/>.
/// </summary>
public sealed class RMCMedicalRecordsSystem : SharedRMCMedicalRecordsSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        _stationRecords.AddRecordEntry(ev.Key, new RMCMedicalRecord());
        _stationRecords.Synchronize(ev.Key);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        EnsureComp<RMCMedicalRecordComponent>(args.Mob);
    }

    public void UpdateMedicalRecordFromScan(EntityUid target)
    {
        if (!TryGetMedicalRecord(target, out var medical))
            return;

        medical.LastScanTime = _timing.CurTime;
        medical.LastScanState = BuildScanSnapshot(target);
        medical.AutodocData = GenerateAutodocData(target);
        Dirty(target, medical);
    }

    public HealthScanState BuildScanSnapshot(EntityUid target, HealthScanDetailLevel detailLevel = HealthScanDetailLevel.BodyScan)
    {
        var blood = default(FixedPoint2);
        var maxBlood = default(FixedPoint2);
        if (_rmcBloodstream.TryGetBloodSolution(target, out var bloodstream))
        {
            blood = bloodstream.Volume;
            maxBlood = bloodstream.MaxVolume;
        }

        _rmcBloodstream.TryGetChemicalSolution(target, out _, out var chemicals);
        _rmcTemperature.TryGetCurrentTemperature(target, out var temperature);

        var pulse = _rmcPulse.TryGetPulseReading(target, true, out _);
        var bleeding = _rmcBloodstream.IsBleeding(target);

        return new HealthScanState(
            GetNetEntity(target),
            blood,
            maxBlood,
            temperature,
            pulse,
            chemicals,
            bleeding,
            detailLevel);
    }

    private List<RMCAutodocRecord> GenerateAutodocData(EntityUid target)
    {
        var time = _timing.CurTime;
        var autodocData = new List<RMCAutodocRecord>();

        if (TryComp<DamageableComponent>(target, out var damageable))
        {
            if (damageable.DamagePerGroup.GetValueOrDefault(BruteGroup) > 0)
                autodocData.Add(new RMCAutodocRecord(time, AutodocProcedures.Brute, Loc.GetString("rmc-records-autodoc-brute")));

            if (damageable.DamagePerGroup.GetValueOrDefault(BurnGroup) > 0)
                autodocData.Add(new RMCAutodocRecord(time, AutodocProcedures.Burn, Loc.GetString("rmc-records-autodoc-burn")));

            if (damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup) > 0)
                autodocData.Add(new RMCAutodocRecord(time, AutodocProcedures.Toxin, Loc.GetString("rmc-records-autodoc-toxin")));
        }

        foreach (var part in _body.GetBodyChildren(target))
        {
            if (HasComp<CMIncisionOpenComponent>(part.Id))
            {
                autodocData.Add(new RMCAutodocRecord(time, AutodocProcedures.CloseIncisions, Loc.GetString("rmc-records-autodoc-incision")));
                break;
            }
        }

        // TODO RMC14 Remove Shrapnel

        if (_rmcBloodstream.TryGetBloodSolution(target, out var bloodstream) && bloodstream.Volume < bloodstream.MaxVolume)
            autodocData.Add(new RMCAutodocRecord(time, AutodocProcedures.Blood, Loc.GetString("rmc-records-autodoc-blood")));

        if (_rmcBloodstream.TryGetChemicalSolution(target, out _, out var chemSol))
        {
            foreach (var reagentQuantity in chemSol.Contents)
            {
                if (!_rmcReagent.TryIndex(reagentQuantity.Reagent, out var reagentProto))
                    continue;

                if (reagentProto.Overdose is { } overdose && reagentQuantity.Quantity >= overdose)
                {
                    autodocData.Add(new RMCAutodocRecord(time, AutodocProcedures.Dialysis, Loc.GetString("rmc-records-autodoc-dialysis")));
                    break;
                }
            }
        }

        // TODO RMC-14 Internal Bleeding, Broken Bones, Organ Damage

        if (TryComp<VictimInfectedComponent>(target, out var infected) && !infected.IsBursting)
            autodocData.Add(new RMCAutodocRecord(time, AutodocProcedures.Larva, Loc.GetString("rmc-records-autodoc-larva")));

        return autodocData;
    }
}
