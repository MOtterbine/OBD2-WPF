using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace OS.AutoScanner.Models
{
    public class DataPlotModel : PlotModel
    {

        private LineSeries _lineSeries = new LineSeries();
        public double XAxisMaxValue {
            get { return this.Axes[0].Maximum; } 
            set { 
                this.Axes[0].Maximum = value;
            }
        }
        public double YAxisMaxValue
        {
            get { return this.Axes[1].Maximum; }
            set 
            { 
                this.Axes[1].Maximum = value;
                this.tempMaxYValue = value;
            }
        }
        public double YAxisMinValue
        {
            get { return this.Axes[1].Minimum; }
            set 
            { 
                this.Axes[1].Minimum = value;
                this.tempMinYValue = value;

            }
        }
        private double tempMaxYValue = 0.0;
        private double tempMinYValue = 0.0;
        public void AddDataPoint(double xValue, double yValue)
        {
            if (yValue > tempMaxYValue)
            {
                tempMaxYValue = 1.1 * yValue;
                this.YAxisMaxValue = tempMaxYValue;

            }
            if (yValue < tempMinYValue)
            {
                tempMinYValue = -1.1 * yValue;
                this.YAxisMinValue = tempMinYValue;

            }
            this.Points.Add(new DataPoint(xValue, yValue));
        }

        public void ResetVerticalRange()
        {
            this.YAxisMaxValue = double.NaN;
            this.YAxisMinValue = 0.0;
            this.tempMaxYValue = 0.0;
            this.tempMinYValue = 0.0;
        }
        public void ClearDataPoints()
        {
            this.Points.Clear();
        }

        public DataPlotModel(string title)
        {
            this.Title = title;
            this.PlotType = PlotType.XY;
            this.TitleFontWeight = 200;
            this.TitleFontSize = 12;
            this.TitleHorizontalAlignment = TitleHorizontalAlignment.CenteredWithinView;
            this.Axes.Add(new LinearAxis() { Position = AxisPosition.Bottom, Minimum = 0, Maximum = 1000, Key = "Horizontal", IsAxisVisible = false });
            this.Axes.Add(new LinearAxis() { Position = AxisPosition.Left, Minimum = 0, /* Maximum = 5, */ Key = "Vertical" });
            this._lineSeries.ItemsSource = new List<DataPoint>();
            this.Series.Add(this._lineSeries);

        }

        public List<DataPoint> Points { get { return this._lineSeries.ItemsSource as List<DataPoint>; } }

    }
}
