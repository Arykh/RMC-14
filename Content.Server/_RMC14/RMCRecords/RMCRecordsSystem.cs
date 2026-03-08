using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.RMCRecords;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.RMCRecords;

public sealed class RMCRecordsSystem : SharedRMCRecordsSystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly RMCPulseSystem _rmcPulse = default!;
    [Dependency] private readonly SharedRMCTemperatureSystem _rmcTemperature = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> AirlossGroup = "Airloss";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RMCGeneralRecordComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var mob = args.Mob;
        var profile = args.Profile;

        // Resolve job title from prototype
        var rank = "Unassigned";
        if (!string.IsNullOrEmpty(args.JobId) && _prototypes.TryIndex<JobPrototype>(args.JobId, out var jobPrototype))
            rank = jobPrototype.LocalizedName;

        // General record
        var general = EnsureComp<RMCGeneralRecordComponent>(mob);
        general.Name = profile.Name;
        general.Rank = rank;
        general.Sex = profile.Sex;
        general.Age = profile.Age;
        general.Species = profile.Species;
        general.PhysicalStatus = RMCPhysicalStatus.Active;
        general.MentalStatus = RMCMentalStatus.Stable;
        Dirty(mob, general);

        // TODO RMC-14 actual blood types — update when blood type system is implemented
        // Medical record
        var medical = EnsureComp<RMCMedicalRecordComponent>(mob);
        medical.BloodType = "O-";
        Dirty(mob, medical);

        // Security record
        var security = EnsureComp<RMCSecurityRecordComponent>(mob);
        security.CriminalStatus = RMCCriminalStatus.None;
        Dirty(mob, security);
    }

    private void OnMobStateChanged(Entity<RMCGeneralRecordComponent> ent, ref MobStateChangedEvent args)
    {
        ent.Comp.PhysicalStatus = MobStateToPhysicalStatus(args.NewMobState);
        Dirty(ent);
    }

    // Used by the body scanner
    public void UpdateMedicalRecordFromScan(EntityUid target)
    {
        if (!TryGetMedicalRecord(target, out var medical))
            return;

        medical.LastScanTime = _timing.CurTime;
        medical.LastScanResult = BuildScanSummary(target);
        Dirty(target, medical);
    }

    private string BuildScanSummary(EntityUid target)
    {
        var parts = new List<string>();

        if (_mobState.IsDead(target))
            parts.Add("Status: Dead");
        else if (_mobState.IsCritical(target))
            parts.Add("Status: Critical");
        else
            parts.Add("Status: Alive");

        // Damage
        if (TryComp<DamageableComponent>(target, out var damageable))
        {
            var health = FixedPoint2.Zero;
            if (_mobThreshold.TryGetThresholdForState(target, MobState.Critical, out var critThreshold))
                health = critThreshold.Value - damageable.TotalDamage;

            parts.Add($"Health: {health}");

            var brute = damageable.DamagePerGroup.GetValueOrDefault(BruteGroup);
            var burn = damageable.DamagePerGroup.GetValueOrDefault(BurnGroup);
            var toxin = damageable.DamagePerGroup.GetValueOrDefault(ToxinGroup);
            var oxygen = damageable.DamagePerGroup.GetValueOrDefault(AirlossGroup);

            parts.Add($"Brute: {brute}");
            parts.Add($"Burn: {burn}");
            parts.Add($"Toxin: {toxin}");
            parts.Add($"Oxygen: {oxygen}");
        }

        // Blood
        if (_rmcBloodstream.TryGetBloodSolution(target, out var bloodstream))
        {
            var bloodPercent = bloodstream.MaxVolume > 0
                ? (bloodstream.Volume / bloodstream.MaxVolume * 100).Float()
                : 0f;
            parts.Add($"Blood: {bloodPercent:F0}% ({bloodstream.Volume}/{bloodstream.MaxVolume})");
        }

        if (_rmcBloodstream.IsBleeding(target))
            parts.Add("Bleeding: Yes");

        // Temperature
        if (_rmcTemperature.TryGetCurrentTemperature(target, out var temperature))
            parts.Add($"Temperature: {temperature:F1}K");

        // Pulse
        var pulse = _rmcPulse.TryGetPulseReading(target, true, out _);
        if (!string.IsNullOrEmpty(pulse))
            parts.Add($"Pulse: {pulse}");

        return string.Join(" | ", parts);
    }

    public void SetCriminalStatus(EntityUid target, RMCCriminalStatus status)
    {
        if (!TryGetSecurityRecord(target, out var security))
            return;

        security.CriminalStatus = status;
        Dirty(target, security);
    }

    public void AddSecurityIncident(EntityUid target, string author, string details)
    {
        if (!TryGetSecurityRecord(target, out var security))
            return;

        security.Incidents.Add(new RMCSecurityIncident(_timing.CurTime, author, details));
        Dirty(target, security);
    }

    public void AddAutodocRecord(EntityUid target, string procedure, string details)
    {
        if (!TryGetMedicalRecord(target, out var medical))
            return;

        medical.AutodocData.Add(new RMCAutodocRecord(_timing.CurTime, procedure, details));
        Dirty(target, medical);
    }
}
