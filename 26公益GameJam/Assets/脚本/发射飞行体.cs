using UnityEngine;

/// <summary>
/// 挂在飞行中的预制体上，自己驱动贝塞尔飞行。
/// 飞行途中物件被销毁（取消交易 / 结账清场）时组件一起消失，不需要外部管理协程。
/// 只由 <see cref="发射动画"/> 在运行时挂载，不需要手动添加。
/// </summary>
[DisallowMultipleComponent]
public class 发射飞行体 : MonoBehaviour
{
    private RectTransform 自身;
    private Vector3 原始缩放;
    private Quaternion 原始旋转;

    private Vector2 起点, 终点, 控制点1, 控制点2;
    private float 延迟, 时长, 已用时;
    private float 起始缩放倍率, 末端缩放倍率, 旋转圈数;
    private AnimationCurve 缓动曲线;

    // 落地收尾（压扁回弹）用的状态
    private float 落地压扁, 落地回弹时长, 落地已用时;
    private bool 落地收尾中;

    private CanvasGroup 遮罩;
    private bool 遮罩为自建;
    private bool 遮罩原有可点击 = true;
    private 物体拖拽 拖拽脚本;

    /// <summary>是否正在飞行。</summary>
    public bool 飞行中状态 { get; private set; }

    public void 初始化(发射动画.飞行参数 参数)
    {
        自身 = transform as RectTransform;
        if (自身 == null)
        {
            Destroy(this);
            return;
        }

        起点 = 参数.起点;
        终点 = 参数.终点;
        延迟 = Mathf.Max(0f, 参数.延迟);
        时长 = Mathf.Max(0.01f, 参数.时长);
        起始缩放倍率 = 参数.起始缩放倍率;
        末端缩放倍率 = 参数.末端缩放倍率;
        旋转圈数 = 参数.旋转圈数;
        缓动曲线 = 参数.缓动曲线;
        原始缩放 = 参数.原始缩放;
        原始旋转 = 参数.原始旋转;
        落地压扁 = 参数.落地压扁;
        落地回弹时长 = 参数.落地回弹时长;
        落地收尾中 = false;
        已用时 = 0f;

        // 二次贝塞尔的两段控制点：先按起终点连线插值，再沿指定方向抬起
        控制点1 = Vector2.Lerp(起点, 终点, 参数.控制点位置A) + 参数.弧线方向向量 * 参数.弧度;
        控制点2 = Vector2.Lerp(起点, 终点, 参数.控制点位置B) + 参数.弧线方向向量 * 参数.弧度 * 参数.弧度衰减;

        拖拽脚本 = GetComponent<物体拖拽>();
        // 告诉 物体拖拽：这段时间的缩放 / 旋转归飞行管，别抢
        if (拖拽脚本 != null) 拖拽脚本.飞行中 = true;

        if (参数.飞行中禁止点击)
        {
            遮罩 = GetComponent<CanvasGroup>();
            if (遮罩 == null)
            {
                遮罩 = gameObject.AddComponent<CanvasGroup>();
                遮罩为自建 = true;
                遮罩原有可点击 = true;
            }
            else
            {
                遮罩原有可点击 = 遮罩.blocksRaycasts;
            }
            遮罩.blocksRaycasts = false;
        }

        飞行中状态 = true;
    }

    private void Update()
    {
        if (落地收尾中)
        {
            更新落地收尾();
            return;
        }

        if (!飞行中状态) return;

        // 被玩家抓起来拖走 → 让位给拖拽，飞行提前收尾
        if (拖拽脚本 != null && (拖拽脚本.正在拖拽 || 拖拽脚本.是否处于长按状态))
        {
            结束飞行(false);
            return;
        }

        已用时 += Time.unscaledDeltaTime;

        float 飞行用时间 = 已用时 - 延迟;
        if (飞行用时间 < 0f)
        {
            // 排队等待起飞：先停在起点
            自身.anchoredPosition = 起点;
            return;
        }

        float 进度 = Mathf.Clamp01(飞行用时间 / 时长);
        // 缓动曲线可以返回大于 1 的值，用来做过冲回弹，所以这里不 Clamp
        float 缓动进度 = 缓动曲线 != null ? 缓动曲线.Evaluate(进度) : 进度;

        自身.anchoredPosition = 贝塞尔(起点, 控制点1, 控制点2, 终点, 缓动进度);
        自身.localScale = 原始缩放 * Mathf.Lerp(起始缩放倍率, 末端缩放倍率, Mathf.Clamp01(缓动进度));
        if (旋转圈数 != 0f)
        {
            自身.localRotation = 原始旋转 * Quaternion.Euler(0f, 0f, 360f * 旋转圈数 * 缓动进度);
        }

        if (进度 >= 1f) 结束飞行(true);
    }

    private void 结束飞行(bool 回到终点)
    {
        飞行中状态 = false;

        if (自身 != null)
        {
            if (回到终点) 自身.anchoredPosition = 终点;
            // 正在被拖拽时缩放交给 物体拖拽 管，这里不要抢
            if (拖拽脚本 == null || !拖拽脚本.正在拖拽) 自身.localScale = 原始缩放;
            自身.localRotation = 原始旋转;
        }

        if (遮罩 != null)
        {
            遮罩.blocksRaycasts = 遮罩原有可点击;
            if (遮罩为自建) Destroy(遮罩);
            遮罩 = null;
        }

        // 落地收尾：压扁一点再弹回原大小，补上「落在桌面上」的重量感。
        // 被玩家在半空中接走时不播，免得和拖拽抢缩放。
        if (回到终点 && 自身 != null && 落地压扁 > 0f && 落地回弹时长 > 0f &&
            (拖拽脚本 == null || !拖拽脚本.正在拖拽))
        {
            // 这里故意不解除 拖拽脚本.飞行中：压扁期间还得靠它挡住
            // 物体拖拽.更新表现，否则压下去的那点缩放每帧都会被插值拉回原大小。
            落地收尾中 = true;
            落地已用时 = 0f;
            return;
        }

        // 没播收尾（或被半空接走）→ 直接把缩放 / 旋转交回 物体拖拽
        交出表现权();
    }

    /// <summary>落地后的压扁回弹：sin 曲线 0 → 1 → 0，和 源后坐力 的做法一致。</summary>
    private void 更新落地收尾()
    {
        // 收尾期间被玩家抓起来 → 立刻收手，把缩放交回 物体拖拽
        if (拖拽脚本 != null && (拖拽脚本.正在拖拽 || 拖拽脚本.是否处于长按状态))
        {
            自身.localScale = 原始缩放;
            交出表现权();
            return;
        }

        落地已用时 += Time.unscaledDeltaTime;

        float 进度 = 落地回弹时长 <= 0f ? 1f : Mathf.Clamp01(落地已用时 / 落地回弹时长);
        自身.localScale = 原始缩放 * (1f - 落地压扁 * Mathf.Sin(进度 * Mathf.PI));

        if (进度 >= 1f)
        {
            自身.localScale = 原始缩放;
            交出表现权();
        }
    }

    /// <summary>收尾结束：解除 物体拖拽 的飞行中标记，把自己摘掉。</summary>
    private void 交出表现权()
    {
        if (拖拽脚本 != null) 拖拽脚本.飞行中 = false;
        Destroy(this);
    }

    private static Vector2 贝塞尔(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;
        return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
    }
}
