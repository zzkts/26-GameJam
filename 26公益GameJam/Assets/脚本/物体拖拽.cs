using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 3.1.1 物体拖拽
/// 触发条件：光标位于物品上方，且物品处于可被移动的状态。
/// 操作规则：鼠标左键按住并移动超过判定距离后确认拖拽，物品按「跟随速度」延迟跟随光标。
/// 表现反馈：确认拖拽时物品按「放大倍数」放大，松手后恢复原始大小。
/// 异常处理：未处于长按状态时速度归零，物品停在原地，不会被甩出屏幕。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class 物体拖拽 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("核心数值")]
    [Tooltip("物品是否处于可被移动的状态")]
    public bool 是否可被移动 = true;

    [Tooltip("跟随速度：数值越大跟得越紧，越小延迟越明显")]
    public int 跟随速度 = 5;

    [Tooltip("是否处于长按状态（运行中状态，松开左键后自动置回 false）")]
    public bool 是否处于长按状态;

    [Tooltip("拖拽放大倍数：最终缩放 = 原始缩放 × (1 + 放大倍数)")]
    public float 放大倍数 = 0.5f;

    [Header("拖拽判定")]
    [Tooltip("按住后光标移动超过该像素距离，才确认进入拖拽（用于区分拖拽与原地点击）")]
    public float 拖拽判定距离 = 10f;

    [Header("拖拽表现")]
    [Tooltip("放大 / 回正 / 倾斜的插值时长（秒），越小手感越干脆，0 = 瞬间生效")]
    public float 表现过渡时长 = 0.08f;

    [Tooltip("拖拽时随横向速度倾斜的最大角度（度），0 = 不倾斜")]
    public float 倾斜角度 = 8f;

    [Tooltip("横向速度 → 倾斜角度的换算系数，越大越容易倾到上限")]
    public float 倾斜灵敏度 = 0.02f;

    [Tooltip("拖起时把物品提到同级最上层，松手后恢复原来的层级")]
    public bool 拖起置顶 = true;

    [Header("运行中状态（只读展示，便于调试）")]
    [Tooltip("物品当前速度，未处于长按状态时会被归零")]
    public Vector2 当前速度;

    /// <summary>由 发射飞行体 写入：物品正在飞行，表现层要让位给飞行，不要抢缩放与旋转</summary>
    [HideInInspector] public bool 飞行中;

    /// <summary>
    /// 是否正在拖拽：<see cref="确认拖拽"/> 置 true，松手（<see cref="结束拖拽"/>）置回 false。
    /// 不要把它当成「本次按下曾经变成过拖拽」的锁存标志——更新表现 用它决定目标缩放是
    /// 「放大后」还是「原始大小」，一锁存物品松手后就会永远停在放大状态。
    /// 松手当帧的误触发由 长按脚本.被拖拽屏蔽 单独挡着，不依赖这个标志。
    /// </summary>
    public bool 正在拖拽 { get; private set; }

    /// <summary>供 LongPressHandler 查询：本次按下已变成拖拽，需屏蔽原有的点击/长按触发</summary>
    public bool 屏蔽长按事件 => 正在拖拽;

    private RectTransform 自身;
    private RectTransform 参考平面;
    private LongPressHandler 长按脚本;

    private Camera 事件相机;
    private Vector2 按下屏幕坐标;
    private Vector2 目标屏幕坐标;
    private Vector3 抓取偏移;
    private Vector3 原始缩放;
    private Quaternion 原始旋转 = Quaternion.identity;
    private int 原层级 = -1;

    /// <summary>
    /// 表现层是否还在过渡中。只有为 true 时才写 localScale / localRotation：
    /// 静止的物品绝不能每帧去写 transform，否则会和 源后坐力 抢同一个 localScale，
    /// 把来源按钮的按压回弹按没。
    /// </summary>
    private bool 表现过渡中;

    private void Awake()
    {
        自身 = GetComponent<RectTransform>();

        // 用于把屏幕坐标换算到物品所在的平面
        参考平面 = 自身.parent as RectTransform;
        if (参考平面 == null) 参考平面 = 自身;

        长按脚本 = GetComponent<LongPressHandler>();
        原始缩放 = 自身.localScale;
        原始旋转 = 自身.localRotation;
    }

    private void OnDisable()
    {
        // 兜底：物体被隐藏或销毁时清掉状态，避免残留
        是否处于长按状态 = false;
        正在拖拽 = false;
        当前速度 = Vector2.zero;
        原层级 = -1;
        表现过渡中 = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 只有左键参与拖拽；右键中途按下时不打断正在进行的左键拖拽
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 新的一次左键按下，先清空上一次的拖拽锁存与屏蔽，
        // 这样「原地短按」才能照旧走 LongPressHandler 的原有逻辑
        正在拖拽 = false;
        if (长按脚本 != null) 长按脚本.被拖拽屏蔽 = false;

        if (!是否可被移动) return;

        是否处于长按状态 = true;
        当前速度 = Vector2.zero;
        事件相机 = eventData.pressEventCamera;
        按下屏幕坐标 = eventData.position;
        目标屏幕坐标 = eventData.position;

        // 基准缩放 / 旋转只在物品完全静止时记录：
        // 飞行途中、以及上一次拖拽的放大还没收敛完的时候，当前缩放都是过渡值，
        // 拿它当基准会让物品越拖越大（连点两下就缩不回去了）
        if (!飞行中 && !表现过渡中)
        {
            原始缩放 = 自身.localScale;
            原始旋转 = 自身.localRotation;
        }

        // 记录光标与物品的世界坐标偏移，避免按下瞬间物品跳到光标正中
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                参考平面, eventData.position, 事件相机, out Vector3 光标世界点))
        {
            抓取偏移 = 自身.position - 光标世界点;
        }
        else
        {
            抓取偏移 = Vector3.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!是否处于长按状态) return;

        目标屏幕坐标 = eventData.position;

        // 移动超过判定距离才确认拖拽，避免原地点击时物品跟着手抖乱动
        if (!正在拖拽 && Vector2.Distance(eventData.position, 按下屏幕坐标) >= 拖拽判定距离)
        {
            确认拖拽();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (!是否处于长按状态) return;

        结束拖拽();
    }

    private void Update()
    {
        // 可移动状态被中途关闭时立即结束拖拽
        if (是否处于长按状态 && !是否可被移动)
        {
            结束拖拽();
        }

        跟随光标();
        更新表现();
    }

    private void 跟随光标()
    {
        if (!是否处于长按状态 || !正在拖拽) return;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                参考平面, 目标屏幕坐标, 事件相机, out Vector3 目标世界点))
        {
            return;
        }

        Vector3 旧位置 = 自身.position;

        // 跟随速度决定每帧向光标插值的比例。
        // Clamp01 保证永远不会越过目标点（也就不会把物品甩出屏幕）。
        float 插值 = Mathf.Clamp01(跟随速度 * Time.deltaTime);
        自身.position = Vector3.Lerp(旧位置, 目标世界点 + 抓取偏移, 插值);

        当前速度 = (自身.position - 旧位置) / Mathf.Max(Time.deltaTime, 0.0001f);
    }

    /// <summary>
    /// 放大 / 回正 / 倾斜都走这里插值，松手后也继续跑，所以缩放和角度是平滑收敛的，
    /// 不会在一帧里从放大直接弹回原始大小。
    /// </summary>
    private void 更新表现()
    {
        // 飞行中让位给 发射飞行体，两边不要抢同一个 localScale / localRotation
        if (飞行中) return;

        // 没在过渡就别碰 transform（见 表现过渡中 的说明）
        if (!表现过渡中) return;

        // 用 unscaledDeltaTime：即使游戏暂停（timeScale = 0）表现也照常收敛
        float 步长 = 表现过渡时长 > 0f ? Mathf.Clamp01(Time.unscaledDeltaTime / 表现过渡时长) : 1f;

        // 目标缩放：拖拽中按放大倍数放大，其余时候回到原始缩放
        Vector3 目标缩放 = 正在拖拽 ? 原始缩放 * (1f + 放大倍数) : 原始缩放;

        // 目标角度：拖拽中按横向速度倾斜（停下就自然回正），松手后回到原始旋转
        float 目标角度 = 0f;
        if (正在拖拽 && 倾斜角度 > 0f)
        {
            目标角度 = Mathf.Clamp(当前速度.x * 倾斜灵敏度, -倾斜角度, 倾斜角度);
        }
        Quaternion 目标旋转 = 原始旋转 * Quaternion.Euler(0f, 0f, 目标角度);

        if (步长 >= 1f)
        {
            自身.localScale = 目标缩放;
            自身.localRotation = 目标旋转;
            表现过渡中 = 正在拖拽;
            return;
        }

        自身.localScale = Vector3.Lerp(自身.localScale, 目标缩放, 步长);
        自身.localRotation = Quaternion.Slerp(自身.localRotation, 目标旋转, 步长);

        // 收敛到精确值，避免留下浮点残差
        bool 缩放到位 = (自身.localScale - 目标缩放).sqrMagnitude < 1e-6f;
        bool 旋转到位 = Quaternion.Angle(自身.localRotation, 目标旋转) < 0.05f;
        if (缩放到位) 自身.localScale = 目标缩放;
        if (旋转到位) 自身.localRotation = 目标旋转;

        // 拖拽中不停手：倾斜角每帧都跟着速度变，停了就等于不倾斜了。
        // 松手后才允许收敛结束，把 transform 让回给别的脚本。
        if (!正在拖拽 && 缩放到位 && 旋转到位) 表现过渡中 = false;
    }

    private void 确认拖拽()
    {
        正在拖拽 = true;
        表现过渡中 = true;

        // 表现与反馈：确认拖拽时按放大倍数放大（真正的插值在 更新表现 里做）
        // 置顶：被拖的钞票 / 商品不该被后来生成的其他物品盖住
        if (拖起置顶 && 自身.parent != null)
        {
            原层级 = 自身.GetSiblingIndex();
            自身.SetAsLastSibling();
        }

        // 通知原有的长按脚本：本次按下已变成拖拽，不要再触发点击/连续触发
        if (长按脚本 != null) 长按脚本.被拖拽屏蔽 = true;
    }

    private void 结束拖拽()
    {
        是否处于长按状态 = false;

        // 异常处理：未处于长按状态 → 速度归零，物品停在原地，不会被甩出屏幕
        当前速度 = Vector2.zero;

        // 恢复原来的层级（拖拽期间可能有别的物品被销毁，索引要夹一下防止越界）
        if (原层级 >= 0)
        {
            int 最大层级 = 自身.parent != null ? 自身.parent.childCount - 1 : 0;
            自身.SetSiblingIndex(Mathf.Clamp(原层级, 0, Mathf.Max(0, 最大层级)));
            原层级 = -1;
        }

        // ★ 松手 = 不再是「正在拖拽」。这里必须在松手时就清掉：
        //   更新表现 拿 正在拖拽 决定目标缩放是「放大后」还是「原始大小」，
        //   要是留着不清（旧写法是锁存到下次按下），物品松手后就会一直停在放大状态。
        if (正在拖拽)
        {
            正在拖拽 = false;
            // 缩放 / 旋转不在这里直接还原：交给 更新表现 平滑收敛回原始值，
            // 这样松手是「缩回去」而不是「一帧弹回去」。
            表现过渡中 = true;
        }

        // 这里故意不解除 长按脚本.被拖拽屏蔽：
        // 同一帧内 LongPressHandler.OnPointerUp 仍可能后于本方法执行并补发一次事件。
        // 屏蔽统一留到下一次 OnPointerDown 时解除。
    }
}
