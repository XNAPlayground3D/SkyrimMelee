using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using SkyrimMelee.Ai;

namespace SkyrimMelee.Ui
{
	public partial class MainMenu
	{
		private readonly List<Widget> _menuOrder = new();
		private readonly List<Widget> _optionOrder = new();
		private readonly List<Widget> _rulesOrder = new();
		private readonly Dictionary<Widget, Action> _actions = new();
		private readonly GameSettings _settings;
		private Desktop? _desktop;

		public event Action? DuelSelected;

		public event Action? PracticeSelected;

		public event Action? QuitSelected;

		public MainMenu(GameSettings settings)
		{
			_settings = settings;
			BuildUI();
			Style();
			Wire();
			Show();
		}

		public AiProfile Profile => _settings.Difficulty;

		public bool OptionsOpen => _optionsContent.Visible;

		public bool RulesOpen => _rulesContent.Visible;

		public void Attach(Desktop desktop)
		{
			_desktop = desktop;
			// The menu owns Enter/Space so a press cannot activate a button twice.
			_desktop.KeyDownHandler = null;
			_desktop.FocusedKeyboardWidget = _duel;
		}

		public void Show()
		{
			_menuContent.Visible = true;
			_optionsContent.Visible = false;
			_rulesContent.Visible = false;
			UpdateProfileLabels();
			UpdateSettingLabels();
		}

		public void MoveFocus(int delta)
		{
			var order = OptionsOpen ? _optionOrder : _menuOrder;
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
			if (RulesOpen)
			{
				CloseRules();
			}
			else if (OptionsOpen)
			{
				CloseOptions();
			}
		}

		private void Style()
		{
			UiSkin.StyleLabel(_title, UiSkin.TitleFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_tagline, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_difficultyHint, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_hint, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_difficulty, UiSkin.BodyFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_optionsTitle, UiSkin.HeadingFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_optionsHint, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_rulesTitle, UiSkin.HeadingFont, UiSkin.Parchment);
			UiSkin.StyleLabel(_rulesHint, UiSkin.SmallFont, UiSkin.Muted);
			UiSkin.StyleLabel(_rulesBody, UiSkin.BodyFont, UiSkin.Parchment);

			foreach (var button in new[]
					 {
						 _duel, _practice, _options, _rules, _quit,
						 _optionsDifficulty, _screenShake, _debugOverlay, _back,
						 _rulesBack
					 })
			{
				UiSkin.StyleButton(button);
			}

			UiSkin.StyleButton(_previousDifficulty);
			UiSkin.StyleButton(_nextDifficulty);
			_previousDifficulty.MinWidth = 54;
			_nextDifficulty.MinWidth = 54;

			_menuOrder.AddRange(new Widget[]
			{
				_duel, _practice, _options, _rules, _quit,
				_previousDifficulty, _nextDifficulty
			});
			_optionOrder.AddRange(new Widget[]
			{
				_optionsDifficulty, _screenShake, _debugOverlay, _back
			});
			_rulesOrder.Add(_rulesBack);

			_actions[_duel] = () => DuelSelected?.Invoke();
			_actions[_practice] = () => PracticeSelected?.Invoke();
			_actions[_options] = OpenOptions;
			_actions[_rules] = OpenRules;
			_actions[_quit] = () => QuitSelected?.Invoke();
			_actions[_previousDifficulty] = () => CycleProfile(-1);
			_actions[_nextDifficulty] = () => CycleProfile(1);
			_actions[_optionsDifficulty] = () => CycleProfile(1);
			_actions[_screenShake] = ToggleShake;
			_actions[_debugOverlay] = ToggleDebug;
			_actions[_back] = CloseOptions;
			_actions[_rulesBack] = CloseRules;
		}

		private void Wire()
		{
			_duel.Click += (_, _) => DuelSelected?.Invoke();
			_practice.Click += (_, _) => PracticeSelected?.Invoke();
			_options.Click += (_, _) => OpenOptions();
			_rules.Click += (_, _) => OpenRules();
			_quit.Click += (_, _) => QuitSelected?.Invoke();
			_previousDifficulty.Click += (_, _) => CycleProfile(-1);
			_nextDifficulty.Click += (_, _) => CycleProfile(1);
			_optionsDifficulty.Click += (_, _) => CycleProfile(1);
			_screenShake.Click += (_, _) => ToggleShake();
			_debugOverlay.Click += (_, _) => ToggleDebug();
			_back.Click += (_, _) => CloseOptions();
			_rulesBack.Click += (_, _) => CloseRules();
		}

		private void OpenOptions()
		{
			_menuContent.Visible = false;
			_rulesContent.Visible = false;
			_optionsContent.Visible = true;
			UpdateProfileLabels();
			_optionOrder[0].SetKeyboardFocus();
		}

		private void OpenRules()
		{
			_menuContent.Visible = false;
			_optionsContent.Visible = false;
			_rulesContent.Visible = true;
			_rulesBack.SetKeyboardFocus();
		}

		private void CloseRules()
		{
			_rulesContent.Visible = false;
			_menuContent.Visible = true;
			_rules.SetKeyboardFocus();
		}

		private void CloseOptions()
		{
			_optionsContent.Visible = false;
			_menuContent.Visible = true;
			_options.SetKeyboardFocus();
		}

		public void CycleProfile(int delta)
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
			UpdateProfileLabels();
		}

		private void UpdateProfileLabels()
		{
			_difficulty.Text = Profile.Name;
			_optionsDifficultyText.Text = $"Difficulty: {Profile.Name}";
		}

		private void ToggleShake()
		{
			_settings.ScreenShake = !_settings.ScreenShake;
			UpdateSettingLabels();
		}

		private void ToggleDebug()
		{
			_settings.DebugOverlay = !_settings.DebugOverlay;
			UpdateSettingLabels();
		}

		private void UpdateSettingLabels()
		{
			_screenShakeText.Text = $"Screen shake: {(_settings.ScreenShake ? "On" : "Off")}";
			_debugOverlayText.Text = $"Debug overlay: {(_settings.DebugOverlay ? "On" : "Off")}";
		}
	}
}
