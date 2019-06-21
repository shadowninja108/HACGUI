using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Linq;
using System.Windows.Media;
using static HACGUI.Extensions.Extensions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Windows.Data;

namespace HACGUI.Utilities
{
    public partial class OrganizableGridView : GridView
    {
        private readonly Queue<GridViewColumn> ColumnQueue;
        private GridViewColumnHeader CurrentOrganizedHeader;
        private SortAdorner CurrentAdorner;
        private ListView Parent;

        public OrganizableGridView() : base()
        {
            if (DesignMode.IsInDesignMode) return;

            ColumnQueue = new Queue<GridViewColumn>();
            Dispatcher.BeginInvoke(new Action(() => 
            {
                Parent = this.FindParent<ListView>();

                foreach (GridViewColumn column in ColumnQueue)
                    PrepareColumn(column);
                
            }));
            Columns.CollectionChanged += ColumnsChanged;
        }

        private void ColumnsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems != null)
                foreach (GridViewColumn column in e.NewItems)
                {
                    if (Parent == null)
                        ColumnQueue.Enqueue(column);
                    else
                        PrepareColumn(column);
                }
        }

        private void PrepareColumn(GridViewColumn column)
        {
            if (column.Header is GridViewColumnHeader header)
                header.Click += (_, __) =>
                {
                    string sortBy = header.Tag as string;

                    ICollectionView dataView = CollectionViewSource.GetDefaultView(Parent.Items);
                    SortDescriptionCollection sortDescriptions = dataView.SortDescriptions;

                    if (CurrentOrganizedHeader != null)
                    {
                        AdornerLayer.GetAdornerLayer(CurrentOrganizedHeader).Remove(CurrentAdorner);
                        sortDescriptions.Clear();
                    }

                    ListSortDirection newDir = ListSortDirection.Ascending;
                    if (CurrentOrganizedHeader == header && CurrentAdorner.Direction == newDir)
                        newDir = ListSortDirection.Descending;

                    CurrentOrganizedHeader = header;
                    CurrentAdorner = new SortAdorner(header, newDir);
                    AdornerLayer.GetAdornerLayer(header).Add(CurrentAdorner);
                    sortDescriptions.Add(new SortDescription(sortBy, newDir));
                };
        }
    }

    public class SortAdorner : Adorner
    {
        public ListSortDirection Direction;
        private readonly GridViewColumnHeader Header;

        public SortAdorner(GridViewColumnHeader header, ListSortDirection direction)
            : base(header)
        {
            Header = header;
            Direction = direction;
        }

        private Geometry GetDefaultGlyph()
        {
            double x1 = Header.ActualWidth / 2 - 6;
            double x2 = x1 + 10;
            double x3 = x1 + 5;
            double y1 = 3/2;
            double y2 = y1 + 5/2;

            if (Direction == ListSortDirection.Ascending)
            {
                double tmp = y1;
                y1 = y2;
                y2 = tmp;
            }

            PathSegmentCollection pathSegmentCollection = new PathSegmentCollection
            {
                new LineSegment(new Point(x2, y1), true),
                new LineSegment(new Point(x3, y2), true)
            };

            PathFigure pathFigure = new PathFigure(
                new Point(x1, y1),
                pathSegmentCollection,
                true);

            PathFigureCollection pathFigureCollection = new PathFigureCollection
            {
                pathFigure
            };

            PathGeometry pathGeometry = new PathGeometry(pathFigureCollection);
            return pathGeometry;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawGeometry(Brushes.LightGray, new Pen(Brushes.Gray, 1.0), GetDefaultGlyph());
        }
    }
}