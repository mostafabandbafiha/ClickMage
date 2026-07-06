using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor;

namespace FPL
{
    public class CustomMaterialEditor : ShaderGUI
    {
        protected MaterialEditor materialEditor { get; set; }
        protected virtual uint materialFilter => uint.MaxValue;
        public bool m_FirstTimeApply = true;
        protected enum Expandable
        {
            LightSettings = 1 << 0,
            LightModules = 1 << 1,
            Advanced = 1 << 2
        }
        public enum QueueControl
        {
            Auto = 0,
            UserOverride = 1
        }

        List<MaterialProperty> LightProperties = new List<MaterialProperty> ();
        List<MaterialProperty> ModulesProperties = new List<MaterialProperty> ();
        List<MaterialProperty> ExtraProperties = new List<MaterialProperty> ();

        protected class Styles
        {
            public static readonly string[] queueControlNames = Enum.GetNames (typeof (QueueControl));
            public static readonly GUIContent LightSettings = EditorGUIUtility.TrTextContent ("Light Settings", "Controls the basic light parameters.");
            public static readonly GUIContent LightModules = EditorGUIUtility.TrTextContent ("Modules", "Add modules to your light for more visual flare. (caution: each module added creates more GPU overhead)");
            public static readonly GUIContent ExtraSettings = EditorGUIUtility.TrTextContent ("Advanced", "Additional settings");
        }

        static Dictionary<string, string> tooltips = new Dictionary<string, string> () {

            {"Gradient Texture", "Specify a color gradient texture to customize the color attenuation of your Light. (This is applied to both the Light and Halo)"},
            {"Light Tint", "Controls the color, intensity and opacity of the Light."},
            {"Light Softness", "Controls the attenuation of the Light."},
            {"Light Posterize", "Posterizes the Light for a more stylized look."},
            {"Shading Blend", "Controls how much light bleeds through surfaces. 0 = fully shaded, 1= fully lit. (Requires Unity�s �DepthNormalTexture� for shading)"},
            {"Shading Softness", "Controls how harsh or soft the shading is.(Requires Unity�s �DepthNormalTexture� for shading)"},

            {"Halo Tint", "Controls the color, intensity and opacity of the Halo."},
            {"Halo Size", "Controls the size of the Halo."},
            {"Halo Posterize", "Posterizes the Halo for a more stylized look."},
            {"Halo Depth Fade", "The distance at which the Halo opacity blends when intersecting with other objects."},

            {"Far Fade", "Distance where the light starts fading out."},
            {"Far Transition", "Distance over which the Light will  fade out."},
            {"Close Fade", "Distance where the light starts fading in."},
            {"Close Transition", " Distance over which the Light will fade in."},

            {"Flicker Intensity", "Controls the intensity of the flickering effect."},
            {"Flicker Hue", "The tint toward which the light will shift while flickering."},
            {"Flicker Speed", "The speed of the flickering loop."},
            {"Flicker Softness", "The abruptness of the flickering transition."},
            {"Size Flickering", "The size of the light is influenced by flickering."},

            {"Noise Texture", "Specify a noise texture to customize the shape of the light."},
            {"Texture Packing", "Select which texture channel to sample. (Red, Red*Green, Alpha)"},
            {"Noisiness", "Controls the noise intensity."},
            {"Noise Scale", "Controls the noise scaling (tilling)."},
            {"Noise Movement", "Controls the noise movement speed."},

            {"Dithering Pattern", "A screen space dithering pattern applied over the light."},
            {"Dither Intensity", "Controls dithering intensity."},

            {"Specular Highlight", "Renders a specular highlight over lit surfaces."},
            {"Spec Intensity", "Controls specular intensity."},

            {"Screen Shadows (HEAVY)", "Heavy experimental screen space effect. Use at your own risks. (Not recommended for low end devices)"},
            {"Shadow Threshold", "Controls the shadow collision threshold. (small values might cause artifacts)"},

            {"Particle Mode", "Makes the light compatible with Particle Systems. *Read �Particle Setup� documentation for more info."},

            {"Accurate Colors", "Blends the light with the opaque texture for a more accurate lighting style instead of superimposing the light."},

            {"Day Fading", "Automatically fades out the material opacity depending on the Directional Light direction. (Fades out when pointing down)."},

            {"Blendmode", "'Contrast' mode might result in better result for colorful games. 'Negative' mode allows you to use the light as a shadow volume"},

            {"Depth Write", "Controls whether the shader writes depth. (For debug purposes only)"}
        };

        public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            if (materialEditorIn == null) throw new ArgumentNullException ("materialEditorIn");

            materialEditor = materialEditorIn;
            Material material = materialEditor.target as Material;

            FindProperties (properties);

            materialEditor.SetDefaultGUIWidths ();

#if UNITY_2021_2_OR_NEWER && UNITY_PIPELINE_URP
            if (m_FirstTimeApply)
            {
                OnOpenGUI (material, materialEditorIn);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI (material);
#else
            //Settings Foldout
            if (CustomEditorUtils.SavedFoldout (material.name, "--- Light Settings ---", true)) DrawLightSettings (material);
            EditorGUILayout.Space (10);
            if (CustomEditorUtils.SavedFoldout (material.name, "--- Modules ---", true)) DrawLightModules (material);
            EditorGUILayout.Space (10);
            if (CustomEditorUtils.SavedFoldout (material.name, "--- Advanced ---", false)) DrawExtraSettings (material);
#endif

            if (material.HasProperty ("_Blendmode"))
            {
                var blendMode = material.GetFloat ("_Blendmode");
                if (blendMode == 0) SetBlendMode (material, properties, UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One);
                else if (blendMode == 1) SetBlendMode (material, properties, UnityEngine.Rendering.BlendMode.SrcColor, UnityEngine.Rendering.BlendMode.SrcAlpha);
                else SetBlendMode (material, properties, UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.Zero);
            }
 
        }

        public virtual void FindProperties(MaterialProperty[] properties)
        {
            var material = materialEditor?.target as Material;
            if (material == null) return;

            LightProperties.Clear ();
            ModulesProperties.Clear ();
            ExtraProperties.Clear ();
            int currentFold = 0;
            for (int i = 0; i < properties.Length; i++)
            {
                if (( properties[i].propertyFlags & ( UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector | UnityEngine.Rendering.ShaderPropertyFlags.PerRendererData ) ) == UnityEngine.Rendering.ShaderPropertyFlags.None)
                {
                    if (properties[i].name == "___Halo___") currentFold++;
                    else if (properties[i].name == "_ParticleMode") currentFold++;

                    if (currentFold == 0) LightProperties.Add (properties[i]);
                    else if (currentFold == 1) ModulesProperties.Add (properties[i]);
                    else ExtraProperties.Add (properties[i]);
                }
            }
        }

#if UNITY_2021_2_OR_NEWER && UNITY_PIPELINE_URP

        readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList (uint.MaxValue & ~(uint)Expandable.Advanced);

        public void ShaderPropertiesGUI(Material material)
        {
            m_MaterialScopeList.DrawHeaders (materialEditor, material);
        }

        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            var filter = (Expandable)materialFilter;

            // Generate the foldouts
            if (filter.HasFlag (Expandable.LightSettings))
                m_MaterialScopeList.RegisterHeaderScope (Styles.LightSettings, (uint)Expandable.LightSettings, DrawLightSettings);

            if (filter.HasFlag (Expandable.LightModules))
                m_MaterialScopeList.RegisterHeaderScope (Styles.LightModules, (uint)Expandable.LightModules, DrawLightModules);

            if (filter.HasFlag (Expandable.Advanced))
                m_MaterialScopeList.RegisterHeaderScope (Styles.ExtraSettings, (uint)Expandable.Advanced, DrawExtraSettings);

        }
#else
        public static class CustomEditorUtils
        {
            // Saves the state of the foldout for a unique key
            public static bool SavedFoldout(string key, string section, bool defaultValue)
            {
                string fullKey = $"CustomMaterialEditor.{key}.{section}";
                bool currentState = EditorPrefs.GetBool (fullKey, defaultValue);

                bool newState = EditorGUILayout.Foldout (currentState, section, true);
                if (newState != currentState)
                {
                    EditorPrefs.SetBool (fullKey, newState);
                }

                return newState;
            }
        }

#endif

        void DrawProperty(MaterialProperty p)
        {
            string DisplayName = p.displayName;
            float propertyHeight = materialEditor.GetPropertyHeight (p, DisplayName);
            Rect controlRect = EditorGUILayout.GetControlRect (true, propertyHeight, EditorStyles.layerMaskField, new GUILayoutOption[0]);


            //generate toolTip from dictionary:
            GUIContent tempGui = null;
            if (tooltips.ContainsKey (DisplayName))
            {
                tempGui = EditorGUIUtility.TrTextContent (DisplayName, tooltips[DisplayName]);
            }


            if (tempGui != null) materialEditor.ShaderProperty (controlRect, p, tempGui);
            else materialEditor.ShaderProperty (controlRect, p, DisplayName);
        }

        public virtual void DrawLightSettings(Material material)
        {
            EditorGUILayout.Space (5);

            for (int i = 0; i < LightProperties.Count; i++)
            {
                DrawProperty (LightProperties[i]);
            }
        }

        public virtual void DrawLightModules(Material material)
        {
            EditorGUILayout.Space (5);
            for (int i = 0; i < ModulesProperties.Count; i++)
            {
                DrawProperty (ModulesProperties[i]);

                if (ModulesProperties[i].name == "___Halo___")
                {
                    if (ModulesProperties[i].floatValue == 0) i += 4; //skip 4
                    continue;
                }

                if (ModulesProperties[i].name == "DistanceFade")
                {
                    if (ModulesProperties[i].floatValue == 0) i += 4; //skip Fade,Transition,CloseFade,CloseTransition
                    continue;
                }

                if (ModulesProperties[i].name == "___Flickering___")
                {
                    if (ModulesProperties[i].floatValue == 0) i += 5; //skip Intensity,Hue,Speed,Softness,Flickering
                    continue;
                }

                if (ModulesProperties[i].name == "___Noise___")
                {
                    if (ModulesProperties[i].floatValue == 0) i += 5; //skip Tex, texturePacking, Noisiness,Scale,Movement,
                    continue;
                }

                if (ModulesProperties[i].name == "_SpecularHighlight")
                {
                    if (ModulesProperties[i].floatValue == 0) i++; //skip

                    continue;
                }

                if (ModulesProperties[i].name == "_DitheringPattern")
                {
                    if (ModulesProperties[i].floatValue == 0) i++; //skip
                    continue;
                }

                if (ModulesProperties[i].name == "_ScreenShadows")
                {
                    if (ModulesProperties[i].floatValue == 0) i++; //skip
                    continue;
                }
            }

        }

        public virtual void DrawExtraSettings(Material material)
        {
            EditorGUILayout.Space ();

            for (int i = 0; i < ExtraProperties.Count; i++)
            {
                DrawProperty (ExtraProperties[i]);
            }
            EditorGUILayout.Space (3);

            materialEditor.RenderQueueField ();

            materialEditor.EnableInstancingField ();

            if (GUILayout.Button ("Read Documentation"))
            {
                Application.OpenURL ("https://docs.google.com/document/d/1JKTsJ4WnPqB3EFZAsmLhPJaeeOSedUEBn2VxukZJLF4");
            }
        }

        static void SetBlendMode(Material material, MaterialProperty[] properties, UnityEngine.Rendering.BlendMode srcBlendMode, UnityEngine.Rendering.BlendMode dstBlendMode)
        {
            if (material.HasProperty ("_SrcBlend")) material.SetFloat ("_SrcBlend", (int)srcBlendMode);
            if (material.HasProperty ("_DstBlend")) material.SetFloat ("_DstBlend", (int)dstBlendMode);
        }


    }

    public class SingleLineTexture : MaterialPropertyDrawer
    {
        public override void OnGUI(Rect position, MaterialProperty prop, String label, MaterialEditor editor)
        {
            EditorGUI.BeginChangeCheck ();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            Texture value = editor.TexturePropertyMiniThumbnail (position, prop, label, string.Empty);

            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck ())
            {
                prop.textureValue = value;
            }
        }
    }
}