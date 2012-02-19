using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using HydroDesktop.Interfaces;
using HydroDesktop.Interfaces.ObjectModel;

namespace HydroDesktop.Database
{
    /// <summary>
    /// Repository for DataThemes
    /// </summary>
    class DataThemesRepository : BaseRepository, IDataThemesRepository
    {
        #region Constants

        private const string OTHER_DATA_SERIES = "Other Data Series";

        #endregion

        #region Constructors

        public DataThemesRepository(DatabaseTypes dbType, string connectionString) : base(dbType, connectionString)
        {
        }

        public DataThemesRepository(IHydroDbOperations db)
            : base(db)
        {
        }

        #endregion

        #region Public methods

        public DataTable GetThemesForAllSeries()
        {
            var dtThemes = DbOperations.LoadTable(TableName, "SELECT ThemeID, ThemeName from DataThemeDescriptions");
            if (Int32.Parse(DbOperations.ExecuteSingleOutput("Select count(*) from DataSeries " +
                                                             "Where SeriesID not in (Select SeriesID from DataThemes)").
                                ToString()) > 0)
            {
                dtThemes.Rows.Add(DBNull.Value, OTHER_DATA_SERIES);
            }
            return dtThemes;
        }
      
        public int? GetID(string themeName)
        {
            const string sql = "SELECT ThemeID from DataThemeDescriptions WHERE ThemeName =?";
            var objThemeId = DbOperations.ExecuteSingleOutput(sql, new[] { themeName });
            if (objThemeId == null || objThemeId == DBNull.Value) return null;
            return Convert.ToInt32(objThemeId);
        }

        /// <summary>
        /// Deletes a theme and all its series as long as the series don't belong to any other theme.
        /// </summary>
        /// <param name="themeID">The Theme ID</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool DeleteTheme(int themeID)
        {
            string sqlTheme = "SELECT SeriesID FROM DataThemes where ThemeID = " + themeID;
            DataTable tblSeries = DbOperations.LoadTable("tblSeries", sqlTheme);

            foreach (DataRow seriesRow in tblSeries.Rows)
            {
                int seriesID = Convert.ToInt32(seriesRow["SeriesID"]);

                var seriesRepository = RepositoryFactory.Instance.Get<IDataSeriesRepository>();
                seriesRepository.DeleteSeries(seriesID);
            }

            //delete the actual theme
            string sqlDeleteTheme = "DELETE FROM DataThemeDescriptions WHERE ThemeID = " + themeID;
            try
            {
                DbOperations.ExecuteNonQuery(sqlDeleteTheme);
            }
            catch { };

            //re-check the number of series in the theme

            return true;
        }

        /// <summary>
        /// Delete a theme - a background worker and progress bar is used
        /// </summary>
        /// <param name="themeID">The themeID (this needs to be a valid ID)</param>
        /// <param name="worker">The background worker component</param>
        /// <param name="e">The arguments for background worker</param>
        /// <returns></returns>
        public bool DeleteTheme(int themeID, BackgroundWorker worker, DoWorkEventArgs e)
        {
            string sqlTheme = "SELECT SeriesID FROM DataThemes where ThemeID = " + themeID;
            DataTable tblSeries = DbOperations.LoadTable("tblSeries", sqlTheme);

            int numSeries = tblSeries.Rows.Count;
            int count = 0;

            if (numSeries == 0)
            {
                return false;
            }

            foreach (DataRow seriesRow in tblSeries.Rows)
            {
                if (worker != null)
                {
                    //check cancellation
                    if (e != null && worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return false;
                    }
                }

                int seriesID = Convert.ToInt32(seriesRow["SeriesID"]);

                var seriesRepository = RepositoryFactory.Instance.Get<IDataSeriesRepository>();
                seriesRepository.DeleteSeries(seriesID);

                //progress report
                count++;

                if (worker != null && worker.WorkerReportsProgress)
                {
                    int percent = (int)(((float)count / (float)numSeries) * 100);
                    string userState = "Deleting series " + count + " of " + numSeries + "...";
                    worker.ReportProgress(percent, userState);
                }
            }

            //delete the actual theme

            string sqlDeleteTheme = "DELETE FROM DataThemeDescriptions WHERE ThemeID = " + themeID;
            try
            {
                DbOperations.ExecuteNonQuery(sqlDeleteTheme);
                e.Result = "Theme deleted successfully";
            }
            catch { };

            return true;
        }


        /// <summary>
        /// Gets all themes from the database ordered by the theme name
        /// </summary>
        /// <returns>The list of all themes</returns>
        public IList<Theme> GetAllThemes()
        {
            string sql = "SELECT ThemeID, ThemeName, ThemeDescription FROM DataThemeDescriptions";
            DataTable table = DbOperations.LoadTable("tblThemes", sql);

            if (table.Rows.Count == 0)
            {
                return new List<Theme>();
            }
            else
            {
                List<Theme> themeList = new List<Theme>();
                foreach (DataRow row in table.Rows)
                {
                    Theme newTheme = new Theme(row[1].ToString(), row[2].ToString());
                    newTheme.Id = Convert.ToInt32(row[0]);
                    themeList.Add(newTheme);
                }
                return themeList;
            }
        }

        #endregion

        public override string TableName
        {
            get { return "DataThemes"; }
        }
    }
}