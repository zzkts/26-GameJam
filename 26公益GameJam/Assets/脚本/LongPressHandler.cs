using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class LongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("长按参数")]
    public float holdTime = 1f;
    public float repeatInterval = 0.1f;

    [Header("左键事件（短按触发1次 / 长按自动连续触发）")]
    public UnityEvent onLeftPress;

    [Header("右键事件（短按触发1次 / 长按自动连续触发）")]
    public UnityEvent onRightPress;

    private Coroutine longPressCoroutine;
    private bool isPressed;
    private bool longPressTriggered;
    private PointerEventData.InputButton pressedButton;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("收到按下: " + eventData.button);
        var btn = eventData.button;
        if (btn != PointerEventData.InputButton.Left && btn != PointerEventData.InputButton.Right) return;

        // 【核心修复】只停协程，千万别调用 StopLongPress()，否则 isPressed 会被设成 false！
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }

        isPressed = true;
        longPressTriggered = false;
        pressedButton = btn;

        longPressCoroutine = StartCoroutine(LongPressRoutine(btn));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;

        // 如果没触发过长按，说明是短按（手动），松开时触发一次
        if (!longPressTriggered)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Debug.Log("触发左键短按 Invoke");
                onLeftPress?.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                Debug.Log("触发右键短按 Invoke");
                onRightPress?.Invoke();
            }
        }

        StopLongPress();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 移出按钮时，只停止长按协程，不重置 isPressed，保证松手时还能触发短按
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private void StopLongPress()
    {
        isPressed = false;
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private IEnumerator LongPressRoutine(PointerEventData.InputButton btn)
    {
        yield return new WaitForSecondsRealtime(holdTime);
        if (!isPressed || pressedButton != btn) yield break;

        longPressTriggered = true;

        // 长按到达时间，开始自动连续触发
        while (isPressed && pressedButton == btn)
        {
            if (btn == PointerEventData.InputButton.Left)
            {
                Debug.Log("触发左键长按 Invoke");
                onLeftPress?.Invoke();
            }
            else if (btn == PointerEventData.InputButton.Right)
            {
                Debug.Log("触发右键长按 Invoke");
                onRightPress?.Invoke();
            }

            yield return new WaitForSecondsRealtime(repeatInterval);
        }
    }

    private void OnDisable() => StopLongPress();
}
