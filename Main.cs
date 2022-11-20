using MelonLoader;
using MoreDegrees;
using Main = MoreDegrees.Main;

[assembly: MelonInfo(typeof(Main), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace MoreDegrees;

using System;
using Assets.Scripts.Models;
using Assets.Scripts.Models.Towers;
using Assets.Scripts.Models.Towers.Behaviors;
using Assets.Scripts.Models.Towers.Projectiles;
using Assets.Scripts.Models.Towers.Projectiles.Behaviors;
using Assets.Scripts.Simulation.Towers;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Unity.UI_New.InGame.AbilitiesMenu;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;

[HarmonyPatch(typeof(ParagonTowerModel.PowerDegreeMutator), nameof(ParagonTowerModel.PowerDegreeMutator.Mutate))]
internal static class PowerDegreeMutatorMutate
{
    [HarmonyPostfix]
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once InconsistentNaming
    private static void Postfix(ParagonTowerModel.PowerDegreeMutator __instance, Model model)
    {
        var t = __instance;
        var tower = model.Cast<TowerModel>();
        var tts = tower.GetAllTowerToSim().Find(tts => tts.GetParagonDegreeMutator().Equals(t));
        var degree = Main.CalculateDegree(tts.GetCurrentParagonInvestment().totalInvestment);
        __instance.degree = degree;
        tts.GetParagonDegreeMutator().degree = degree;
    }
}

public class Main : BloonsTD6Mod
{
    private static readonly ModSettingInt MaxDegree = new(1000000000)
    {
        min = 100,
        max = int.MaxValue,
        description = "The maximum degree a paragon can be",
        displayName = "Max Degree"
    };

    private static readonly ModSettingCategory Nerdy = new("Nerdy Stuff")
    {
        collapsed = true,
        icon = VanillaSprites.UltraboostUpgradeIcon
    };

    private static readonly ModSettingDouble A = new(-1)
    {
        category = Nerdy,
        description = "The a value of the cubic equation that determines the power required for each degree. Setting it to -1 will use the vanilla equation.",
        displayName = "A Value"
    };

    private static readonly ModSettingDouble B = new(-1)
    {
        category = Nerdy,
        description = "The b value of the cubic equation that determines the power required for each degree. Setting it to -1 will use the vanilla equation.",
        displayName = "B Value"
    };

    private static readonly ModSettingDouble C = new(-1)
    {
        category = Nerdy,
        description = "The c value of the cubic equation that determines the power required for each degree. Setting it to -1 will use the vanilla equation.",
        displayName = "C Value"
    };

    private static readonly ModSettingDouble D = new(-1)
    {
        category = Nerdy,
        description = "The d value of the cubic equation that determines the power required for each degree, note that the total investment is subtracted from this value. Setting it to -1 will use the vanilla equation.",
        displayName = "D Value"
    };

    public override void OnTowerModelChanged(Tower tower, Model newModel)
    {
        base.OnTowerModelChanged(tower, newModel);
        var towerModel = newModel.Cast<TowerModel>();
        var tts = tower.GetTowerToSim();
        if (!towerModel.isParagon || tts.GetParagonDegreeMutator() == null) return;

        var original = InGame.instance.GetGameModel().GetTowerFromId(towerModel.GetTowerId());
        var degree = tower.GetTowerToSim().GetParagonDegreeMutator().degree = CalculateDegree(tower.GetTowerToSim().GetCurrentParagonInvestment().totalInvestment);
        for (var i = 0; i < towerModel.GetWeapons().Count; i++)
        {
            var weapon = towerModel.GetWeapons()[i];
            weapon.rate = CalculateSpeed(degree, original.GetWeapons()[i].rate);
            weapon.projectile.pierce = CalculatePierce(degree, original.GetWeapons()[i].projectile.pierce);

            if (!weapon.projectile.HasBehavior<DamageModifierForTagModel>()) continue;
            if (weapon.projectile.GetBehavior<DamageModifierForTagModel>().tag == "Boss")
            {
                weapon.projectile.GetBehavior<DamageModifierForTagModel>().damageMultiplier = 2f;
                continue;
            }
            weapon.projectile.GetBehavior<DamageModifierForTagModel>().damageAddative = CalculateAdditive(degree, original.GetWeapons()[i].projectile.GetBehavior<DamageModifierForTagModel>().damageAddative);
        }

        for (var i = 0; i < towerModel.GetDescendants<ProjectileModel>().ToList().Count; i++)
        {
            var projectile = towerModel.GetDescendants<ProjectileModel>().ToList()[i];
            if (projectile.GetDamageModel() is null) continue;
            projectile.GetDamageModel().damage = CalculateDamage(degree, original.GetDescendants<ProjectileModel>().ToList()[i].GetDamageModel().damage);
        }

        for (var i = 0; i < towerModel.GetAbilities().Count && towerModel.GetAbilities().Count > 0; i++)
        {
            var ability = towerModel.GetAbilities()[i];
            if (ability is null) continue;
            ability.Cooldown = CalculateSpeed(degree, original.GetAbilities()[i].Cooldown);
            AbilityMenu.instance.TowerChanged(tts);
            AbilityMenu.instance.RebuildAbilities();
        }
    }

    private static float CalculateAdditive(int degree, float baseAdditive)
    {
        var damage = baseAdditive * (1f + 0.01f * (degree - 1f));
        if (damage is >= int.MaxValue or <= int.MinValue)
            return int.MaxValue;
        return damage;
    }

    private static float CalculatePierce(int degree, float basePierce)
    {
        var damage = basePierce * (1f + 0.01f * (degree - 1f)) + (degree - 1f);
        if (damage is >= int.MaxValue or <= int.MinValue)
            return int.MaxValue;
        return damage;
    }

    private static float CalculateSpeed(int degree, float baseSpeed)
    {
        var damage = (float) (baseSpeed / (1f + 0.01f * Math.Sqrt(50f * degree - 50f)));
        if (damage is >= int.MaxValue or <= int.MinValue)
            return int.MaxValue;
        return damage;
    }

    private static float CalculateDamage(int degree, float baseDamage)
    {
        var damage = baseDamage * (1f + 0.01f * (degree - 1f)) + (degree - 1f) / 10f;
        if (damage is >= int.MaxValue or <= int.MinValue)
            return int.MaxValue;
        return damage;
    }

    public static int CalculateDegree(float investment)
    {
        if (investment == 0) return 1;
        double a;
        double b;
        double c;
        double d;
        if (Math.Abs(A - -1f) > 64)
            a = A;
        else
            a = 0.08474576271;

        if (Math.Abs(B - -1f) > 64)
            b = B;
        else
            b = 8.516949153;

        if (Math.Abs(C - -1f) > 64)
            c = C;
        else
            c = 285.2949153;

        if (Math.Abs(D - -1f) > 64)
            d = D - investment;
        else
            d = 1428.813559 - investment;

        int degree;
        var roots = CubicSolver.Solve(a, b, c, d);
        if (roots[0].Magnitude >= int.MaxValue || roots[0].Magnitude <= int.MinValue)
            degree = int.MaxValue;
        else degree = Convert.ToInt32(roots[0].Magnitude);

        if (degree > MaxDegree || degree is >= int.MaxValue or <= int.MinValue)
            return MaxDegree;

        if (degree < 1) return 1;

        return degree;
    }

    public override void OnInGameLoaded(InGame inGame)
    {
        base.OnInGameLoaded(inGame);
        var result = inGame.GetGameModel();
        result.paragonDegreeDataModel.maxPowerFromPops = int.MaxValue;
        result.paragonDegreeDataModel.maxPowerFromMoneySpent = int.MaxValue;
        result.paragonDegreeDataModel.maxPowerFromNonTier5Count = int.MaxValue;
        result.paragonDegreeDataModel.maxPowerFromTier5Count = int.MaxValue;
    }

    public override void OnNewGameModel(GameModel result)
    {
        base.OnNewGameModel(result);
        result.paragonDegreeDataModel.degreeCount = MaxDegree;
    }
}
