using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace SkyrimMelee.Ui
{
	partial class HudPanel
	{
		private const float GhostHoldSeconds = 0.35f;
		private const float GhostDrainPerSecond = 90f;
		private const float HealthDrainPerSecond = 420f;
		private const float StaminaDrainPerSecond = 900f;

		private static readonly IBrush StaminaFiller = new SolidBrush(new Color(0x6F, 0xA7, 0x6F));
		private static readonly IBrush StaminaHot = new SolidBrush(new Color(0xF0, 0xB0, 0x50));

		private float _playerHealthValue;
		private float _playerGhostValue;
		private float _playerGhostHold;
		private bool _playerGhostDecaying;
		private bool _playerInitialized;
		private float _playerStaminaValue;

		private float _opponentHealthValue;
		private float _opponentGhostValue;
		private float _opponentGhostHold;
		private bool _opponentGhostDecaying;
		private bool _opponentInitialized;
		private float _opponentStaminaValue;

		private bool _staminaHot;
		private int _shownRound = int.MinValue;
		private int _shownPlayerScore;
		private int _shownOpponentScore;
		private bool _shownPractice;

		private float _bannerTimer;
		private float _bannerDuration;
		private Color _bannerColor = UiSkin.Parchment;

		public HudPanel()
		{
			BuildUI();

			UiSkin.StyleLabel(_playerName, UiSkin.SmallFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_opponentName, UiSkin.SmallFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_playerPip0, UiSkin.BodyFont, UiSkin.Accent);
			UiSkin.StyleLabel(_playerPip1, UiSkin.BodyFont, UiSkin.Accent);
			UiSkin.StyleLabel(_opponentPip0, UiSkin.BodyFont, UiSkin.Danger);
			UiSkin.StyleLabel(_opponentPip1, UiSkin.BodyFont, UiSkin.Danger);
			UiSkin.StyleLabel(_roundLabel, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_banner, UiSkin.HeadingFont, UiSkin.Parchment);
		}

		/// <summary>Shows the render/playback and controls panels only while F1 is open.</summary>
		public void SetDebugVisible(bool visible)
		{
			if (_renderStats.Visible == visible)
			{
				return;
			}

			_renderStats.Visible = visible;
			_controls.Visible = visible;
		}

		public void UpdateStats(float fps, int drawCalls, int primitives)
		{
			if (!_renderStats.Visible)
			{
				return;
			}

			_fps.Text = FormattableString.Invariant($"FPS  {fps:0.0}");
			_drawCalls.Text = $"Draw calls  {drawCalls}";
			_primitives.Text = $"Primitives  {primitives}";
		}

		public void UpdateDuel(int round, int playerScore, int opponentScore,
			bool practice = false)
		{
			if (round == _shownRound && playerScore == _shownPlayerScore &&
				opponentScore == _shownOpponentScore && practice == _shownPractice)
			{
				return;
			}

			_shownRound = round;
			_shownPlayerScore = playerScore;
			_shownOpponentScore = opponentScore;
			_shownPractice = practice;
			_roundLabel.Text = practice
				? "PRACTICE"
				: FormattableString.Invariant($"ROUND {round}");
			_playerPip0.Text = playerScore >= 1 ? "●" : "○";
			_playerPip1.Text = playerScore >= 2 ? "●" : "○";
			_opponentPip0.Text = opponentScore >= 1 ? "●" : "○";
			_opponentPip1.Text = opponentScore >= 2 ? "●" : "○";
		}

		/// <summary>Per-frame fight HUD: animated health, delayed damage ghost, stamina, banner.</summary>
		public void UpdateFight(
			float playerHealth, float playerMaxHealth,
			float playerStamina, float playerStaminaMax,
			float opponentHealth, float opponentMaxHealth,
			float opponentStamina, float opponentStaminaMax,
			float staminaCue, float deltaSeconds)
		{
			UpdateHealth(
				ref _playerHealthValue, ref _playerGhostValue, ref _playerGhostHold,
				ref _playerGhostDecaying, ref _playerInitialized,
				playerHealth, playerMaxHealth, _playerHealth, _playerHealthGhost,
				deltaSeconds);
			UpdateHealth(
				ref _opponentHealthValue, ref _opponentGhostValue, ref _opponentGhostHold,
				ref _opponentGhostDecaying, ref _opponentInitialized,
				opponentHealth, opponentMaxHealth, _opponentHealth, _opponentHealthGhost,
				deltaSeconds);

			_playerStaminaValue = MoveTowards(
				_playerStaminaValue, playerStamina, StaminaDrainPerSecond * deltaSeconds);
			_playerStamina.Maximum = playerStaminaMax;
			_playerStamina.Value = Math.Clamp(_playerStaminaValue, 0f, playerStaminaMax);

			_opponentStaminaValue = MoveTowards(
				_opponentStaminaValue, opponentStamina, StaminaDrainPerSecond * deltaSeconds);
			_opponentStamina.Maximum = opponentStaminaMax;
			_opponentStamina.Value = Math.Clamp(
				_opponentStaminaValue, 0f, opponentStaminaMax);

			var hot = HudMath.StaminaFlashActive(staminaCue);
			if (hot != _staminaHot)
			{
				_staminaHot = hot;
				_playerStamina.Filler = hot ? StaminaHot : StaminaFiller;
			}

			UpdateBanner(deltaSeconds);
		}

		public void ShowBanner(string text, float seconds, Color color)
		{
			_banner.Text = text;
			_bannerDuration = MathF.Max(0.01f, seconds);
			_bannerTimer = _bannerDuration;
			_bannerColor = color;
			_banner.Visible = true;
		}

		public void UpdatePause(bool paused)
		{
			if (!_renderStats.Visible)
			{
				return;
			}

			_pause.Text = paused ? "PAUSE  ON" : "PAUSE  off";
			_pause.TextColor = paused
				? new Color(0xF0, 0xB0, 0x50)
				: new Color(0xE6, 0xDC, 0xC8);
		}

		public void UpdatePlayback(string clipName, float speed, float timeSeconds, float durationSeconds)
		{
			if (!_renderStats.Visible)
			{
				return;
			}

			_clipName.Text = $"Clip  {clipName}";
			_clipSpeed.Text = FormattableString.Invariant($"Speed  {speed:0.00}x");

			var currentMs = (int)MathF.Round(timeSeconds * 1000f);
			var totalMs = (int)MathF.Round(durationSeconds * 1000f);
			_clipProgress.Text = FormattableString.Invariant($"Progress  {currentMs}ms/{totalMs}ms");
		}

		private void UpdateHealth(
			ref float health, ref float ghost, ref float hold,
			ref bool decaying, ref bool initialized,
			float current, float max,
			HorizontalProgressBar bar, HorizontalProgressBar ghostBar,
			float deltaSeconds)
		{
			if (!initialized)
			{
				health = current;
				ghost = current;
				hold = GhostHoldSeconds;
				initialized = true;
			}

			health = MoveTowards(health, current, HealthDrainPerSecond * deltaSeconds);

			if (current < ghost)
			{
				if (!decaying)
				{
					hold -= deltaSeconds;
					if (hold <= 0f)
					{
						decaying = true;
					}
				}

				if (decaying)
				{
					ghost = MathF.Max(current, ghost - GhostDrainPerSecond * deltaSeconds);
				}
			}
			else
			{
				ghost = current;
				hold = GhostHoldSeconds;
				decaying = false;
			}

			bar.Maximum = max;
			bar.Value = Math.Clamp(health, 0f, max);
			ghostBar.Maximum = max;
			ghostBar.Value = Math.Clamp(ghost, 0f, max);
		}

		private static float MoveTowards(float current, float target, float maxDelta) =>
			current < target
				? MathF.Min(current + maxDelta, target)
				: MathF.Max(current - maxDelta, target);

		private void UpdateBanner(float deltaSeconds)
		{
			if (_bannerTimer <= 0f)
			{
				return;
			}

			_bannerTimer = MathF.Max(0f, _bannerTimer - deltaSeconds);
			if (_bannerTimer <= 0f)
			{
				_banner.Visible = false;
				return;
			}

			var elapsed = _bannerDuration - _bannerTimer;
			var alpha = MathF.Min(1f, MathF.Min(elapsed / 0.15f, _bannerTimer / 0.25f));
			_banner.TextColor = _bannerColor * alpha;
		}
	}
}
