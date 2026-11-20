using System.Buffers.Binary;
using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Validation;

/// <summary>
/// Flattens a loaded <see cref="DrModel"/> into <see cref="ModelFacts"/>.
/// The validator never parses the GLB itself; DigitalRiseModel owns the import.
/// </summary>
public static class ModelFactsReader
{
	public static ModelFacts FromModel(DrModel model, string file, ModelKind kind)
	{
		var instance = new DrModelInstance(model);
		instance.ResetTransforms();

		var bones = model.Bones
			.Select(bone => new BoneFact(
				bone.Name,
				bone.Parent?.Name,
				instance.GetBoneGlobalTransform(bone.Index).Translation))
			.ToArray();

		var skins = new List<SkinFact>();
		foreach (var part in model.Meshes.SelectMany(mesh => mesh.MeshParts))
		{
			if (part.Skin is null || skins.Any(skin => skin.Index == part.Skin.SkinIndex))
			{
				continue;
			}

			var joints = part.Skin.Joints
				.Select(joint => new BoneFact(
					joint.Bone.Name,
					joint.Bone.Parent?.Name,
					instance.GetBoneGlobalTransform(joint.Bone.Index).Translation))
				.ToArray();

			skins.Add(new SkinFact(part.Skin.SkinIndex, part.Skin.Name, joints));
		}

		var materialIndices = new Dictionary<DrMaterial, int>(ReferenceEqualityComparer.Instance);
		var materials = new List<MaterialFact>();
		var parts = new List<PartFact>();

		foreach (var part in model.Meshes.SelectMany(mesh => mesh.MeshParts))
		{
			var materialIndex = -1;
			if (part.Material is not null)
			{
				if (!materialIndices.TryGetValue(part.Material, out materialIndex))
				{
					materialIndex = materials.Count;
					materialIndices.Add(part.Material, materialIndex);
					var texture = part.Material.DiffuseTexture;
					materials.Add(new MaterialFact(
						materialIndex,
						part.Material.Name,
						texture?.Width ?? 0,
						texture?.Height ?? 0));
				}
			}

			var skinning = ReadSkinning(part);
			parts.Add(new PartFact(
				materialIndex,
				part.PrimitiveType,
				part.PrimitiveType == PrimitiveType.TriangleList ? part.PrimitiveCount : 0,
				part.Skin is not null,
				part.Skin?.SkinIndex ?? -1,
				part.Skin?.Joints.Length ?? 0,
				skinning.InfluenceCount,
				skinning.MinWeightSum,
				skinning.MaxWeightSum,
				skinning.MaxJointIndex,
				part.HasNormals,
				skinning.Error));
		}

		var clips = model.Animations is null
			? Array.Empty<ClipFact>()
			: model.Animations.Values
				.Select(clip => new ClipFact(
					clip.Name,
					(float)clip.Duration.TotalSeconds,
					clip.Channels.Select(channel => new ChannelFact(
						channel.BoneIndex >= 0 && channel.BoneIndex < model.Bones.Length
							? model.Bones[channel.BoneIndex].Name
							: $"bone_{channel.BoneIndex}",
						channel.Keyframes.Select(keyframe => new KeyframeFact(
							(float)keyframe.Time.TotalSeconds,
							keyframe.Pose.Translation,
							keyframe.Pose.Rotation,
							keyframe.Pose.Scale)).ToArray())).ToArray()))
				.ToArray();

		return new ModelFacts(file, kind, bones, skins, parts, clips, instance.BoundingBox, materials);
	}

	private static SkinningFacts ReadSkinning(DrMeshPart part)
	{
		var declaration = part.VertexBuffer.VertexDeclaration;
		var elements = declaration.GetVertexElements();
		var indexElements = elements
			.Where(element => element.VertexElementUsage == VertexElementUsage.BlendIndices)
			.ToArray();
		var weightElements = elements
			.Where(element => element.VertexElementUsage == VertexElementUsage.BlendWeight)
			.ToArray();

		var indexComponents = indexElements.Sum(element => ComponentCount(element.VertexElementFormat));
		var weightComponents = weightElements.Sum(element => ComponentCount(element.VertexElementFormat));
		var influenceCount = Math.Max(indexComponents, weightComponents);

		if (indexElements.Length == 0 || weightElements.Length == 0 || part.VertexBuffer.VertexCount == 0)
		{
			return new SkinningFacts(
				influenceCount,
				float.NaN,
				float.NaN,
				-1,
				indexElements.Length == 0 || weightElements.Length == 0
					? "the vertex declaration has no BlendIndices/BlendWeight elements"
					: "the mesh part has no vertices");
		}

		byte[] data;
		try
		{
			data = new byte[declaration.VertexStride * part.VertexBuffer.VertexCount];
			part.VertexBuffer.GetData(data);
		}
		catch (Exception exception) when (
			exception is InvalidOperationException or NotSupportedException or ArgumentException)
		{
			return new SkinningFacts(
				influenceCount,
				float.NaN,
				float.NaN,
				-1,
				$"the vertex buffer is not readable: {exception.Message}");
		}

		var minSum = float.MaxValue;
		var maxSum = float.MinValue;
		var maxJoint = -1;

		for (var vertex = 0; vertex < part.VertexBuffer.VertexCount; vertex++)
		{
			var vertexStart = vertex * declaration.VertexStride;
			var sum = 0f;

			foreach (var element in weightElements)
			{
				var components = ComponentCount(element.VertexElementFormat);
				for (var component = 0; component < components; component++)
				{
					if (!TryReadWeight(data, vertexStart, element, component, out var weight, out var error))
					{
						return new SkinningFacts(influenceCount, float.NaN, float.NaN, -1, error);
					}

					sum += weight;
				}
			}

			minSum = MathF.Min(minSum, sum);
			maxSum = MathF.Max(maxSum, sum);

			foreach (var element in indexElements)
			{
				var components = ComponentCount(element.VertexElementFormat);
				for (var component = 0; component < components; component++)
				{
					if (!TryReadIndex(data, vertexStart, element, component, out var joint, out var error))
					{
						return new SkinningFacts(influenceCount, float.NaN, float.NaN, -1, error);
					}

					maxJoint = Math.Max(maxJoint, joint);
				}
			}
		}

		return new SkinningFacts(influenceCount, minSum, maxSum, maxJoint, null);
	}

	private static bool TryReadWeight(
		byte[] data,
		int vertexStart,
		VertexElement element,
		int component,
		out float value,
		out string error)
	{
		value = 0f;
		error = string.Empty;
		var size = ComponentSize(element.VertexElementFormat);
		var offset = vertexStart + element.Offset + component * size;
		if (size == 0)
		{
			error = $"unsupported blend weight format {element.VertexElementFormat}";
			return false;
		}

		if (offset + size > data.Length)
		{
			error = $"blend weight {element.VertexElementFormat} at {offset} exceeds {data.Length} bytes";
			return false;
		}

		value = element.VertexElementFormat switch
		{
			VertexElementFormat.Single => BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset)),
			VertexElementFormat.Vector4 => BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset)),
			VertexElementFormat.Vector3 => BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset)),
			VertexElementFormat.Vector2 => BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset)),
			VertexElementFormat.Byte4 or VertexElementFormat.Color => data[offset] / 255f,
			VertexElementFormat.Short4 or VertexElementFormat.NormalizedShort4 =>
				Math.Max(BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset)) / 32767f, -1f),
			VertexElementFormat.Short2 or VertexElementFormat.NormalizedShort2 =>
				Math.Max(BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset)) / 32767f, -1f),
			VertexElementFormat.HalfVector2 or VertexElementFormat.HalfVector4 =>
				(float)BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset))),
			_ => float.NaN
		};

		if (float.IsNaN(value))
		{
			error = $"unsupported blend weight format {element.VertexElementFormat}";
			return false;
		}

		return true;
	}

	private static bool TryReadIndex(
		byte[] data,
		int vertexStart,
		VertexElement element,
		int component,
		out int value,
		out string error)
	{
		value = 0;
		error = string.Empty;
		var size = ComponentSize(element.VertexElementFormat);
		var offset = vertexStart + element.Offset + component * size;
		if (size == 0)
		{
			error = $"unsupported blend index format {element.VertexElementFormat}";
			return false;
		}

		if (offset + size > data.Length)
		{
			error = $"blend index {element.VertexElementFormat} at {offset} exceeds {data.Length} bytes";
			return false;
		}

		switch (element.VertexElementFormat)
		{
			case VertexElementFormat.Byte4:
			case VertexElementFormat.Color:
				value = data[offset];
				return true;
			case VertexElementFormat.Short4:
			case VertexElementFormat.NormalizedShort4:
			case VertexElementFormat.Short2:
			case VertexElementFormat.NormalizedShort2:
				value = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset));
				return true;
			case VertexElementFormat.Single:
				value = (int)BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset));
				return true;
			case VertexElementFormat.Vector4:
			case VertexElementFormat.Vector3:
			case VertexElementFormat.Vector2:
				value = (int)BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(offset));
				return true;
			default:
				error = $"unsupported blend index format {element.VertexElementFormat}";
				return false;
		}
	}

	private static int ComponentCount(VertexElementFormat format) => format switch
	{
		VertexElementFormat.Single => 1,
		VertexElementFormat.Vector2 or VertexElementFormat.HalfVector2 or VertexElementFormat.NormalizedShort2 => 2,
		VertexElementFormat.Vector3 => 3,
		VertexElementFormat.Vector4 or VertexElementFormat.Byte4 or VertexElementFormat.Color or
			VertexElementFormat.Short4 or VertexElementFormat.NormalizedShort4 or VertexElementFormat.HalfVector4 => 4,
		_ => 0
	};

	private static int ComponentSize(VertexElementFormat format) => format switch
	{
		VertexElementFormat.Single or VertexElementFormat.Vector2 or
			VertexElementFormat.Vector3 or VertexElementFormat.Vector4 => 4,
		VertexElementFormat.Byte4 or VertexElementFormat.Color => 1,
		VertexElementFormat.Short2 or VertexElementFormat.NormalizedShort2 or
			VertexElementFormat.Short4 or VertexElementFormat.NormalizedShort4 => 2,
		VertexElementFormat.HalfVector2 or VertexElementFormat.HalfVector4 => 2,
		_ => 0
	};

	private readonly record struct SkinningFacts(
		int InfluenceCount,
		float MinWeightSum,
		float MaxWeightSum,
		int MaxJointIndex,
		string? Error);
}
