using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityVolumeRendering
{
    [CustomEditor(typeof(VolumeRenderedObject))]
    public class VolumeRenderedObjectCustomInspector : Editor, IProgressView
    {
        private bool tfSettings = true;
        private bool lightSettings = true;
        private bool otherSettings = true;
        private float currentProgress = 1.0f;
        private string currentProgressDescrition = "";
        private bool progressDirty = false;

        public void StartProgress(string title, string description)
        {
        }

        public void FinishProgress(ProgressStatus status = ProgressStatus.Succeeded)
        {
            currentProgress = 1.0f;
        }

        public void UpdateProgress(float totalProgress, float currentStageProgress, string description)
        {
            currentProgressDescrition = description;
            currentProgress = totalProgress;
            progressDirty = true;
        }
        public override bool RequiresConstantRepaint()
        {
            return progressDirty;
        }

        private float sigmaRange = 50;
        private float sigmaSpace = 1;

        private float sigmaRangeGrad = 5;
        private float sigmaSpaceGrad = 0.5f;


        public override void OnInspectorGUI()
        {
            VolumeRenderedObject volrendObj = (VolumeRenderedObject)target;

            if (currentProgress < 1.0f)
            {
                Rect rect = EditorGUILayout.BeginVertical();
                EditorGUI.ProgressBar(rect, currentProgress, currentProgressDescrition);
                GUILayout.Space(18);
                EditorGUILayout.EndVertical();
            }
            progressDirty = false;

            // Render mode
            RenderMode oldRenderMode = volrendObj.GetRenderMode();
            RenderMode newRenderMode = (RenderMode)EditorGUILayout.EnumPopup("Render mode", oldRenderMode);
            if (newRenderMode != oldRenderMode)
            {
                Task task = volrendObj.SetRenderModeAsync(newRenderMode, new ProgressHandler(this));
            }

            // Visibility window
            Vector2 visibilityWindow = volrendObj.GetVisibilityWindow();
            EditorGUILayout.MinMaxSlider("Visible value range", ref visibilityWindow.x, ref visibilityWindow.y, 0.0f, 1.0f);
            volrendObj.SetVisibilityWindow(visibilityWindow);

            if (newRenderMode == RenderMode.IsosurfaceRendering)
            {
                float oldThreshold = volrendObj.GetGradientVisibilityThreshold();
                float oldThresholdSqrt = Mathf.Sqrt(oldThreshold); // Convert to square root scaling (=> more precision close to 0)
                float newThreshold = EditorGUILayout.Slider(
                    new GUIContent("Gradient visibility threshold", "Minimum gradient maginitude value that will be visible"),
                    oldThresholdSqrt, 0.0f, 1.0f
                );
                newThreshold = newThreshold * newThreshold; // Convert back to linear scaling
                if (newThreshold != oldThreshold)
                    volrendObj.SetGradientVisibilityThreshold(newThreshold);
            }

            // Transfer function settings
            EditorGUILayout.Space();
            tfSettings = EditorGUILayout.Foldout(tfSettings, "Transfer function");
            if (tfSettings)
            {
                // Transfer function type
                TFRenderMode tfMode = (TFRenderMode)EditorGUILayout.EnumPopup("Transfer function type", volrendObj.GetTransferFunctionMode());
                if (tfMode != volrendObj.GetTransferFunctionMode())
                {
                    Task task = volrendObj.SetTransferFunctionModeAsync(tfMode, new ProgressHandler(this));
                }

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
                {
                    Task task = volrendObj.SetLightingEnabledAsync(GUILayout.Toggle(volrendObj.GetLightingEnabled(), "Enable lighting"), new ProgressHandler(this));
                }
                else
                    volrendObj.SetLightingEnabled(false);

                if (volrendObj.GetLightingEnabled() || volrendObj.GetRenderMode() == RenderMode.IsosurfaceRendering)
                {
                    LightSource oldLightSource = volrendObj.GetLightSource();
                    LightSource newLightSource = (LightSource)EditorGUILayout.EnumPopup("Light source", oldLightSource);
                    if (newLightSource != oldLightSource)
                        volrendObj.SetLightSource(newLightSource);

                    // Gradient lighting threshold: Threshold for how low gradients can contribute to lighting.
                    Vector2 gradLightThreshold = volrendObj.GetGradientLightingThreshold();
                    // Convert to square root scaling (=> more precision close to 0)
                    gradLightThreshold = new Vector2(Mathf.Sqrt(gradLightThreshold.x), Mathf.Sqrt(gradLightThreshold.y));
                    EditorGUILayout.MinMaxSlider(
                        new GUIContent("Gradient lighting threshold",
                            "Minimum and maximum threshold for gradient contribution to lighting.\n"
                            + "Voxels with gradient less than min will be unlit, and with gradient >= max will fully shaded."),
                        ref gradLightThreshold.x, ref gradLightThreshold.y, 0.0f, 1.0f
                    );
                    // Convert back to linear scale, before setting updated value.
                    volrendObj.SetGradientLightingThreshold(new Vector2(gradLightThreshold.x * gradLightThreshold.x, gradLightThreshold.y * gradLightThreshold.y));
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
                // Early ray termination
                volrendObj.SetRayTerminationEnabled(GUILayout.Toggle(volrendObj.GetRayTerminationEnabled(), "Enable early ray termination"));

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
