using System;
using Server;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Mobiles;

[Collection("Sequential UOContent Tests")]
public class PlayerSkillBankPersistenceTests
{
    [Fact]
    public void BankPayloadRoundTripsWithActiveSkills()
    {
        var original = new PlayerMobile();
        original.DefaultMobileInit();
        original.Skills[SkillName.Anatomy].BaseFixedPoint = 732;
        original.SkillBankData = "{\"version\":1,\"balances\":[{\"skillId\":1,\"tenths\":10}]}";
        original.SkillBankRecoveryArchive = "malformed original payload";
        var writer = new BufferWriter(true);

        try
        {
            original.Serialize(writer);
            var bytes = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            var copy = new PlayerMobile(World.NewMobile);
            try
            {
                var reader = new BufferReader(bytes);
                copy.Deserialize(reader);

                Assert.Equal(bytes.Length, reader.Position);
                Assert.Equal(732, copy.Skills[SkillName.Anatomy].BaseFixedPoint);
                Assert.Equal(original.SkillBankData, copy.SkillBankData);
                Assert.Equal(original.SkillBankRecoveryArchive, copy.SkillBankRecoveryArchive);
            }
            finally
            {
                copy.Delete();
            }
        }
        finally
        {
            original.Delete();
        }
    }
}
