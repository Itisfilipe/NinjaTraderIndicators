#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// MACD Histogram indicator with dynamic configurable colors and histogram bar width matching the chart’s bar width.
    /// - When above 0: bright if rising and dark if falling.
    /// - When below 0: bright if falling (more negative) and dark if rising (back toward 0).
    /// - A neutral color is used when the value is 0.
    /// </summary>
    public class MACDHistogram : Indicator
    {
        private Series<double> fastEma;
        private Series<double> slowEma;
        private Series<double> macdAvgSeries;
        private double constant1;
        private double constant2;
        private double constant3;
        private double constant4;
        private double constant5;
        private double constant6;
        private Brush PositiveRisingColor;
        private Brush PositiveFallingColor;
        private Brush NegativeFallingColor;
        private Brush NegativeRisingColor;
        private Brush NeutralColor;

        #region MACD Parameter Properties
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Fast", Order = 0, GroupName = "NinjaScriptParameters")]
        public int Fast { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Slow", Order = 1, GroupName = "NinjaScriptParameters")]
        public int Slow { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Smooth", Order = 2, GroupName = "NinjaScriptParameters")]
        public int Smooth { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "MACD Histogram indicator with dynamic configurable colors based on momentum. Histogram bar width matches the chart’s bar width by default.";
                Name = "MACD Histogram";
                Fast = 12;
                Slow = 26;
                Smooth = 9;
                IsSuspendedWhileInactive = true;
				

                // Set default color values.
                PositiveRisingColor = Brushes.Lime;
                PositiveFallingColor = Brushes.DarkGreen;
                NegativeFallingColor = Brushes.Red;
                NegativeRisingColor = Brushes.DarkRed;
                NeutralColor = Brushes.Gray;

                // Create a Stroke using the PositiveRisingColor as an initial color.
                Stroke histStroke = new Stroke(PositiveRisingColor);
                AddPlot(histStroke, PlotStyle.Bar, "Histogram");
				Plots[0].AutoWidth = true;
            }
            else if (State == State.Configure)
            {
                // Pre-calculate constants for exponential smoothing.
                constant1 = 2.0 / (1 + Fast);
                constant2 = 1 - constant1;
                constant3 = 2.0 / (1 + Slow);
                constant4 = 1 - constant3;
                constant5 = 2.0 / (1 + Smooth);
                constant6 = 1 - constant5;
            }
            else if (State == State.DataLoaded)
            {
                fastEma = new Series<double>(this);
                slowEma = new Series<double>(this);
                macdAvgSeries = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            double input0 = Input[0];

            if (CurrentBar == 0)
            {
                fastEma[0] = input0;
                slowEma[0] = input0;
                macdAvgSeries[0] = 0;
                Value[0] = 0;
                PlotBrushes[0][0] = PositiveRisingColor;
            }
            else
            {
                double fastEma0 = constant1 * input0 + constant2 * fastEma[1];
                double slowEma0 = constant3 * input0 + constant4 * slowEma[1];
                double macd = fastEma0 - slowEma0;
                double macdAvg = constant5 * macd + constant6 * macdAvgSeries[1];
                macdAvgSeries[0] = macdAvg;
                double histogram = macd - macdAvg;
                Value[0] = histogram;

                // Set dynamic colors based on the histogram direction.
                if (histogram > 0)
                {
                    // When above 0, if the histogram is rising, use PositiveRisingColor; if falling, use PositiveFallingColor.
                    PlotBrushes[0][0] = histogram > Value[1] ? PositiveRisingColor : PositiveFallingColor;
                }
                else if (histogram < 0)
                {
                    // When below 0, if the histogram is falling further (more negative), use NegativeFallingColor; otherwise use NegativeRisingColor.
                    PlotBrushes[0][0] = histogram < Value[1] ? NegativeFallingColor : NegativeRisingColor;
                }
                else
                {
                    PlotBrushes[0][0] = NeutralColor;
                }

                fastEma[0] = fastEma0;
                slowEma[0] = slowEma0;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MACDHistogram[] cacheMACDHistogram;
		public MACDHistogram MACDHistogram(int fast, int slow, int smooth)
		{
			return MACDHistogram(Input, fast, slow, smooth);
		}

		public MACDHistogram MACDHistogram(ISeries<double> input, int fast, int slow, int smooth)
		{
			if (cacheMACDHistogram != null)
				for (int idx = 0; idx < cacheMACDHistogram.Length; idx++)
					if (cacheMACDHistogram[idx] != null && cacheMACDHistogram[idx].Fast == fast && cacheMACDHistogram[idx].Slow == slow && cacheMACDHistogram[idx].Smooth == smooth && cacheMACDHistogram[idx].EqualsInput(input))
						return cacheMACDHistogram[idx];
			return CacheIndicator<MACDHistogram>(new MACDHistogram(){ Fast = fast, Slow = slow, Smooth = smooth }, input, ref cacheMACDHistogram);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MACDHistogram MACDHistogram(int fast, int slow, int smooth)
		{
			return indicator.MACDHistogram(Input, fast, slow, smooth);
		}

		public Indicators.MACDHistogram MACDHistogram(ISeries<double> input , int fast, int slow, int smooth)
		{
			return indicator.MACDHistogram(input, fast, slow, smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MACDHistogram MACDHistogram(int fast, int slow, int smooth)
		{
			return indicator.MACDHistogram(Input, fast, slow, smooth);
		}

		public Indicators.MACDHistogram MACDHistogram(ISeries<double> input , int fast, int slow, int smooth)
		{
			return indicator.MACDHistogram(input, fast, slow, smooth);
		}
	}
}

#endregion
