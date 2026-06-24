using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Lightweight coroutine-based tweening — drop-in for the subset of DOTween used here.
public static class Tween
{
    public enum Ease { Linear, OutQuad, OutBack, OutBounce, InQuad }

    // ── Float ─────────────────────────────────────────────────────────────────

    public static Coroutine To(MonoBehaviour host, Func<float> getter,
        Action<float> setter, float target, float duration,
        Ease ease = Ease.OutQuad, float delay = 0f)
    {
        return host.StartCoroutine(FloatRoutine(getter, setter, target, duration, ease, delay));
    }

    static IEnumerator FloatRoutine(Func<float> getter, Action<float> setter,
        float target, float duration, Ease ease, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float start = getter();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            setter(Mathf.LerpUnclamped(start, target, Evaluate(ease, elapsed / duration)));
            yield return null;
        }
        setter(target);
    }

    // ── Scale ─────────────────────────────────────────────────────────────────

    public static Coroutine Scale(MonoBehaviour host, Transform t,
        Vector3 target, float duration, Ease ease = Ease.OutQuad, float delay = 0f)
    {
        return host.StartCoroutine(ScaleRoutine(t, target, duration, ease, delay));
    }

    static IEnumerator ScaleRoutine(Transform t, Vector3 target,
        float duration, Ease ease, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        Vector3 start = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (t == null) yield break;
            t.localScale = Vector3.LerpUnclamped(start, target, Evaluate(ease, elapsed / duration));
            yield return null;
        }
        if (t != null) t.localScale = target;
    }

    // ── Punch Scale ───────────────────────────────────────────────────────────

    public static Coroutine PunchScale(MonoBehaviour host, Transform t,
        Vector3 punch, float duration)
    {
        return host.StartCoroutine(PunchScaleRoutine(t, punch, duration));
    }

    static IEnumerator PunchScaleRoutine(Transform t, Vector3 punch, float duration)
    {
        Vector3 origin = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = elapsed / duration;
            float wave = Mathf.Sin(p * Mathf.PI) * (1f - p);
            if (t == null) yield break;
            t.localScale = origin + punch * wave;
            yield return null;
        }
        if (t != null) t.localScale = origin;
    }

    // ── Shake Position ────────────────────────────────────────────────────────

    public static Coroutine ShakePosition(MonoBehaviour host, Transform t,
        float duration, float strength)
    {
        return host.StartCoroutine(ShakeRoutine(t, duration, strength));
    }

    static IEnumerator ShakeRoutine(Transform t, float duration, float strength)
    {
        Vector3 origin = t.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - elapsed / duration;
            if (t == null) yield break;
            t.localPosition = origin + UnityEngine.Random.insideUnitSphere * strength * decay;
            yield return null;
        }
        if (t != null) t.localPosition = origin;
    }

    // ── AnchoredPosition ──────────────────────────────────────────────────────

    public static Coroutine AnchorPos(MonoBehaviour host, RectTransform rt,
        Vector2 target, float duration, Ease ease = Ease.OutBack, float delay = 0f)
    {
        return host.StartCoroutine(AnchorPosRoutine(rt, target, duration, ease, delay));
    }

    static IEnumerator AnchorPosRoutine(RectTransform rt, Vector2 target,
        float duration, Ease ease, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        Vector2 start = rt.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (rt == null) yield break;
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target,
                Evaluate(ease, elapsed / duration));
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = target;
    }

    // ── Colour ────────────────────────────────────────────────────────────────

    public static Coroutine Color(MonoBehaviour host, Graphic g,
        UnityEngine.Color target, float duration, float delay = 0f)
    {
        return host.StartCoroutine(ColorRoutine(g, target, duration, delay));
    }

    static IEnumerator ColorRoutine(Graphic g, UnityEngine.Color target,
        float duration, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        UnityEngine.Color start = g.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (g == null) yield break;
            g.color = UnityEngine.Color.LerpUnclamped(start, target, elapsed / duration);
            yield return null;
        }
        if (g != null) g.color = target;
    }

    // ── Counter (numeric text) ────────────────────────────────────────────────

    public static Coroutine Counter(MonoBehaviour host, Text text,
        float from, float to, float duration, string suffix = "")
    {
        return host.StartCoroutine(CounterRoutine(text, from, to, duration, suffix));
    }

    static IEnumerator CounterRoutine(Text text, float from, float to,
        float duration, string suffix)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (text == null) yield break;
            float v = Mathf.LerpUnclamped(from, to, Evaluate(Ease.OutQuad, elapsed / duration));
            text.text = Mathf.RoundToInt(v) + suffix;
            yield return null;
        }
        if (text != null) text.text = Mathf.RoundToInt(to) + suffix;
    }

    // ── Easing ────────────────────────────────────────────────────────────────

    static float Evaluate(Ease ease, float t)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case Ease.Linear:    return t;
            case Ease.OutQuad:   return 1f - (1f - t) * (1f - t);
            case Ease.InQuad:    return t * t;
            case Ease.OutBack:
            {
                float c1 = 1.70158f, c3 = c1 + 1f;
                return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            }
            case Ease.OutBounce:
            {
                float n1 = 7.5625f, d1 = 2.75f;
                if (t < 1f / d1)      return n1 * t * t;
                if (t < 2f / d1)    { t -= 1.5f   / d1; return n1 * t * t + 0.75f; }
                if (t < 2.5f / d1)  { t -= 2.25f  / d1; return n1 * t * t + 0.9375f; }
                                      t -= 2.625f  / d1; return n1 * t * t + 0.984375f;
            }
            default: return t;
        }
    }
}
