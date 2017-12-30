using System;
using UnityEngine;
using System.IO;

namespace UnityStandardAssets.CinematicEffects
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Image Effects/Stylistic Fog")]
#if UNITY_5_4_OR_NEWER
    [ImageEffectAllowedInSceneView]
#endif
	public class StylisticFog : MonoBehaviour
	{

		public delegate string WarningDelegate();

		[AttributeUsage(AttributeTargets.Field)]
		public class SettingsGroup : Attribute
		{ }

		[Serializable]
		public enum ColorSelectionType
		{
			Gradient = 1,
			TextureRamp = 2,
			CopyOther = 3,
		}

		private enum FogTypePass
		{
			DistanceOnly              = 0,
			HeightOnly                = 1,
			BothSharedColorSettings   = 2,
			BothSeperateColorSettinsg = 3,
			None,
		}

		#region settings
		[Serializable]
		public struct FogColorSource
		{
			[AttributeUsage(AttributeTargets.Field)]
			public class DisplayOnSelectionType : Attribute
			{
				public readonly ColorSelectionType selectionType;
				public DisplayOnSelectionType(ColorSelectionType _selectionType)
				{
					selectionType = _selectionType;
				}
			}

			[Tooltip("Color gradient.")]
			[DisplayOnSelectionType(ColorSelectionType.Gradient)]
			public Gradient gradient;

			[Tooltip("Custom fog color ramp.")]
			[DisplayOnSelectionType(ColorSelectionType.TextureRamp)]
			public Texture2D colorRamp;

			public static FogColorSource defaultSettings
			{
				get
				{
					GradientAlphaKey firstAlpha = new GradientAlphaKey(0f, 0f);
					GradientAlphaKey lastAlpha = new GradientAlphaKey(1f, 1f);
					GradientAlphaKey[] initialAlphaKeys = { firstAlpha, lastAlpha };
					FogColorSource source =  new FogColorSource()
					{
						gradient = new Gradient(),
						colorRamp = null,
					};
					source.gradient.alphaKeys = initialAlphaKeys;
					return source;
				}
			}
		}

		[Serializable]
		public struct DistanceFogSettings
		{
			[Tooltip("Wheter or not to apply distance based fog.")]
			public bool enabled;

			[Tooltip("Wheter or not to apply distance based fog to the skybox.")]
			public bool fogSkybox;

			[Tooltip("Fog is fully saturated beyond this distance.")]
			public float endDistance;

			[Tooltip("Color selection for distance fog")]
			public ColorSelectionType colorSelectionType;

			public static DistanceFogSettings defaultSettings
			{
				get
				{
					return new DistanceFogSettings()
					{
						enabled = true,
						fogSkybox = false,
						endDistance = 100f,
						colorSelectionType = ColorSelectionType.Gradient,
					};
				}
			}
		}

		[Serializable]
		public struct HeightFogSettings
		{
			[Tooltip("Wheter or not to apply height based fog.")]
			public bool enabled;

			[Tooltip("Wheter or not to apply height based fog to the skybox.")]
			public bool fogSkybox;

			[Tooltip("Height where the fog starts.")]
			public float baseHeight;

			[Tooltip("Fog density at fog altitude given by height.")]
			public float baseDensity;

			[Tooltip("The rate at which the thickness of the fog decays with altitude.")]
			[Range(0.001f, 1f)]
			public float densityFalloff;

			[Tooltip("Color selection for height fog.")]
			public ColorSelectionType colorSelectionType;

			public static HeightFogSettings defaultSettings
			{
				get
				{
					return new HeightFogSettings()
					{
						enabled = false,
						fogSkybox = true,
						baseHeight = 0f,
						baseDensity = 0.1f,
						densityFalloff = 0.5f,
						colorSelectionType = ColorSelectionType.CopyOther,
					};
				}
			}
		}
		#endregion

		#region settingFields
		[SettingsGroup, SerializeField]
		public DistanceFogSettings distanceFog = DistanceFogSettings.defaultSettings;

		[SettingsGroup, SerializeField]
		public HeightFogSettings heightFog = HeightFogSettings.defaultSettings;

		[SerializeField]
		public FogColorSource distanceColorSource = FogColorSource.defaultSettings;

		[SerializeField]
		public FogColorSource heightColorSource = FogColorSource.defaultSettings;
		#endregion

		#region fields
		private Camera m_Camera;
		public Camera camera_
		{
			get
			{
				if (m_Camera == null)
					m_Camera = GetComponent<Camera>();

				return m_Camera;
			}
		}

		[SerializeField]
		private Texture2D m_DistanceColorTexture;
		public Texture2D distanceColorTexture
		{
			get
			{
				if (m_DistanceColorTexture == null)
				{
					m_DistanceColorTexture = new Texture2D(1024, 1, TextureFormat.ARGB32, false, false)
					{
						name = "Fog property",
						wrapMode = TextureWrapMode.Clamp,
						filterMode = FilterMode.Bilinear,
						anisoLevel = 0,
					};
					BakeFogColor(m_DistanceColorTexture, distanceColorSource.gradient);
				}
				return m_DistanceColorTexture;
			}
		}
		
		[SerializeField]
		private Texture2D m_HeightColorTexture;
		public Texture2D heightColorTexture
		{
			get
			{
				if (m_HeightColorTexture == null)
				{
					m_HeightColorTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false)
					{
						name = "Fog property",
						wrapMode = TextureWrapMode.Clamp,
						filterMode = FilterMode.Bilinear,
						anisoLevel = 0,
					};
					BakeFogColor(m_HeightColorTexture, heightColorSource.gradient);
				}
				return m_HeightColorTexture;
			}
		}

		[SerializeField]
		private Texture2D m_distanceFogIntensityTexture;
		public Texture2D distanceFogIntensityTexture
		{
			get
			{
				if (m_distanceFogIntensityTexture == null)
				{
					m_distanceFogIntensityTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, false)
					{
						name = "Fog Height density",
						wrapMode = TextureWrapMode.Clamp,
						filterMode = FilterMode.Bilinear,
						anisoLevel = 0,
					};
				}
				return m_distanceFogIntensityTexture;
			}
		}

		[SerializeField, HideInInspector]
		private Shader m_Shader;
		public Shader shader
		{
			get
			{
				if (m_Shader == null)
				{
					const string shaderName = "Hidden/Image Effects/StylisticFog";
					m_Shader = Shader.Find(shaderName);
				}

				return m_Shader;
			}
		}

		private Material m_Material;
		public Material material
		{
			get
			{
				if (m_Material == null)
					m_Material = ImageEffectHelper.CheckShaderAndCreateMaterial(shader);

				return m_Material;
			}
		}
		#endregion 

		public void UpdateProperties()
		{
			// Check if both color selction types are to copy
			// If so, change one / show warning?
			bool selectionTypeSame = distanceFog.colorSelectionType == heightFog.colorSelectionType;
			bool distanceSelectedCopy = distanceFog.colorSelectionType == ColorSelectionType.CopyOther;
			if (selectionTypeSame && distanceSelectedCopy)
			{
				distanceFog.colorSelectionType = ColorSelectionType.Gradient;
				distanceSelectedCopy = false;
			}

			UpdateDistanceFogTextures(distanceFog.colorSelectionType);
			UpdateHeightFogTextures(heightFog.colorSelectionType);
		}

		private void UpdateDistanceFogTextures(ColorSelectionType selectionType)
		{
			// If the gradient texture is not used, delete it.
			if (selectionType != ColorSelectionType.Gradient)
			{
				if (m_DistanceColorTexture != null)
					DestroyImmediate(m_DistanceColorTexture);
				m_DistanceColorTexture = null;
			}

			if (selectionType == ColorSelectionType.Gradient)
			{
				BakeFogColor(distanceColorTexture, distanceColorSource.gradient);
			}
		}

		private void UpdateHeightFogTextures(ColorSelectionType selectionType)
		{
			// If the gradient texture is not used, delete it.
			if (selectionType != ColorSelectionType.Gradient)
			{
				if (m_HeightColorTexture != null)
					DestroyImmediate(m_HeightColorTexture);
				m_HeightColorTexture = null;
			}

			if (selectionType == ColorSelectionType.Gradient)
			{
				BakeFogColor(heightColorTexture, heightColorSource.gradient);
			}
		}

		#region Private Members
		private void OnEnable()
		{
			if (!ImageEffectHelper.IsSupported(shader, true, false, this))
				enabled = false;

			camera_.depthTextureMode |= DepthTextureMode.Depth;

			UpdateProperties();
		}

		private void OnDisable()
		{
			if (m_Material != null)
				DestroyImmediate(m_Material);

			if (m_DistanceColorTexture != null)
				DestroyImmediate(m_DistanceColorTexture);

			if (m_HeightColorTexture != null)
				DestroyImmediate(m_HeightColorTexture);

			m_Material = null;
		}


		private void SetDistanceFogUniforms()
		{
			material.SetFloat("_FogEndDistance", distanceFog.endDistance);
		}

		private void SetHeightFogUniforms()
		{
			material.SetFloat("_Height", heightFog.baseHeight);
			material.SetFloat("_BaseDensity", heightFog.baseDensity);
			material.SetFloat("_DensityFalloff", heightFog.densityFalloff);
		}

		private FogTypePass SetMaterialUniforms()
		{

			// Determine the fog type pass
			FogTypePass fogType = FogTypePass.DistanceOnly;

			if(!distanceFog.enabled && heightFog.enabled)
				fogType = FogTypePass.HeightOnly;

			// Share color settings if one of the sources are set to copy the other
			bool sharedColorSettings = (distanceFog.colorSelectionType == ColorSelectionType.CopyOther)
										|| (heightFog.colorSelectionType == ColorSelectionType.CopyOther);

			if(distanceFog.enabled && heightFog.enabled)
			{
				if(sharedColorSettings)
				{
					fogType = FogTypePass.BothSharedColorSettings;
				}
				else
				{
					fogType = FogTypePass.BothSeperateColorSettinsg;
				}
			}

			if (!distanceFog.enabled && !heightFog.enabled)
				return FogTypePass.None;

			// Get the inverse view matrix for converting depth to world position.
			Matrix4x4 inverseViewMatrix = camera_.cameraToWorldMatrix;
			material.SetMatrix("_InverseViewMatrix", inverseViewMatrix);

			// Decide wheter the skybox should have fog applied
			material.SetInt("_ApplyDistToSkybox", distanceFog.fogSkybox ? 1 : 0);
			material.SetInt("_ApplyHeightToSkybox", heightFog.fogSkybox ? 1 : 0);

			// Is the shared color sampled from a texture? Otherwise it's from a single color( picker)
			if (sharedColorSettings)
			{
				bool selectingFromDistance = true;
				FogColorSource activeSelectionSource = distanceColorSource;
				ColorSelectionType activeSelectionType = distanceFog.colorSelectionType;
				if (activeSelectionType == ColorSelectionType.CopyOther)
				{
					activeSelectionType = heightFog.colorSelectionType;
					activeSelectionSource = heightColorSource;
					selectingFromDistance = false;
				}

				SetDistanceFogUniforms();
				SetHeightFogUniforms();

				if(activeSelectionType == ColorSelectionType.Gradient)
					material.SetTexture("_FogColorTexture0", selectingFromDistance ? distanceColorTexture : heightColorTexture);
				else
					material.SetTexture("_FogColorTexture0", selectingFromDistance ? distanceColorSource.colorRamp : heightColorSource.colorRamp);
			}
			else
			{
				if (distanceFog.enabled)
					material.SetTexture("_FogColorTexture0", distanceFog.colorSelectionType == ColorSelectionType.Gradient ? distanceColorTexture : distanceColorSource.colorRamp);

				if (heightFog.enabled)
				{
					string colorTextureIdentifier = fogType == FogTypePass.HeightOnly ? "_FogColorTexture0" : "_FogColorTexture1";
					material.SetTexture(colorTextureIdentifier, heightFog.colorSelectionType == ColorSelectionType.Gradient ? heightColorTexture : heightColorSource.colorRamp);
				}
			}

			// Set distance fog properties
			if (distanceFog.enabled)
			{
				SetDistanceFogUniforms();
			}

			// Set height fog properties
			if (heightFog.enabled)
			{
				SetHeightFogUniforms();
			}

			return fogType;
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			FogTypePass fogType = SetMaterialUniforms();
			if (fogType == FogTypePass.None)
				Graphics.Blit(source, destination);
			else
				Graphics.Blit(source, destination, material, (int)fogType);
		}

		public void BakeFogColor(Texture2D target, Gradient gradient)
		{
			if (target == null)
			{
				return;
			}

			float fWidth = target.width;
			Color[] pixels = new Color[target.width];

			for (float i = 0f; i <= 1f; i += 1f / fWidth)
			{
				Color color = gradient.Evaluate(i);
				pixels[(int)Mathf.Floor(i * (fWidth - 1f))] = color;
			}

			target.SetPixels(pixels);
			target.Apply();
		}
		#endregion

	}
}
