using AssetManagementBase;
using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Validation;

/// <summary>
/// One-frame game used by <c>--validate-models</c>. It exists only to own a
/// GraphicsDevice so DigitalRiseModel can import the GLBs and build their textures.
/// </summary>
internal sealed class ModelValidationGame : Game
{
	private readonly GraphicsDeviceManager _graphics;
	private readonly string _directory;
	private readonly ValidationReport _report = new();

	private ModelValidationGame(string directory)
	{
		_directory = directory;
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 64,
			PreferredBackBufferHeight = 64
		};

		Window.Title = "SkyrimMelee model validation";
		IsMouseVisible = false;
		IsFixedTimeStep = false;
	}

	public ValidationReport Report => _report;

	public static ValidationReport Run(string directory)
	{
		using var game = new ModelValidationGame(directory);
		game.Run();
		return game._report;
	}

	protected override void LoadContent()
	{
		base.LoadContent();

		try
		{
			var manager = AssetManager.CreateFileAssetManager(_directory);
			LoadAndValidate(manager, ModelSpec.FighterFile, ModelKind.Fighter);
			LoadAndValidate(manager, ModelSpec.SwordFile, ModelKind.Sword);
			manager.Unload();
		}
		catch (Exception exception)
		{
			_report.Error(
				_directory,
				"load",
				$"model validation failed: {exception.GetBaseException().Message}");
		}
		finally
		{
			Exit();
		}
	}

	private void LoadAndValidate(AssetManager manager, string file, ModelKind kind)
	{
		try
		{
			var model = manager.LoadModel(GraphicsDevice, file, ModelLoadFlags.ReadableBuffers);
			try
			{
				_report.Merge(ModelValidator.Validate(ModelFactsReader.FromModel(model, file, kind)));
			}
			finally
			{
				model.Dispose();
			}
		}
		catch (Exception exception)
		{
			_report.Error(file, "load", $"could not import the model: {exception.GetBaseException().Message}");
		}
	}
}
