using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DxfViewer.Controls
{
    /// <summary>
    /// DxfEntityObject.xaml の相互作用ロジック
    /// </summary>
    public partial class DxfEntityObject : UserControl
    {
        public DxfEntityObject()
        {
            InitializeComponent();
        }

        //依存関係プロパティ
        public static readonly DependencyProperty EntityObjectProperty =
                  DependencyProperty.Register(
                      nameof(EntityObject),
                      typeof(netDxf.Entities.EntityObject),
                      typeof(DxfEntityObject),
                      new FrameworkPropertyMetadata(
                          null,
                          new PropertyChangedCallback(OnEntityObjectChanged)));

        //外部に公開するプロパティ
        public netDxf.Entities.EntityObject EntityObject
        {
            get { 
                return (netDxf.Entities.EntityObject)GetValue(EntityObjectProperty);
            }
            set { 
                SetValue(EntityObjectProperty, value);
            }
        }

        //コールバックイベントの処理
        private static void OnEntityObjectChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (DxfEntityObject)obj;


            if (e.NewValue != e.OldValue)
            {
                Brush stroke = Brushes.Black;

                var value = (netDxf.Entities.EntityObject)e.NewValue;
                var type = value.Type;
                switch (type)
                {
                    case netDxf.Entities.EntityType.Line:
                        {
                            Line drawLine = CreateLine(stroke, (netDxf.Entities.Line)value);
                            ctrl.m_canvas.Children.Add(drawLine);
                            break;
                        }
                    case netDxf.Entities.EntityType.Circle:
                        {
                            Ellipse drawCircle = CreateCircle(stroke, (netDxf.Entities.Circle)value);
                            ctrl.m_canvas.Children.Add(drawCircle);
                            break;
                        }
                    case netDxf.Entities.EntityType.Ellipse:
                        {
                            Path drawEllipse = CreateEllipse(stroke, (netDxf.Entities.Ellipse)value);
                            ctrl.m_canvas.Children.Add(drawEllipse);
                            break;
                        }
                    case netDxf.Entities.EntityType.Arc:
                        {
                            var path = CreateArcPath(stroke, (netDxf.Entities.Arc)value);
                            ctrl.m_canvas.Children.Add(path);
                            break;
                        }

                    case netDxf.Entities.EntityType.Polyline2D:
                        {
                            List<Shape> lines = CreatePolyLine(stroke, (netDxf.Entities.Polyline2D)value);
                            lines.ForEach(x => ctrl.m_canvas.Children.Add(x));
                            break;
                        }
                    case netDxf.Entities.EntityType.Solid:
                        {
                            Path path = CreateSolid(stroke, (netDxf.Entities.Solid)value);
                            ctrl.m_canvas.Children.Add(path);
                            break;
                        }
                    case netDxf.Entities.EntityType.Dimension:
                        {
                            var dimension = (netDxf.Entities.Dimension)value;
                            break;
                        }
                    case netDxf.Entities.EntityType.MText:
                        {
                            var MText = (netDxf.Entities.MText)value;
                            TextBlock textBlock = CreateMText(stroke, (netDxf.Entities.MText)value);
                            ctrl.m_canvas.Children.Add(textBlock);
                            break;
                        }
                }
            }
            
            // control.uxProductCode.Text = (obj != null) ? control.ProductCode : control.uxProductCode.Text;
        }

        internal static TextBlock CreateMText(Brush stroke, netDxf.Entities.MText mText)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = mText.Value;
            textBlock.Foreground = stroke;
            textBlock.IsHitTestVisible = false;
            textBlock.FontSize = 1.5D * mText.Height;

            var formattedText = new FormattedText(
                mText.Value,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);


            Canvas.SetLeft(textBlock, mText.Position.X);
            Canvas.SetBottom(textBlock, mText.Position.Y);

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new TranslateTransform(-formattedText.Width / 2D, 0));
            transformGroup.Children.Add(new RotateTransform(-mText.Rotation, 0, formattedText.Height / 2D));
            textBlock.RenderTransform = transformGroup;


            return textBlock;
        }

        internal static Path CreateSolid(Brush stroke, netDxf.Entities.Solid solid)
        {
            Path path = new Path();
            PathGeometry geometry = CreateSolidGeometry(solid);
            path.Data = geometry;
            path.Fill = stroke;
            path.StrokeThickness = 0.5;
            return path;
        }

        internal static PathGeometry CreateSolidGeometry(netDxf.Entities.Solid solid)
        {
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();
            PathSegmentCollection group = new PathSegmentCollection
            {
                //reverse 3, 2 because ordering of vertices is different in WPF
                new LineSegment(new Point(solid.SecondVertex.X, -solid.SecondVertex.Y), true),

                new LineSegment(new Point(solid.ThirdVertex.X, -solid.ThirdVertex.Y), true),

                new LineSegment(new Point(solid.FourthVertex.X, -solid.FourthVertex.Y), true),
                
                new LineSegment(new Point(solid.FirstVertex.X, -solid.FirstVertex.Y), true)
            };

            figure.IsFilled = true;
            figure.StartPoint = new Point(solid.FirstVertex.X, -solid.FirstVertex.Y);
            figure.Segments = group;

            geometry.Figures.Add(figure);

            return geometry;
        }

        internal static Path CreateArcPath(Brush stroke, netDxf.Entities.Arc arc)
        {
            Path path = new Path() { 
                StrokeThickness = 0.5
            };
            path.Stroke = stroke;

            PathGeometry geometry = CreateArcPathGeometry(arc);
            path.Data = geometry;
            path.IsHitTestVisible = false;

            return path;
        }

        private static PathGeometry CreateArcPathGeometry(netDxf.Entities.Arc arc)
        {
            Point endPoint = new Point(
                (arc.Center.X + Math.Cos(arc.EndAngle * Math.PI / 180) * arc.Radius),
                (-arc.Center.Y - Math.Sin(arc.EndAngle * Math.PI / 180) * arc.Radius));

            Point startPoint = new Point(
                (arc.Center.X + Math.Cos(arc.StartAngle * Math.PI / 180) * arc.Radius),
                (-arc.Center.Y - Math.Sin(arc.StartAngle * Math.PI / 180) * arc.Radius));

            ArcSegment arcSegment = new ArcSegment();
            double sweep;
            if (arc.EndAngle < arc.StartAngle)
                sweep = (360 + arc.EndAngle) - arc.StartAngle;
            else sweep = Math.Abs(arc.EndAngle - arc.StartAngle);

            arcSegment.IsLargeArc = sweep >= 180;
            arcSegment.Point = endPoint;
            arcSegment.Size = new Size(arc.Radius, arc.Radius);
            arcSegment.SweepDirection = arc.Normal.Z >= 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

            PathGeometry geometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = startPoint;
            pathFigure.Segments.Add(arcSegment);
            geometry.Figures.Add(pathFigure);
            return geometry;
        }

        internal static List<Shape> CreatePolyLine(Brush stroke, netDxf.Entities.Polyline2D polyLine)
        {
            bool isClosed = polyLine.IsClosed;

            int count = isClosed ? polyLine.Vertexes.Count : polyLine.Vertexes.Count - 1;
            List<Shape> lines = new List<Shape>();
            for (int i = 1; i <= count; i++)
            {
                netDxf.Entities.Polyline2DVertex vertex1 = (i == polyLine.Vertexes.Count) ?
                    (netDxf.Entities.Polyline2DVertex)polyLine.Vertexes[0] : (netDxf.Entities.Polyline2DVertex)polyLine.Vertexes[i];
                netDxf.Entities.Polyline2DVertex vertex2 = (netDxf.Entities.Polyline2DVertex)polyLine.Vertexes[i - 1];

                Point start = new Point((float)vertex1.Position.X, (float)-vertex1.Position.Y);
                Point end = new Point((float)vertex2.Position.X, (float)-vertex2.Position.Y);

                // TODO: Handle Vertex.Buldge http://www.afralisp.net/archive/lisp/Bulges1.htm

                Line drawLine = new Line
                {
                    Stroke = stroke,
                    StrokeThickness = 0.5,
                    X1 = end.X,
                    X2 = start.X,
                    Y1 = end.Y,
                    Y2 = start.Y,
                    IsHitTestVisible = false
                };

                lines.Add(drawLine);
            }

            return lines;
        }


        internal static Line CreateLine(Brush stroke, netDxf.Entities.Line line)
        {
            Point start = new Point(line.StartPoint.X, -line.StartPoint.Y);
            Point end = new Point(line.EndPoint.X, -line.EndPoint.Y);

            Line drawLine = new Line
            {
                Stroke = stroke,
                StrokeThickness = 0.5,
                X1 = end.X,
                X2 = start.X,
                Y1 = end.Y,
                Y2 = start.Y,
                IsHitTestVisible = false
            };

            return drawLine;
        }

        internal static Ellipse CreateCircle(Brush stroke, netDxf.Entities.Circle circle)
        {
            Ellipse drawCircle = new Ellipse
            {
                Stroke = stroke,
                StrokeThickness = 0.5,
                Width = circle.Radius * 2,
                Height = circle.Radius * 2,
                IsHitTestVisible = false
            };

            Point center = new Point((double)circle.Center.X, (double)circle.Center.Y);
            var left = (center.X - circle.Radius);
            var top = (-center.Y - circle.Radius);
            Canvas.SetLeft(drawCircle, left);
            Canvas.SetTop(drawCircle, top);

            return drawCircle;
        }

        internal static Path CreateEllipse(Brush stroke, netDxf.Entities.Ellipse ellipse)
        {
            Path path = new Path() { 
                StrokeThickness = 0.5,
            };
            path.Stroke = stroke;
            path.IsHitTestVisible = false;

            var angle = -ellipse.Rotation;//(180 / Math.PI) * Math.Atan2(-ellipse.Value, ellipse.MainAxis.X.Value);
            path.RenderTransform = new RotateTransform(angle, ellipse.Center.X, -ellipse.Center.Y);

            Geometry geometry = CreateEllipseGeometry(ellipse);
            path.Data = geometry;

            return path;
        }

        private static double[] GetEllipseParameters(netDxf.Entities.Ellipse ellipse)
        {
            double atan1;
            double atan2;
            if (ellipse.IsFullEllipse)
            {
                atan1 = 0.0;
                atan2 = Math.PI * 2D;
            }
            else
            {
                netDxf.Vector2 startPoint = ellipse.PolarCoordinateRelativeToCenter(ellipse.StartAngle);
                netDxf.Vector2 endPoint = ellipse.PolarCoordinateRelativeToCenter(ellipse.EndAngle);
                double a = 1 / (0.5 * ellipse.MajorAxis);
                double b = 1 / (0.5 * ellipse.MinorAxis);
                atan1 = Math.Atan2(startPoint.Y * b, startPoint.X * a);
                atan2 = Math.Atan2(endPoint.Y * b, endPoint.X * a);
            }
            return new[] { atan1, atan2 };
        }

        private static Geometry CreateEllipseGeometry(netDxf.Entities.Ellipse ellipse)
        {
            double[] param = GetEllipseParameters(ellipse);
            Geometry geometry;
            var radiusX = ellipse.MajorAxis / 2D;//Math.Sqrt(Math.Pow(ellipse.MainAxis.X.Value, 2) + Math.Pow(ellipse.MainAxis.Y.Value, 2));
            var radiusY = ellipse.MinorAxis / 2D;// radiusX * ellipse.AxisRatio;
            var startAngle = param[0] * 180 / Math.PI;
            var endAngle = param[1] * 180 / Math.PI;

            if (!ellipse.IsFullEllipse)//endAngle - startAngle < 360)
            {
                Point endPoint = new Point(
                    (ellipse.Center.X + Math.Cos(endAngle / 180D * Math.PI) * radiusX),
                    (-ellipse.Center.Y - Math.Sin(endAngle / 180D * Math.PI) * radiusY));

                Point startPoint = new Point(
                    (ellipse.Center.X + Math.Cos(startAngle / 180D * Math.PI) * radiusX),
                    (-ellipse.Center.Y - Math.Sin(startAngle / 180D * Math.PI) * radiusY));

                ArcSegment arcSegment = new ArcSegment();
                double sweep;
                if (endAngle < startAngle)
                    sweep = (360 + endAngle) - startAngle;
                else sweep = Math.Abs(endAngle - startAngle);

                arcSegment.IsLargeArc = sweep >= 180;
                arcSegment.Point = endPoint;
                arcSegment.Size = new Size(radiusX, radiusY);
                arcSegment.SweepDirection = ellipse.Normal.Z >= 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

                PathGeometry pathGeometry = new PathGeometry();
                PathFigure pathFigure = new PathFigure();
                pathFigure.StartPoint = startPoint;
                pathFigure.Segments.Add(arcSegment);
                pathGeometry.Figures.Add(pathFigure);
                geometry = pathGeometry;
            }
            else
            {
                EllipseGeometry ellipseGeometry = new EllipseGeometry();
                ellipseGeometry.Center = new Point(ellipse.Center.X, -ellipse.Center.Y);
                ellipseGeometry.RadiusX = radiusX;//Math.Sqrt(Math.Pow(ellipse.MainAxis.X.Value, 2) + Math.Pow(ellipse.MainAxis.Y.Value, 2));
                ellipseGeometry.RadiusY = radiusY;//ellipseGeometry.RadiusX * ellipse.AxisRatio;
                geometry = ellipseGeometry;
            }

            return geometry;
        }
    }
}
