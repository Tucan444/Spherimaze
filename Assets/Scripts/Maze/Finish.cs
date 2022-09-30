using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Finish : MonoBehaviour
{
    SphCircle finish;
    
    public void GoBack() {
        int bi = SceneManager.GetActiveScene().buildIndex;
        if (bi == 0) {
            Application.Quit();
        } else {
            SceneManager.LoadScene(0);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        finish = GetComponent<SphCircle>();
    }

    // Update is called once per frame
    void Update()
    {
        if (finish.triggered) {
            GoBack();
        }
    }
}
