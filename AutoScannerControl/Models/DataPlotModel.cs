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
            set { this.Axes[0].Maximum = value; }
        }
        public double YAxisMaxValue
        {
            get { return this.Axes[1].Maximum; }
            set { this.Axes[1].Maximum = value; }
        }

        public DataPlotModel()
        {
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
