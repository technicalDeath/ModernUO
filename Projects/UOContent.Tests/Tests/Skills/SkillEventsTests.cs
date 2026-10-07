using System.Collections.Generic;
using Server;
using Server.Misc;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class SkillEventsTests
{
    private sealed class Recorder
    {
        public Mobile From;
        public Skill Skill;
        public bool Success;
        public int Calls;

        public void Handle(Mobile from, Skill skill, bool success)
        {
            From = from;
            Skill = skill;
            Success = success;
            Calls++;
        }
    }

    [Fact]
    public void DirectTarget_RolledAttempt_RaisesOnceWithTheReturnedOutcome()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        var recorder = new Recorder();

        SkillEvents.SkillUsed += recorder.Handle;
        try
        {
            var rolled = SkillCheck.Mobile_SkillCheckDirectTarget(from, SkillName.Mining, null, 0.5);
            Assert.Equal(1, recorder.Calls);
            Assert.Same(from, recorder.From);
            Assert.Same(skill, recorder.Skill);
            Assert.Equal(rolled, recorder.Success);

            Assert.False(SkillCheck.Mobile_SkillCheckDirectTarget(from, SkillName.Mining, null, 0.0));
            Assert.Equal(2, recorder.Calls);
            Assert.False(recorder.Success);
        }
        finally
        {
            SkillEvents.SkillUsed -= recorder.Handle;
            from.Delete();
        }
    }

    [Fact]
    public void ShortCircuits_StillRaise_WithTheHandlerOutcome()
    {
        var from = new Mobile();
        var recorder = new Recorder();

        SkillEvents.SkillUsed += recorder.Handle;
        try
        {
            Assert.True(SkillCheck.Mobile_SkillCheckDirectLocation(from, SkillName.Mining, 1.0));
            Assert.Equal(1, recorder.Calls);
            Assert.True(recorder.Success);

            Assert.False(SkillCheck.Mobile_SkillCheckDirectTarget(from, SkillName.Mining, null, -0.1));
            Assert.Equal(2, recorder.Calls);
            Assert.False(recorder.Success);

            Assert.False(SkillCheck.Mobile_SkillCheckLocation(from, SkillName.Mining, 50.0, 100.0));
            Assert.Equal(3, recorder.Calls);
            Assert.False(recorder.Success);

            Assert.True(SkillCheck.Mobile_SkillCheckTarget(from, SkillName.Mining, null, 0.0, 0.0));
            Assert.Equal(4, recorder.Calls);
            Assert.True(recorder.Success);
        }
        finally
        {
            SkillEvents.SkillUsed -= recorder.Handle;
            from.Delete();
        }
    }

    [Fact]
    public void CheckSkill_Direct_DoesNotRaise()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        var recorder = new Recorder();

        SkillEvents.SkillUsed += recorder.Handle;
        try
        {
            SkillCheck.CheckSkill(from, skill, null, 1.0);
            Assert.Equal(0, recorder.Calls);
        }
        finally
        {
            SkillEvents.SkillUsed -= recorder.Handle;
            from.Delete();
        }
    }

    [Fact]
    public void NoSubscriber_DoesNotThrow()
    {
        var from = new Mobile();
        var recorder = new Recorder();

        try
        {
            SkillEvents.SkillUsed += recorder.Handle;
            SkillEvents.SkillUsed -= recorder.Handle;

            Assert.True(SkillCheck.Mobile_SkillCheckDirectLocation(from, SkillName.Mining, 1.0));
            Assert.Equal(0, recorder.Calls);
        }
        finally
        {
            from.Delete();
        }
    }

    [Fact]
    public void GainOverride_RunsAfterEligibilityAndSuppressesStockGain()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        skill.Base = 50.0;
        var calls = 0;

        bool Handle(Mobile mobile, Skill gainedSkill, bool success)
        {
            calls++;
            Assert.Same(from, mobile);
            Assert.Same(skill, gainedSkill);
            return true;
        }

        SkillEvents.SkillGainOverride += Handle;
        try
        {
            SkillCheck.CheckSkill(from, skill, new object(), 0.5);
            Assert.Equal(1, calls);
            Assert.Equal(50.0, skill.Base);
        }
        finally
        {
            SkillEvents.SkillGainOverride -= Handle;
            from.Delete();
        }
    }

    [Fact]
    public void GainChanceMultiplier_ScalesTheStockRollOnly()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        skill.Base = 50.0;
        var calls = 0;

        double Boost(Mobile mobile, Skill gainedSkill)
        {
            calls++;
            Assert.Same(from, mobile);
            Assert.Same(skill, gainedSkill);
            return 1000.0;
        }

        SkillEvents.GainChanceMultiplier = Boost;
        try
        {
            // A multiplier large enough that the stock probability reaches 1 gains on every eligible check.
            SkillCheck.CheckSkill(from, skill, new object(), 0.5);
            Assert.Equal(1, calls);
            Assert.Equal(50.1, skill.Base, 3);

            // Sub-10 gain is unconditional in stock and never consults the multiplier.
            skill.Base = 5.0;
            SkillCheck.CheckSkill(from, skill, new object(), 0.5);
            Assert.Equal(1, calls);
            Assert.True(skill.Base > 5.0);
        }
        finally
        {
            SkillEvents.GainChanceMultiplier = null;
            from.Delete();
        }
    }

    [Fact]
    public void GainChanceMultiplier_IsNotConsultedWhenAnOverrideHandlesTheAttempt()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        skill.Base = 50.0;
        var calls = 0;

        SkillEvents.GainChanceMultiplier = (_, _) =>
        {
            calls++;
            return 1000.0;
        };

        bool Handle(Mobile mobile, Skill gainedSkill, bool success) => true;

        SkillEvents.SkillGainOverride += Handle;
        try
        {
            SkillCheck.CheckSkill(from, skill, new object(), 0.5);
            Assert.Equal(0, calls);
            Assert.Equal(50.0, skill.Base);
        }
        finally
        {
            SkillEvents.SkillGainOverride -= Handle;
            SkillEvents.GainChanceMultiplier = null;
            from.Delete();
        }
    }

    [Fact]
    public void GainChanceMultiplier_DefaultIsStock()
    {
        Assert.Null(SkillEvents.GainChanceMultiplier);

        var from = new Mobile();
        try
        {
            Assert.Equal(1.0, SkillEvents.InvokeGainChanceMultiplier(from, from.Skills[SkillName.Mining]));
        }
        finally
        {
            from.Delete();
        }
    }

    [Fact]
    public void SubTenEligibleAttemptCanUseTheRestorationOverride()
    {
        var from = new Mobile();
        var skill = from.Skills[SkillName.Mining];
        skill.Base = 5.0;
        var calls = 0;

        bool Handle(Mobile mobile, Skill gainedSkill, bool success)
        {
            Assert.Same(from, mobile);
            Assert.Same(skill, gainedSkill);
            calls++;
            return true;
        }

        SkillEvents.SkillGainOverride += Handle;
        try
        {
            SkillCheck.CheckSkill(from, skill, new object(), 0.5);
            Assert.Equal(1, calls);
            Assert.Equal(5.0, skill.Base);
        }
        finally
        {
            SkillEvents.SkillGainOverride -= Handle;
            from.Delete();
        }
    }

    [Fact]
    public void SubTenAntiMacroDenialStillBanksItsStockCapDisplacement()
    {
        if (AntiMacroSystem.Settings == null)
        {
            AntiMacroSystem.Configure();
        }

        var originalSettings = AntiMacroSystem.Settings;
        var settingsProperty = typeof(AntiMacroSystem).GetProperty(nameof(AntiMacroSystem.Settings));
        settingsProperty.SetValue(null, originalSettings with { Enabled = true, Allowance = 1 });

        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        player.Map = Map.Felucca;
        player.Skills.Cap = 1000;
        var gaining = player.Skills[SkillName.Anatomy];
        var displaced = player.Skills[SkillName.Tactics];
        gaining.BaseFixedPoint = 50;
        displaced.BaseFixedPoint = 950;
        displaced.SetLockNoRelay(SkillLock.Down);
        var target = new object();
        var reportedTenths = 0;

        void Observe(Mobile mobile, Skill skill, int tenths)
        {
            Assert.Same(player, mobile);
            Assert.Same(displaced, skill);
            reportedTenths += tenths;
        }

        SkillEvents.SkillDisplaced += Observe;
        try
        {
            Assert.True(AntiMacroSystem.UseAntiMacro((int)SkillName.Anatomy));

            SkillCheck.CheckSkill(player, gaining, target, 0.5);
            Assert.True(reportedTenths > 0);
            var bankableAfterEligibleUse = reportedTenths;
            var activeAfterEligibleUse = gaining.BaseFixedPoint;
            var displacedAfterEligibleUse = displaced.BaseFixedPoint;

            // The second use is refused by the anti-macro boundary, but the stock sub-10 gain still happens and still
            // takes points from the Down skill. Owner ruling 2026-10-07: every point a Down skill loses is published,
            // so the bank can keep it.
            SkillCheck.CheckSkill(player, gaining, target, 0.5);
            Assert.True(gaining.BaseFixedPoint > activeAfterEligibleUse);
            Assert.True(displaced.BaseFixedPoint < displacedAfterEligibleUse);
            Assert.Equal(bankableAfterEligibleUse + (displacedAfterEligibleUse - displaced.BaseFixedPoint), reportedTenths);
        }
        finally
        {
            SkillEvents.SkillDisplaced -= Observe;
            settingsProperty.SetValue(null, originalSettings);
            player.Delete();
        }
    }

    [Fact]
    public void OrganicGainReportsExactDisplacedSkillAndAmount()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        var gaining = player.Skills[SkillName.Anatomy];
        var displaced = player.Skills[SkillName.Tactics];
        player.Skills.Cap = 1000;
        gaining.BaseFixedPoint = 999;
        displaced.BaseFixedPoint = 1;
        displaced.SetLockNoRelay(SkillLock.Down);
        var calls = 0;

        void Observe(Mobile mobile, Skill skill, int tenths)
        {
            Assert.Same(player, mobile);
            Assert.Same(displaced, skill);
            Assert.Equal(1, tenths);
            calls++;
        }

        SkillEvents.SkillDisplaced += Observe;
        try
        {
            SkillCheck.Gain(player, gaining, organicAttempt: true);
            Assert.Equal(1, calls);
            Assert.Equal(1000, gaining.BaseFixedPoint);
            Assert.Equal(0, displaced.BaseFixedPoint);

            SkillCheck.Gain(player, gaining, organicAttempt: true);
            Assert.Equal(1, calls);
        }
        finally
        {
            SkillEvents.SkillDisplaced -= Observe;
            player.Delete();
        }
    }

    [Fact]
    public void SubTenOrganicGainReportsItsVariableExactDisplacement()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        player.Skills.Cap = 1000;
        var gaining = player.Skills[SkillName.Anatomy];
        var displaced = player.Skills[SkillName.Tactics];
        gaining.BaseFixedPoint = 50;
        displaced.BaseFixedPoint = 950;
        displaced.SetLockNoRelay(SkillLock.Down);
        var previousGaining = gaining.BaseFixedPoint;
        var previousDisplaced = displaced.BaseFixedPoint;
        var reports = new List<int>();
        void Observe(Mobile mobile, Skill skill, int tenths)
        {
            Assert.Same(player, mobile);
            Assert.Same(displaced, skill);
            reports.Add(tenths);
        }

        SkillEvents.SkillDisplaced += Observe;
        try
        {
            SkillCheck.Gain(player, gaining, organicAttempt: true);

            var reported = Assert.Single(reports);
            Assert.InRange(reported, 1, 4);
            Assert.Equal(previousDisplaced - displaced.BaseFixedPoint, reported);
            Assert.Equal(gaining.BaseFixedPoint - previousGaining, reported);
            Assert.Equal(1000, player.Skills.Total);
        }
        finally
        {
            SkillEvents.SkillDisplaced -= Observe;
            player.Delete();
        }
    }

    [Fact]
    public void DirectScriptGainDoesNotClaimOrganicDisplacement()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        player.Skills.Cap = 1000;
        var gaining = player.Skills[SkillName.Anatomy];
        var down = player.Skills[SkillName.Tactics];
        gaining.BaseFixedPoint = 999;
        down.BaseFixedPoint = 1;
        down.SetLockNoRelay(SkillLock.Down);
        var calls = 0;

        void Observe(Mobile mobile, Skill skill, int tenths) => calls++;

        SkillEvents.SkillDisplaced += Observe;
        try
        {
            SkillCheck.Gain(player, gaining);
            Assert.Equal(0, calls);
        }
        finally
        {
            SkillEvents.SkillDisplaced -= Observe;
            player.Delete();
        }
    }

    [Theory]
    [InlineData(SkillName.Necromancy)]
    [InlineData(SkillName.Focus)]
    [InlineData(SkillName.Chivalry)]
    [InlineData(SkillName.Bushido)]
    [InlineData(SkillName.Ninjitsu)]
    [InlineData(SkillName.Spellweaving)]
    [InlineData(SkillName.Mysticism)]
    [InlineData(SkillName.Imbuing)]
    [InlineData(SkillName.Throwing)]
    public void UorSkillChecksAndGainCannotUseLaterEraSkills(SkillName skillName)
    {
        var previous = Core.Expansion;
        var from = new Mobile();
        var skill = from.Skills[skillName];
        var usedCalls = 0;
        void Observe(Mobile mobile, Skill used, bool success) => usedCalls++;

        try
        {
            Core.Expansion = Expansion.UOR;
            skill.Base = 50.0;
            SkillEvents.SkillUsed += Observe;

            Assert.False(SkillCheck.Mobile_SkillCheckLocation(from, skillName, 0.0, 0.0));
            Assert.False(SkillCheck.Mobile_SkillCheckDirectLocation(from, skillName, 1.0));
            Assert.False(SkillCheck.Mobile_SkillCheckTarget(from, skillName, new object(), 0.0, 0.0));
            Assert.False(SkillCheck.Mobile_SkillCheckDirectTarget(from, skillName, new object(), 1.0));
            Assert.False(SkillCheck.CheckSkill(from, skill, new object(), 1.0));
            SkillCheck.Gain(from, skill);

            Assert.Equal(50.0, skill.Base);
            Assert.Equal(0, usedCalls);
        }
        finally
        {
            SkillEvents.SkillUsed -= Observe;
            Core.Expansion = previous;
            from.Delete();
        }
    }

    [Fact]
    public void TemporarySkillPenaltyDoesNotReportBankableLoss()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        player.Skills.Cap = 1000;
        var gaining = player.Skills[SkillName.Anatomy];
        var penalized = player.Skills[SkillName.Tactics];
        gaining.BaseFixedPoint = 800;
        penalized.BaseFixedPoint = 200;
        penalized.SetLockNoRelay(SkillLock.Down);
        var baseBefore = penalized.BaseFixedPoint;
        var totalBefore = player.Skills.Total;
        var valueBefore = penalized.NonRacialValue;
        var calls = 0;
        var reportedTenths = 0;

        void Observe(Mobile mobile, Skill skill, int tenths)
        {
            Assert.Same(player, mobile);
            Assert.Same(penalized, skill);
            reportedTenths += tenths;
            calls++;
        }

        SkillEvents.SkillDisplaced += Observe;
        var penalty = new DefaultSkillMod(SkillName.Tactics, "TemporarySkillBankTest", true, -10.0);
        try
        {
            player.AddSkillMod(penalty);
            Assert.True(penalized.NonRacialValue < valueBefore);
            Assert.Equal(baseBefore, penalized.BaseFixedPoint);
            Assert.Equal(totalBefore, player.Skills.Total);
            Assert.Equal(0, calls);

            SkillCheck.Gain(player, gaining, organicAttempt: true);
            Assert.Equal(1, calls);
            Assert.Equal(1, reportedTenths);
            Assert.Equal(baseBefore - 1, penalized.BaseFixedPoint);
            Assert.Equal(totalBefore, player.Skills.Total);

            player.RemoveSkillMod(penalty);
            Assert.True(penalized.NonRacialValue > valueBefore - 1.0);
            Assert.Equal(1, calls);
        }
        finally
        {
            player.RemoveSkillMod(penalty);
            SkillEvents.SkillDisplaced -= Observe;
            player.Delete();
        }
    }

    [Fact]
    public void ExpiringSkillBonusDoesNotReportBankableLoss()
    {
        var player = new PlayerMobile(World.NewMobile);
        player.DefaultMobileInit();
        player.Player = true;
        var skill = player.Skills[SkillName.Tactics];
        skill.BaseFixedPoint = 200;
        var baseBefore = skill.BaseFixedPoint;
        var totalBefore = player.Skills.Total;
        var valueBefore = skill.NonRacialValue;
        var calls = 0;

        void Observe(Mobile mobile, Skill displaced, int tenths) => calls++;

        SkillEvents.SkillDisplaced += Observe;
        var bonus = new DefaultSkillMod(SkillName.Tactics, "TemporarySkillBankBonusTest", true, 10.0);
        try
        {
            player.AddSkillMod(bonus);
            Assert.True(skill.NonRacialValue > valueBefore);

            player.RemoveSkillMod(bonus);
            Assert.Equal(valueBefore, skill.NonRacialValue);
            Assert.Equal(baseBefore, skill.BaseFixedPoint);
            Assert.Equal(totalBefore, player.Skills.Total);
            Assert.Equal(0, calls);
        }
        finally
        {
            player.RemoveSkillMod(bonus);
            SkillEvents.SkillDisplaced -= Observe;
            player.Delete();
        }
    }
}
