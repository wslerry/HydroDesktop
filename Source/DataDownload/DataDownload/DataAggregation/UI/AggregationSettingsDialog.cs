﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Windows.Forms;
using DotSpatial.Controls;
using DotSpatial.Data;
using DotSpatial.Symbology;
using HydroDesktop.Common.Tools;
using HydroDesktop.Configuration;
using HydroDesktop.Database;
using HydroDesktop.Interfaces;
using IProgressHandler = HydroDesktop.Common.IProgressHandler;

namespace HydroDesktop.DataDownload.DataAggregation.UI
{
    /// <summary>
    /// Settings form for aggregation
    /// </summary>
    public partial class AggregationSettingsDialog : Form, IProgressHandler
    {
        #region Fields

        private readonly IFeatureLayer _layer;
        private readonly AggregationSettings _settings;

        #endregion

        #region Constructors

        /// <summary>
        /// Create new instance of <see cref="AggregationSettingsDialog"/>
        /// </summary>
        /// <param name="layer">Layer to aggregate</param>
        public AggregationSettingsDialog(IFeatureLayer layer)
        {
            if (layer == null) throw new ArgumentNullException("layer");
            Contract.EndContractBlock();

            InitializeComponent();

            _layer = layer;
            _settings = new AggregationSettings();

            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs eventArgs)
        {
            // Set bindings
            cmbMode.DataSource = Enum.GetValues(typeof(AggregationMode));
            cmbMode.Format += delegate(object s, ListControlConvertEventArgs args)
            {
                args.Value = ((AggregationMode)args.ListItem).Description();
            };
            cmbMode.DataBindings.Clear();
            cmbMode.DataBindings.Add("SelectedItem", _settings, "AggregationMode", true,
                                     DataSourceUpdateMode.OnPropertyChanged);

            dtpStartTime.DataBindings.Clear();
            dtpStartTime.DataBindings.Add("Value", _settings, "StartTime", true, DataSourceUpdateMode.OnPropertyChanged);

            dtpEndTime.DataBindings.Clear();
            dtpEndTime.DataBindings.Add("Value", _settings, "EndTime", true, DataSourceUpdateMode.OnPropertyChanged);

            cmbVariable.DataBindings.Clear();
            cmbVariable.DataBindings.Add("SelectedItem", _settings, "VariableCode", true,
                                         DataSourceUpdateMode.OnPropertyChanged);

            chbCreateNewLayer.DataBindings.Clear();
            chbCreateNewLayer.DataBindings.Add("Checked", _settings, "CreateNewLayer", true,
                                               DataSourceUpdateMode.OnPropertyChanged);

            // Set initial CreateNewLayer
            _settings.CreateNewLayer = true;

            // Set initial StartTime, EndTime
            var minStartTime = DateTime.MaxValue;
            var maxEndTime = DateTime.MinValue;

            foreach (var feature in _layer.DataSet.Features)
            {
                var startDateRow = feature.DataRow["StartDate"];
                var endDateRow = feature.DataRow["EndDate"];

                var startDate = Convert.ToDateTime(startDateRow);
                var endDate = Convert.ToDateTime(endDateRow);

                if (minStartTime > startDate)
                {
                    minStartTime = startDate;
                }
                if (maxEndTime < endDate)
                {
                    maxEndTime = endDate;
                }
            }
            if (minStartTime == DateTime.MaxValue)
            {
                minStartTime = DateTime.Now;
            }
            if (maxEndTime == DateTime.MinValue)
            {
                maxEndTime = DateTime.Now;
            }
            _settings.StartTime = minStartTime;
            _settings.EndTime = maxEndTime;

            // Get all variables associated with current layer
            var seriesRepo = RepositoryFactory.Instance.Get<IDataSeriesRepository>(DatabaseTypes.SQLite,
                                                                                   Settings.Instance.
                                                                                       DataRepositoryConnectionString);
            var uniqueVariables = new List<string>();
            foreach (var feature in _layer.DataSet.Features)
            {
                var seriesIDValue = feature.DataRow["SeriesID"];
                if (seriesIDValue == null || seriesIDValue == DBNull.Value)
                    continue;
                var seriesID = Convert.ToInt64(seriesIDValue);
                var series = seriesRepo.GetSeriesByID(seriesID);
                if (series == null) continue;

                var curVar = series.Variable.Code;
                if (!uniqueVariables.Contains(curVar))
                {
                    uniqueVariables.Add(curVar);
                }
            }
            if (uniqueVariables.Count > 0)
            {
                _settings.VariableCode = uniqueVariables[0];
            }
            cmbVariable.DataSource = uniqueVariables;

            //
            btnOK.Enabled = _layer.DataSet.Features.Count > 0;
        }

        #endregion

        #region Private methods

        private BackgroundWorker _backgroundWorker;

        private void btnOK_Click(object sender, EventArgs e)
        {
            SetControlsToCalculation();

            _backgroundWorker = new BackgroundWorker { WorkerReportsProgress = true };
            _backgroundWorker.DoWork += delegate(object o, DoWorkEventArgs args)
                                 {
                                     var aggregator = new Aggregator(_settings, _layer)
                                                          {
                                                              ProgressHandler = this,
                                                              MaxPercentage = 97,
                                                          };
                                     args.Result = aggregator.Calculate();
                                 };
            _backgroundWorker.RunWorkerCompleted += delegate(object o, RunWorkerCompletedEventArgs args)
                                             {
                                                 if (args.Error != null)
                                                 {
                                                     MessageBox.Show("Error occured:" + Environment.NewLine + 
                                                                     args.Error.Message, "Aggregation",
                                                                     MessageBoxButtons.OK, MessageBoxIcon.Error);
                                                 }
                                                 else
                                                 {
                                                     // This actions must be executed only in UI thread

                                                     var featureSet = (IFeatureSet)args.Result;

                                                     // Save updated data
                                                     ReportProgress(98, "Saving data");
                                                     if (!string.IsNullOrEmpty(featureSet.Filename))
                                                     {
                                                         featureSet.Save();
                                                     }
                                                     
                                                     if (_settings.CreateNewLayer)
                                                     {
                                                         ReportProgress(99, "Adding layer to map");
                                                         
                                                         var mapLayer = new MapPointLayer(featureSet) { LegendText = Path.GetFileNameWithoutExtension(featureSet.Filename) };
                                                         _layer.MapFrame.Add(mapLayer);
                                                     }

                                                     ReportProgress(100, "Finished");

                                                     DialogResult = DialogResult.OK;
                                                     Close();   
                                                 }
                                             };
            _backgroundWorker.RunWorkerAsync();
        }

        private void SetControlsToCalculation()
        {
            paSettings.Enabled = false;
            pbProgress.Visible = lblProgress.Visible = true;
            btnOK.Enabled = btnCancel.Enabled = false;
        }

        #endregion

        #region IProgressHandler implementation

        public void ReportProgress(int persentage, object state)
        {
            pbProgress.UIThread(() => pbProgress.Value = persentage);
            lblProgress.UIThread(() => lblProgress.Text = state != null ? state.ToString() : string.Empty);
        }

        public void CheckForCancel()
        {
            var bw = _backgroundWorker;
            if (bw != null && bw.WorkerSupportsCancellation)
            {
                bw.CancelAsync();
            }
        }

        public void ReportMessage(string message)
        {
            lblProgress.UIThread(() => lblProgress.Text = message);
        }

        #endregion
    }
}
