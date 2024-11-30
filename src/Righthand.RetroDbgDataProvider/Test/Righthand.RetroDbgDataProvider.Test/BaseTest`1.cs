using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;
using System.Diagnostics;

namespace Righthand.RetroDbgDataProvider.Test;

public abstract class BaseTest<T>
    where T : class
{
    protected Fixture Fixture = default!;
    private T? _target;

    protected T Target
    {
        [DebuggerStepThrough]
        get
        {
            if (_target is null)
            {
                _target = Fixture.Build<T>().OmitAutoProperties().Create();
            }
            return _target;
        }
    }

    [SetUp]
    public void SetUp()
    {
        Fixture = new Fixture();
        Fixture.Customize(new AutoNSubstituteCustomization());
    }
    [TearDown]
    public void TearDown()
    {
        _target = null!;
    }
    protected static string LoadKickAssSampleFile(string directory, string name)
    {
        return File.ReadAllText(Path.Combine("Samples", "KickAssembler", directory, "build", name));
    }
    protected static string LoadKickAssSample(string name)
    {
        return File.ReadAllText(Path.Combine("Samples", "KickAssembler", name));
    }
}