using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

public class GameEventSystemTests
{
    private GameEventSystem _bus;

    [SetUp]
    public void SetUp()
    {
        _bus = new GameEventSystem();
    }

    [TearDown]
    public void TearDown()
    {
        _bus.ClearAllListeners();
    }

    private struct DummyEvent { public int Value; }

    [Test]
    public void Subscribe_Publish_InvokesListenerOnce()
    {
        int calls = 0;
        _bus.Subscribe<DummyEvent>(e => calls++);

        _bus.Publish(new DummyEvent { Value = 1 });

        Assert.AreEqual(1, calls);
    }

    [Test]
    public void Unsubscribe_RemovesListener()
    {
        int calls = 0;
        Action<DummyEvent> handler = e => calls++;
        _bus.Subscribe(handler);
        _bus.Unsubscribe(handler);

        _bus.Publish(new DummyEvent { Value = 42 });

        Assert.AreEqual(0, calls);
    }

    [Test]
    public void Publish_WithNoListeners_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _bus.Publish(new DummyEvent { Value = 7 }));
    }

    [Test]
    public void ClearListeners_ForType_RemovesOnlyThatType()
    {
        int a = 0;
        int b = 0;
        _bus.Subscribe<DummyEvent>(_ => a++);
        _bus.Subscribe<ScoreChangedEvent>(_ => b++);

        _bus.ClearListeners<DummyEvent>();
        _bus.Publish(new DummyEvent { Value = 1 });
        _bus.Publish(new ScoreChangedEvent { CurrentScore = 1, DeltaScore = 1 });

        Assert.AreEqual(0, a);
        Assert.AreEqual(1, b);
    }

    [Test]
    public void ListenerException_IsCaughtAndLogged_NotPropagated()
    {
        _bus.Subscribe<DummyEvent>(_ => throw new Exception("boom"));
        _bus.Subscribe<DummyEvent>(_ => { /* still called */ });

        // Expect Unity to log an error and don't let it fail the test
        LogAssert.Expect(LogType.Error, new Regex(@"Error invoking event listener.*boom"));

        Assert.DoesNotThrow(() => _bus.Publish(new DummyEvent { Value = 1 }));
        LogAssert.NoUnexpectedReceived();
    }
}
