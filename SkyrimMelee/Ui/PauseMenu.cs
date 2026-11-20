using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using SkyrimMelee.Ai;

namespace SkyrimMelee.Ui
{
	public partial class PauseMenu
	{
		private readonly List<Widget> _pauseOrder = new();
		private readonly List<Widget> _optionOrder = new();
		private readonly List<Widget> _resultOrder = new();
		private readonly Dictionary<Widget, Action> _actions = new();
		private readonly GameSettings _settings;
		private Desktop? _desktop;

		public event Action? ResumeRequested;

		public event Action? QuitToMenuRequested;

		public event Action? RematchRequested;

		public event Action? MenuRequested;

		public PauseMenu(GameSettings settings)
		{
			_settings = settings;
			BuildUI();
			Style();
			Wire();
			UpdateLabels();
		}

		public bool IsOpen => Visible;

		public bool OptionsOpen => _optionsContent.Visible;

		public bool ResultsOpen => _resultsContent.Visible;

		public void Attach(Desktop desktop)
		{
			_desktop = desktop;
			_desktop.KeyDownHandler = null;
			_desktop.FocusedKeyboardWidget = _resume;
		}

		public void OpenPause()
		{
			Visible = true;
			_pauseContent.Visible = true;
			_optionsContent.Visible = false;
			_resultsContent.Visible = false;
			UpdateLabels();
			_resume.SetKeyboardFocus();
		}

		public void OpenOptions()
		{
			_pauseContent.Visible = false;
			_optionsContent.Visible = true;
			_resultsContent.Visible = false;
			UpdateLabels();
			_volumeDown.SetKeyboardFocus();
		}

		public void ShowResults(
			bool victory, string score, string rounds, string hits, string defense)
		{
			Visible = true;
			_pauseContent.Visible = false;
			_optionsContent.Visible = false;
			_resultsContent.Visible = true;
			_resultTitle.Text = victory ? "VICTORY" : "DEFEAT";
			_resultTitle.TextColor = victory ? UiSkin.Accent : UiSkin.Danger;
			_resultScore.Text = score;
			_resultRounds.Text = rounds;
			_resultHits.Text = hits;
			_resultDefense.Text = defense;
			_rematch.SetKeyboardFocus();
		}

		public void Hide()
		{
			Visible = false;
			_pauseContent.Visible = false;
			_optionsContent.Visible = false;
			_resultsContent.Visible = false;
		}

		public void MoveFocus(int delta)
		{
			var order = ActiveOrder();
			if (order.Count == 0)
			{
				return;
			}

			var current = _desktop?.FocusedKeyboardWidget;
			var index = current is null ? -1 : order.IndexOf(current);
			index = index < 0
				? 0
				: (index + delta + order.Count) % order.Count;
			order[index].SetKeyboardFocus();
		}

		public void ActivateFocused()
		{
			if (_desktop?.FocusedKeyboardWidget is { } focused &&
				_actions.TryGetValue(focused, out var action))
			{
				action();
			}
		}

		public void Back()
		{
			if (OptionsOpen)
			{
				OpenPause();
			}
			else if (ResultsOpen)
			{
				MenuRequested?.Invoke();
			}
			else
			{
				ResumeRequested?.Invoke();
			}
		}

		private List<Widget> ActiveOrder() =>
			ResultsOpen ? _resultOrder : OptionsOpen ? _optionOrder : _pauseOrder;

		private void Style()
		{
			UiSkin.StyleLabel(_pauseTitle, UiSkin.TitleFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_optionsTitle, UiSkin.TitleFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_resultTitle, UiSkin.TitleFont, UiSkin.Accent);
			UiSkin.StyleLabel(_pauseHint, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_optionsHint, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_resultsHint, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_resultScore, UiSkin.HeadingFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_resultRounds, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_resultHits, UiSkin.BodyFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_resultDefense, UiSkin.BodyFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_volumeText, UiSkin.BodyFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_difficultyText, UiSkin.BodyFont, UiSkin.Parchment);

			foreach (var button in new[]
					 {
						 _resume, _pauseOptions, _pauseQuit,
						 _volumeDown, _volumeUp, _shake, _debug,
						 _difficultyDown, _difficultyUp, _back,
						 _rematch, _resultsMenu
					 })
			{
				UiSkin.StyleButton(button);
			}

			foreach (var button in new[]
					 {
						 _volumeDown, _volumeUp, _difficultyDown, _difficultyUp
					 })
			{
				button.MinWidth = 54;
			}

			_pauseOrder.AddRange(new Widget[]
			{
				_resume, _pauseOptions, _pauseQuit
			});
			_optionOrder.AddRange(new Widget[]
			{
				_volumeDown, _volumeUp, _shake, _debug,
				_difficultyDown, _difficultyUp, _back
			});
			_resultOrder.AddRange(new Widget[]
			{
				_rematch, _resultsMenu
			});

			_actions[_resume] = () => ResumeRequested?.Invoke();
			_actions[_pauseOptions] = OpenOptions;
			_actions[_pauseQuit] = () => QuitToMenuRequested?.Invoke();
			_actions[_volumeDown] = () => ChangeVolume(-0.1f);
			_actions[_volumeUp] = () => ChangeVolume(0.1f);
			_actions[_shake] = ToggleShake;
			_actions[_debug] = ToggleDebug;
			_actions[_difficultyDown] = () => CycleDifficulty(-1);
			_actions[_difficultyUp] = () => CycleDifficulty(1);
			_actions[_back] = OpenPause;
			_actions[_rematch] = () => RematchRequested?.Invoke();
			_actions[_resultsMenu] = () => MenuRequested?.Invoke();
		}

		private void Wire()
		{
			_resume.Click += (_, _) => ResumeRequested?.Invoke();
			_pauseOptions.Click += (_, _) => OpenOptions();
			_pauseQuit.Click += (_, _) => QuitToMenuRequested?.Invoke();
			_volumeDown.Click += (_, _) => ChangeVolume(-0.1f);
			_volumeUp.Click += (_, _) => ChangeVolume(0.1f);
			_shake.Click += (_, _) => ToggleShake();
			_debug.Click += (_, _) => ToggleDebug();
			_difficultyDown.Click += (_, _) => CycleDifficulty(-1);
			_difficultyUp.Click += (_, _) => CycleDifficulty(1);
			_back.Click += (_, _) => OpenPause();
			_rematch.Click += (_, _) => RematchRequested?.Invoke();
			_resultsMenu.Click += (_, _) => MenuRequested?.Invoke();
		}

		private void ChangeVolume(float delta)
		{
			_settings.MasterVolume = Math.Clamp(_settings.MasterVolume + delta, 0f, 1f);
			UpdateLabels();
		}

		private void ToggleShake()
		{
			_settings.ScreenShake = !_settings.ScreenShake;
			UpdateLabels();
		}

		private void ToggleDebug()
		{
			_settings.DebugOverlay = !_settings.DebugOverlay;
			UpdateLabels();
		}

		private void CycleDifficulty(int delta)
		{
			var index = 0;
			for (var i = 0; i < AiProfile.All.Count; i++)
			{
				if (ReferenceEquals(AiProfile.All[i], _settings.Difficulty))
				{
					index = i;
					break;
				}
			}

			index = (index + delta + AiProfile.All.Count) % AiProfile.All.Count;
			_settings.Difficulty = AiProfile.All[index];
			UpdateLabels();
		}

		private void UpdateLabels()
		{
			_volumeText.Text = FormattableString.Invariant(
				$"Volume {_settings.MasterVolume * 100:0}%");
			_shakeText.Text = $"Screen shake: {(_settings.ScreenShake ? "On" : "Off")}";
			_debugText.Text = $"Debug overlay: {(_settings.DebugOverlay ? "On" : "Off")}";
			_difficultyText.Text = $"Difficulty: {_settings.Difficulty.Name}";
		}
	}
}
