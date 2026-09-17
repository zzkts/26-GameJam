using UnityEngine;

/// <summary>
/// 发射源标记（可选）：挂在可点击物体或其父节点上。
///
/// 它不实现任何点击接口，所以不会抢掉原有的点击 / 长按 / 拖拽逻辑，
/// 只是告诉 <see cref="发射动画"/>：这个按钮的发射起点在哪、手感要不要和全局不一样。
///
/// 例：想让「购买商品」处的猪与猫从贴图本身飞出去，就把本组件挂在按钮上，
/// 再把「起点锚点」拖到猪 / 猫的贴图子物体上。
/// </summary>
[DisallowMultipleComponent]
public class 发射源 : MonoBehaviour
{
    [Tooltip("发射起点：留空则使用本物体自身的位置（可以指向猪、猫这类贴图子物体）")]
    public Transform 起点锚点;

    [Tooltip("本次发射的飞行时长倍率，1 = 使用全局设置，2 = 慢一倍")]
    public float 时长倍率 = 1f;

    [Tooltip("本次发射的曲线弧度倍率，1 = 使用全局设置，0 = 直线飞过去")]
    public float 弧度倍率 = 1f;

    [Tooltip("覆盖全局的弧线方向")]
    public bool 覆盖弧线方向;

    [Tooltip("勾选「覆盖弧线方向」后生效")]
    public 发射动画.弧线方向 弧线方向 = 发射动画.弧线方向.上;
}
