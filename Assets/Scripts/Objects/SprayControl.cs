using System;
using System.Collections.Generic;
using UnityEngine;

public class SprayControl : UiObjectBase
{
    [Header("Spray Children")]
    [SerializeField] private List<GameObject> sprayObjects = new();

    protected override void EventSubscriber()
    {
       Manager.Data .OnDataChanged += Test; 
    }
    public void Test(Dictionary<string, Datas> datas)
    {
        if (datas.ContainsKey(ObjectID))
        {
            sprayObjects[0].SetActive(Convert.ToBoolean(datas[ObjectID].Value));
        }
            
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sprayObjects == null)
        {
            sprayObjects = new List<GameObject>();
        }

        if (sprayObjects.Count == 0 && transform.childCount >= 2)
        {
            sprayObjects.Add(transform.GetChild(0).gameObject);
            sprayObjects.Add(transform.GetChild(1).gameObject);
        }
    }
#endif
}