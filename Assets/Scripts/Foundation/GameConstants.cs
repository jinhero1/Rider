using UnityEngine;

/// <summary>
/// Centralized game constants to eliminate magic numbers
/// Follows Clean Code principles - all magic numbers converted to meaningful names
/// </summary>
public static class GameConstants
{
    // Physics Constants
    public static class Physics
    {
        public const float DEFAULT_MOTOR_TORQUE = 2000f;
        public const float DEFAULT_MAX_SPEED = 100f;
        public const float DEFAULT_AIR_ROTATION_SPEED = 250f;
        public const float DEFAULT_FORWARD_FORCE = 100f;
        public const float DEFAULT_SPEED_MULTIPLIER = 1.5f;
        public const float SPEED_VELOCITY_LIMIT_MULTIPLIER = 2f;
        public const float GROUND_STABILIZATION_ANGLE_THRESHOLD = 5f;
        public const float GROUND_STABILIZATION_ANGLE_MAX = 90f;
        public const float GROUND_STABILIZATION_TORQUE_MULTIPLIER = 2f;
        public const float CENTER_OF_MASS_Y_OFFSET = -0.15f;
    }

    // Flip Detection Constants
    public static class FlipDetection
    {
        public const float DEFAULT_FLIP_THRESHOLD = 320f;
        public const float FLIP_PROGRESS_LOG_THRESHOLD = 90f;
        public const float FULL_ROTATION_DEGREES = 360f;
        public const float INCOMPLETE_FLIP_THRESHOLD = 100f;
    }

    // Animation Constants
    public static class Animation
    {
        public const float DEFAULT_ROTATION_SPEED = 50f;
        public const float DEFAULT_FLOAT_SPEED = 2f;
        public const float DEFAULT_FLOAT_AMOUNT = 0.3f;
        public const float DEFAULT_BOB_SPEED = 2f;
        public const float DEFAULT_BOB_HEIGHT = 0.2f;
        public const float GLOW_SPEED = 2f;
        public const float GLOW_INTENSITY = 0.3f;
    }

    // Boost Platform Constants
    public static class Boost
    {
        public const float DEFAULT_BOOST_FORCE = 500f;
        public const float DEFAULT_SHAKE_INTENSITY = 0.1f;
        public const float DEFAULT_COOLDOWN_TIME = 1f;
        public const float CONTINUOUS_FORCE_MULTIPLIER = 0.5f;
        public const float ARROW_LENGTH_SCALE = 2f;
        public const float ARROW_HEAD_ANGLE = 30f;
        public const float ARROW_HEAD_LENGTH = 0.5f;
        public const float FORCE_VISUALIZATION_DIVISOR = 100f;
    }

    // Camera Constants
    public static class Camera
    {
        public const float DEFAULT_SMOOTH_SPEED = 5f;
        public const float DEFAULT_LOOK_AHEAD_DISTANCE = 2f;
        public const float DEFAULT_SHAKE_DURATION = 0.3f;
        public const float DEFAULT_SHAKE_MAGNITUDE = 0.3f;
        public const float CRASH_SHAKE_INTENSITY = 0.3f;
        public const float SPIKE_SHAKE_INTENSITY = 0.5f;
    }

    // Collectible Constants
    public static class Collectibles
    {
        public const float COLLECTION_TRIGGER_RADIUS = 0.5f;
        public const float EFFECT_DESTROY_DELAY = 2f;
        public const float CAMERA_SHAKE_DURATION = 0.1f;
        public const float CAMERA_SHAKE_INTENSITY = 0.1f;
        public const int BONUS_LETTER_COUNT = 5;
        public const int DEFAULT_TREASURE_CHEST_POINTS = 100;
    }

    // UI Constants
    public static class UI
    {
        public const float FLIP_MESSAGE_DURATION = 1f;
        public const float BONUS_ANIMATION_DELAY = 0.3f;
        public const float BONUS_ANIMATION_DURATION = 0.5f;
        public const float BONUS_FONT_SIZE_MULTIPLIER = 1.5f;
        public const float SCORE_SCALE_PULSE = 0.1f;
        public const float SCORE_PULSE_FREQUENCY = 4f;
        public const float LEVEL_INFO_FADE_IN_DURATION = 0.5f;
        public const float LEVEL_INFO_FADE_OUT_DURATION = 0.5f;
        public const float LEVEL_INFO_AUTO_HIDE_DELAY = 3f;
        public const float TRANSITION_FADE_DURATION = 0.3f;
        public const float CHEST_OPEN_DELAY = 0.5f;
        public const float CHEST_OPEN_DURATION = 0.3f;
        public const float CHEST_SCALE_IN_DURATION = 0.5f;
        public const float CHEST_BOUNCE_MULTIPLIER = 0.2f;
    }

    // Score Constants
    public static class Score
    {
        public const int DEFAULT_FLIP_BONUS_POINTS = 1;
        public const float DEFAULT_DISTANCE_MULTIPLIER = 1f;
        public const string HIGH_SCORE_PLAYER_PREF_KEY = "HighScore";
    }

    // Gizmo Constants
    public static class Gizmos
    {
        public const float COLLECTIBLE_GIZMO_SIZE = 0.5f;
        public const float SPIKE_WARNING_OFFSET = 1.5f;
        public const float SPIKE_WARNING_HEIGHT = 0.5f;
        public const float SPIKE_WARNING_RADIUS = 0.2f;
        public const float SPIKE_DANGER_RADIUS = 2f;
        public const float FINISH_LINE_FLAG_HEIGHT = 2f;
        public const float FINISH_LINE_FLAG_WIDTH = 1f;
        public const float FINISH_LINE_DETECTION_RADIUS = 2f;
    }

    // Scene Management Constants
    public static class SceneManagement
    {
        public const float SCENE_LOAD_WAIT_TIME = 0.1f;
        public const float SCENE_LOAD_WAIT_TIME_LONG = 0.2f;
    }

    // Tag Constants
    public static class Tags
    {
        public const string PLAYER = "Player";
        public const string GROUND = "Ground";
    }

    // Layer Constants
    public static class Layers
    {
        public const string PLAYER = "Player";
    }

    // Color Constants
    public static class Colors
    {
        public static readonly Color BONUS_YELLOW = Color.yellow;
        public static readonly Color COLLECTIBLE_GREEN = Color.green;
        public static readonly Color COLLECTED_GRAY = Color.gray;
        public static readonly Color SPIKE_RED = new Color(1f, 0.2f, 0.2f);
        public static readonly Color UNCOLLECTED_LETTER_GRAY = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public static readonly Color BOOST_PLATFORM_GREEN = Color.green;
    }
}
