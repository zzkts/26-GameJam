using UnityEngine;

/// <summary>
/// 金钱 / 物品「发射」飞行控制器（所有可调参数都集中在本组件的 Inspector 面板上）。
///
/// 效果：点击 Canvas 中的「支付钱」物体（或「购买商品」处的猪与猫）时，
/// 原本直接出现在提供区域 / 获得区域的预制体，改为从被点击的位置出发，
/// 沿贝塞尔曲线平滑飞到落点，并带有起飞缩放、自转、连发错开与来源后坐力等表现。
///
/// 使用方式：
/// 1. 在场景任意物体（建议「Canvas」上的「控制程序」）挂载本组件，手感参数在 Inspector 里调。
/// 2. 场景里没有本组件时，运行时会自动补一个默认参数的实例，并打印一条提示。
/// 3. 想让某个按钮的发射起点或手感与全局不同，可在该按钮（或其父节点）上挂 <see cref="发射源"/> 组件。
/// 4. 客户给钱 / 客户买货（交易类型 0）默认不做飞行，把「客户锚点」指向客户物体后即可让它从客户那边飞过来。
/// </summary>
[DisallowMultipleComponent]
public class 发射动画 : MonoBehaviour
{
    /// <summary>弧线鼓起的朝向（相对父节点坐标系，上 = 屏幕上方）。</summary>
    public enum 弧线方向 { 上, 下, 左, 右, 随机上下 }

    public static 发射动画 Instance { get; private set; }

    // ==================== ① 总开关 ====================

    [Header("① 总开关")]
    [Tooltip("关闭后恢复原逻辑：预制体直接出现在落点，不做飞行动画")]
    public bool 启用发射 = true;

    // ==================== ② 飞行时间 ====================

    [Header("② 飞行时间")]
    [Tooltip("一次飞行的大致时长（秒）")]
    public float 飞行时长 = 0.38f;

    [Tooltip("每个物体在时长上的随机浮动（秒），0 = 所有物体时长一致")]
    public float 时长随机浮动 = 0.08f;

    [Tooltip("连发错开：0.2 秒内连续发射时，每多一个物体就多等这么久再起飞，形成错落的拖尾")]
    public float 连发错开 = 0.03f;

    [Tooltip("连发错开的累计上限（秒），避免连点太快时后面的一直不起飞")]
    public float 连发错开上限 = 0.15f;

    // ==================== ③ 贝塞尔曲线 ====================

    [Header("③ 贝塞尔曲线")]
    public 弧线方向 弧度方向 = 弧线方向.上;

    [Tooltip("弧线高度：控制点相对起终点连线的垂直偏移（像素）")]
    public float 曲线弧度 = 90f;

    [Tooltip("每次发射时弧度的随机浮动（像素）")]
    public float 弧度随机浮动 = 35f;

    [Tooltip("第一个控制点的位置（0~1），越小越早把物体抛起来")]
    public float 控制点位置A = 0.25f;

    [Tooltip("第二个控制点的位置（0~1），越大落点前越晚收拢")]
    public float 控制点位置B = 0.75f;

    [Tooltip("第二个控制点的弧度比例（0~1），越小越像抛物线一样被抛出去")]
    [Range(0f, 1f)] public float 弧度衰减 = 0.6f;

    [Tooltip("时间 → 进度的缓动曲线；把中间某个关键帧拖到 1 以上，可以做出一头扎进落点再弹回来的过冲效果")]
    public AnimationCurve 缓动曲线 = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ==================== ④ 飞行表现 ====================

    [Header("④ 飞行表现")]
    [Tooltip("起飞瞬间的缩放倍率（相对预制体原缩放）")]
    public float 起始缩放倍率 = 1.15f;

    [Tooltip("落点处的缩放倍率（相对预制体原缩放），1 = 恢复成预制体原本的大小")]
    public float 末端缩放倍率 = 1f;

    [Tooltip("飞行过程中自转的圈数，0 = 不旋转")]
    public float 旋转圈数;

    [Tooltip("起飞位置的随机抖动半径（像素），避免多张钞票完全重叠在一起")]
    public float 起飞抖动 = 10f;

    [Tooltip("飞行途中禁止点击，避免误点到正在飞的物体")]
    public bool 飞行中禁止点击 = true;

    // ==================== ⑤ 发射源锚点 ====================

    [Header("⑤ 发射源锚点（点击来源缺失时的兜底）")]
    [Tooltip("支付钱锚点：金钱没有点击来源时从这里飞出")]
    public Transform 支付钱锚点;

    [Tooltip("购买商品锚点：商品没有点击来源时从这里飞出")]
    public Transform 购买商品锚点;

    [Tooltip("客户锚点：客户给钱 / 客户买货（交易类型 0）时从这里飞出；留空则该路径不做飞行动画")]
    public Transform 客户锚点;

    [Tooltip("点击来源的有效期（秒），超过这个时间没有新点击就认为来源已失效，0 = 永不过期")]
    public float 点击来源有效期;

    // ==================== ⑥ 发射源反馈 ====================

    [Header("⑥ 发射源反馈")]
    [Tooltip("发射时让来源物体（钞票 / 猪 / 猫）做一次按压回弹")]
    public bool 源后坐力 = true;

    [Tooltip("后坐力的收缩幅度（相对原缩放）")]
    [Range(0f, 0.5f)] public float 后坐力幅度 = 0.12f;

    [Tooltip("后坐力的时长（秒）")]
    public float 后坐力时长 = 0.12f;

    // ==================== ⑦ 调试 ====================

    [Header("⑦ 调试")]
    [Tooltip("打印每次发射的起点、落点与飞行参数")]
    public bool 输出调试日志;

    /// <summary>一次飞行的全部参数，由 发射() 组装、发射飞行体 消费。</summary>
    public struct 飞行参数
    {
        public Vector2 起点;
        public Vector2 终点;
        public float 延迟;
        public float 时长;
        public float 弧度;
        public Vector2 弧线方向向量;
        public float 控制点位置A;
        public float 控制点位置B;
        public float 弧度衰减;
        public AnimationCurve 缓动曲线;
        public float 起始缩放倍率;
        public float 末端缩放倍率;
        public float 旋转圈数;
        public Vector3 原始缩放;
        public Quaternion 原始旋转;
        public bool 飞行中禁止点击;
    }

    /// <summary>发射起点：位置 + 可选的手感覆盖标记（标记来自按钮上的 发射源 组件）。</summary>
    public struct 发射起点
    {
        public Transform 位置;
        public 发射源 标记;
    }

    // ==================== 运行时状态 ====================

    private static Transform 最近点击物体;
    private static float 最近点击时间 = -999f;
    private static float 上次发射时间 = -999f;
    private static int 连发计数;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>全局配置；场景里没有挂载时自动补一个使用默认参数的实例。</summary>
    public static 发射动画 配置
    {
        get
        {
            if (Instance != null) return Instance;

            Instance = FindObjectOfType<发射动画>();
            if (Instance != null) return Instance;

            var go = new GameObject("发射动画（运行时自动创建）");
            Instance = go.AddComponent<发射动画>();
            Debug.LogWarning("[发射动画] 场景里没有找到 发射动画 组件，已自动创建一个默认参数的实例。" +
                             "想调整飞行手感，请在场景物体上手动挂载 发射动画 组件。");
            return Instance;
        }
    }

    // ==================== 发射起点 ====================

    /// <summary>记录玩家点击的物体（由 LongPressHandler 在按下时调用）。</summary>
    public static void 记录点击来源(Transform 物体)
    {
        if (物体 == null) return;
        最近点击物体 = 物体;
        最近点击时间 = Time.unscaledTime;
    }

    /// <summary>最近一次被点击的物体；超过有效期则认为没有来源。</summary>
    public static Transform 点击来源()
    {
        if (最近点击物体 == null) return null;

        // 这里只读已有实例的有效期配置，不触发自动创建
        var 动画 = Instance;
        float 有效期 = 动画 != null ? 动画.点击来源有效期 : 3f;
        if (有效期 > 0f && Time.unscaledTime - 最近点击时间 > 有效期) return null;
        return 最近点击物体;
    }

    /// <summary>把「玩家刚点击的物体」解析成发射起点。</summary>
    public static 发射起点 取点击起点()
    {
        return 解析起点(点击来源());
    }

    /// <summary>把一个固定锚点解析成发射起点。</summary>
    public static 发射起点 取锚点(Transform 锚点)
    {
        return 解析起点(锚点);
    }

    private static 发射起点 解析起点(Transform 物体)
    {
        var 起点 = new 发射起点();
        if (物体 == null) return 起点;

        // 按钮（或其父节点）上可以挂 发射源 标记，用来指定真正的发射起点与手感覆盖
        var 标记 = 物体.GetComponentInParent<发射源>();
        起点.标记 = 标记;
        起点.位置 = (标记 != null && 标记.起点锚点 != null) ? 标记.起点锚点 : 物体;
        return 起点;
    }

    // ==================== 发射 ====================

    /// <summary>
    /// 生成预制体，并让它从 起点 沿贝塞尔曲线飞到 落点。
    /// 总开关关闭、起点为空、或对象不是 UI 时，退化成「直接出现在落点」的原逻辑。
    /// </summary>
    /// <param name="预制体">要生成的预制体</param>
    /// <param name="父级">生成后挂到哪个节点下（决定 anchoredPosition 的坐标系）</param>
    /// <param name="落点">落点，相对 父级 的 anchoredPosition</param>
    /// <param name="起点">发射起点；位置为空时直接放到落点</param>
    /// <param name="交易类型">仅用于调试日志</param>
    public static GameObject 发射(GameObject 预制体, Transform 父级, Vector2 落点, 发射起点 起点, int 交易类型 = -1)
    {
        if (预制体 == null || 父级 == null) return null;

        var go = Instantiate(预制体, 父级);
        var rt = go.transform as RectTransform;
        var 动画 = 配置;

        if (rt == null || !动画.启用发射 || 起点.位置 == null || 动画.飞行时长 <= 0f)
        {
            // 退化路径：与原逻辑一致，直接放到落点
            if (rt != null) rt.anchoredPosition = 落点;
            else go.transform.position = new Vector3(落点.x, 落点.y, 0f);
            return go;
        }

        // 起点换算：直接对齐源的世界坐标，再读回同一父级下的 anchoredPosition，
        // 这样源无论在哪个面板、哪一层父节点下都能对准，也不会在生成瞬间闪到落点。
        go.transform.position = 起点.位置.position;
        Vector2 实际起点 = rt.anchoredPosition + Random.insideUnitCircle * 动画.起飞抖动;
        rt.anchoredPosition = 实际起点;

        Vector3 原始缩放 = rt.localScale;
        Quaternion 原始旋转 = rt.localRotation;
        rt.localScale = 原始缩放 * 动画.起始缩放倍率;

        // 连发错开：短时间内连续发射（长按连点）时依次起飞
        float 现在 = Time.unscaledTime;
        连发计数 = (现在 - 上次发射时间 <= 0.2f) ? 连发计数 + 1 : 0;
        上次发射时间 = 现在;
        float 延迟 = Mathf.Min(连发计数 * 动画.连发错开, 动画.连发错开上限);

        // 手感覆盖：按钮上的 发射源 标记可以单独改时长 / 弧度 / 弧线方向
        var 标记 = 起点.标记;
        float 时长 = Mathf.Max(0.01f, 动画.飞行时长 + Random.Range(-动画.时长随机浮动, 动画.时长随机浮动));
        float 弧度 = Mathf.Max(0f, 动画.曲线弧度 + Random.Range(-动画.弧度随机浮动, 动画.弧度随机浮动));
        if (标记 != null)
        {
            时长 *= Mathf.Max(0.01f, 标记.时长倍率);
            弧度 *= Mathf.Max(0f, 标记.弧度倍率);
        }

        var 参数 = new 飞行参数
        {
            起点 = 实际起点,
            终点 = 落点,
            延迟 = 延迟,
            时长 = 时长,
            弧度 = 弧度,
            弧线方向向量 = 取弧线方向向量(标记 != null && 标记.覆盖弧线方向 ? 标记.弧线方向 : 动画.弧度方向),
            控制点位置A = 动画.控制点位置A,
            控制点位置B = 动画.控制点位置B,
            弧度衰减 = 动画.弧度衰减,
            缓动曲线 = 动画.缓动曲线,
            起始缩放倍率 = 动画.起始缩放倍率,
            末端缩放倍率 = 动画.末端缩放倍率,
            旋转圈数 = 动画.旋转圈数,
            原始缩放 = 原始缩放,
            原始旋转 = 原始旋转,
            飞行中禁止点击 = 动画.飞行中禁止点击,
        };

        var 飞行体 = go.GetComponent<发射飞行体>();
        if (飞行体 == null) 飞行体 = go.AddComponent<发射飞行体>();
        飞行体.初始化(参数);

        // 来源后坐力：源本身正在飞的时候跳过，免得两个脚本抢同一个缩放
        if (动画.源后坐力 && 起点.位置 != null && 起点.位置.GetComponent<发射飞行体>() == null)
        {
            发后坐力(起点.位置, 动画.后坐力幅度, 动画.后坐力时长);
        }

        if (动画.输出调试日志)
        {
            Debug.Log("[发射动画] 交易类型" + 交易类型 + " 从「" + 起点.位置.name + "」飞到 " + 落点 +
                      "（时长 " + 时长.ToString("F2") + "s，弧度 " + 弧度.ToString("F0") + "）");
        }

        return go;
    }

    private static void 发后坐力(Transform 源, float 幅度, float 时长)
    {
        if (源 == null) return;

        var 回弹 = 源.GetComponent<源后坐力>();
        if (回弹 == null)
        {
            回弹 = 源.gameObject.AddComponent<源后坐力>();
            回弹.基准缩放 = 源.localScale;
        }
        回弹.幅度 = 幅度;
        回弹.时长 = 时长;
        回弹.重播();
    }

    private static Vector2 取弧线方向向量(弧线方向 方向)
    {
        switch (方向)
        {
            case 弧线方向.下: return Vector2.down;
            case 弧线方向.左: return Vector2.left;
            case 弧线方向.右: return Vector2.right;
            case 弧线方向.随机上下: return Random.value < 0.5f ? Vector2.up : Vector2.down;
            default: return Vector2.up;
        }
    }
}
