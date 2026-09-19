using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 新增商品 : MonoBehaviour
{
    public string 商品名称;
    public float 进货价;
    public float 售卖价;
    public bool 确认新增商品;
    public bool 是给供应商;//不是供应商给到自己
    public List<string> 商品名称列表;
    public List<float> 进货价列表;
    public List<float> 售卖价列表;
    public void Update()
    {
        if (确认新增商品)
        {
            if (商品名称列表.Count == 进货价列表.Count && 商品名称列表.Count == 售卖价列表.Count)
            {
                for (int i = 0; i < 商品名称列表.Count; i++)
                {
                    商品 商品 = new 商品();
                    商品.商品名称 = 商品名称列表[i];
                    商品.进货价 = 进货价列表[i];
                    商品.售卖价 = 售卖价列表[i];
                    if (是给供应商)
                    {
                        交易控制器.Instance.商品待购买列表.Add(商品);
                    }
                    else
                    {
                        交易控制器.Instance.商品已拥有列表.Add(商品);
                    }
                }
            }
            if (商品名称!=null&& 商品名称 !="")
            {
                商品 商品 = new 商品();
                商品.商品名称 = 商品名称;
                商品.进货价 = 进货价;
                商品.售卖价 = 售卖价;
                if (是给供应商)
                {
                    交易控制器.Instance.商品待购买列表.Add(商品);
                }
                else
                {
                    交易控制器.Instance.商品已拥有列表.Add(商品);
                }
            }
            确认新增商品 = false;
        }
    }
}
