using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsSceneManager : MonoBehaviour
{
    [Header("BGM / SE Toggles")]
    [SerializeField] private Toggle toggleBgmOn;
    [SerializeField] private Toggle toggleBgmOff;
    [SerializeField] private Toggle toggleSeOn;
    [SerializeField] private Toggle toggleSeOff;

    [Header("Screen Toggle")]
    [SerializeField] private Toggle toggleFullScreen;

    [Header("Other UI")]
    [SerializeField] private Text versionText;         // TextMeshPro を使うなら Text ではなく TMP_Text にする
    [SerializeField] private string titleSceneName = "TitleScene";

    // PlayerPrefs のキー
    private const string KEY_BGM_ON = "BgmOn";
    private const string KEY_SE_ON = "SeOn";
    private const string KEY_FULLSCREEN = "FullScreen";

    private void Start()
    {
        // バージョン表示（右下）
        if (versionText != null)
        {
            versionText.text = "バージョン：" + Application.version;
        }

        // 保存済み設定を読み込んで UI に反映
        LoadSettingsToUI();
    }

    // ====== UI → 実際の設定 への反映 ======

    public void OnChangeBgm(bool _)
    {
        // BGM は「オン側のトグルが ON かどうか」だけ見ればOK
        bool isBgmOn = toggleBgmOn != null ? toggleBgmOn.isOn : true;

        // ここで BGM のミュートなどを切り替える（AudioManager を後で作る想定）
        // 例： if (bgmSource != null) bgmSource.mute = !isBgmOn;

        Debug.Log("BGM ON = " + isBgmOn);
    }

    public void OnChangeSe(bool _)
    {
        bool isSeOn = toggleSeOn != null ? toggleSeOn.isOn : true;

        // 効果音を鳴らすときに isSeOn を見て再生するか決めるようにする
        Debug.Log("SE ON = " + isSeOn);
    }

    public void OnChangeFullScreen(bool isOn)
    {
        ApplyFullScreen(isOn);
    }

    private void ApplyFullScreen(bool isOn)
    {
        Screen.fullScreen = isOn;
    }

    // ====== ボタン用メソッド ======

    // 「ほぞんしてもどる」
    public void OnClickSaveAndBack()
    {
        SaveSettings();
        SceneManager.LoadScene(titleSceneName);
    }

    // 「デフォルトにもどす」
    public void OnClickResetDefault()
    {
        // 仕様上のデフォルトは BGM ON / SE ON / フルスクリーン ON
        bool isBgmOn = true;
        bool isSeOn = true;
        bool isFullScreen = true;

        if (toggleBgmOn != null) toggleBgmOn.isOn = isBgmOn;
        if (toggleBgmOff != null) toggleBgmOff.isOn = !isBgmOn;

        if (toggleSeOn != null) toggleSeOn.isOn = isSeOn;
        if (toggleSeOff != null) toggleSeOff.isOn = !isSeOn;

        if (toggleFullScreen != null) toggleFullScreen.isOn = isFullScreen;

        ApplyFullScreen(isFullScreen);

        // PlayerPrefs もデフォルト値で上書き
        PlayerPrefs.SetInt(KEY_BGM_ON, isBgmOn ? 1 : 0);
        PlayerPrefs.SetInt(KEY_SE_ON, isSeOn ? 1 : 0);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, isFullScreen ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("設定をデフォルト値に戻しました");
    }

    // ====== PlayerPrefs の読み書き ======

    private void LoadSettingsToUI()
    {
        // 保存値がなければ BGM/SE/FullScreen すべて ON をデフォルトにする  :contentReference[oaicite:5]{index=5}
        bool isBgmOn = PlayerPrefs.GetInt(KEY_BGM_ON, 1) == 1;
        bool isSeOn = PlayerPrefs.GetInt(KEY_SE_ON, 1) == 1;
        bool isFullScreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;

        if (toggleBgmOn != null) toggleBgmOn.isOn = isBgmOn;
        if (toggleBgmOff != null) toggleBgmOff.isOn = !isBgmOn;

        if (toggleSeOn != null) toggleSeOn.isOn = isSeOn;
        if (toggleSeOff != null) toggleSeOff.isOn = !isSeOn;

        if (toggleFullScreen != null) toggleFullScreen.isOn = isFullScreen;

        ApplyFullScreen(isFullScreen);
    }

    private void SaveSettings()
    {
        bool isBgmOn = toggleBgmOn != null ? toggleBgmOn.isOn : true;
        bool isSeOn = toggleSeOn != null ? toggleSeOn.isOn : true;
        bool isFullScreen = toggleFullScreen != null ? toggleFullScreen.isOn : true;

        PlayerPrefs.SetInt(KEY_BGM_ON, isBgmOn ? 1 : 0);
        PlayerPrefs.SetInt(KEY_SE_ON, isSeOn ? 1 : 0);
        PlayerPrefs.SetInt(KEY_FULLSCREEN, isFullScreen ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("設定を保存しました");
    }
}
