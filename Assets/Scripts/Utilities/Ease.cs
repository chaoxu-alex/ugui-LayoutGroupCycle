using System;
using UnityEngine;

public static class Ease
{
    static private Func<float, float, float, float>[] m_FuncMap = null;
    /// <summary>
    /// The easing type
    /// </summary>
    [Serializable]
    public enum Type
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
        EaseInOutElastic,
        Custom
    }

    public static float Evaluate(Type type, float start, float end, float progress)
    {
        if (m_FuncMap == null)
        {
            m_FuncMap = new Func<float, float, float, float>[(int)Type.Custom];
            m_FuncMap[(int)Type.Instant] = Instant;
            m_FuncMap[(int)Type.Linear] = Linear;
            m_FuncMap[(int)Type.Spring] = Spring;
            m_FuncMap[(int)Type.EaseInQuad] = EaseInQuad;
            m_FuncMap[(int)Type.EaseOutQuad] = EaseOutQuad;
            m_FuncMap[(int)Type.EaseInOutQuad] = EaseInOutQuad;
            m_FuncMap[(int)Type.EaseInCubic] = EaseInCubic;
            m_FuncMap[(int)Type.EaseOutCubic] = EaseOutCubic;
            m_FuncMap[(int)Type.EaseInOutCubic] = EaseInOutCubic;
            m_FuncMap[(int)Type.EaseInQuart] = EaseInQuart;
            m_FuncMap[(int)Type.EaseOutQuart] = EaseOutQuart;
            m_FuncMap[(int)Type.EaseInOutQuart] = EaseInOutQuart;
            m_FuncMap[(int)Type.EaseInQuint] = EaseInQuint;
            m_FuncMap[(int)Type.EaseOutQuint] = EaseOutQuint;
            m_FuncMap[(int)Type.EaseInOutQuint] = EaseInOutQuint;
            m_FuncMap[(int)Type.EaseInSine] = EaseInSine;
            m_FuncMap[(int)Type.EaseOutSine] = EaseOutSine;
            m_FuncMap[(int)Type.EaseInOutSine] = EaseInOutSine;
            m_FuncMap[(int)Type.EaseInExpo] = EaseInExpo;
            m_FuncMap[(int)Type.EaseOutExpo] = EaseOutExpo;
            m_FuncMap[(int)Type.EaseInOutExpo] = EaseInOutExpo;
            m_FuncMap[(int)Type.EaseInCirc] = EaseInCirc;
            m_FuncMap[(int)Type.EaseOutCirc] = EaseOutCirc;
            m_FuncMap[(int)Type.EaseInOutCirc] = EaseInOutCirc;
            m_FuncMap[(int)Type.EaseInBounce] = EaseInBounce;
            m_FuncMap[(int)Type.EaseOutBounce] = EaseOutBounce;
            m_FuncMap[(int)Type.EaseInOutBounce] = EaseInOutBounce;
            m_FuncMap[(int)Type.EaseInBack] = EaseInBack;
            m_FuncMap[(int)Type.EaseOutBack] = EaseOutBack;
            m_FuncMap[(int)Type.EaseInOutBack] = EaseInOutBack;
            m_FuncMap[(int)Type.EaseInElastic] = EaseInElastic;
            m_FuncMap[(int)Type.EaseOutElastic] = EaseOutElastic;
            m_FuncMap[(int)Type.EaseInOutElastic] = EaseInOutElastic;
        }

        return m_FuncMap[(int)type](start, end, progress);
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