using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 动画播放器 : MonoBehaviour
{
    public List<GameObject> 动画图片;
    public float 间隔时间;
    public int 当前索引;
    public float 计时器;
    void Start()
    {
        // 初始化：只显示第一个，隐藏其他
        if (动画图片.Count > 0)
        {
            for (int i = 0; i < 动画图片.Count; i++)
            {
                动画图片[i].SetActive(i == 0);
            }
        }
    }
    void Update()
    {
        if (动画图片.Count == 0) return; // 防止空列表报错

        计时器 += Time.deltaTime;

        if (计时器 >= 间隔时间)
        {
            计时器 = 0f;

            // 切换到下一个（循环）
            当前索引 = (当前索引 + 1) % 动画图片.Count;

            // 只更新当前显示和上一个（优化）
            for (int i = 0; i < 动画图片.Count; i++)
            {
                动画图片[i].SetActive(i == 当前索引);
            }
        }
    }
    public void 动画播放()
    {
        //每隔间隔时间关闭所有图片，只打开动画图片[当前动画+1],直到重置动画图片
    }
}
