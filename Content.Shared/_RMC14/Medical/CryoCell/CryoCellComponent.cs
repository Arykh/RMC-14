using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.CryoCell;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedCryoCellSystem))]
public sealed partial class CryoCellComponent : Component
{
    [DataField]
    public string ContainerId = "cryo_cell";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    [DataField]
    public string BeakerContainerId = "cryo_cell_beaker";

    [DataField, AutoNetworkedField]
    public bool IsOn;

    // Temperatures in Kelvin
    [DataField]
    public float CryoLiquidThreshold = 210f;

    [DataField, AutoNetworkedField]
    public bool AutoEject;

    [DataField, AutoNetworkedField]
    public bool ReleaseNotice;

    [DataField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(3.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick;

    [DataField]
    public float OxyHealAmount = 1f;

    [DataField]
    public float PassiveBruteHealAmount = 1f;

    [DataField]
    public float PassiveBurnHealAmount = 1f;

    [DataField]
    public float PassiveToxHealAmount = 1f;

    [DataField]
    public float BeakerTransferAmount = 5f;

    [DataField]
    public SoundSpecifier HealingCompleteSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier WarningSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");
}
