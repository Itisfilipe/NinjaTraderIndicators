#region Using declarations
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ErgonomicCharts : Indicator
    {
        // Import keybd_event to simulate key presses.
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        private const byte VK_CONTROL = 0x11;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x1;
        private const uint KEYEVENTF_KEYUP = 0x2;

        private ChartControl chartControl;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Allows free panning (horizontally, vertically and diagonally) without holding Ctrl by simulating it, and zooms with the scroll wheel.";
                Name = "Ergonomic Charts";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = false;
                DrawHorizontalGridLines = false;
                DrawVerticalGridLines = false;
                PaintPriceMarkers = false;
                ScaleJustification = ScaleJustification.Right;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.Realtime)
            {
                if (ChartControl != null)
                {
                    chartControl = ChartControl;
                    chartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        AddIndicator();
                    }));
                }
            }
            else if (State == State.Terminated)
            {
                if (chartControl != null)
                {
                    chartControl.Dispatcher.InvokeAsync((Action)(() =>
                    {
                        DisposeCleanUp();
                    }));
                }
            }
        }
        
        private void AddIndicator()
        {
            // Attach our mouse event handlers directly to the ChartControl.
            chartControl.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            chartControl.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            chartControl.PreviewMouseWheel += OnMouseWheel;
            
            // Subscribe to LostFocus event to force a Ctrl key release.
            chartControl.LostFocus += OnChartLostFocus;
        }
        
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Check for double-click using the ClickCount property.
            if (e.ClickCount == 2)
            {
                // On double-click, force a release of the Ctrl key.
                keybd_event(VK_CONTROL, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            else
            {
                // Simulate holding down the Ctrl key so that free panning is activated.
                keybd_event(VK_CONTROL, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            }
            // Let the event propagate so that NinjaTrader's built-in panning occurs.
        }
        
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Simulate releasing the Ctrl key.
            keybd_event(VK_CONTROL, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        // Ensure the Ctrl key is released if the chart loses focus.
        private void OnChartLostFocus(object sender, RoutedEventArgs e)
        {
            keybd_event(VK_CONTROL, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (chartControl != null && ChartBars != null)
            {
                // Always zoom regardless of any modifier keys.
                if (e.Delta < 0)
                {
                    chartControl.Properties.BarDistance = (float)(chartControl.Properties.BarDistance * 0.9);
                    chartControl.BarWidth = chartControl.BarWidth * 0.9;
                }
                else if (e.Delta > 0)
                {
                    chartControl.Properties.BarDistance = (float)(chartControl.Properties.BarDistance / 0.9);
                    chartControl.BarWidth = chartControl.BarWidth / 0.9;
                }
                e.Handled = true;
                chartControl.InvalidateVisual();
                ForceRefresh();
            }
        }
        
        private void DisposeCleanUp()
        {
            chartControl.PreviewMouseLeftButtonDown -= OnMouseLeftButtonDown;
            chartControl.PreviewMouseLeftButtonUp -= OnMouseLeftButtonUp;
            chartControl.PreviewMouseWheel -= OnMouseWheel;
            chartControl.LostFocus -= OnChartLostFocus;
        }
        
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ErgonomicCharts[] cacheErgonomicCharts;
		public ErgonomicCharts ErgonomicCharts()
		{
			return ErgonomicCharts(Input);
		}

		public ErgonomicCharts ErgonomicCharts(ISeries<double> input)
		{
			if (cacheErgonomicCharts != null)
				for (int idx = 0; idx < cacheErgonomicCharts.Length; idx++)
					if (cacheErgonomicCharts[idx] != null &&  cacheErgonomicCharts[idx].EqualsInput(input))
						return cacheErgonomicCharts[idx];
			return CacheIndicator<ErgonomicCharts>(new ErgonomicCharts(), input, ref cacheErgonomicCharts);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ErgonomicCharts ErgonomicCharts()
		{
			return indicator.ErgonomicCharts(Input);
		}

		public Indicators.ErgonomicCharts ErgonomicCharts(ISeries<double> input )
		{
			return indicator.ErgonomicCharts(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ErgonomicCharts ErgonomicCharts()
		{
			return indicator.ErgonomicCharts(Input);
		}

		public Indicators.ErgonomicCharts ErgonomicCharts(ISeries<double> input )
		{
			return indicator.ErgonomicCharts(input);
		}
	}
}

#endregion
