using UnityEngine;
using UnityEditor;

namespace UnityVolumeRendering
{
    [CustomEditor(typeof(VolumeRenderedObject))]
    public class VolumeRenderedObjectCustomInspector : Editor
    {
        private bool tfSettings = true;
        private bool lightSettings = true;
        private bool otherSettings = true;

        private float sigmaRange = 50;
        private float sigmaSpace = 1;

        private float sigmaRangeGrad = 5;
        private float sigmaSpaceGrad = 0.5f;


        public override void OnInspectorGUI()
        {
            VolumeRenderedObject volrendObj = (VolumeRenderedObject)target;

            // Render mode
            RenderMode oldRenderMode = volrendObj.GetRenderMode();
            RenderMode newRenderMode = (RenderMode)EditorGUILayout.EnumPopup("Render mode", oldRenderMode);
            if (newRenderMode != oldRenderMode)
                volrendObj.SetRenderMode(newRenderMode);

            // Visibility window
            Vector2 visibilityWindow = volrendObj.GetVisibilityWindow();
            EditorGUILayout.MinMaxSlider("Visible value range", ref visibilityWindow.x, ref visibilityWindow.y, 0.0f, 1.0f);
            volrendObj.SetVisibilityWindow(visibilityWindow);

            // Transfer function settings
            EditorGUILayout.Space();
            tfSettings = EditorGUILayout.Foldout(tfSettings, "Transfer function");
            if (tfSettings)
            {
                // Transfer function type
                TFRenderMode tfMode = (TFRenderMode)EditorGUILayout.EnumPopup("Transfer function type", volrendObj.GetTransferFunctionMode());
                if (tfMode != volrendObj.GetTransferFunctionMode())
                    volrendObj.SetTransferFunctionMode(tfMode);

                // Show TF button
                if (GUILayout.Button("Edit transfer function"))
                {
                    if (tfMode == TFRenderMode.TF1D)
                        TransferFunctionEditorWindow.ShowWindow(volrendObj);
                    else
                        TransferFunction2DEditorWindow.ShowWindow();
                }
            }

            // Lighting settings
            GUILayout.Space(10);
            lightSettings = EditorGUILayout.Foldout(lightSettings, "Lighting");
            if (lightSettings)
            {
                if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
                    volrendObj.SetLightingEnabled(GUILayout.Toggle(volrendObj.GetLightingEnabled(), "Enable lighting"));
                else
                    volrendObj.SetLightingEnabled(false);

                if (volrendObj.GetLightingEnabled() || volrendObj.GetRenderMode() == RenderMode.IsosurfaceRendering)
                {
                    LightSource oldLightSource = volrendObj.GetLightSource();
                    LightSource newLightSource = (LightSource)EditorGUILayout.EnumPopup("Light source", oldLightSource);
                    if (newLightSource != oldLightSource)
                        volrendObj.SetLightSource(newLightSource);
                }
                if (volrendObj.GetLightingEnabled())
                {
                    volrendObj.SetDenoiseGradientEnabled(GUILayout.Toggle(volrendObj.GetDenoiseGradientEnabled(), "Enable volume gradient denoise"));
                    if (volrendObj.GetDenoiseGradientEnabled())
                    {
                        sigmaRangeGrad = EditorGUILayout.Slider("Sigma range", sigmaRangeGrad, 0.01f, 500);
                        sigmaSpaceGrad = EditorGUILayout.Slider("Sigma space", sigmaSpaceGrad, 0.01f, 10);
                        if (GUILayout.Button("Update Denoise Gradient Param"))
                        {
                            volrendObj.UpdateDenoiseGradValue(sigmaSpaceGrad, sigmaRangeGrad);
                        }
                    }
                }
            }

            // Other settings
            GUILayout.Space(10);
            otherSettings = EditorGUILayout.Foldout(otherSettings, "Other Settings");
            if (otherSettings)
            {
                if (volrendObj.GetRenderMode() == RenderMode.DirectVolumeRendering)
                {
                    // Back-to-front rendering option
                    volrendObj.SetDVRBackwardEnabled(GUILayout.Toggle(volrendObj.GetDVRBackwardEnabled(), "Enable Back-to-Front Direct Volume Rendering"));

                    // Early ray termination for Front-to-back DVR
                    if (!volrendObj.GetDVRBackwardEnabled())
                    {
                        volrendObj.SetRayTerminationEnabled(GUILayout.Toggle(volrendObj.GetRayTerminationEnabled(), "Enable early ray termination"));
                    }

                }
                volrendObj.SetCubicInterpolationEnabled(GUILayout.Toggle(volrendObj.GetCubicInterpolationEnabled(), "Enable cubic interpolation (better quality)"));

                volrendObj.SetDenoiseEnabled(GUILayout.Toggle(volrendObj.GetDenoiseEnabled(), "Enable volume denoise"));
                if (volrendObj.GetDenoiseEnabled())
                {
                    sigmaRange = EditorGUILayout.Slider("Sigma range", sigmaRange, 0.01f, 500);
                    sigmaSpace = EditorGUILayout.Slider("Sigma space", sigmaSpace, 0.01f, 10);
                    if (GUILayout.Button("Update Denoise Param"))
                    {
                        volrendObj.UpdateDenoiseValue(sigmaSpace, sigmaRange);
                    }
                }

            }
        }
    }
}
