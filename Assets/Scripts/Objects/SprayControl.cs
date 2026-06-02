using System;
using System.Collections.Generic;
using UnityEngine;

public class SprayControl : UiObjectBase
{
    [Header("Spray Object")]
    [SerializeField] private GameObject sprayObject;

    
    protected override void EventSubscriber()
    {
       Manager.Data .OnDataChanged += Test; 
    }
    public void Test(Dictionary<string, Datas> datas)
    {
        if (datas.ContainsKey(ObjectID))
        {
            sprayObject.SetActive(Convert.ToBoolean(datas[ObjectID].Value));
        }
            
    }


}