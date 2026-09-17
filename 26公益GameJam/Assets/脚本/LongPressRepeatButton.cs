using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 必须有这个
using UnityEngine.Events;
using UnityEngine.EventSystems;

// 强制指定继承自 UGUI 的 Button
public class LongPressRepeatButton : UnityEngine.UI.Button
{
    public float holdTime = 1f;
    public float repeatInterval = 0.1f;
    public UnityEvent onLeftLongPressStart;

    protected override void OnEnable()
    {
        base.OnEnable();
    }
}
