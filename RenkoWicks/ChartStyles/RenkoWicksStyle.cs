#region Using declarations
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using SharpDX;
using SharpDX.Direct2D1;
using System;
#endregion

namespace NinjaTrader.NinjaScript.ChartStyles
{
    public class RenkoWickStyle : ChartStyle
    {

        public override int GetBarPaintWidth(int barWidth) 
        { 
            return 1 + 2 * (barWidth - 1) + 2 * (int)Math.Round(Stroke.Width); 
        }


        public override void OnRender(ChartControl chartControl, ChartScale chartScale, ChartBars chartBars)
        {
            Bars bars = chartBars.Bars;
            float barWidth = GetBarPaintWidth(BarWidthUI);
            RectangleF rect = new RectangleF();

            for (int idx = chartBars.FromIndex; idx <= chartBars.ToIndex; idx++)
            {
                // Retrieve any override brushes (if set)
                Brush overriddenBrush = chartControl.GetBarOverrideBrush(chartBars, idx);
                Brush overriddenOutlineBrush = chartControl.GetCandleOutlineOverrideBrush(chartBars, idx);

                // Get price values from the bar (which now include wick extremes)
                double closeValue = bars.GetClose(idx);
                double openValue  = bars.GetOpen(idx);
                double highValue  = bars.GetHigh(idx);
                double lowValue   = bars.GetLow(idx);

                float closeY = chartScale.GetYByValue(closeValue);
                float openY  = chartScale.GetYByValue(openValue);
                float highY  = chartScale.GetYByValue(highValue);
                float lowY   = chartScale.GetYByValue(lowValue);
                float x      = chartControl.GetXByBarIndex(chartBars, idx);

                // Choose outline based on up or down brick.
                Gui.Stroke outlineStroke = closeValue >= openValue ? Stroke : Stroke2;

                // Draw the open/close box.
                rect.X = x - barWidth * 0.5f + 0.5f;
                rect.Y = Math.Min(openY, closeY);
                rect.Width = barWidth - 1;
                rect.Height = Math.Abs(openY - closeY);

                Brush fillBrush = overriddenBrush ?? (closeValue >= openValue ? UpBrushDX : DownBrushDX);
                if (!(fillBrush is SolidColorBrush))
                    TransformBrush(fillBrush, rect);
                RenderTarget.FillRectangle(rect, fillBrush);

                Brush outlineBrush = overriddenOutlineBrush ?? outlineStroke.BrushDX;
                if (!(outlineBrush is SolidColorBrush))
                    TransformBrush(outlineBrush, rect);
                RenderTarget.DrawRectangle(rect, outlineBrush, outlineStroke.Width, outlineStroke.StrokeStyle);

                // Draw the upper wick if the recorded high is above the top of the body.
                if (highValue > Math.Max(openValue, closeValue))
                {
                    float bodyTopY = Math.Min(openY, closeY);
                    float wickTopY = highY;
                    RenderTarget.DrawLine(new Vector2(x, wickTopY), new Vector2(x, bodyTopY), outlineBrush, outlineStroke.Width);
                }

                // Draw the lower wick if the recorded low is below the bottom of the body.
                if (lowValue < Math.Min(openValue, closeValue))
                {
                    float bodyBottomY = Math.Max(openY, closeY);
                    float wickBottomY = lowY;
                    RenderTarget.DrawLine(new Vector2(x, bodyBottomY), new Vector2(x, wickBottomY), outlineBrush, outlineStroke.Width);
                }
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "Renko with Wicks";
                Description = "ChartStyle to be used with Renko Wicks";
                ChartStyleType = (ChartStyleType) 2588;
                BarWidth = 3;
            }
            else if (State == State.Configure)
            {
                SetPropertyName("BarWidth", Custom.Resource.NinjaScriptChartStyleBarWidth);
	            SetPropertyName("DownBrush", Custom.Resource.NinjaScriptChartStyleCandleDownBarsColor);
	            SetPropertyName("UpBrush", Custom.Resource.NinjaScriptChartStyleCandleUpBarsColor);
	            SetPropertyName("Stroke", Custom.Resource.NinjaScriptChartStyleCandleOutline);
                SetPropertyName("Stroke2", Custom.Resource.NinjaScriptChartStyleCandleWick);
                Properties.Remove(Properties.Find("Name", true));
            }
        }
    }
}
