﻿namespace DaLion.Professions.Framework.Patchers.Combat;

#region using directives

using DaLion.Professions.Framework.VirtualProperties;
using DaLion.Shared.Extensions.Stardew;
using DaLion.Shared.Harmony;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

#endregion using directives

[UsedImplicitly]
internal sealed class GreenSlimeUpdatePatcher : HarmonyPatcher
{
    /// <summary>Initializes a new instance of the <see cref="GreenSlimeUpdatePatcher"/> class.</summary>
    /// <param name="harmonizer">The <see cref="Harmonizer"/> instance that manages this patcher.</param>
    internal GreenSlimeUpdatePatcher(Harmonizer harmonizer)
        : base(harmonizer)
    {
        this.Target = this.RequireMethod<GreenSlime>(
            nameof(GreenSlime.update), [typeof(GameTime), typeof(GameLocation)]);
    }

    #region harmony patches

    /// <summary>Patch for Slimes to damage monsters around Piper.</summary>
    [HarmonyPostfix]
    private static void GreenSlimeUpdatePostfix(GreenSlime __instance, ref int ___readyToJump, GameTime time)
    {
        if (__instance.Get_Piped() is not { } piped)
        {
            return;
        }

        __instance.Speed = piped.Piper.Speed;,
        if (piped.PipeTimer > 0)
        {
            piped.PipeTimer -= time.ElapsedGameTime.Milliseconds;
            if (piped.PipeTimer <= 0)
            {
                piped.Burst();
            }
        }

        if (!piped.FakeFarmer.IsEnemy && time.ElapsedGameTime.Milliseconds % 4 == 0)
        {
            ___readyToJump = -1;
            __instance.timeSinceLastJump = 0;
            var approximatePosition =
                Reflector.GetUnboundMethodDelegate<Func<Debris, Vector2>>(
                    typeof(Debris),
                    "approximatePosition");
            for (var i = __instance.currentLocation.debris.Count - 1; i >= 0; i--)
            {
                var debris = __instance.currentLocation.debris[i];
                if (debris.itemId is null)
                {
                    continue;
                }

                if (__instance.GetBoundingBox().Contains(approximatePosition(debris)) &&
                    __instance.CollectDebris(debris))
                {
                    __instance.currentLocation.debris.RemoveAt(i);
                }
            }
        }

        foreach (var character in __instance.currentLocation.characters)
        {
            if (character is not Monster { IsMonster: true } monster
                || (monster.IsGlider() && !(__instance.Scale > 1.8f || __instance.Get_Jumping()))
                || monster.IsSlime()
                || !monster.CanBeDamaged())
            {
                continue;
            }

            var monsterBox = monster.GetBoundingBox();
            if (!monsterBox.Intersects(__instance.GetBoundingBox()))
            {
                continue;
            }

            // damage monster
            var randomizedDamage = __instance.DamageToFarmer +
                                   Game1.random.Next(-__instance.DamageToFarmer / 4, __instance.DamageToFarmer / 4);
            var damageToMonster = (int)Math.Max(1, randomizedDamage * __instance.Scale) - monster.resilience.Value;
            var (xTrajectory, yTrajectory) = monster.Slipperiness < 0
                ? Vector2.Zero
                : Utility.getAwayFromPositionTrajectory(monsterBox, __instance.getStandingPosition()) / 2f;
            monster.takeDamage(damageToMonster, (int)xTrajectory, (int)yTrajectory, false, 1d, "slime");
            monster.currentLocation.debris.Add(new Debris(
                damageToMonster,
                new Vector2(monsterBox.Center.X + 16, monsterBox.Center.Y),
                new Color(255, 130, 0),
                1f,
                monster));
            monster.setInvincibleCountdown(piped.Piper.Get_IsLimitBreaking().Value ? 300 : 450);

            // aggro monsters
            if (monster.Get_Taunter() is null)
            {
                monster.Set_Taunter(__instance);
            }

            var fakeFarmer = monster.Get_TauntFakeFarmer();
            if (fakeFarmer is not null)
            {
                fakeFarmer.Position = __instance.Position;
            }

            // get damaged by monster
            randomizedDamage = monster.DamageToFarmer +
                               Game1.random.Next(-monster.DamageToFarmer / 4, monster.DamageToFarmer / 4);
            var damageToSlime = Math.Max(1, randomizedDamage) - __instance.resilience.Value;
            __instance.takeDamage(damageToSlime, (int)-xTrajectory, (int)-yTrajectory, false, 1d, "slime");
            if (__instance.Health <= 0)
            {
                break;
            }
        }
    }

    #endregion harmony patches
}
