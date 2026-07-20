using UnityEngine;

namespace VeilSight
{
    internal sealed class ExposureMeter : MonoBehaviour
    {
        private const float MeterWidth = 184f;
        private const float MeterHeight = 20f;
        private const float DimThreshold = 0.325f;
        private const float BrightThreshold = 0.45f;
        private const float GaugeDimStart = 0.34f;
        private const float GaugeBrightStart = 0.62f;

        private float _displayed;
        private float _velocity;
        private Texture2D _pixel;
        private bool _hasValue;

        private void Awake()
        {
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply(false, true);
        }

        private void Update()
        {
            if (!ModConfig.ShowMeter.Value)
            {
                ResetDisplay();
                return;
            }

            var snapshot = ExposureSampler.Current;
            float age = Time.realtimeSinceStartup - snapshot.SampleTime;
            float maximumAge = Mathf.Max(1f,
                Mathf.Clamp(ModConfig.SampleInterval.Value, 0.25f, 0.5f) * 3f);
            if (!snapshot.Valid || age < 0f || age > maximumAge)
            {
                ResetDisplay();
                return;
            }

            float target = ToGaugePosition(snapshot.Score);
            if (!_hasValue)
            {
                _displayed = target;
                _velocity = 0f;
                _hasValue = true;
                return;
            }

            float smoothTime = Mathf.Clamp(ModConfig.MeterSmoothTime.Value, 0.05f, 1.5f);
            _displayed = Mathf.SmoothDamp(_displayed, target, ref _velocity,
                smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (!_hasValue || _pixel == null || Event.current.type != EventType.Repaint)
                return;

            float scale = Mathf.Clamp(ModConfig.MeterScale.Value, 0.5f, 2f);
            float width = MeterWidth * scale;
            float height = MeterHeight * scale;
            float x = ClampX(Screen.width * Mathf.Clamp01(ModConfig.MeterPositionX.Value) - width * 0.5f, width);
            float y = ClampY(Screen.height * Mathf.Clamp01(ModConfig.MeterPositionY.Value) - height * 0.5f, height);
            DrawAmberGauge(new Rect(x, y, width, height), Mathf.Clamp01(_displayed), scale);
            GUI.color = Color.white;
        }

        private void DrawAmberGauge(Rect rect, float value, float scale)
        {
            float glow = Mathf.SmoothStep(0f, 1f, value);
            float brightness = Mathf.Lerp(0.28f, 1f, glow);

            Draw(rect, LitColor(0.055f, 0.033f, 0.010f, Mathf.Lerp(0.70f, 0.94f, glow), brightness));
            var inner = Inset(rect, 2f * scale);
            Draw(inner, LitColor(0.34f, 0.17f, 0.030f, Mathf.Lerp(0.34f, 0.72f, glow), brightness));
            DrawTicks(inner, 16, scale,
                LitColor(1f, 0.54f, 0.10f, Mathf.Lerp(0.20f, 0.48f, glow), brightness));

            float y = inner.yMax - 4f * scale;
            Draw(new Rect(inner.x, y, inner.width * value, 2f * scale),
                LitColor(1f, 0.47f, 0.06f, Mathf.Lerp(0.42f, 0.92f, glow), brightness));
            DrawNeedle(inner, value, scale,
                LitColor(1f, 0.72f, 0.22f, Mathf.Lerp(0.72f, 1f, glow), brightness), 2f);
        }

        private void DrawNeedle(Rect rect, float value, float scale, Color color, float width)
        {
            float needleWidth = Mathf.Max(1f, width * scale);
            float x = rect.x + rect.width * value - needleWidth * 0.5f;
            Draw(new Rect(x + Mathf.Max(1f, scale), rect.y, needleWidth, rect.height),
                new Color(0f, 0f, 0f, 0.46f));
            Draw(new Rect(x, rect.y, needleWidth, rect.height), color);
        }

        private void DrawTicks(Rect rect, int count, float scale, Color color)
        {
            float width = Mathf.Max(1f, scale * 0.75f);
            for (int i = 0; i <= count; i++)
            {
                float position = i / (float)count;
                float height = (i % 2 == 0 ? 6f : 3f) * scale;
                Draw(new Rect(rect.x + rect.width * position - width * 0.5f,
                    rect.center.y - height * 0.5f, width, height), color);
            }
        }

        private void Draw(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, _pixel, ScaleMode.StretchToFill, true);
        }

        private static Color LitColor(float r, float g, float b, float alpha, float brightness)
        {
            return new Color(r * brightness, g * brightness, b * brightness, alpha);
        }

        private static Rect Inset(Rect rect, float amount)
        {
            return new Rect(rect.x + amount, rect.y + amount,
                Mathf.Max(0f, rect.width - amount * 2f), Mathf.Max(0f, rect.height - amount * 2f));
        }

        private static float ClampX(float x, float width)
        {
            return Mathf.Clamp(x, 4f, Mathf.Max(4f, Screen.width - width - 4f));
        }

        private static float ClampY(float y, float height)
        {
            return Mathf.Clamp(y, 4f, Mathf.Max(4f, Screen.height - height - 4f));
        }

        private static float ToGaugePosition(float score)
        {
            score = Mathf.Clamp01(score);
            if (score < DimThreshold)
                return Mathf.InverseLerp(0f, DimThreshold, score) * GaugeDimStart;
            if (score < BrightThreshold)
                return Mathf.Lerp(GaugeDimStart, GaugeBrightStart,
                    Mathf.InverseLerp(DimThreshold, BrightThreshold, score));
            return Mathf.Lerp(GaugeBrightStart, 1f,
                Mathf.InverseLerp(BrightThreshold, 1f, score));
        }

        private void ResetDisplay()
        {
            _hasValue = false;
            _displayed = 0f;
            _velocity = 0f;
        }

        private void OnDestroy()
        {
            if (_pixel != null)
                Destroy(_pixel);
        }
    }
}
