using System.Numerics;
using Content.Shared._RMC14.Furniture;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Furniture;

public sealed class RMCChairStackVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string StackLayerPrefix = "rmc_chair_stack_";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCChairStackableComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<RMCChairStackableComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<int>(ent, RMCChairStackVisuals.StackSize, out var stackSize))
            stackSize = 0;

        UpdateStackLayers(ent, args.Sprite, stackSize);
    }

    private void UpdateStackLayers(EntityUid uid, SpriteComponent sprite, int stackSize)
    {
        Entity<SpriteComponent?> spriteEnt = (uid, sprite);

        // Remove all existing stack layers first
        for (var i = 0; i < 50; i++) // arbitrary max to avoid infinite loop
        {
            var key = StackLayerPrefix + i;
            if (_sprite.LayerMapTryGet(spriteEnt, key, out var index, false))
            {
                _sprite.LayerMapRemove(spriteEnt, key);
                _sprite.RemoveLayer(spriteEnt, index);
            }
            else
            {
                break;
            }
        }

        if (stackSize <= 0)
            return;

        // Get the RSI and state from the first (unfolded) layer for the overlay sprite
        var rsi = _sprite.LayerGetEffectiveRsi(spriteEnt, 0)?.Path;
        if (rsi == null)
            return;

        var state = _sprite.LayerGetRsiState(spriteEnt, 0).ToString();
        if (string.IsNullOrWhiteSpace(state))
            return;

        var xform = Transform(uid);
        var dir = xform.LocalRotation.GetCardinalDir();

        const float pixelToWorld = 1f / 32f;

        var prevOffsetX = 0f;
        var prevOffsetY = 0f;

        for (var i = 0; i < stackSize; i++)
        {
            float offsetX;
            float offsetY;

            if (i == 0)
            {
                // First stacked chair offset from base
                switch (dir)
                {
                    case Direction.North:
                    case Direction.South:
                        offsetX = 0;
                        offsetY = 2 * pixelToWorld;
                        break;
                    case Direction.East:
                        offsetX = 1 * pixelToWorld;
                        offsetY = 3 * pixelToWorld;
                        break;
                    case Direction.West:
                        offsetX = -1 * pixelToWorld;
                        offsetY = 3 * pixelToWorld;
                        break;
                    default:
                        offsetX = 0;
                        offsetY = 2 * pixelToWorld;
                        break;
                }
            }
            else
            {
                // Subsequent chairs build on previous offset
                switch (dir)
                {
                    case Direction.North:
                    case Direction.South:
                        offsetX = prevOffsetX;
                        offsetY = prevOffsetY + 2 * pixelToWorld;
                        break;
                    case Direction.East:
                        offsetX = prevOffsetX + 1 * pixelToWorld;
                        offsetY = prevOffsetY + 3 * pixelToWorld;
                        break;
                    case Direction.West:
                        offsetX = prevOffsetX - 1 * pixelToWorld;
                        offsetY = prevOffsetY + 3 * pixelToWorld;
                        break;
                    default:
                        offsetX = prevOffsetX;
                        offsetY = prevOffsetY + 2 * pixelToWorld;
                        break;
                }
            }

            // Instability jitter for stacks > maxStableStack
            if (stackSize > 8)
            {
                // Small visual jitter - alternate direction based on index
                offsetX += (i % 2 == 0 ? -1 : 1) * pixelToWorld;
            }

            var layerData = new PrototypeLayerData
            {
                RsiPath = rsi.ToString(),
                State = state,
                Offset = new Vector2(offsetX, offsetY),
                Visible = true,
            };

            var key = StackLayerPrefix + i;
            var layerIndex = _sprite.AddLayer(spriteEnt, layerData, null);
            _sprite.LayerMapSet(spriteEnt, key, layerIndex);

            prevOffsetX = offsetX;
            prevOffsetY = offsetY;
        }
    }
}
