using UnityEngine;
using UnityEngine.SceneManagement;

public class NisaIntroPanelController : MonoBehaviour
{
    [Header("Explain Panels")]
    public GameObject panelExplain1;
    public GameObject panelExplain2;
    public GameObject panelExplain3;

    [Header("Scene Name")]
    public string titleSceneName = "TitleScene";

    private int currentIndex = 0;
    private GameObject[] panels;

    void Start()
    {
        panels = new GameObject[]
        {
            panelExplain1,
            panelExplain2,
            panelExplain3
        };

        ShowPanel(0);
    }

    // ▶ つぎへ
    public void OnClickNext()
    {
        if (currentIndex >= panels.Length - 1) return;

        currentIndex++;
        ShowPanel(currentIndex);
    }

    // ◀ もどる（説明パネル）
    public void OnClickBackPanel()
    {
        if (currentIndex <= 0) return;

        currentIndex--;
        ShowPanel(currentIndex);
    }

    // ⬅ タイトルへ戻る
    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    private void ShowPanel(int index)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == index);
        }
    }
}
