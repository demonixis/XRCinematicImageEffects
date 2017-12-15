using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CinematicEffects;

public sealed class EffectTester : MonoBehaviour
{
    private int m_Method = 0;

    [SerializeField]
    private AntiAliasing m_AntiAliasing = null;
    [SerializeField]
    private Text m_DebugText = null;

    private void Start()
    {
        UpdateText("AA Disabled");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            m_AntiAliasing.enabled = false;
            UpdateText("AA Disabled");
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            m_AntiAliasing.enabled = true;
            m_AntiAliasing.method = (int)AntiAliasing.Method.Fxaa;
            m_Method = 1;
            SetFXAAPreset(FXAA.Preset.defaultPreset, "Default");
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            m_AntiAliasing.enabled = true;
            m_AntiAliasing.method = (int)AntiAliasing.Method.Smaa;
            m_Method = 2;
            SetSMAAPreset(SMAA.QualitySettings.presetQualitySettings[1], "Medium");
        }

        if (m_Method == 1)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SetFXAAPreset(FXAA.Preset.extremePerformancePreset, "Extreme Performance");
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                SetFXAAPreset(FXAA.Preset.performancePreset, "Performance");
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                SetFXAAPreset(FXAA.Preset.defaultPreset, "Default");
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                SetFXAAPreset(FXAA.Preset.qualityPreset, "Quality");
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                SetFXAAPreset(FXAA.Preset.extremeQualityPreset, "Extreme Quality");
        }
        else if (m_Method == 2)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SetSMAAPreset(SMAA.QualitySettings.presetQualitySettings[0], "Low");
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                SetSMAAPreset(SMAA.QualitySettings.presetQualitySettings[1], "Medium");
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                SetSMAAPreset(SMAA.QualitySettings.presetQualitySettings[2], "High");
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                SetSMAAPreset(SMAA.QualitySettings.presetQualitySettings[3], "Ultra");
        }

    }

    private void SetFXAAPreset(FXAA.Preset preset, string profileName)
    {
        var fxaa = (FXAA)m_AntiAliasing.current;
        fxaa.preset = preset;
        UpdateText("FXAA: " + profileName);
    }

    private void SetSMAAPreset(SMAA.QualitySettings preset, string profileName)
    {
        var smaa = (SMAA)m_AntiAliasing.current;
        smaa.quality = preset;
        UpdateText("SMAA: " + profileName);
    }

    private void UpdateText(string text)
    {
        m_DebugText.text = text;
    }
}
