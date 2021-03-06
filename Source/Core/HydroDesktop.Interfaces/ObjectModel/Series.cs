﻿using System;
using System.Collections.Generic;

namespace HydroDesktop.Interfaces.ObjectModel
{
    /// <summary>
    /// Represents a time series. The time series is a combination of a specific site, variable,
    /// method, source and quality control level.
    /// </summary>
    public class Series : BaseEntity
    {
        #region Constructors

        /// <summary>
        /// Creates a new series with properties set to default.
        /// </summary>
        public Series()
        {
            DataValueList = new List<DataValue>();
            ValueCount = 0;
            ThemeList = new List<Theme>();
            Method = Method.Unknown;
            Source = Source.Unknown;
            QualityControlLevel = QualityControlLevel.Unknown;
        }

        /// <summary>
        /// Creates a new data series associated with the specific site, variable,
        /// method, quality control level and source. This series will contain zero
        /// data values after creation.
        /// </summary>
        /// <param name="site">the observation site (location of measurement)</param>
        /// <param name="variable">the observed variable</param>
        /// <param name="method">the observation method</param>
        /// <param name="qualControl">the quality control level of observed values</param>
        /// <param name="source">the source of the data values for this series</param>
        public Series(Site site, Variable variable, Method method, QualityControlLevel qualControl, Source source)
        {
            DataValueList = new List<DataValue>();
            ValueCount = 0;
            ThemeList = new List<Theme>();
            
            Site = site;
            Variable = variable;
            Method = method;
            QualityControlLevel = qualControl;
            Source = source;
        }

        /// <summary>
        /// Creates a copy of the original series. If copyDataValues is set to true,
        /// then the data values are also copied. 
        /// The new series shares the same site, variable, source, method and quality
        /// control level. The new series does not belong to any data theme.
        /// </summary>
        /// <param name="original">The original series</param>
        /// <param name="copyDataValues">if set to true, then all data values are copied</param>
        public Series(Series original, bool copyDataValues)
        {
            //TODO: need to include series provenance information
            
            BeginDateTime = original.BeginDateTime;
            EndDateTime = original.EndDateTime;
            CreationDateTime = DateTime.Now;
            DataValueList = original.DataValueList;
            EndDateTime = original.EndDateTime;
            EndDateTimeUTC = original.EndDateTimeUTC;
            IsCategorical = original.IsCategorical;
            Method = original.Method;
            QualityControlLevel = original.QualityControlLevel;
            Source = original.Source;
            UpdateDateTime = DateTime.Now;
            ValueCount = original.ValueCount;
            Variable = original.Variable;

            //to copy the data values
            if (copyDataValues)
            {
                foreach (DataValue originalDataValue in original.DataValueList)
                {
                    AddDataValue(originalDataValue.Copy());
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// True if the series represents categorical data
        /// </summary>
        public virtual bool IsCategorical { get; set; }

        /// <summary>
        /// The local time when the first value of the series was measured
        /// </summary>
        public virtual DateTime BeginDateTime { get; set; }

        /// <summary>
        /// The local time when the last value of the series was measured
        /// </summary>
        public virtual DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the begin date time of series in UTC
        /// </summary>
        public virtual DateTime BeginDateTimeUTC { get; set; }

        /// <summary>
        /// Gets or sets the end date time of the series in UTC
        /// </summary>
        public virtual DateTime EndDateTimeUTC { get; set; }

        /// <summary>
        /// The number of data values in this series
        /// </summary>
        public virtual int ValueCount { get; set; }

        /// <summary>
        /// The time when the series has been saved to the HydroDesktop 
        /// repository
        /// </summary>
        public virtual DateTime CreationDateTime { get; set; }

        /// <summary>
        /// A 'Subscribed' Data series may be regularly updated by appending data
        /// </summary>
        public virtual bool Subscribed { get; set; }

        /// <summary>
        /// The time when this data series was last updated (its data values were changed)
        /// </summary>
        public virtual DateTime UpdateDateTime { get; set; }
        
        /// <summary>
        /// Time when this series was last checked
        /// </summary>
        public virtual DateTime LastCheckedDateTime { get; set; }

        /// <summary>
        /// The site where the data is measured
        /// </summary>
        public virtual Site Site { get; set; }

        /// <summary>
        /// The measured variable
        /// </summary>
        public virtual Variable Variable { get; set; }

        /// <summary>
        /// The method of measurement
        /// </summary>
        public virtual Method Method { get; set; }      

        /// <summary>
        /// The primary source of the data
        /// </summary>
        public virtual Source Source { get; set; }      

        /// <summary>
        /// The primary quality control level of the data
        /// </summary>
        public virtual QualityControlLevel QualityControlLevel { get; set; }

        /// <summary>
        /// The list of all values belonging to this data series
        /// </summary>
        public virtual IList<DataValue> DataValueList { get; protected set; }

        /// <summary>
        /// The list of all themes containing this series
        /// </summary>
        public virtual IList<Theme> ThemeList { get; protected set; }
     
        #endregion

        #region Public methods

        public void UpdateSeriesInfoFromDataValues()
        {
            if (DataValueList.Count > 0)
            {
                ValueCount = DataValueList.Count;
                EndDateTimeUTC = DataValueList[DataValueList.Count - 1].DateTimeUTC;
                BeginDateTimeUTC = DataValueList[0].DateTimeUTC;

                EndDateTime = DataValueList[DataValueList.Count - 1].LocalDateTime;
                BeginDateTime = DataValueList[0].LocalDateTime;
            }
            else
            {
                ValueCount = 0;
            }
        }

        /// <summary>
        /// String representation of the series
        /// <returns>SiteName | VariableName | DataType</returns>
        /// </summary>
        public override string ToString()
        {
            return Site.Name + "|" + Variable.Name + "|" + Variable.DataType;
        }

        /// <summary>
        /// Associates an existing data value with this data series
        /// </summary>
        /// <param name="val"></param>
        public virtual void AddDataValue(DataValue val)
        { 
            DataValueList.Add(val);
            val.Series = this;
            UpdateSeriesInfoFromDataValues();
        }

        /// <summary>
        /// Adds a data value to the end of this series
        /// </summary>
        /// <param name="time">the local observation time of the data value</param>
        /// <param name="value">the observation value</param>
        /// <returns>the DataValue object</returns>
        public virtual void AddDataValue(DateTime time, double value)
        {
            var val = new DataValue(value, time, 0.0);
            AddDataValue(val);
        }

        /// <summary>
        /// Adds a data value to the end of this series
        /// </summary>
        /// <param name="time">the local observation time of the data value</param>
        /// <param name="value">the observed value</param>
        /// <param name="utcOffset">the difference between UTC and local time</param>
        /// <param name="qualifier">the qualifier (contains information about specific
        ///   observation conditions</param>
        /// <returns>the DataValue object</returns>
        public virtual void AddDataValue(DateTime time, double value, double utcOffset, Qualifier qualifier)
        {
            var val = new DataValue(value, time, utcOffset) {Qualifier = qualifier};
            AddDataValue(val);
        }

        /// <summary>
        /// Shortcut method, to obtain the ValueCount from the DataValueList
        /// </summary>
        /// <returns>The number of DataValues in the DataValueList</returns>
        public virtual int GetValueCount()
        {
            return DataValueList == null ? 0 : DataValueList.Count;
        }

        #endregion
    }
}
