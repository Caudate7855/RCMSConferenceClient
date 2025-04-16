#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_TVOS
	#define UNITY_PLATFORM_SUPPORTS_YPCBCR
#endif

using UnityEngine;

//-----------------------------------------------------------------------------
// Copyright 2015-2022 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo
{
	/// <summary>
	/// Sets up a material to display the video from a MediaPlayer
	/// </summary>
	[AddComponentMenu("AVPro Video/Apply To Material", 300)]
	[HelpURL("https://www.renderheads.com/products/avpro-video/")]
	public sealed class ApplyToMaterial : ApplyToBase
	{
		[Header("Display")]
		[Space(8f)]

		[Tooltip("Default texture to display when the video texture is preparing")]
		[SerializeField] Texture2D _defaultTexture = null;

		public Texture2D DefaultTexture
		{
			get { return _defaultTexture; }
			set { if (_defaultTexture != value) { _defaultTexture = value; _isDirty = true; } }
		}

		[Space(8f)]
		[Header("Material Target")]

		[SerializeField] Material[] _materials = null;

		public Material[] Materials
		{
			get { return _materials; }
			set { if (_materials != value) { _materials = value; _isDirty = true; } }
		}

		[SerializeField] string _texturePropertyName = Helper.UnityBaseTextureName;

		public string TexturePropertyName
		{
			get { return _texturePropertyName; }
			set
			{
				if (_texturePropertyName != value)
				{
					_texturePropertyName = value;
					// TODO: if the property changes, remove it from the perioud SetTexture()
					_propTexture = new LazyShaderProperty(_texturePropertyName);
					_isDirty = true;
				}
			}
		}

		[SerializeField] Vector2 _offset = Vector2.zero;

		public Vector2 Offset
		{
			get { return _offset; }
			set { if (_offset != value) { _offset = value; _isDirty = true; } }
		}

		[SerializeField] Vector2 _scale = Vector2.one;

		public Vector2 Scale
		{
			get { return _scale; }
			set { if (_scale != value) { _scale = value; _isDirty = true; } }
		}

		private Texture _lastTextureApplied;
		private LazyShaderProperty _propTexture;

		private Texture _originalTexture;
		private Vector2 _originalScale = Vector2.one;
		private Vector2 _originalOffset = Vector2.zero;

		// We do a LateUpdate() to allow for any changes in the texture that may have happened in Update()
		private void LateUpdate()
		{
			Apply();
		}

		public override void Apply()
		{
			bool applied = false;

			if (_media != null && _media.TextureProducer != null)
			{
				Texture resamplerTex = _media.FrameResampler == null || _media.FrameResampler.OutputTexture == null ? null : _media.FrameResampler.OutputTexture[0];
				Texture texture = _media.UseResampler ? resamplerTex : _media.TextureProducer.GetTexture(0);
				if (texture != null)
				{
					// Check for changing texture
					if (texture != _lastTextureApplied)
					{
						_isDirty = true;
					}

					if (_isDirty)
					{
						int planeCount = _media.UseResampler ? 1 : _media.TextureProducer.GetTextureCount();
						for (int plane = 0; plane < planeCount; ++plane)
						{
							Texture resamplerTexPlane = _media.FrameResampler == null || _media.FrameResampler.OutputTexture == null ? null : _media.FrameResampler.OutputTexture[plane];
							texture = _media.UseResampler ? resamplerTexPlane : _media.TextureProducer.GetTexture(plane);
							if (texture != null)
							{
								foreach (var material in _materials)
									ApplyMapping(material, texture, _media.TextureProducer.RequiresVerticalFlip(), plane);
							}
						}
					}
					applied = true;
				}
			}

			// If the media didn't apply a texture, then try to apply the default texture
			if (!applied)
			{
				if (_defaultTexture != _lastTextureApplied)
				{
					_isDirty = true;
				}
				if (_isDirty)
				{
					foreach (var material in _materials)
					{
#if UNITY_PLATFORM_SUPPORTS_YPCBCR
						if (material != null && material.HasProperty(VideoRender.PropUseYpCbCr.Id))
						{
							material.DisableKeyword(VideoRender.Keyword_UseYpCbCr);
						}
#endif
						ApplyMapping(material, _defaultTexture, false);
					}
				}
			}
		}


		private void ApplyMapping(Material material, Texture texture, bool requiresYFlip, int plane = 0)
		{
			if (material != null)
			{
				_isDirty = false;

				if (plane == 0)
				{
					VideoRender.SetupMaterialForMedia(material, _media, _propTexture.Id, texture, texture == _defaultTexture);
					_lastTextureApplied = texture;

					#if (!UNITY_EDITOR && UNITY_ANDROID)
					if (texture == _defaultTexture)	{ material.EnableKeyword("USING_DEFAULT_TEXTURE"); }
					else							{ material.DisableKeyword("USING_DEFAULT_TEXTURE"); }
					#endif

					if (texture != null)
					{
						if (requiresYFlip)
						{
							material.SetTextureScale(_propTexture.Id, new Vector2(_scale.x, -_scale.y));
							material.SetTextureOffset(_propTexture.Id, Vector2.up + _offset);
						}
						else
						{
							material.SetTextureScale(_propTexture.Id, _scale);
							material.SetTextureOffset(_propTexture.Id, _offset);
						}
					}
				}
				else if (plane == 1)
				{
					if (texture != null)
					{
						if (requiresYFlip)
						{
							material.SetTextureScale(VideoRender.PropChromaTex.Id, new Vector2(_scale.x, -_scale.y));
							material.SetTextureOffset(VideoRender.PropChromaTex.Id, Vector2.up + _offset);
						}
						else
						{
							material.SetTextureScale(VideoRender.PropChromaTex.Id, _scale);
							material.SetTextureOffset(VideoRender.PropChromaTex.Id, _offset);
						}
					}
				}
			}
		}

		protected override void SaveProperties()
		{
			foreach (var material in _materials)
			{
				if (material != null)
				{
					if (string.IsNullOrEmpty(_texturePropertyName))
					{
						_originalTexture = material.mainTexture;
						_originalScale = material.mainTextureScale;
						_originalOffset = material.mainTextureOffset;
					}
					else
					{
						_originalTexture = material.GetTexture(_texturePropertyName);
						_originalScale = material.GetTextureScale(_texturePropertyName);
						_originalOffset = material.GetTextureOffset(_texturePropertyName);
					}
				}
			}
			
			_propTexture = new LazyShaderProperty(_texturePropertyName);
		}

		protected override void RestoreProperties()
		{
			foreach (var material in _materials)
			{
				if (material != null)
				{
					if (string.IsNullOrEmpty(_texturePropertyName))
					{
						material.mainTexture = _originalTexture;
						material.mainTextureScale = _originalScale;
						material.mainTextureOffset = _originalOffset;
					}
					else
					{
						material.SetTexture(_texturePropertyName, _originalTexture);
						material.SetTextureScale(_texturePropertyName, _originalScale);
						material.SetTextureOffset(_texturePropertyName, _originalOffset);
					}
				}
			}
		}
	}
}