using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class 显示控制 : MonoBehaviour
{
    public static 显示控制 Instance { get; private set; }
    public TMP_Text 总财富;
    public TMP_Text 对话框;
    public TMP_Text 显示1毛数量;
    public TMP_Text 显示5毛数量;
    public TMP_Text 显示1元数量;
    public TMP_Text 显示5元数量;
    public TMP_Text 显示10元数量;
    public TMP_Text 显示20元数量;
    public TMP_Text 显示50元数量;
    public TMP_Text 显示100元数量;
    public TMP_Text 物品A数量;
    public TMP_Text 物品B数量;
    public GameObject 待交易1毛;
    public GameObject 待交易5毛;
    public GameObject 待交易1元;
    public GameObject 待交易5元;
    public GameObject 待交易10元;
    public GameObject 待交易20元;
    public GameObject 待交易50元;
    public GameObject 待交易100元;
    public GameObject 待交易物品A;
    public GameObject 待交易物品B;
    public List<GameObject> 提供对象 = new List<GameObject>();
    public List<int> 提供对象交易类型 = new List<int>();
    public List<float> 提供对象面值 = new List<float>();
    public List<string> 提供对象名称 = new List<string>();

    public List<GameObject> 获得对象 = new List<GameObject>();
    public List<int> 获得对象交易类型 = new List<int>();
    public List<float> 获得对象面值 = new List<float>();
    public List<string> 获得对象名称 = new List<string>();
    public Transform 每日进度条;
    public List<GameObject> 不同保底节点;

    private void Awake()
    {
        Instance = this;
    }
}
