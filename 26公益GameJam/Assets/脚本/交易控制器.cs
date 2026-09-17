using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public class 交易控制器 : MonoBehaviour
{
    public static 交易控制器 Instance { get; private set; }
    public float 钱;
    public float 需支付钱;//玩家需要提供的钱
    public float 已准备钱;//玩家已经提供的钱
    public float 待收取钱;//玩家获得的钱
    public float 商品价值;//商品的售价
    public int 客户上限;
    public float 找零概率;
    public float 出新客户概率;
    public float 多件商品购买概率;
    public int 多件商品购买数量;
    public float 讨价还价率;
    public float 供应商算错率;
    public List<商品> 商品已拥有列表 = new List<商品>();//自己可以卖的商品
    public List<商品> 商品待购买列表 = new List<商品>();//供货商的商品
    public List<商品> 临时待交易商品 = new List<商品>();//正在被选择交易的商品
    public List<现金> 所有现金 = new List<现金>();//自己可以支付的现金
    public List<现金> 临时待支付现金 = new List<现金>();//正在被选择交易的现金
    public List<现金> 临时待获得现金 = new List<现金>();//正在被选择交易的现金
    //public float 总现金价值;
    public int 总现金数量;
    public int 交易类型;//0是客户买商品，1是自己买商品，0是对方给钱，1是自己给钱

    public float 顾客出错率;
    public float 找零精度;
    public float 大面值概率;//越高客户支付大面值的概率越高，受到（总财富/现金数量）动态调整
    public float 单位货币数量的价值;
    public int 第X次反馈;//第一次方向性模糊信息反馈，第二次精准信息反馈，第三次精准操作反馈，三次后清空
    //public bool 选择商品;
    //public string 选择商品名称;
    //public bool 取消商品;
    //public int 取消商品的序号;
    //public bool 选择现金;
    //public float 选择现金面值;
    //public bool 取消现金;
    //public int 取消现金的序号;
    //public bool 确认交易;
    //public bool 开始接客;
    private void Awake()
    {
        Instance = this;
    }
    public void Update()
    {
        //if (选择商品)
        //{
        //    选择购买商品(1,选择商品名称);
        //    选择商品 = false;
        //}
        //if (取消商品)
        //{
        //    在供货商取消商品(取消商品的序号);
        //    取消商品 = false;
        //}
        //if (选择现金)
        //{
        //    选择要支付的钱(1,选择现金面值);
        //    选择现金 = false;
        //}
        //if (取消现金)
        //{
        //    取消要支付的钱(1, 选择现金面值);
        //    取消现金 = false;
        //}
        //if (开始接客)
        //{
        //    客户选择商品();
        //    开始接客 = false;
        //}
        //if (确认交易)
        //{
        //    交易判断();
        //    确认交易 = false;
        //}
    }
    public void 玩家选择购买商品(string 选择商品名称)
    {
        this.交易类型 = 1;
        选择购买商品(1, 选择商品名称);
    }

    public void 选择购买商品(int 交易类型, string 选择商品名称)//交易类型决定商品的摆放位置
    {
        if (选择商品名称 == null || 选择商品名称 == "")
        {
            return;
        }
        if (交易类型 == 0)
        {
            for (int i = 0; i < 商品已拥有列表.Count; i++)
            {
                if (商品已拥有列表[i].商品名称 == 选择商品名称)
                {
                    print(选择商品名称);
                    //不存在无法再选中新商品
                    //待收取钱 += 商品已拥有列表[i].售卖价;
                    bool 已存在 = false;
                    for (int j = 0; j < 临时待交易商品.Count; j++)
                    {
                        if (临时待交易商品[j].商品名称 == 商品已拥有列表[i].商品名称)
                        {
                            临时待交易商品[j].数量 += 1;
                            print(临时待交易商品[j].数量);
                            已存在 = true;
                            break;
                        }
                    }
                    if (!已存在)
                    {
                        //如果临时待交易商品没有同名称再新建
                        var 待交易商品 = new 商品
                        {
                            商品名称 = 商品已拥有列表[i].商品名称,
                            进货价 = 商品已拥有列表[i].进货价,
                            售卖价 = 商品已拥有列表[i].售卖价,
                            数量 = 1,
                        };
                        临时待交易商品.Add(待交易商品);
                    }
                    GameObject prefab = null;
                    if (商品已拥有列表[i].商品名称 == "A")
                    {
                        prefab = 显示控制.Instance.待交易物品A;
                    }
                    if (商品已拥有列表[i].商品名称 == "B")
                    {
                        prefab = 显示控制.Instance.待交易物品B;
                    }
                    if (prefab != null)
                    {
                        //根据“显示控制.Instance.待交易物品A”预制体在x200-650y-50-150;
                        float x = UnityEngine.Random.Range(-200f, -650f);
                        float y = UnityEngine.Random.Range(-50f, 150f);

                        // ★ 发射：从客户（需在「发射动画」上配置客户锚点）沿贝塞尔曲线飞到获得区域
                        var go = 发射动画.发射(
                            prefab,
                            显示控制.Instance.transform,
                            new Vector2(x, y),
                            取本次发射起点(交易类型, 发射动画.配置.购买商品锚点),
                            交易类型);
                        显示控制.Instance.获得对象.Add(go);
                        // ★ 记录这个采购商品对象的信息
                        显示控制.Instance.获得对象交易类型.Add(交易类型);
                        显示控制.Instance.获得对象面值.Add(0);
                        显示控制.Instance.获得对象名称.Add(选择商品名称);
                    }
                }
            }
        }
        if (交易类型 == 1)
        {
            for (int i = 0; i < 商品待购买列表.Count; i++)
            {
                if (商品待购买列表[i].商品名称 == 选择商品名称)
                {
                    if (钱 - 需支付钱 < 商品待购买列表[i].进货价)
                    {
                        //无法再选中新商品
                        显示控制.Instance.对话框.text = "你确定有买这些商品的钱吗？";
                        return;
                    }
                    else
                    {
                        需支付钱 = Mathf.Round((需支付钱 + 商品待购买列表[i].进货价) * 100f) / 100f;
                        显示控制.Instance.对话框.text = "购买这些物品需要提供" + 需支付钱 + "¥";
                        bool 已存在 = false;
                        for (int j = 0; j < 临时待交易商品.Count; j++)
                        {
                            if (临时待交易商品[j].商品名称 == 商品待购买列表[i].商品名称)
                            {
                                临时待交易商品[j].数量 += 1;
                                已存在 = true;
                                break;
                            }
                        }
                        if (!已存在)
                        {
                            //如果临时待交易商品没有同名称再新建
                            var 待交易商品 = new 商品
                            {
                                商品名称 = 商品待购买列表[i].商品名称,
                                进货价 = 商品待购买列表[i].进货价,
                                售卖价 = 商品待购买列表[i].售卖价,
                                数量 = 1,
                            };
                            临时待交易商品.Add(待交易商品);
                        }
                        GameObject prefab = null;
                        if (商品待购买列表[i].商品名称 == "A")
                        {
                            prefab = 显示控制.Instance.待交易物品A;
                        }
                        if (商品待购买列表[i].商品名称 == "B")
                        {
                            prefab = 显示控制.Instance.待交易物品B;
                        }
                        if (prefab != null)
                        {
                            //根据“显示控制.Instance.待交易物品A”预制体在x200-650y-50-150;
                            float x = UnityEngine.Random.Range(200f, 650f);
                            float y = UnityEngine.Random.Range(-50f, 150f);
                            // ★ 发射：从被点击的猪 / 猫沿贝塞尔曲线飞到获得区域
                            var go = 发射动画.发射(
                                prefab,
                                显示控制.Instance.transform,
                                new Vector2(x, y),
                                取本次发射起点(交易类型, 发射动画.配置.购买商品锚点),
                                交易类型);
                            显示控制.Instance.获得对象.Add(go);
                            // ★ 同步记录到获得对象的并行列表
                            显示控制.Instance.获得对象交易类型.Add(交易类型);
                            显示控制.Instance.获得对象面值.Add(0);
                            显示控制.Instance.获得对象名称.Add(选择商品名称);
                        }
                        break;
                    }
                }
            }
        }

    }

    public void 玩家支付现金(float 面值)
    {
        print("左");
        选择要支付的钱(1, 面值);
    }
    public void 玩家取消现金(float 面值)
    {
        print("右");
        取消要支付的钱(1, 面值);
    }

    // ★ 取本次「发射」的起点：
    //   交易类型 1（我出钱 / 我买东西）= 玩家刚点击的物体（支付钱面板上的钞票、购买商品处的猪与猫）
    //   交易类型 0（客户给钱 / 客户买货）= 「发射动画」上配置的客户锚点
    //   两者都取不到时用兜底锚点，兜底锚点也为空则由「发射动画」退化成直接出现在落点
    private 发射动画.发射起点 取本次发射起点(int 交易类型, Transform 兜底锚点)
    {
        if (交易类型 == 1)
        {
            var 点击起点 = 发射动画.取点击起点();
            if (点击起点.位置 != null) return 点击起点;
            return 发射动画.取锚点(兜底锚点);
        }

        var 客户起点 = 发射动画.取锚点(发射动画.配置.客户锚点);
        if (客户起点.位置 != null) return 客户起点;
        return 发射动画.取锚点(兜底锚点);
    }

    public void 选择要支付的钱(int 交易类型, float 面值)
    {
        bool 已新建 = false;
        GameObject prefab = null;
        if (交易类型 == 1)
        {
            for (int i = 0; i < 所有现金.Count; i++)
            {
                if (所有现金[i].面值 == 面值 && 所有现金[i].数量 > 0)
                {
                    for (int j = 0; j < 临时待支付现金.Count; j++)
                    {
                        if (面值 == 临时待支付现金[j].面值)
                        {
                            if (所有现金[i].数量 - 临时待支付现金[j].数量 <= 0)//没有足够钱可以准备
                            {
                                return;
                            }
                            else
                            {
                                临时待支付现金[j].数量 += 1;
                                已准备钱 = Mathf.Round((已准备钱 + 面值) * 100f) / 100f;
                                已新建 = true;
                                break;
                            }
                        }
                    }
                    if (!已新建)
                    {
                        //提供钱
                        临时待支付现金.Add(new 现金 { 面值 = 面值, 数量 = 1 });
                        已准备钱 = Mathf.Round((已准备钱 + 面值) * 100f) / 100f;
                    }
                    if (所有现金[i].面值 == 0.1f)
                    {
                        prefab = 显示控制.Instance.待交易1毛;
                        显示控制.Instance.显示1毛数量.text = (int.Parse(显示控制.Instance.显示1毛数量.text) - 1).ToString();
                    }
                    if (所有现金[i].面值 == 0.5f)
                    {
                        prefab = 显示控制.Instance.待交易5毛;
                        显示控制.Instance.显示5毛数量.text = (int.Parse(显示控制.Instance.显示5毛数量.text) - 1).ToString();
                    }
                    if (所有现金[i].面值 == 1)
                    {
                        prefab = 显示控制.Instance.待交易1元;
                        显示控制.Instance.显示1元数量.text = (int.Parse(显示控制.Instance.显示1元数量.text) - 1).ToString();
                    }
                    if (所有现金[i].面值 == 5)
                    {
                        prefab = 显示控制.Instance.待交易5元;
                        显示控制.Instance.显示5元数量.text = (int.Parse(显示控制.Instance.显示5元数量.text) - 1).ToString();
                    }
                    if (所有现金[i].面值 == 10)
                    {
                        prefab = 显示控制.Instance.待交易10元;
                        显示控制.Instance.显示10元数量.text = (int.Parse(显示控制.Instance.显示10元数量.text) - 1).ToString();
                    }
                    if (所有现金[i].面值 == 20)
                    {
                        prefab = 显示控制.Instance.待交易20元;
                        显示控制.Instance.显示20元数量.text = (int.Parse(显示控制.Instance.显示20元数量.text) - 1).ToString();
                    }
                    if (所有现金[i].面值 == 50)
                    {
                        prefab = 显示控制.Instance.待交易50元;
                        显示控制.Instance.显示50元数量.text = (int.Parse(显示控制.Instance.显示50元数量.text) - 1).ToString();
                    }
                    if (所有现金[i].面值 == 100)
                    {
                        prefab = 显示控制.Instance.待交易100元;
                        显示控制.Instance.显示100元数量.text = (int.Parse(显示控制.Instance.显示100元数量.text) - 1).ToString();
                    }
                }
            }
        }
        if (交易类型 == 0)
        {
            for (int j = 0; j < 临时待获得现金.Count; j++)
            {
                if (面值 == 临时待获得现金[j].面值)
                {
                    临时待获得现金[j].数量 += 1;
                    待收取钱 = Mathf.Round((待收取钱 + 面值) * 100f) / 100f;
                    已新建 = true;
                }
            }
            if (!已新建)
            {
                //提供钱
                临时待获得现金.Add(new 现金 { 面值 = 面值, 数量 = 1 });
                待收取钱 = Mathf.Round((待收取钱 + 面值) * 100f) / 100f;
            }
            print(面值);
            if (面值 == 0.1f)
            {
                prefab = 显示控制.Instance.待交易1毛;
            }
            if (面值 == 0.5f)
            {
                prefab = 显示控制.Instance.待交易5毛;
            }
            if (面值 == 1)
            {
                prefab = 显示控制.Instance.待交易1元;
            }
            if (面值 == 5)
            {
                prefab = 显示控制.Instance.待交易5元;
            }
            if (面值 == 10)
            {
                prefab = 显示控制.Instance.待交易10元;
            }
            if (面值 == 20)
            {
                prefab = 显示控制.Instance.待交易20元;
            }
            if (面值 == 50)
            {
                prefab = 显示控制.Instance.待交易50元;
            }
            if (面值 == 100)
            {
                prefab = 显示控制.Instance.待交易100元;
            }
        }
        if (prefab != null)
        {
            //根据“显示控制.Instance.待交易物品A”预制体在x200-650y-50-150;
            float x = 0;
            if (交易类型 == 1)
            {
                x = UnityEngine.Random.Range(-200f, -650f);
            }
            if (交易类型 == 0)
            {
                x = UnityEngine.Random.Range(200f, 650f);
            }
            float y = UnityEngine.Random.Range(-50f, 150f);

            // ★ 发射：从被点击的支付钱物体（或客户处）沿贝塞尔曲线飞到提供区域
            var go = 发射动画.发射(
                prefab,
                显示控制.Instance.transform,
                new Vector2(x, y),
                取本次发射起点(交易类型, 发射动画.配置.支付钱锚点),
                交易类型);
            显示控制.Instance.提供对象.Add(go);
            // ★ 记录这个对象属于哪种交易类型、什么面值
            显示控制.Instance.提供对象交易类型.Add(交易类型);
            显示控制.Instance.提供对象面值.Add(面值);
            显示控制.Instance.提供对象名称.Add("");
        }
    }
    public void 取消要支付的钱(int 交易类型, float 面值)
    {
        //if (取消现金的序号 < 0 || 取消现金的序号 >= 临时待支付现金.Count) { return; }
        //var 待取消 = 临时待支付现金[取消现金的序号];
        //已准备钱 -= 待取消.面值;
        //待取消.数量 -= 1;
        //if (待取消.数量 <= 0)
        //{
        //    临时待支付现金.RemoveAt(取消现金的序号);
        //}
        // 交易类型 1：取消“要支付的钱”，是 选择要支付的钱(1, 面值) 的反方法
        if (交易类型 == 1)
        {
            // 从最后一个开始往前找，符合“从列表最后一个开始扣”
            for (int i = 临时待支付现金.Count - 1; i >= 0; i--)
            {
                var 待取消 = 临时待支付现金[i];

                if (待取消.面值 == 面值 && 待取消.数量 > 0)
                {
                    待取消.数量 -= 1;
                    已准备钱 = Mathf.Round((已准备钱 - 面值) * 100f) / 100f;
                    // 反向恢复 UI 显示数量：选择时是 -1，取消时 +1
                    if (面值 == 0.1f)
                    {
                        显示控制.Instance.显示1毛数量.text =
                            (int.Parse(显示控制.Instance.显示1毛数量.text) + 1).ToString();
                    }
                    else if (面值 == 0.5f)
                    {
                        显示控制.Instance.显示5毛数量.text =
                            (int.Parse(显示控制.Instance.显示5毛数量.text) + 1).ToString();
                    }
                    else if (面值 == 1)
                    {
                        显示控制.Instance.显示1元数量.text =
                            (int.Parse(显示控制.Instance.显示1元数量.text) + 1).ToString();
                    }
                    else if (面值 == 5)
                    {
                        显示控制.Instance.显示5元数量.text =
                            (int.Parse(显示控制.Instance.显示5元数量.text) + 1).ToString();
                    }
                    else if (面值 == 10)
                    {
                        显示控制.Instance.显示10元数量.text =
                            (int.Parse(显示控制.Instance.显示10元数量.text) + 1).ToString();
                    }
                    else if (面值 == 20)
                    {
                        显示控制.Instance.显示20元数量.text =
                            (int.Parse(显示控制.Instance.显示20元数量.text) + 1).ToString();
                    }
                    else if (面值 == 50)
                    {
                        显示控制.Instance.显示50元数量.text =
                            (int.Parse(显示控制.Instance.显示50元数量.text) + 1).ToString();
                    }
                    else if (面值 == 100)
                    {
                        显示控制.Instance.显示100元数量.text =
                            (int.Parse(显示控制.Instance.显示100元数量.text) + 1).ToString();
                    }

                    // 反向销毁一个待交易预制体：只销毁最后一个“该面值 + 交易类型1”的对象
                    for (int k = 显示控制.Instance.提供对象.Count - 1; k >= 0; k--)
                    {
                        if (显示控制.Instance.提供对象交易类型[k] == 1 &&
                            显示控制.Instance.提供对象面值[k] == 面值)
                        {
                            if (显示控制.Instance.提供对象[k] != null)
                                Destroy(显示控制.Instance.提供对象[k]);
                            显示控制.Instance.提供对象.RemoveAt(k);
                            显示控制.Instance.提供对象交易类型.RemoveAt(k);
                            显示控制.Instance.提供对象面值.RemoveAt(k);
                            显示控制.Instance.提供对象名称.RemoveAt(k);
                            break;
                        }
                    }

                    // 数量减到 0，从临时待支付现金列表移除
                    if (待取消.数量 <= 0)
                    {
                        临时待支付现金.RemoveAt(i);
                    }

                    return;
                }
            }
        }

        // 交易类型 0：取消“待获得现金”，是 选择要支付的钱(0, 面值) 的反方法
        if (交易类型 == 0)
        {
            for (int k = 显示控制.Instance.提供对象.Count - 1; k >= 0; k--)
            {
                if (显示控制.Instance.提供对象交易类型[k] == 0 &&
                    显示控制.Instance.提供对象面值[k] == 面值)
                {
                    if (显示控制.Instance.提供对象[k] != null)
                        Destroy(显示控制.Instance.提供对象[k]);
                    显示控制.Instance.提供对象.RemoveAt(k);
                    显示控制.Instance.提供对象交易类型.RemoveAt(k);
                    显示控制.Instance.提供对象面值.RemoveAt(k);
                    显示控制.Instance.提供对象名称.RemoveAt(k);
                    break;
                }
            }
        }
    }
    public void 客户选择商品()
    {
        // 按方法名和 for 里的列表来看，应该是在“商品待购买列表”里随机选 1 个
        if (商品已拥有列表 == null || 商品已拥有列表.Count == 0)
        {
            Debug.LogWarning("商品待购买列表为空，客户无法选择商品");
            return;
        }
        this.交易类型 = 0;
        // 随机一个索引
        //int 随机索引 = UnityEngine.Random.Range(0, 商品已拥有列表.Count);
        //var 选中商品 = 商品已拥有列表[随机索引];

        int 总数 = 商品已拥有列表.Count;

        // 记录已经试过的索引，避免重复随机到同一个
        HashSet<int> 已尝试索引 = new HashSet<int>();
        商品 选中商品 = null;
        while (已尝试索引.Count < 总数)
        {
            int 随机索引 = UnityEngine.Random.Range(0, 总数);

            // 如果这个索引已经试过，就重新随机
            if (已尝试索引.Contains(随机索引))
                continue;

            已尝试索引.Add(随机索引);

            var 候选商品 = 商品已拥有列表[随机索引];

            // 数量 > 0，选中并结束
            if (候选商品.数量 > 0)
            {
                选中商品 = 候选商品;
                break;
            }
            // 数量 <= 0，继续随机下一个
        }
        if (选中商品 == null)
        {
            显示控制.Instance.对话框.text = "都买光了吗？";
            return;
        }
        //临时待交易商品.Add(选中商品);
        //加入选择
        商品价值 = Mathf.Round((商品价值 + 选中商品.售卖价) * 100f) / 100f;
        选择购买商品(0, 选中商品.商品名称);
        print(选中商品.商品名称);
        显示控制.Instance.对话框.text = "我想买个" + 选中商品.商品名称;
        选中商品.数量 -= 1;

        if (选中商品.商品名称 == "A")
        {
            显示控制.Instance.物品A数量.text =
                (int.Parse(显示控制.Instance.物品A数量.text) - 1).ToString();
        }
        if (选中商品.商品名称 == "B")
        {
            显示控制.Instance.物品B数量.text =
                (int.Parse(显示控制.Instance.物品B数量.text) - 1).ToString();
        }
        //客户给钱
        //将"选中商品.售卖价"按照规则，以及参数"拆分精度"和"顾客出错率"进行拆分
        //如果"顾客出错率"越高，顾客给的钱比"选中商品.售卖价"少的概率更高，具体少：0到选中商品.售卖价的10*顾客出错率*顾客出错率之间范围，获得一个临时参数：实际总支出
        //如果"拆分精度"越高，其会给实际总支出准确的随机面值（1、5、10、20、50、100）数量，比如：实际总支出=113，其会给100*1+10*1+3*1或者50*1+20*3+3*1等所有可能。
        //如果"拆分精度"越低，其会给大于实际总支出的随机面值组合，且出现更大面值的概率也更大，比如：实际总支出=113，拆分精度=0，其会给100*2，且随拆分精度越高，其会给100*1+50*1甚至100*1+20*1，要求找零。但不会给100*2+50*2，完全多给1张
        float 初始售价 = 选中商品.售卖价;
        float 减少比例 = 10f * 顾客出错率 * 顾客出错率;
        float 最大减少 = Mathf.FloorToInt(初始售价 * 减少比例);
        最大减少 = Mathf.Clamp(最大减少, 0, 初始售价);
        int 减少额 = UnityEngine.Random.Range(0, (int)最大减少);
        float 目标支付总额 = 初始售价 - 减少额;
        目标支付总额 = Mathf.Round(目标支付总额 * 100f) / 100f; // 保留两位小数，避免浮点误差

        // ★ 判断目标支付总额是否有小数
        bool 有小数 = Mathf.Abs(目标支付总额 - Mathf.Round(目标支付总额)) > 0.001f;

        // ★ 根据是否有小数，决定使用哪套面额列表
        float[] 面额列表;
        if (有小数)
        {
            print("有小数");
            面额列表 = new float[] { 100, 50, 20, 10, 5, 1, 0.5f };
        }
        else
        {
            面额列表 = new float[] { 100, 50, 20, 10, 5 };
        }

        float 剩余 = 目标支付总额;
        StringBuilder 结果 = new StringBuilder();

        foreach (float 面额 in 面额列表)
        {
            if (剩余 > 0.001f && 面额 < 钱)
            {
                int 最大张数 = (int)(剩余 / 面额) + 1;
                int 最小张数 = Mathf.Max(0, 最大张数 - 1);

                找零精度 = Mathf.Clamp01(找零精度);
                float 随机数 = Random.value;
                int 张数 = 随机数 < 找零精度 ? 最小张数 : 最大张数;
                if (面额!=1)
                {
                    张数 = (int)(张数 * 大面值概率);
                }

                // 保底不能超过剩余需要，防止越减越负
                //张数 = Mathf.Min(张数, (int)(剩余 / 面额));

                for (int i = 0; i < 张数; i++)
                {
                    选择要支付的钱(0, 面额);
                }
                剩余 -= 张数 * 面额;
                剩余 = Mathf.Round(剩余 * 100f) / 100f;

                if (张数 > 0)
                {
                    if (结果.Length > 0) 结果.Append("+");
                    结果.Append(面额).Append("*").Append(张数);
                }
            }
        }

        // ★ 兜底补零：有小数用 0.1，没有小数用 1
        if (剩余 > 0.001f)
        {
            float 兜底面额 = 有小数 ? 0.1f : 1f;
            int 张数 = Mathf.RoundToInt(剩余 / 兜底面额);
            for (int i = 0; i < 张数; i++)
            {
                选择要支付的钱(0, 兜底面额);
            }
            if (结果.Length > 0) 结果.Append("+");
            结果.Append(兜底面额).Append("*").Append(张数);
        }
        print(结果.ToString());
    } 
    public void 交易判断()
    {
        if (已准备钱 - 待收取钱 != 需支付钱 - 商品价值)//正确交易
        {
            if (找零精度 + 0.35 > 1)
            {
                找零精度 = 1;
            }
            else
            {
                找零精度 += 0.35f;
            }
        }
        if (第X次反馈 == 0)
        {
            第X次反馈 += 1;
            if (已准备钱 - 待收取钱 < 需支付钱 - 商品价值)
            {
                if (需支付钱 - 商品价值 - 已准备钱 + 待收取钱 > (需支付钱 - 商品价值) * 0.8f)//小于需付钱的0.8倍
                {
                    显示控制.Instance.对话框.text = "钱远远不够";
                }
                else
                {
                    显示控制.Instance.对话框.text = "还差一点钱";
                }
                return;
            }
            else if (已准备钱 - 待收取钱 > 需支付钱 - 商品价值)
            {
                显示控制.Instance.对话框.text = "钱给多了吧？";
                return;
            }
        }
        if (第X次反馈 == 1)
        {
            第X次反馈 += 1;
            if (已准备钱 - 待收取钱 < 需支付钱 - 商品价值)
            {
                显示控制.Instance.对话框.text = "你还差我" + (-已准备钱 + 待收取钱 + 需支付钱 - 商品价值).ToString() + "¥";
                return;
            }
            else if (已准备钱 - 待收取钱 > 需支付钱 - 商品价值)
            {
                显示控制.Instance.对话框.text = "你多给我" + (-需支付钱 + 商品价值 + 已准备钱 - 待收取钱).ToString() + "¥";
                return;
            }
        }
        if (第X次反馈 == 2)
        {
            第X次反馈 += 1;
            if (已准备钱 - 待收取钱 < 需支付钱 - 商品价值)
            {
                显示控制.Instance.对话框.text = "你还差我" + 拆分面值(-已准备钱 + 待收取钱 + 需支付钱 - 商品价值);
                return;
            }
            else if (已准备钱 - 待收取钱 > 需支付钱 - 商品价值)
            {
                显示控制.Instance.对话框.text = "你多给我" + 拆分面值(-需支付钱 + 商品价值 + 已准备钱 - 待收取钱);
                return;
            }
        }
        if (第X次反馈 == 3)
        {
            if (交易类型 == 1)
            {
                if (已准备钱 - 待收取钱 < 需支付钱 - 商品价值)
                {
                    //客户失去耐心放弃购买
                    显示控制.Instance.对话框.text = "钱不够,换个买呗？";
                    return;
                }
                else if (已准备钱 - 待收取钱 > 需支付钱 - 商品价值)
                {
                    显示控制.Instance.对话框.text = "老板很会做生意";
                    第X次反馈 = 0;
                }
            }
            if (交易类型 == 0)
            {
                if (已准备钱 - 待收取钱 < 需支付钱 - 商品价值)
                {
                    显示控制.Instance.对话框.text = "太墨迹，不买了";
                    第X次反馈 = 0;
                    取消交易();
                    return;
                }
                else if (已准备钱 - 待收取钱 > 需支付钱 - 商品价值)
                {
                    显示控制.Instance.对话框.text = "谢谢，多的钱我收了";
                    第X次反馈 = 0;
                }
            }
            第X次反馈 = 0;
        }
        if (已准备钱 - 待收取钱 == 需支付钱 - 商品价值)//正确交易
        {
            if (找零精度 - 0.15 < 0)
            {
                找零精度 = 0;
            }
            else
            {
                找零精度 -= 0.15f;
            }
            显示控制.Instance.对话框.text = "顺利完成交易";
        }
        单位货币数量的价值 = 钱 / 总现金数量;
        if (钱 / 总现金数量 > 3)
        {
            if (大面值概率 - 0.35f < 0)
            {
                大面值概率 = 0;
            }
            else
            {
                大面值概率 -= 0.35f;
            }
        }
        if (钱 / 总现金数量 < 1.5)
        {
            if (大面值概率 + 0.1f > 1)
            {
                大面值概率 = 1;
            }
            else
            {
                大面值概率 += 0.1f;
            }
        }
        结账();
    }
    public void 要求顾客找零()
    {
        float 应净支付 = 需支付钱 - 商品价值;
        float 实净支付 = 已准备钱 - 待收取钱;

        // 浮点数容差比较
        if (实净支付 < 应净支付 - 0.001f)
        {
            显示控制.Instance.对话框.text = "给少了找不了";
            return;
        }
        else if (Mathf.Abs(实净支付 - 应净支付) < 0.001f)
        {
            显示控制.Instance.对话框.text = "刚好，不用找零";
            return;
        }

        // ★ 判断玩家支付中最大单张面额
        float 最大单张面额 = 0f;
        for (int i = 0; i < 临时待支付现金.Count; i++)
        {
            if (临时待支付现金[i].数量 > 0 && 临时待支付现金[i].面值 > 最大单张面额)
            {
                最大单张面额 = 临时待支付现金[i].面值;
            }
        }

        // 如果最大单张面额不超过应付金额，说明玩家是用零钱拼凑的
        // 例如：买3元，玩家给了5张1元，顾客不找零，让玩家把多的拿回去
        if (最大单张面额 <= 应净支付 + 0.001f)
        {
            显示控制.Instance.对话框.text = "多的你拿回去就行";
            return;
        }

        // 正常找零：只有玩家给了大面额钞票时才走这里
        float 找零金额 = 实净支付 - 应净支付;
        找零金额 = Mathf.Round(找零金额 * 100f) / 100f;

        float[] 面额列表 = { 100f, 50f, 20f, 10f, 5f, 1f };
        float 剩余 = 找零金额;

        foreach (float 面额 in 面额列表)
        {
            if (剩余 <= 0.001f) break;

            int 张数 = Mathf.FloorToInt(剩余 / 面额 + 0.001f);
            if (张数 > 0)
            {
                for (int i = 0; i < 张数; i++)
                {
                    // 交易类型 0：客户给玩家钱，待收取钱增加
                    选择要支付的钱(0, 面额);
                }
                剩余 -= 张数 * 面额;
                剩余 = Mathf.Round(剩余 * 100f) / 100f;
            }
        }

        // 处理剩余的小数（通常是 0.1 的误差）
        if (剩余 > 0.001f)
        {
            int 张数 = Mathf.RoundToInt(剩余 / 0.1f);
            for (int i = 0; i < 张数; i++)
            {
                选择要支付的钱(0, 0.1f);
            }
            剩余 = 0;
        }

        显示控制.Instance.对话框.text = "客户找零 " + 找零金额.ToString("F2") + "¥";
    }
    public string 拆分面值(float 目标总额)
    {
        StringBuilder 结果 = new StringBuilder();

        // ★ 改为 float，并补上 1、0.5、0.1
        float[] 大面值 = { 100f, 50f, 20f, 10f, 5f, 1f, 0.5f, 0.1f };

        foreach (float 面额 in 大面值)
        {
            int 该面值的数量 = 0;
            for (int i = 0; i < 所有现金.Count; i++)
            {
                // ★ 浮点面额不能用 == 比较，改用容差
                if (Mathf.Abs(所有现金[i].面值 - 面额) < 0.001f)
                {
                    该面值的数量 = 所有现金[i].数量;
                    break;
                }
            }

            if (目标总额 > 0.001f && 面额 < 钱 && 该面值的数量 > 0)
            {
                // ★ 显式转 int，避免 float / float 直接赋给 int 报错
                int 最大张数 = (int)(目标总额 / 面额) + 1;
                int 最小张数 = Mathf.Max(0, 最大张数 - 1);
                float 随机数 = Random.value;
                int 张数 = 随机数 < 1 ? 最小张数 : 最大张数;
                张数 = (int)(张数 * 大面值概率);
                张数 = Mathf.Min(张数, 该面值的数量);

                目标总额 -= 张数 * 面额;
                目标总额 = Mathf.Round(目标总额 * 100f) / 100f; // ★ 抹掉浮点误差

                if (张数 > 0)
                {
                    if (结果.Length > 0) 结果.Append("+");
                    结果.Append(张数).Append("张").Append(面额).Append("¥");
                }
            }
        }

        // ★ 兜底：还有剩余就按 0.1 补足
        if (目标总额 > 0.001f)
        {
            if (结果.Length > 0) 结果.Append("+");
            int 张数 = Mathf.RoundToInt(目标总额 / 0.1f);
            结果.Append(张数).Append("枚").Append("0.1¥");
        }

        print(结果.ToString());
        return 结果.ToString();
    }
    public void 结账()
    {
        //获得钱和物品
        if (交易类型 == 1)
        {
            for (int i = 0; i < 临时待交易商品.Count; i++)
            {
                bool 已添加 = false;
                for (int j = 0; j < 商品已拥有列表.Count; j++)
                {
                    if (临时待交易商品[i].商品名称 == 商品已拥有列表[j].商品名称)
                    {
                        商品已拥有列表[j].数量 += 临时待交易商品[i].数量;
                        已添加 = true;
                    }
                }
                if (!已添加)
                {
                    商品已拥有列表.Add(临时待交易商品[i]);
                }
                if (临时待交易商品[i].商品名称 == "A")
                {
                    显示控制.Instance.物品A数量.text = (int.Parse(显示控制.Instance.物品A数量.text) + 临时待交易商品[i].数量).ToString();
                }
                if (临时待交易商品[i].商品名称 == "B")
                {
                    显示控制.Instance.物品B数量.text = (int.Parse(显示控制.Instance.物品B数量.text) + 临时待交易商品[i].数量).ToString();
                }
            }
        }
        //获得收益或被找零
        for (int i = 0; i < 临时待获得现金.Count; i++)
        {
            bool 已添加 = false;
            for (int j = 0; j < 所有现金.Count; j++)
            {
                if (临时待获得现金[i].面值 == 所有现金[j].面值)
                {
                    所有现金[j].数量 += 临时待获得现金[i].数量;
                    //总现金价值 += 临时待获得现金[i].面值 * 临时待获得现金[i].数量;
                    总现金数量 += 临时待获得现金[i].数量;
                    已添加 = true;
                }
            }
            if (!已添加)
            {
                所有现金.Add(临时待获得现金[i]);
                //总现金价值 += 临时待获得现金[i].面值 * 临时待获得现金[i].数量;
                总现金数量 += 临时待获得现金[i].数量;
            }
            if (临时待获得现金[i].面值 == 0.1f)
            {
                print(0.1f);
                显示控制.Instance.显示1毛数量.text = (int.Parse(显示控制.Instance.显示1毛数量.text) + 临时待获得现金[i].数量).ToString();
            }
            if (临时待获得现金[i].面值 == 0.5f)
            {
                显示控制.Instance.显示5毛数量.text = (int.Parse(显示控制.Instance.显示5毛数量.text) + 临时待获得现金[i].数量).ToString();
            }
            if (临时待获得现金[i].面值 == 1)
            {
                显示控制.Instance.显示1元数量.text = (int.Parse(显示控制.Instance.显示1元数量.text) + 临时待获得现金[i].数量).ToString();
            }
            if (临时待获得现金[i].面值 == 5)
            {
                显示控制.Instance.显示5元数量.text = (int.Parse(显示控制.Instance.显示5元数量.text) + 临时待获得现金[i].数量).ToString();
            }
            if (临时待获得现金[i].面值 == 10)
            {
                显示控制.Instance.显示10元数量.text = (int.Parse(显示控制.Instance.显示10元数量.text) + 临时待获得现金[i].数量).ToString();
            }
            if (临时待获得现金[i].面值 == 20)
            {
                显示控制.Instance.显示20元数量.text = (int.Parse(显示控制.Instance.显示20元数量.text) + 临时待获得现金[i].数量).ToString();
            }
            if (临时待获得现金[i].面值 == 50)
            {
                显示控制.Instance.显示50元数量.text = (int.Parse(显示控制.Instance.显示50元数量.text) + 临时待获得现金[i].数量).ToString();
            }
            if (临时待获得现金[i].面值 == 100)
            {
                显示控制.Instance.显示100元数量.text = (int.Parse(显示控制.Instance.显示100元数量.text) + 临时待获得现金[i].数量).ToString();
            }
        }
        //扣除支出
        for (int i = 0; i < 临时待支付现金.Count; i++)
        {
            for (int j = 0; j < 所有现金.Count; j++)
            {
                if (临时待支付现金[i].面值 == 所有现金[j].面值)
                {
                    所有现金[j].数量 -= 临时待支付现金[i].数量;
                    //总现金价值-= 临时待支付现金[i].面值 * 临时待支付现金[i].数量;
                    总现金数量 -= 临时待支付现金[i].数量;
                }
            }
        }
        foreach (var go in 显示控制.Instance.提供对象)
        {
            if (go != null) Destroy(go);
        }
        显示控制.Instance.提供对象.Clear();
        显示控制.Instance.提供对象交易类型.Clear();
        显示控制.Instance.提供对象面值.Clear();
        显示控制.Instance.提供对象名称.Clear();

        foreach (var go in 显示控制.Instance.获得对象)
        {
            if (go != null) Destroy(go);
        }
        显示控制.Instance.获得对象.Clear();
        显示控制.Instance.获得对象交易类型.Clear();
        显示控制.Instance.获得对象面值.Clear();
        显示控制.Instance.获得对象名称.Clear();
        钱 = Mathf.Round((钱+待收取钱 - 已准备钱) * 100f) / 100f;
        显示控制.Instance.总财富.text = 钱.ToString();
        临时待交易商品.Clear();
        临时待获得现金.Clear();
        临时待支付现金.Clear();
        已准备钱 = 0;
        需支付钱 = 0;
        待收取钱 = 0;
        商品价值 = 0;
        事件控制器.Instance.完成交易 = true;
    }
    public void 取消交易()
    {
        // 1. 回退「临时待支付现金」：恢复 UI 显示数量
        for (int i = 0; i < 临时待支付现金.Count; i++)
        {
            var 现金项 = 临时待支付现金[i];
            int 数量 = 现金项.数量;

            if (现金项.面值 == 0.1f)
            {
                显示控制.Instance.显示1毛数量.text =
                    (int.Parse(显示控制.Instance.显示1毛数量.text) + 数量).ToString();
            }
            else if (现金项.面值 == 0.5f)
            {
                显示控制.Instance.显示5毛数量.text =
                    (int.Parse(显示控制.Instance.显示5毛数量.text) + 数量).ToString();
            }
            else if (现金项.面值 == 1)
            {
                显示控制.Instance.显示1元数量.text =
                    (int.Parse(显示控制.Instance.显示1元数量.text) + 数量).ToString();
            }
            else if (现金项.面值 == 5)
            {
                显示控制.Instance.显示5元数量.text =
                    (int.Parse(显示控制.Instance.显示5元数量.text) + 数量).ToString();
            }
            else if (现金项.面值 == 10)
            {
                显示控制.Instance.显示10元数量.text =
                    (int.Parse(显示控制.Instance.显示10元数量.text) + 数量).ToString();
            }
            else if (现金项.面值 == 20)
            {
                显示控制.Instance.显示20元数量.text =
                    (int.Parse(显示控制.Instance.显示20元数量.text) + 数量).ToString();
            }
            else if (现金项.面值 == 50)
            {
                显示控制.Instance.显示50元数量.text =
                    (int.Parse(显示控制.Instance.显示50元数量.text) + 数量).ToString();
            }
            else if (现金项.面值 == 100)
            {
                显示控制.Instance.显示100元数量.text =
                    (int.Parse(显示控制.Instance.显示100元数量.text) + 数量).ToString();
            }
        }

        // 2. 「临时待获得现金」在选择时没有改动 UI 文本，所以不需要回退 UI，
        //    但也要清空列表，让下一次交易从头开始。

        // 3. 清空所有临时数据
        临时待支付现金.Clear();
        临时待获得现金.Clear();
        临时待交易商品.Clear();

        if (显示控制.Instance.提供对象 != null)
        {
            foreach (var go in 显示控制.Instance.提供对象)
            {
                if (go != null) Destroy(go);
            }
            显示控制.Instance.提供对象.Clear();
            显示控制.Instance.提供对象交易类型.Clear();
            显示控制.Instance.提供对象面值.Clear();
            显示控制.Instance.提供对象名称.Clear();
        }

        if (显示控制.Instance.获得对象 != null)
        {
            foreach (var go in 显示控制.Instance.获得对象)
            {
                if (go != null) Destroy(go);
            }
            显示控制.Instance.获得对象.Clear();
            显示控制.Instance.获得对象交易类型.Clear();
            显示控制.Instance.获得对象面值.Clear();
            显示控制.Instance.获得对象名称.Clear();
        }

        // 5. 重置所有金额与计数
        已准备钱 = 0;
        待收取钱 = 0;
        需支付钱 = 0;
        商品价值 = 0;
        第X次反馈 = 0;
        事件控制器.Instance.完成交易 = true;
    }
    public void 在供货商取消商品(string 取消商品名称)
    {
        if (string.IsNullOrEmpty(取消商品名称)) return;

        // 从临时待交易商品里从后往前找同名商品
        for (int i = 临时待交易商品.Count - 1; i >= 0; i--)
        {
            if (临时待交易商品[i].商品名称 != 取消商品名称) continue;

            // 1. 回退需支付钱
            需支付钱 = Mathf.Round((需支付钱 - 临时待交易商品[i].进货价) * 100f) / 100f;

            // 2. 数量 -1，减到 0 就移除
            if (临时待交易商品[i].数量 > 1)
            {
                临时待交易商品[i].数量 -= 1;
            }
            else
            {
                临时待交易商品.RemoveAt(i);
            }

            // 3. 从获得对象里按名称从后往前找匹配的预制体，销毁
            for (int k = 显示控制.Instance.获得对象.Count - 1; k >= 0; k--)
            {
                // 防越界：并行列表长度不一致时跳过
                if (k >= 显示控制.Instance.获得对象名称.Count) continue;

                if (显示控制.Instance.获得对象名称[k] == 取消商品名称)
                {
                    if (显示控制.Instance.获得对象[k] != null)
                        Destroy(显示控制.Instance.获得对象[k]);
                    显示控制.Instance.获得对象.RemoveAt(k);
                    显示控制.Instance.获得对象交易类型.RemoveAt(k);
                    显示控制.Instance.获得对象面值.RemoveAt(k);
                    显示控制.Instance.获得对象名称.RemoveAt(k);
                    break;
                }
            }

            显示控制.Instance.对话框.text = "多买点别取消呀！";
            return;
        }
    }
}
public class 商品
{
    public string 商品名称;
    public int 数量;
    public float 进货价;
    public float 售卖价;
}
public class 现金
{
    public float 面值;
    public int 数量;
}
public class 客户
{
    public int 客户状态;
}
