using GISTech.GISTerrainLoader;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Progress : MonoBehaviour
{
    public Scrollbar GenerationProgress;
    public Text Phasename;
    public Text progressValue;
 
    // Start is called before the first frame update
    void Start()
    {
        RuntimeTerrainGenerator.OnProgress += OnGeneratingTerrainProg;

    }
    private void OnDisable()
    {
        RuntimeTerrainGenerator.OnProgress -= OnGeneratingTerrainProg;

    }
    // Update is called once per frame
    void Update()
    {

    }
    private void OnGeneratingTerrainProg(string phase, float progress)
    {
        if (!phase.Equals("Finalization"))
        {
            GenerationProgress.transform.parent.gameObject.SetActive(true);

            Phasename.text = phase.ToString();

            GenerationProgress.value = progress / 100;

            progressValue.text = (progress).ToString() + "%";
        }
        else
        {
            GenerationProgress.transform.parent.gameObject.SetActive(false);
        }
    }
}
