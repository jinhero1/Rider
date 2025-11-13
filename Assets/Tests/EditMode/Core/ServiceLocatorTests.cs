using NUnit.Framework;

public class ServiceLocatorTests
{
    [SetUp]
    public void SetUp()
    {
        ServiceLocator.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        ServiceLocator.Reset();
    }

    private class Dummy { public int X; }

    [Test]
    public void RegisterAndGet_Works()
    {
        var d = new Dummy { X = 7 };
        ServiceLocator.Instance.Register(d);

        var got = ServiceLocator.Instance.Get<Dummy>();
        Assert.AreSame(d, got);
    }

    [Test]
    public void TryGet_ReturnsFalseWhenMissing()
    {
        var ok = ServiceLocator.Instance.TryGet<Dummy>(out var svc);
        Assert.IsFalse(ok);
        Assert.IsNull(svc);
    }

    [Test]
    public void IsRegistered_ReflectsRegistration()
    {
        Assert.IsFalse(ServiceLocator.Instance.IsRegistered<Dummy>());
        ServiceLocator.Instance.Register(new Dummy());
        Assert.IsTrue(ServiceLocator.Instance.IsRegistered<Dummy>());
    }

    [Test]
    public void Clear_RemovesAll()
    {
        ServiceLocator.Instance.Register(new Dummy());
        ServiceLocator.Instance.Clear();

        Assert.IsFalse(ServiceLocator.Instance.IsRegistered<Dummy>());
    }
}
