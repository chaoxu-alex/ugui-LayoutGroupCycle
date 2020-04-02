using System;
using UnityEngine;

public static class Easy
{
    static private Func<float, float, float, float>[] m_TweenFuncMap = null;
    /// <summary>
    /// The easing type
    /// </summary>
    [Serializable]
    public enum TweenType
    {
        Instant,
        Linear,
        Spring,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseInQuint,
        EaseOutQuint,
        EaseInOutQuint,
        EaseInSine,
        EaseOutSine,
        EaseInOutSine,
        EaseInExpo,
        EaseOutExpo,
        EaseInOutExpo,
        EaseInCirc,
        EaseOutCirc,
        EaseInOutCirc,
        EaseInBounce,
        EaseOutBounce,
        EaseInOutBounce,
        EaseInBack,
        EaseOutBack,
        EaseInOutBack,
        EaseInElastic,
        EaseOutElastic,
        EaseInOutElastic
    }

    public static float Tween(TweenType tweenType, float start, float end, float progress)
    {
        if (m_TweenFuncMap == null)
        {
            m_TweenFuncMap = new Func<float, float, float, float>[(int)TweenType.EaseInOutElastic + 1];
            m_TweenFuncMap[(int)TweenType.Instant] = Instant;
            m_TweenFuncMap[(int)TweenType.Linear] = Linear;
            m_TweenFuncMap[(int)TweenType.Spring] = Spring;
            m_TweenFuncMap[(int)TweenType.EaseInQuad] = EaseInQuad;
            m_TweenFuncMap[(int)TweenType.EaseOutQuad] = EaseOutQuad;
            m_TweenFuncMap[(int)TweenType.EaseInOutQuad] = EaseInOutQuad;
            m_TweenFuncMap[(int)TweenType.EaseInCubic] = EaseInCubic;
            m_TweenFuncMap[(int)TweenType.EaseOutCubic] = EaseOutCubic;
            m_TweenFuncMap[(int)TweenType.EaseInOutCubic] = EaseInOutCubic;
            m_TweenFuncMap[(int)TweenType.EaseInQuart] = EaseInQuart;
            m_TweenFuncMap[(int)TweenType.EaseOutQuart] = EaseOutQuart;
            m_TweenFuncMap[(int)TweenType.EaseInOutQuart] = EaseInOutQuart;
            m_TweenFuncMap[(int)TweenType.EaseInQuint] = EaseInQuint;
            m_TweenFuncMap[(int)TweenType.EaseOutQuint] = EaseOutQuint;
            m_TweenFuncMap[(int)TweenType.EaseInOutQuint] = EaseInOutQuint;
            m_TweenFuncMap[(int)TweenType.EaseInSine] = EaseInSine;
            m_TweenFuncMap[(int)TweenType.EaseOutSine] = EaseOutSine;
            m_TweenFuncMap[(int)TweenType.EaseInOutSine] = EaseInOutSine;
            m_TweenFuncMap[(int)TweenType.EaseInExpo] = EaseInExpo;
            m_TweenFuncMap[(int)TweenType.EaseOutExpo] = EaseOutExpo;
            m_TweenFuncMap[(int)TweenType.EaseInOutExpo] = EaseInOutExpo;
            m_TweenFuncMap[(int)TweenType.EaseInCirc] = EaseInCirc;
            m_TweenFuncMap[(int)TweenType.EaseOutCirc] = EaseOutCirc;
            m_TweenFuncMap[(int)TweenType.EaseInOutCirc] = EaseInOutCirc;
            m_TweenFuncMap[(int)TweenType.EaseInBounce] = EaseInBounce;
            m_TweenFuncMap[(int)TweenType.EaseOutBounce] = EaseOutBounce;
            m_TweenFuncMap[(int)TweenType.EaseInOutBounce] = EaseInOutBounce;
            m_TweenFuncMap[(int)TweenType.EaseInBack] = EaseInBack;
            m_TweenFuncMap[(int)TweenType.EaseOutBack] = EaseOutBack;
            m_TweenFuncMap[(int)TweenType.EaseInOutBack] = EaseInOutBack;
            m_TweenFuncMap[(int)TweenType.EaseInElastic] = EaseInElastic;
            m_TweenFuncMap[(int)TweenType.EaseOutElastic] = EaseOutElastic;
            m_TweenFuncMap[(int)TweenType.EaseInOutElastic] = EaseInOutElastic;
        }

        return m_TweenFuncMap[(int)tweenType](start, end, progress);
    }

    private static float Instant(float start, float end, float val)
    {
        return end;
    }

    private static float Linear(float start, float end, float val)
    {
        return Mathf.Lerp(start, end, val);
    }

    private static float Spring(float start, float end, float val)
    {
        val = Mathf.Clamp01(val);
        val = (Mathf.Sin(val * Mathf.PI * (0.2f + 2.5f * val * val * val)) * Mathf.Pow(1f - val, 2.2f) + val) * (1f + (1.2f * (1f - val)));
        return start + (end - start) * val;
    }

    private static float EaseInQuad(float start, float end, float val)
    {
        end -= start;
        return end * val * val + start;
    }

    private static float EaseOutQuad(float start, float end, float val)
    {
        end -= start;
        return -end * val * (val - 2) + start;
    }

    private static float EaseInOutQuad(float start, float end, float val)
    {
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val + start;
        val--;
        return -end / 2 * (val * (val - 2) - 1) + start;
    }

    private static float EaseInCubic(float start, float end, float val)
    {
        end -= start;
        return end * val * val * val + start;
    }

    private static float EaseOutCubic(float start, float end, float val)
    {
        val--;
        end -= start;
        return end * (val * val * val + 1) + start;
    }

    private static float EaseInOutCubic(float start, float end, float val)
    {
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val * val + start;
        val -= 2;
        return end / 2 * (val * val * val + 2) + start;
    }

    private static float EaseInQuart(float start, float end, float val)
    {
        end -= start;
        return end * val * val * val * val + start;
    }

    private static float EaseOutQuart(float start, float end, float val)
    {
        val--;
        end -= start;
        return -end * (val * val * val * val - 1) + start;
    }

    private static float EaseInOutQuart(float start, float end, float val)
    {
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val * val * val + start;
        val -= 2;
        return -end / 2 * (val * val * val * val - 2) + start;
    }

    private static float EaseInQuint(float start, float end, float val)
    {
        end -= start;
        return end * val * val * val * val * val + start;
    }

    private static float EaseOutQuint(float start, float end, float val)
    {
        val--;
        end -= start;
        return end * (val * val * val * val * val + 1) + start;
    }

    private static float EaseInOutQuint(float start, float end, float val)
    {
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * val * val * val * val * val + start;
        val -= 2;
        return end / 2 * (val * val * val * val * val + 2) + start;
    }

    private static float EaseInSine(float start, float end, float val)
    {
        end -= start;
        return -end * Mathf.Cos(val / 1 * (Mathf.PI / 2)) + end + start;
    }

    private static float EaseOutSine(float start, float end, float val)
    {
        end -= start;
        return end * Mathf.Sin(val / 1 * (Mathf.PI / 2)) + start;
    }

    private static float EaseInOutSine(float start, float end, float val)
    {
        end -= start;
        return -end / 2 * (Mathf.Cos(Mathf.PI * val / 1) - 1) + start;
    }

    private static float EaseInExpo(float start, float end, float val)
    {
        end -= start;
        return end * Mathf.Pow(2, 10 * (val / 1 - 1)) + start;
    }

    private static float EaseOutExpo(float start, float end, float val)
    {
        end -= start;
        return end * (-Mathf.Pow(2, -10 * val / 1) + 1) + start;
    }

    private static float EaseInOutExpo(float start, float end, float val)
    {
        val /= .5f;
        end -= start;
        if (val < 1) return end / 2 * Mathf.Pow(2, 10 * (val - 1)) + start;
        val--;
        return end / 2 * (-Mathf.Pow(2, -10 * val) + 2) + start;
    }

    private static float EaseInCirc(float start, float end, float val)
    {
        end -= start;
        return -end * (Mathf.Sqrt(1 - val * val) - 1) + start;
    }

    private static float EaseOutCirc(float start, float end, float val)
    {
        val--;
        end -= start;
        return end * Mathf.Sqrt(1 - val * val) + start;
    }

    private static float EaseInOutCirc(float start, float end, float val)
    {
        val /= .5f;
        end -= start;
        if (val < 1) return -end / 2 * (Mathf.Sqrt(1 - val * val) - 1) + start;
        val -= 2;
        return end / 2 * (Mathf.Sqrt(1 - val * val) + 1) + start;
    }

    private static float EaseInBounce(float start, float end, float val)
    {
        end -= start;
        float d = 1f;
        return end - EaseOutBounce(0, end, d - val) + start;
    }

    private static float EaseOutBounce(float start, float end, float val)
    {
        val /= 1f;
        end -= start;
        if (val < (1 / 2.75f))
        {
            return end * (7.5625f * val * val) + start;
        }
        else if (val < (2 / 2.75f))
        {
            val -= (1.5f / 2.75f);
            return end * (7.5625f * (val) * val + .75f) + start;
        }
        else if (val < (2.5 / 2.75))
        {
            val -= (2.25f / 2.75f);
            return end * (7.5625f * (val) * val + .9375f) + start;
        }
        else
        {
            val -= (2.625f / 2.75f);
            return end * (7.5625f * (val) * val + .984375f) + start;
        }
    }

    private static float EaseInOutBounce(float start, float end, float val)
    {
        end -= start;
        float d = 1f;
        if (val < d / 2) return EaseInBounce(0, end, val * 2) * 0.5f + start;
        else return EaseOutBounce(0, end, val * 2 - d) * 0.5f + end * 0.5f + start;
    }

    private static float EaseInBack(float start, float end, float val)
    {
        end -= start;
        val /= 1;
        float s = 1.70158f;
        return end * (val) * val * ((s + 1) * val - s) + start;
    }

    private static float EaseOutBack(float start, float end, float val)
    {
        float s = 1.70158f;
        end -= start;
        val = (val / 1) - 1;
        return end * ((val) * val * ((s + 1) * val + s) + 1) + start;
    }

    private static float EaseInOutBack(float start, float end, float val)
    {
        float s = 1.70158f;
        end -= start;
        val /= .5f;
        if ((val) < 1)
        {
            s *= (1.525f);
            return end / 2 * (val * val * (((s) + 1) * val - s)) + start;
        }
        val -= 2;
        s *= (1.525f);
        return end / 2 * ((val) * val * (((s) + 1) * val + s) + 2) + start;
    }

    private static float EaseInElastic(float start, float end, float val)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s = 0;
        float a = 0;

        if (val == 0) return start;
        val = val / d;
        if (val == 1) return start + end;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p / 4;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }
        val = val - 1;
        return -(a * Mathf.Pow(2, 10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p)) + start;
    }

    private static float EaseOutElastic(float start, float end, float val)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s = 0;
        float a = 0;

        if (val == 0) return start;

        val = val / d;
        if (val == 1) return start + end;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p / 4;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }

        return (a * Mathf.Pow(2, -10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p) + end + start);
    }

    private static float EaseInOutElastic(float start, float end, float val)
    {
        end -= start;

        float d = 1f;
        float p = d * .3f;
        float s = 0;
        float a = 0;

        if (val == 0) return start;

        val = val / (d / 2);
        if (val == 2) return start + end;

        if (a == 0f || a < Mathf.Abs(end))
        {
            a = end;
            s = p / 4;
        }
        else
        {
            s = p / (2 * Mathf.PI) * Mathf.Asin(end / a);
        }

        if (val < 1)
        {
            val = val - 1;
            return -0.5f * (a * Mathf.Pow(2, 10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p)) + start;
        }
        val = val - 1;
        return a * Mathf.Pow(2, -10 * val) * Mathf.Sin((val * d - s) * (2 * Mathf.PI) / p) * 0.5f + end + start;
    }
}