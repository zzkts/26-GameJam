using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class 事件控制器 : MonoBehaviour
{
    public static 事件控制器 Instance { get; private set; }
    public List<GameObject> 事件列表;
    public int 当前天数;
    public List<新增现金> 刷新初始资金;
    public int 保底客户数量=5;//第一周5，后面日常3，宣传日1
    public float 单日总时长;//初始60s，每天加5s
    public float 当前分段时长;
    public float 当日当前时长;
    public float 保底顾客间隔时间;
    public int 当前保底客户序数;
    public bool 确认是否营业;
    public bool 确认保底顾客间隔时间;
    public bool 开启宣传日;
    public bool 完成交易; 
    private void Awake()
    {
        Instance = this;
    }
    public void 重制游戏()
    {
        //清除所有财富
        交易控制器.Instance.钱 = 12;
        //获得初始财富
        for (int i = 0; i < 刷新初始资金.Count; i++)
        {
            刷新初始资金[i].确认新增现金 = true;
        }
        //更新基础参数
        当前天数 = 1;
        保底客户数量 = 5;
        单日总时长 = 60;
        当前保底客户序数 = 0;
        当前分段时长 = 0;
        当日当前时长 = 0;
    }
    public void 打开事件(int 打开序列)
    {
        for (int i = 0;i < 事件列表.Count; i++)
        {
            事件列表[i].SetActive(false);
        }
        事件列表[打开序列].SetActive(true);
    }
    public void Update()
    {
        if (确认是否营业)
        {
            if (确认保底顾客间隔时间)
            {
                保底顾客间隔时间 = 单日总时长 / 保底客户数量;
                确认保底顾客间隔时间 = false;
            }
            开门营业();
        }
    }
    public void 确认营业()
    {
        确认是否营业 = true;
    }
    public void 开门营业()
    {
        if (交易控制器.Instance.商品已拥有列表 == null || 交易控制器.Instance.商品已拥有列表.Count == 0|| !交易控制器.Instance.商品已拥有列表.Any(商品 => 商品.数量 > 0))//没有商品可卖
        {
            当前保底客户序数 = 0;
            当前分段时长 = 0;
            当日当前时长 = 0;
            if (显示控制.Instance.每日进度条 != null)
            {
                显示控制.Instance.每日进度条.localPosition = new Vector2(0, 0);
                显示控制.Instance.每日进度条.localScale = new Vector2(0, 1);
            }
            if (完成交易)
            {
                关门休息();
                确认是否营业 = false;
            }
        }
        if (当前分段时长 < 保底顾客间隔时间)
        {
            if (当前保底客户序数 >= 保底客户数量)
            {
                当前保底客户序数 = 0;
                当前分段时长 = 0;
                当日当前时长 = 0;
                //打开关门休息的UI
                if (显示控制.Instance.每日进度条 != null)
                {
                    显示控制.Instance.每日进度条.localPosition = new Vector2(0, 0);
                    显示控制.Instance.每日进度条.localScale = new Vector2(0, 1);
                }
                关门休息();
                确认是否营业 = false;
            }
            else
            {
                当前分段时长 += Time.deltaTime;
                当日当前时长 += Time.deltaTime;
                float 移动比例 = 当日当前时长 / 单日总时长;
                if (显示控制.Instance.每日进度条 != null)
                {
                    // 宽度从 0 到 500
                    //float 宽度 = 500f * ;

                    // 中心点 X：-250 + 250 * 比例
                    // 这样左端始终是 -250，右端到 250
                    float 中心X = -250f + 250f * 移动比例;

                    显示控制.Instance.每日进度条.localPosition = new Vector2(中心X, 0);
                    显示控制.Instance.每日进度条.localScale = new Vector2(移动比例, 1);
                }
                if (完成交易)
                {
                    交易控制器.Instance.客户选择商品();
                    完成交易 = false;
                }
            }
        }
        else
        {
            if (完成交易)
            {
                当前保底客户序数 += 1;
                当前分段时长 = 0; 
            }
        }
    }
    public void 关门休息()
    {
        打开事件(2);
        显示控制.Instance.对话框.text = "";
        //出账单

    }
    public void 进入下一天()
    {
        确认保底顾客间隔时间 = true;
        //更新保底客户数量
        if (当前天数<=7)
        {
            保底客户数量 = 5;
        }
        else
        {
            if (!开启宣传日)
            {
                保底客户数量 = 3;
            }
            else
            {
                保底客户数量 = 1;
            }
        }
        当前保底客户序数 = 0;
        当前分段时长 = 0;
        当日当前时长 = 0;
        打开事件(0);
    }
}
