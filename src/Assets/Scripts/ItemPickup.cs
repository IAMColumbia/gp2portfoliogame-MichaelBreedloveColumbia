using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public List<GameObject> PossibleItems = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        SelectItem();
    }

    void SelectItem()
    {
        Instantiate(PossibleItems[UnityEngine.Random.Range(0, PossibleItems.Count)], transform.position, Quaternion.identity); //TODO: This will need to be refined to prevent duplicate item spawns.
        Destroy(gameObject);
    }
}
