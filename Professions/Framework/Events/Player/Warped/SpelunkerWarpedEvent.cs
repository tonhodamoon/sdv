﻿namespace DaLion.Professions.Framework.Events.Player.Warped;

#region using directives

using DaLion.Shared.Events;
using DaLion.Shared.Extensions;
using DaLion.Shared.Extensions.Stardew;
using DaLion.Shared.Extensions.Xna;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

#endregion using directives

[UsedImplicitly]
internal sealed class SpelunkerWarpedEvent : WarpedEvent
{
    private static int _previousMineLevel;

    /// <summary>Initializes a new instance of the <see cref="SpelunkerWarpedEvent"/> class.</summary>
    /// <param name="manager">The <see cref="EventManager"/> instance that manages this event.</param>
    internal SpelunkerWarpedEvent(EventManager? manager = null)
        : base(manager ?? ProfessionsMod.EventManager)
    {
    }

    /// <inheritdoc />
    public override bool IsEnabled => Game1.player.HasProfession(Profession.Spelunker);

    /// <inheritdoc />
    protected override void OnWarpedImpl(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
        {
            return;
        }

        var player = e.Player;
        var oldLocation = e.OldLocation;
        var newLocation = e.NewLocation;
        if (oldLocation is MineShaft && player.HasProfession(Profession.Spelunker, true))
        {
            if (oldLocation is MineShaft)
            {
                foreach (var debris in oldLocation.debris)
                {
                    if (debris.itemId?.Value.StartsWith("(O)") == true && Game1.random.NextBool(0.2))
                    {
                        State.SpelunkerUncollectedItems.Add(debris.itemId.Value);
                    }
                }
            }

            if (newLocation.Name is "Mine" or "SkullCave")
            {
                var mapWidth = newLocation.Map.DisplayWidth;
                var mapHeight = newLocation.Map.DisplayHeight;
                var spawnTiles = player.Tile.GetTwentyFourNeighbors(mapWidth, mapHeight).ToArray();
                foreach (var id in State.SpelunkerUncollectedItems)
                {
                    Game1.createItemDebris(
                        ItemRegistry.Create(id),
                        spawnTiles.Choose(Game1.random) * Game1.tileSize,
                        -1,
                        newLocation);
                }

                State.SpelunkerUncollectedItems.Clear();
            }
        }

        if (newLocation is not MineShaft && oldLocation is MineShaft)
        {
            State.SpelunkerLadderStreak = 0;
            _previousMineLevel = 0;
            return;
        }

        if (newLocation is not MineShaft shaft || shaft.mineLevel <= _previousMineLevel)
        {
            return;
        }

        State.SpelunkerLadderStreak++;
        _previousMineLevel = shaft.mineLevel;
        if (!player.HasProfession(Profession.Spelunker, true) || !shaft.IsTreasureOrSafeRoom())
        {
            return;
        }

        player.health = Math.Min(player.health + (int)(player.maxHealth * 0.05f), player.maxHealth);
        player.Stamina = Math.Min(player.Stamina + (player.MaxStamina * 0.05f), player.MaxStamina);
    }
}
