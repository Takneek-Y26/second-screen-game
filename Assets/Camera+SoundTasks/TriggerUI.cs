using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerUI : MonoBehaviour
{
    public GameObject UIToEnable;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("The Player has entered the trigger zone!");
            UIToEnable.SetActive(true);
            // Do your logic here (e.g., collect an item, open a door, damage enemy)
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("The Player has entered the trigger zone!");
            UIToEnable.SetActive(false);
            // Do your logic here (e.g., collect an item, open a door, damage enemy)
        }
    }

}
