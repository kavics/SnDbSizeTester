using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace SnDbSizeTesterApp
{
    /// <summary>
    /// Represents the view-model for the main window.
    /// </summary>
    public class MainViewModel
    {
        private Random _rnd = new Random();
        private LineSeries[] _series;
        private double[][] _data;

        /// <summary>
        /// Gets the plot model.
        /// </summary>
        public PlotModel Model { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        public MainViewModel()
        {
            // Create initial data
            _data = new [] {new double[100], new double[100], new double[100]};
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 100; j++)
                    _data[i][j] = _rnd.NextDouble() * 4.0 + i * 5;

            // Create line series with random values
            _series = new LineSeries[]
            {
                new LineSeries {Title = "Data %", MarkerType = MarkerType.None},
                new LineSeries {Title = "Log %", MarkerType = MarkerType.None},
                new LineSeries {Title = "Temp %", MarkerType = MarkerType.None},
            };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 100; j++)
                    _series[i].Points.Add(new DataPoint(j, _data[i][j]));

            // Create the plot model
            var tmp = new PlotModel { Title = "", Subtitle = "" };
            tmp.Series.Add(_series[0]);
            tmp.Series.Add(_series[1]);
            tmp.Series.Add(_series[2]);

            // Axes are created automatically if they are not defined
            tmp.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom, Minimum = 0, Maximum = 100,
                /*MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = OxyColor.FromRgb(0x80, 0x80, 0x80*/
            });
            tmp.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 100,
                MajorGridlineStyle = LineStyle.Dash,
                MajorGridlineColor = OxyColor.FromRgb(0xC0, 0xC0, 0xC0),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromRgb(0xC0, 0xC0, 0xC0)
            });

            // Set the Model property, the INotifyPropertyChanged event will make the WPF Plot control update its content
            this.Model = tmp;
        }

        public void Advance(double[] values)
        {
            try
            {
                for (var i = 0; i < _series.Length; i++)
                {
                    var p = _series[i].Points;
                    // Scroll left
                    for (int x = 1; x < 100; x++)
                        p[x - 1] = new DataPoint(x - 1, p[x].Y);
                    // Add new point
                    p[99] = new DataPoint(99, values[i]);
                }
                Model.InvalidatePlot(true);
            }
            catch (Exception e)
            {
                int q = 1;
            }
        }

    }
}
