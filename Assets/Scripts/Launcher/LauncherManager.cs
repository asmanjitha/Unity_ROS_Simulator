using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LauncherManager : MonoBehaviour
{
    public Toggle detectHumans;
    public ToggleGroup lightingToggleGroup;
    public Toggle lighting1;
    public Toggle lighting2;
    public TMP_InputField mazeSize;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mazeSizeInputBehaviour();
        detectHumansToggleBehaviour();
        lightingToggleGroupBehaviour();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void StartWarehouse(){
        SceneManager.LoadScene("Warehouse");
    }

    public void detectHumansToggleBehaviour(){
        if(detectHumans.isOn){
            PlayerPrefs.SetInt("DetectHumans", 1);
        }else{
            PlayerPrefs.SetInt("DetectHumans", 0);
        }
    }

    public void lightingToggleGroupBehaviour(){
        if(lighting1.isOn && lighting2.isOn == false){
            PlayerPrefs.SetInt("Lighting", 0);
            Debug.Log("Lighting Updated");
        }else if(lighting2.isOn && lighting1.isOn == false){
            PlayerPrefs.SetInt("Lighting", 1);
            Debug.Log("Lighting Updated");
        }
    }

    public void mazeSizeInputBehaviour(){
        int size = int.Parse(mazeSize.text);
        PlayerPrefs.SetInt("MazeSize", size);
    }

    

    
}
