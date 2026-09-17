using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 新增现金 : MonoBehaviour
{
    public float 面值;
    public int 数量;
    public bool 确认新增现金;
    public void Update()
    {
        if (确认新增现金)
        {
            现金 现金 = new 现金();
            现金.面值 = 面值;
            现金.数量 = 数量;
            交易控制器.Instance.所有现金.Add(现金);
            确认新增现金 = false;
        }
    }
}
