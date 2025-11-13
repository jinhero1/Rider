using NUnit.Framework;

public class GameStateServiceTests
{
    private GameEventSystem _bus;
    private GameStateService _state;
    private GameStateChangedEvent? _lastEvent;

    [SetUp]
    public void SetUp()
    {
        _bus = new GameEventSystem();
        _state = new GameStateService(_bus);
        _lastEvent = null;
        _bus.Subscribe<GameStateChangedEvent>(e => _lastEvent = e);
    }

    [Test]
    public void DefaultState_IsPlaying()
    {
        Assert.AreEqual(GameState.Playing, _state.CurrentState);
        Assert.IsTrue(_state.IsState(GameState.Playing));
    }

    [Test]
    public void ChangeState_PublishesEvent_WhenStateChanges()
    {
        _state.ChangeState(GameState.Crashed);

        Assert.AreEqual(GameState.Crashed, _state.CurrentState);
        Assert.IsTrue(_lastEvent.HasValue);
        Assert.AreEqual(GameState.Playing, _lastEvent.Value.PreviousState);
        Assert.AreEqual(GameState.Crashed, _lastEvent.Value.NewState);
    }

    [Test]
    public void ChangeState_NoEvent_WhenSettingSameState()
    {
        _state.ChangeState(GameState.Playing);
        Assert.IsFalse(_lastEvent.HasValue);
    }
}
