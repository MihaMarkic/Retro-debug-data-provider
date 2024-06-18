using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;
using System.Diagnostics;

namespace Righthand.RetroDbgDataProvider.Test;

public abstract class BaseTest<T>
    where T : class
{
    protected Fixture fixture = default!;
    T target = default!;
    public T Target
    {
        [DebuggerStepThrough]
        get
        {
            if (target is null)
            {
                target = fixture.Build<T>().OmitAutoProperties().Create();
            }
            return target;
        }
    }

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        fixture.Customize(new AutoNSubstituteCustomization());
    }
    [TearDown]
    public void TearDown()
    {
        target = null!;
    }
    protected static string LoadKickAssSample(string name)
    {
        return File.ReadAllText(Path.Combine("Samples", "KickAssembler", name));
    }
}