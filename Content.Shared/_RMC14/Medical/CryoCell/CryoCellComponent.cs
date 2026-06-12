using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.CryoCell;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCryoCellSystem))]
public sealed partial class CryoCellComponent : Component
{
    [DataField]
    public string ContainerId = "cryo_cell";

    [DataField, AutoNetworkedField]
    public EntityUid? Occupant;

    [DataField]
    public SoundSpecifier HealingCompleteSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier WarningSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg");
}

[Serializable, NetSerializable]
public enum CryoCellVisuals : byte
{
    Occupied
}

[Serializable, NetSerializable]
public enum CryoCellVisualLayers
{
    Base
}
