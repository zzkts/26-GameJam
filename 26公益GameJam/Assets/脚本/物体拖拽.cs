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

    [Header("运行中状态（只读展示，便于调试）")]
    [Tooltip("物品当前速度，未处于长按状态时会被归零")]
    public Vector2 当前速度;

    /// <summary>
    /// 是否已确认进入拖拽。本次按下超过判定距离后锁存为 true，
    /// 松手后仍保持 true，直到下一次左键按下才清零（用于屏蔽松手瞬间的误触发）。
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

    private void Awake()
    {
        自身 = GetComponent<RectTransform>();

        // 用于把屏幕坐标换算到物品所在的平面
        参考平面 = 自身.parent as RectTransform;
        if (参考平面 == null) 参考平面 = 自身;

        长按脚本 = GetComponent<LongPressHandler>();
        原始缩放 = 自身.localScale;
    }

    private void OnDisable()
    {
        // 兜底：物体被隐藏或销毁时清掉状态，避免残留
        是否处于长按状态 = false;
        正在拖拽 = false;
        当前速度 = Vector2.zero;
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
        原始缩放 = 自身.localScale;

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
            return;
        }

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

    private void 确认拖拽()
    {
        正在拖拽 = true;

        // 表现与反馈：确认拖拽时按放大倍数放大
        自身.localScale = 原始缩放 * (1f + 放大倍数);

        // 通知原有的长按脚本：本次按下已变成拖拽，不要再触发点击/连续触发
        if (长按脚本 != null) 长按脚本.被拖拽屏蔽 = true;
    }

    private void 结束拖拽()
    {
        是否处于长按状态 = false;

        // 异常处理：未处于长按状态 → 速度归零，物品停在原地，不会被甩出屏幕
        当前速度 = Vector2.zero;

        if (正在拖拽)
        {
            自身.localScale = 原始缩放;
        }

        // 这里故意不解除 长按脚本.被拖拽屏蔽：
        // 同一帧内 LongPressHandler.OnPointerUp 仍可能后于本方法执行并补发一次事件。
        // 屏蔽统一留到下一次 OnPointerDown 时解除。
    }
}
