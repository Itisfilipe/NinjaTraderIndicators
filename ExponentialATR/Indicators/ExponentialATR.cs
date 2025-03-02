#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// The Average True Range (ATR) calculated using an exponential moving average formula.
    /// </summary>
    public class ExponentialATR : Indicator
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description               = "The Average True Range (ATR) calculated using an exponential moving average formula.";
                Name                      = "Exponential ATR";
                IsSuspendedWhileInactive  = true;
                Period                    = 14;

                AddPlot(System.Windows.Media.Brushes.Blue, "ATR");
            }
        }

        protected override void OnBarUpdate()
        {
            double high0 = High[0];
            double low0  = Low[0];

            // Calculate True Range
            double trueRange = (CurrentBar == 0) ? (high0 - low0) 
                : Math.Max(Math.Abs(low0 - Close[1]), Math.Max(high0 - low0, Math.Abs(high0 - Close[1])));
            
            // Define the multiplier for exponential smoothing
            double multiplier = 2.0 / (Period + 1);
            
            if (CurrentBar == 0)
            {
                // For the first bar, set ATR equal to the true range.
                Value[0] = trueRange;
            }
            else
            {
                // Exponential smoothing formula:
                // ATR = ((TrueRange - PreviousATR) * multiplier) + PreviousATR
                Value[0] = ((trueRange - Value[1]) * multiplier) + Value[1];
            }
        }

        #region Properties
        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
        public int Period { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ExponentialATR[] cacheExponentialATR;
		public ExponentialATR ExponentialATR(int period)
		{
			return ExponentialATR(Input, period);
		}

		public ExponentialATR ExponentialATR(ISeries<double> input, int period)
		{
			if (cacheExponentialATR != null)
				for (int idx = 0; idx < cacheExponentialATR.Length; idx++)
					if (cacheExponentialATR[idx] != null && cacheExponentialATR[idx].Period == period && cacheExponentialATR[idx].EqualsInput(input))
						return cacheExponentialATR[idx];
			return CacheIndicator<ExponentialATR>(new ExponentialATR(){ Period = period }, input, ref cacheExponentialATR);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ExponentialATR ExponentialATR(int period)
		{
			return indicator.ExponentialATR(Input, period);
		}

		public Indicators.ExponentialATR ExponentialATR(ISeries<double> input , int period)
		{
			return indicator.ExponentialATR(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ExponentialATR ExponentialATR(int period)
		{
			return indicator.ExponentialATR(Input, period);
		}

		public Indicators.ExponentialATR ExponentialATR(ISeries<double> input , int period)
		{
			return indicator.ExponentialATR(input, period);
		}
	}
}

#endregion
