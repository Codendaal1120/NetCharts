﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NetCharts.ChartElements;
using NetCharts.Component;
using NetCharts.Svg;

namespace NetCharts
{
    public class LineChart : Chart
    {
        public override ChartType Type => ChartType.Line;

        /// <summary>
        /// Enables/Disables data points on the default embedded styles.
        /// Custom styles are not affected by this property.
        /// </summary>
        public bool DrawDefaultDataPoints
        {
            get => ChartArea.DrawDefaultDataPointMarkers;
            set => ChartArea.DrawDefaultDataPointMarkers = value;
        }

        /// <summary>
        /// Enables/Disables data point labels on the default embedded styles.
        /// Custom styles are not affected by this property.
        /// </summary>
        public bool DrawDefaultDataPointLabels
        {
            get => ChartArea.DrawDefaultDataPointLabels;
            set => ChartArea.DrawDefaultDataPointLabels = value;
        }

        public LineType LineType
        {
            get => ChartArea.LineType;
            set => ChartArea.LineType = value;
        }

        public LineChartArea ChartArea { get; }
        public XAxis XAxis { get; }
        public YAxis YAxis { get; }

        internal ScaleInfo XScale { get; } = new ScaleInfo();
        internal ScaleInfo YScale { get; } = new ScaleInfo();

        public LineChart(ChartSeries[] series, string[] labels) : base(series, labels)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));
            var maxSeriesCount = series.Max(s => s.DataValues.Length);
            if (maxSeriesCount > labels.Length)
            {
                throw new ArgumentException($"Invalid number of labels, max number of DataValues cannot exceed label count");
            }

            ChartArea = new LineChartArea(series);
            XAxis = new XAxis() { StartOnMajor = false };
            YAxis = new YAxis( series.Max(s => s.DataValues.Max(v => v.ToString(CultureInfo.InvariantCulture).Length)) + 1 );
            Height = 300;
            Width = 400;
        }

        /// <summary>
        /// Calculates chart elements, should be called before creating SVG elements.
        /// </summary>
        protected override void GenerateChart()
        {
            Legend.CalculateSize(Height, Width);
            Title.CalculateSize(Height, Width);

            var paddingTop = AdditionalPaddingTop + PaddingTop;
            var paddingLeft = AdditionalPaddingLeft + PaddingLeft;
            var paddingBottom = AdditionalPaddingBottom + PaddingBottom;
            var paddingRight = AdditionalPaddingRight + PaddingRight;

            ChartArea.SizeChartArea(Height, paddingTop, paddingBottom, Width, paddingLeft, paddingRight);
            CalculateXScaleInfo();
            CalculateYScaleInfo();
            ChartArea.CreateDataPoints(XScale, YScale);
        }

        protected override double AdditionalPaddingRight => DrawDebug ? 10 : 0;

        protected override double AdditionalPaddingBottom
        {
            get
            {
                var additionalSize = XAxis.Draw ? XAxis.DynamicSize: 0;
                additionalSize += DrawDebug ? 10 : 0;
                return additionalSize;
            }
        }

        protected override double AdditionalPaddingTop
        {
            get
            {
                if (!Legend.Draw && !Title.Draw)
                {
                    return 0;
                }

                var height = Legend.Draw ? Legend.Height : 0;
                height += Title.Draw ? Title.Height : 0;
                return height;
            }
        }

        protected override double AdditionalPaddingLeft => YAxis.Draw ? YAxis.DynamicSize : 0;

        internal override IReadOnlyCollection<Element> GetSvgElements()
        {
            var valueLabels = GetValueLabels();
            var elements = XAxis.GetSvgElements(XScale, ChartArea.TopLeftX, ChartArea.BottomRightY, Labels);
            elements = elements.Concat(YAxis.GetSvgElements(YScale, ChartArea.TopLeftY, ChartArea.TopLeftX, valueLabels));
            elements = elements.Concat(ChartArea.GetSvgElements(XScale, YScale, valueLabels, DrawDebug));
            elements = elements.Concat(Legend.GetSvgElements(Title.Height));
            elements = elements.Concat(Title.GetSvgElements());

            return elements.ToArray();
        }
        
        internal override ScaleInfo GetScaleInfo(AxisType type)
        {
            return type == AxisType.XAxis
                ? XScale
                : YScale;
        }
        
        /// <summary>
        /// Calculates scale based on chart size
        /// </summary>
        private void CalculateXScaleInfo()
        {
            var divisor = XAxis.StartOnMajor
                ? Labels.Length - 1 
                : Labels.Length;
            XScale.StartOnMajor = XAxis.StartOnMajor;
            XScale.Max = divisor;
            XScale.MajorInterval = 1;
            XScale.MinorInterval = XScale.MajorInterval / 2;
            XScale.Scale = (ChartArea.Width) / (XScale.Max);
        }

        /// <summary>
        /// Calculates scale based on chart size
        /// </summary>
        private void CalculateYScaleInfo()
        {
            var ave = ChartArea.Series.SelectMany(s => s.DataValues).Average(p => p);
            var aveYRound = (int)ave;
            var aveUpper = (int)Math.Pow(10, (aveYRound.ToString(CultureInfo.InvariantCulture).Trim().Length));
            
            YScale.Max = ChartArea.Series.SelectMany(s => s.DataValues).Max(p => p);
            YScale.MajorInterval = (int)(aveUpper * 0.10);
            if ((YScale.Max / YScale.MajorInterval) > 10)
            {
                //large scale, increase the interval
                YScale.MajorInterval *= 2;
            }
            YScale.MinorInterval = (int)(YScale.MajorInterval / 10);
            YScale.Max = ((int)(YScale.Max / YScale.MajorInterval) * YScale.MajorInterval) + YScale.MajorInterval;
            YScale.Max += YScale.StartOnMajor ? YScale.MinorInterval : 0;

            YScale.Scale = (ChartArea.Height) / YScale.Max;
        }

        private IReadOnlyCollection<string> GetValueLabels()
        {
            CalculateYScaleInfo();

            var labels = new List<string>();
            for (var i = YScale.Max; i >= 0;)
            {
                labels.Add(i.ToString(CultureInfo.InvariantCulture));
                i -= YScale.MajorInterval;
            }

            return labels.ToArray();
        }
    }

}
