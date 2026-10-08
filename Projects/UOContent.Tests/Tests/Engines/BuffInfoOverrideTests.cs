using System;
using Server.Engines.BuffIcons;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BuffInfoOverrideTests
{
    [Fact]
    public void WithoutAnOverrideABuffIsAddedAsItIs()
    {
        BuffInfo.Override = null;
        var buff = new BuffInfo(BuffIcon.Bless, 1075847, 1075848, TimeSpan.FromSeconds(30), "11\t11\t11");

        Assert.Same(buff, BuffInfo.Present(null, buff));
    }

    [Fact]
    public void AnOverrideMayReplaceABuff()
    {
        var replacement = new BuffInfo(BuffIcon.Bless, 1042971, 0, TimeSpan.FromSeconds(30), "Bless");

        try
        {
            BuffInfo.Override = (_, _) => replacement;

            Assert.Same(replacement, BuffInfo.Present(null, new BuffInfo(BuffIcon.Bless, 1075847)));
        }
        finally
        {
            BuffInfo.Override = null;
        }
    }

    [Fact]
    public void AnOverrideMayHideABuff()
    {
        try
        {
            BuffInfo.Override = (_, _) => null;

            Assert.Null(BuffInfo.Present(null, new BuffInfo(BuffIcon.Protection, 1075814)));
        }
        finally
        {
            BuffInfo.Override = null;
        }
    }
}
