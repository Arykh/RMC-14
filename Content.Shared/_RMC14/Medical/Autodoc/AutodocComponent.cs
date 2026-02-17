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
    public Vector2 ConsoleSpawnOffset = new(1, 0);

    /// <summary>
    /// Is the autodoc currently performing surgery?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsSurgeryInProgress;

    /// <summary>
    /// Is healing brute damage currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HealingBrute;

    /// <summary>
    /// Is healing burn damage currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HealingBurn;

    /// <summary>
    /// Is healing toxin damage currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HealingToxin;

    /// <summary>
    /// Is blood transfusion currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BloodTransfusion;

    /// <summary>
    /// Is dialysis (filtering) currently enabled?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Filtering;

    /// <summary>
    /// Amount to heal per tick for brute/burn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealAmount = FixedPoint2.New(3);

    /// <summary>
    /// Amount to heal per tick for toxin.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ToxinHealAmount = FixedPoint2.New(3);

    /// <summary>
    /// Amount to filter per tick for dialysis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DialysisAmount = FixedPoint2.New(3);

    /// <summary>
    /// Amount of blood to transfuse per tick.
    /// </summary>
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

    /// <summary>
    /// Stun applied when exiting the autodoc.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ExitStun = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether to auto eject dead patients.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AutoEjectDead;

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
}

