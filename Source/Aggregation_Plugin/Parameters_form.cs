﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DotSpatial.Controls;
using DotSpatial.Controls.Header;
using System.Windows.Controls;
using DotSpatial.Data;
using DotSpatial.Topology;

namespace Aggregation_Plugin
{
    public partial class Parameters_form : Form
    {
        AppManager App;
        FeatureSet polygons = new FeatureSet(FeatureType.Polygon);

        public Parameters_form(AppManager App)
        {
            InitializeComponent();
            this.App = App;
            populatePolygonLayerDropdown();
            populateSites();
            App.Map.MapFrame.SelectionChanged += SelectionChanged;
            PolygonLayerList.SelectedValueChanged += SelectionChanged;
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            if (PolygonLayerList.SelectedValue != null)
                getPolygons((IMapPolygonLayer)PolygonLayerList.SelectedValue);
        }

        private void populatePolygonLayerDropdown()
        {
            var map = (Map)App.Map;
            Dictionary<IMapPolygonLayer, string> layer = new Dictionary<IMapPolygonLayer, string>();

            foreach (var polygonLayer in map.GetAllLayers().OfType<IMapPolygonLayer>().Reverse())
                layer.Add(polygonLayer, polygonLayer.LegendText);

            if (layer.Count > 0)
            {
                PolygonLayerList.DataSource = new BindingSource(layer, null);
                PolygonLayerList.DisplayMember = "Value";
                PolygonLayerList.ValueMember = "Key";
            }
        }

        private void populateSites()
        {
            var map = (Map)App.Map;
            Dictionary<IMapPointLayer, string> layer = new Dictionary<IMapPointLayer, string>();

            foreach (var pointLayer in map.GetAllLayers().OfType<IMapPointLayer>().Reverse())
                layer.Add(pointLayer, pointLayer.LegendText);

            if (layer.Count > 0)
            {
                SiteList.DataSource = new BindingSource(layer, null);
                SiteList.DisplayMember = "Value";
                SiteList.ValueMember = "Key";
            }
        }

        private void populateVariables()
        {
            foreach(var item in SiteList.Items)
            {
                IMapPointLayer pointLayer = ((KeyValuePair<IMapPointLayer, string>)item).Key;
                var features = pointLayer.DataSet.Features;
                //Dictionay of points

                foreach(var point in features)
                {
                    //point.Intersects(
                }
            }
        }

        private void getPolygons(IMapPolygonLayer polyLayer)
        {
            polygons.Features.Clear();

            if (polyLayer.IsVisible && polyLayer.Selection.Count > 0)
            {
                foreach (var f in polyLayer.Selection.ToFeatureList())
                {
                    polygons.Features.Add(f);
                }

                polygons.Projection = App.Map.Projection;
            }

            populateVariables();
        }

    }
}
