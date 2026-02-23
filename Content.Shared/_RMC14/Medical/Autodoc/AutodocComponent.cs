using System.Numerics;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.Autodoc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedAutodocSystem))]
public sealed partial class AutodocComponent : Component
{
    [DataField]
    public string ContainerId = "autodoc";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    /// <summary>
    /// The prototype to spawn the console. If null, no console is spawned.
    /// </summary>
    [DataField]
    public EntProtoId<AutodocConsoleComponent>? SpawnConsolePrototype = "RMCAutodocConsole";

    /// <summary>
    /// Offset for spawning the console relative to the autodoc.
    /// This is applied based on the autodoc's rotation.
    /// </summary>
    [DataField]
    public Vector2 ConsoleSpawnOffset = new(0, 1);

    // External treatments (continuous)
    [DataField, AutoNetworkedField]
    public bool HealingBrute;

    [DataField, AutoNetworkedField]
    public bool HealingBurn;

    [DataField, AutoNetworkedField]
    public bool HealingToxin;

    [DataField, AutoNetworkedField]
    public bool BloodTransfusion;

    [DataField, AutoNetworkedField]
    public bool Filtering;

    // Surgical procedures (queued)
    [DataField, AutoNetworkedField]
    public bool RemoveLarva;

    [DataField, AutoNetworkedField]
    public bool CloseIncisions;

    [DataField, AutoNetworkedField]
    public bool RemoveShrapnel;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BruteHealAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public FixedPoint2 BurnHealAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public FixedPoint2 ToxinHealAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public FixedPoint2 DialysisAmount = FixedPoint2.New(3);

    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodTransfusionAmount = FixedPoint2.New(8);

    /// <summary>
    /// Delay between processing ticks.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time of next processing tick.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick;

    [DataField, AutoNetworkedField]
    public TimeSpan LarvaExtractionTime = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan CloseIncisionTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan ShrapnelRemovalTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public bool IsSurgeryInProgress;

    [DataField, AutoNetworkedField]
    public AutodocSurgeryType CurrentSurgeryType;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SurgeryCompleteAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    [DataField, AutoNetworkedField]
    public EntityUid? SpawnedConsole;

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_3.ogg");

    [DataField]
    public SoundSpecifier AutoEjectDeadSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    [DataField]
    public SoundSpecifier SurgeryCompleteSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier SurgeryStartSound = new SoundPathSpecifier("/Audio/Machines/airlock_close.ogg");

    [DataField]
    public SoundSpecifier SurgeryStepSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");
}

public enum AutodocSurgeryType : byte
{
    None = 0,
    LarvaExtraction,
    CloseIncision,
    ShrapnelRemoval
}
