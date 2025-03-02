#region Using declarations
using System;
using NinjaTrader;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.BarsTypes
{
    // This class defines a custom Renko chart type that also tracks wick data.
    public class RenkoWicksBarsType : BarsType
    {
        // The brick size (offset) in price terms.
        private double offset;
        // Upper and lower boundaries of the current Renko brick.
        private double renkoHigh;
        private double renkoLow;
        // Tracks the highest high and lowest low within the current brick (wicks).
        private double currentWickHigh;
        private double currentWickLow;

        // No default base period values to apply in this implementation.
        public override void ApplyDefaultBasePeriodValue(BarsPeriod period) { }

        // Sets a default brick size value.
        public override void ApplyDefaultValue(BarsPeriod period)
        {
            period.Value = 20;
        }

        // Returns a chart label for the given time (formatted as time string).
        public override string ChartLabel(DateTime time) 
        { 
            return time.ToString("T", Core.Globals.GeneralOptions.CurrentCulture); 
        }

        // Determines the number of days of historical data to load (3 days in this case).
        public override int GetInitialLookBackDays(BarsPeriod period, TradingHours tradingHours, int barsBack) 
        { 
            return 3; 
        }

        // This method returns 0 since the brick completion percentage is not used here.
        public override double GetPercentComplete(Bars bars, DateTime now) 
        { 
            return 0; 
        }

        // Indicates that the implementation supports removal of the last bar.
        public override bool IsRemoveLastBarSupported { get { return true; } }

        // Main method that processes each incoming data point.
        protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
        {
            // Ensure the session iterator is initialized.
            if (SessionIterator == null)
                SessionIterator = new SessionIterator(bars);

            // Calculate the brick offset once. It is defined as the user-specified value times the instrument tick size.
            offset = bars.BarsPeriod.Value * bars.Instrument.MasterInstrument.TickSize;

            // Check once if a new trading session is starting for this data point.
            bool newSession = SessionIterator.IsNewSession(time, isBar);
            if (newSession)
                SessionIterator.GetNextSession(time, isBar);

            // Handle session initialization or first bar scenario.
            if (bars.Count == 0 || (bars.IsResetOnNewTradingDay && newSession))
            {
                if (bars.Count > 0)
                {
                    // For an existing session, "close" out the last bar by ensuring open equals close.
                    double lastBarClose = bars.GetClose(bars.Count - 1);
                    DateTime lastBarTime = bars.GetTime(bars.Count - 1);
                    long lastBarVolume = bars.GetVolume(bars.Count - 1);
                    RemoveLastBar(bars);
                    AddBar(bars, lastBarClose, lastBarClose, lastBarClose, lastBarClose, lastBarTime, lastBarVolume);
                }

                // Initialize Renko boundaries around the current price.
                renkoHigh = close + offset;
                renkoLow = close - offset;
                // Set wick extremes to the current close price.
                currentWickHigh = close;
                currentWickLow = close;

                // If a new session is detected, update the session iterator.
                if (newSession)
                    SessionIterator.GetNextSession(time, isBar);

                // Add the initial bar with all price values equal to the current close.
                AddBar(bars, close, close, close, close, time, volume);
                bars.LastPrice = close;
                return;
            }

            // Update the wick extremes based on the current data point.
            currentWickHigh = Math.Max(currentWickHigh, high);
            currentWickLow  = Math.Min(currentWickLow, low);

            // Cache values of the current (last) bar to avoid repeated method calls.
            int lastIndex = bars.Count - 1;
            double barOpen = bars.GetOpen(lastIndex);
            double barHigh = bars.GetHigh(lastIndex);
            double barLow  = bars.GetLow(lastIndex);
            long barVolume = bars.GetVolume(lastIndex);
            DateTime barTime = bars.GetTime(lastIndex);

            // If the Renko boundaries have not been initialized (i.e. are zero), set them based on previous bar(s).
            if (renkoHigh.ApproxCompare(0.0) == 0 || renkoLow.ApproxCompare(0.0) == 0)
            {
                if (bars.Count == 1)
                {
                    renkoHigh = barOpen + offset;
                    renkoLow = barOpen - offset;
                }
                else if (bars.GetClose(bars.Count - 2) > bars.GetOpen(bars.Count - 2))
                {
                    // If the previous bar was bullish, adjust the boundaries accordingly.
                    renkoHigh = bars.GetClose(bars.Count - 2) + offset;
                    renkoLow = bars.GetClose(bars.Count - 2) - offset * 2;
                }
                else
                {
                    // Otherwise, if the previous bar was bearish.
                    renkoHigh = bars.GetClose(bars.Count - 2) + offset * 2;
                    renkoLow = bars.GetClose(bars.Count - 2) - offset;
                }
            }

            // -------------------
            // Upward Brick Logic
            // -------------------
            if (close.ApproxCompare(renkoHigh) >= 0)
            {
                // Calculate the brick's open level for upward movement.
                double brickOpenUp = renkoHigh - offset;
                // Clamp the wick high so it does not exceed the brick's upper boundary.
                double effectiveWickHigh = Math.Min(currentWickHigh, renkoHigh);
                double effectiveWickLow = currentWickLow;
                // If the current bar's open, high, or low does not match the expected values,
                // remove the last bar and add a corrected version.
                if (barOpen.ApproxCompare(brickOpenUp) != 0 ||
                    barHigh.ApproxCompare(Math.Max(brickOpenUp, effectiveWickHigh)) != 0 ||
                    barLow.ApproxCompare(Math.Min(brickOpenUp, effectiveWickLow)) != 0)
                {
                    RemoveLastBar(bars);
                    AddBar(bars, brickOpenUp,
                           Math.Max(brickOpenUp, effectiveWickHigh),
                           Math.Min(brickOpenUp, effectiveWickLow),
                           renkoHigh, barTime, barVolume);
                }

                // Update Renko boundaries for the next brick.
                renkoLow = renkoHigh - 2.0 * offset;
                renkoHigh = renkoHigh + offset;

                // Update session information if a new session is detected.
                if (SessionIterator.IsNewSession(time, isBar))
                    SessionIterator.GetNextSession(time, isBar);

                // Fill in any "empty" bricks if the price moves several brick sizes at once.
                while (close.ApproxCompare(renkoHigh) >= 0)
                {
                    double brickOpenEmpty = renkoHigh - offset;
                    AddBar(bars, brickOpenEmpty,
                           Math.Max(brickOpenEmpty, renkoHigh),
                           Math.Min(brickOpenEmpty, renkoHigh),
                           renkoHigh, time, 0);
                    // Update boundaries for the next potential brick.
                    renkoLow = renkoHigh - 2.0 * offset;
                    renkoHigh = renkoHigh + offset;
                }

                // Reset wick tracking for the new brick.
                currentWickHigh = close;
                currentWickLow  = close;
                double newBrickOpenUp = renkoHigh - offset;
                // Add the new brick with the latest price data.
                AddBar(bars, newBrickOpenUp,
                       Math.Max(newBrickOpenUp, currentWickHigh),
                       Math.Min(newBrickOpenUp, currentWickLow),
                       close, time, volume);
            }
            // ---------------------
            // Downward Brick Logic
            // ---------------------
            else if (close.ApproxCompare(renkoLow) <= 0)
            {
                // Calculate the brick's open level for downward movement.
                double brickOpenDown = renkoLow + offset;
                // Clamp the wick low so it does not go below the brick's lower boundary.
                double effectiveWickLow = Math.Max(currentWickLow, renkoLow);
                double effectiveWickHigh = currentWickHigh;
                // If the current bar's open, high, or low is not aligned with the expected values,
                // correct the bar by removing and re-adding it.
                if (barOpen.ApproxCompare(brickOpenDown) != 0 ||
                    barHigh.ApproxCompare(Math.Max(brickOpenDown, effectiveWickHigh)) != 0 ||
                    barLow.ApproxCompare(Math.Min(brickOpenDown, effectiveWickLow)) != 0)
                {
                    RemoveLastBar(bars);
                    AddBar(bars, brickOpenDown,
                           Math.Max(brickOpenDown, effectiveWickHigh),
                           Math.Min(brickOpenDown, effectiveWickLow),
                           renkoLow, barTime, barVolume);
                }

                // Update Renko boundaries for the next brick.
                renkoHigh = renkoLow + 2.0 * offset;
                renkoLow = renkoLow - offset;

                // Update session information if needed.
                if (SessionIterator.IsNewSession(time, isBar))
                    SessionIterator.GetNextSession(time, isBar);

                // Fill in any empty bricks if the price move spans multiple brick sizes.
                while (close.ApproxCompare(renkoLow) <= 0)
                {
                    double brickOpenEmptyDown = renkoLow + offset;
                    AddBar(bars, brickOpenEmptyDown,
                           Math.Max(brickOpenEmptyDown, renkoLow),
                           Math.Min(brickOpenEmptyDown, renkoLow),
                           renkoLow, time, 0);
                    // Update boundaries for subsequent bricks.
                    renkoHigh = renkoLow + 2.0 * offset;
                    renkoLow = renkoLow - offset;
                }

                // Reset wick tracking for the new brick.
                currentWickHigh = close;
                currentWickLow  = close;
                double newBrickOpenDown = renkoLow + offset;
                // Add the new brick with updated wick information.
                AddBar(bars, newBrickOpenDown,
                       Math.Max(newBrickOpenDown, currentWickHigh),
                       Math.Min(newBrickOpenDown, currentWickLow),
                       close, time, volume);
            }
            // -------------------------------
            // No New Brick; Update Current Brick
            // -------------------------------
            else
            {
                // If the price hasn't moved enough to form a new brick,
                // simply update the current brick's wick extremes.
                UpdateBar(bars, 
						  Math.Max(barOpen, currentWickHigh),
				          Math.Min(barOpen, currentWickLow), 
				          close, time, volume);
            }

            // Update the last price of the bars.
            bars.LastPrice = close;
        }

        // Handles initialization and configuration states.
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                // Set initial default properties for the chart.
                Name                = "Renko with Wicks";
                BarsPeriod          = new BarsPeriod { BarsPeriodType = (BarsPeriodType)2588 };
                BuiltFrom           = BarsPeriodType.Tick;
                DaysToLoad          = 10;
                DefaultChartStyle   = (Gui.Chart.ChartStyleType)2588;
                IsIntraday          = true;
                IsTimeBased         = false;
            }
            else if (State == State.Configure)
            {
                // Update the name to include the brick size.
                Name = string.Format(Core.Globals.GeneralOptions.CurrentCulture, "Renko Wicks {0}", BarsPeriod.Value);
                // Remove properties that are not applicable for this Renko implementation.
                Properties.Remove(Properties.Find("BaseBarsPeriodType",         true));
                Properties.Remove(Properties.Find("BaseBarsPeriodValue",        true));
                Properties.Remove(Properties.Find("PointAndFigurePriceType",    true));
                Properties.Remove(Properties.Find("ReversalType",               true));
                Properties.Remove(Properties.Find("Value2",                     true));

                // Rename the "Value" property to "Brick Size" for clarity.
                SetPropertyName("Value", "Brick Size");
            }
        }
    }
}
