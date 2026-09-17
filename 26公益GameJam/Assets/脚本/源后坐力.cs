using UnityEngine;

/// <summary>
/// 发射时给来源物体加一次「按压回弹」，播完自动移除。
/// 只由 <see cref="发射动画"/> 在运行时挂载，不需要手动添加。
/// </summary>
[DisallowMultipleComponent]
public class 源后坐力 : MonoBehaviour
{
    [HideInInspector] public Vector3 基准缩放 = Vector3.one;
    [HideInInspector] public float 幅度 = 0.12f;
    [HideInInspector] public float 时长 = 0.12f;

    private float 已用时;

    /// <summary>连续发射时重播动画，而不是再挂一个组件。</summary>
    public void 重播()
    {
        已用时 = 0f;
    }

    private void Update()
    {
        已用时 += Time.unscaledDeltaTime;

        float 进度 = 时长 <= 0f ? 1f : Mathf.Clamp01(已用时 / 时长);
        // sin 曲线：0 → 1 → 0，先被按下去再弹回来
        transform.localScale = 基准缩放 * (1f - 幅度 * Mathf.Sin(进度 * Mathf.PI));

        if (进度 >= 1f)
        {
            transform.localScale = 基准缩放;
            Destroy(this);
        }
    }
}
