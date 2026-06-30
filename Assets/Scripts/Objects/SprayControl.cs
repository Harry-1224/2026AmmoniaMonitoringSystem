using System;
using System.Collections.Generic;
using UnityEngine;

public class SprayControl : UiObjectBase
{
    [Header("Spray Object")]
    [SerializeField] public GameObject sprayObject;

    
    protected override void EventSubscriber()
    {
        base.EventSubscriber();
        Manager.Data.OnDataChanged += DataChange; 
    }
    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();
        Manager.Data.OnDataChanged -= DataChange;
    }
    public void DataChange(Dictionary<string, Datas> datas)
    {
        if (datas.ContainsKey(ObjectID))
        {
            sprayObject.SetActive(Convert.ToBoolean(datas[ObjectID].Value));
        }
    }


}