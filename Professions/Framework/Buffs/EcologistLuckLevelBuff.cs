﻿namespace DaLion.Professions.Framework.Buffs;

#region using directives

using StardewValley.Buffs;

#endregion using directives

internal sealed class EcologistLuckLevelBuff : Buff
{
    internal const string ID = "DaLion.Professions.Buffs.EcologistP.LuckLevel";

    internal EcologistLuckLevelBuff(float intensity = 0.5f)
        : base(
            id: ID,
            source: "Ecologist",
            displaySource: _I18n.Get("ecologist.title.prestiged" + (Game1.player.IsMale ? ".male" : ".female")),
            duration: 60000,
            effects: GetBuffEffects(intensity))
    {
    }

    private static BuffEffects GetBuffEffects(float added)
    {
        if (Game1.player.buffs.AppliedBuffs.TryGetValue(ID, out var current))
        {
            return new BuffEffects { LuckLevel = { current.effects.LuckLevel.Value + added } };
        }

        return new BuffEffects { LuckLevel = { added } };
    }
}
