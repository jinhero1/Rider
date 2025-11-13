using NUnit.Framework;
using UnityEngine;

public class ScoreServiceTests
{
    private GameEventSystem _bus;
    private ScoreService _score;
    private ScoreChangedEvent? _lastScoreEvent;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteAll();
        _bus = new GameEventSystem();
        _score = new ScoreService(_bus);
        _lastScoreEvent = null;
        _bus.Subscribe<ScoreChangedEvent>(e => _lastScoreEvent = e);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
    }

    [Test]
    public void AddScore_IncreasesCurrentScore_AndPublishesEvent()
    {
        _score.AddScore(5);

        Assert.AreEqual(5, _score.CurrentScore);
        Assert.IsTrue(_lastScoreEvent.HasValue);
        Assert.AreEqual(5, _lastScoreEvent.Value.CurrentScore);
        Assert.AreEqual(5, _lastScoreEvent.Value.DeltaScore);
    }

    [Test]
    public void ResetScore_SetsToZero_AndPublishesNegativeDelta()
    {
        _score.AddScore(10);
        _lastScoreEvent = null;

        _score.ResetScore();

        Assert.AreEqual(0, _score.CurrentScore);
        Assert.IsTrue(_lastScoreEvent.HasValue);
        Assert.AreEqual(0, _lastScoreEvent.Value.CurrentScore);
        Assert.AreEqual(-10, _lastScoreEvent.Value.DeltaScore);
    }

    [Test]
    public void SaveHighScore_UpdatesOnlyWhenHigher()
    {
        _score.AddScore(10);
        _score.SaveHighScore();
        Assert.AreEqual(10, _score.HighScore);

        _score.ResetScore();
        _score.AddScore(5);
        _score.SaveHighScore();
        Assert.AreEqual(10, _score.HighScore);

        _score.ResetScore();
        _score.AddScore(15);
        _score.SaveHighScore();
        Assert.AreEqual(15, _score.HighScore);
    }

    [Test]
    public void LoadHighScore_ReadsFromPlayerPrefsUsingConstantKey()
    {
        PlayerPrefs.SetInt(GameConstants.Score.HIGH_SCORE_PLAYER_PREF_KEY, 123);
        PlayerPrefs.Save();

        var s = new ScoreService(_bus);
        Assert.AreEqual(123, s.HighScore);
    }
}
