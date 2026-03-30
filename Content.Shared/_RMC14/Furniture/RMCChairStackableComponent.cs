using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Furniture;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCChairStackableComponent : Component
{
    /// <summary>
    /// Maximum number of chairs that can be stacked stably.
    /// Beyond this, the stack has a chance to collapse when adding more.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxStableStack = 8;

    /// <summary>
    /// Current number of extra folded chairs stacked on this chair.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentStackSize;

    /// <summary>
    /// The container ID used to store stacked folded chairs.
    /// </summary>
    [DataField]
    public string ContainerId = "rmc_chair_stack";
}

[Serializable, NetSerializable]
public enum RMCChairStackVisuals : byte
{
    StackSize
}
