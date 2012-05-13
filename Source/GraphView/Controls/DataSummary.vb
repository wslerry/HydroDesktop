﻿Imports HydroDesktop.Interfaces.ObjectModel

Namespace Controls

    Public Class DataSummary

        Private _seriesPlotInfo As SeriesPlotInfo

        Public Sub New()

            ' This call is required by the Windows Form Designer.
            InitializeComponent()

            AddHandler VisibleChanged, AddressOf OnDataSummaryVisibleChanged
        End Sub

        Public Sub Plot(ByVal seriesPlotInfo As SeriesPlotInfo)

            _seriesPlotInfo = Nothing
            If Not Visible Then
                _seriesPlotInfo = seriesPlotInfo
                Return
            End If

            ClearStatTables()
            For Each seriesInfo In seriesPlotInfo.GetSeriesInfo()
                Plot(seriesInfo)
            Next
            StatTableStyling()
        End Sub

#Region "Private methods"

        Private Sub OnDataSummaryVisibleChanged(ByVal sender As Object, ByVal e As EventArgs)
            If Not Visible Then Return
            If _seriesPlotInfo Is Nothing Then Return
            Plot(_seriesPlotInfo)
        End Sub

        Private Sub Plot(ByRef options As OneSeriesPlotInfo)
            Dim siteName = options.SiteName
            Dim variableName = options.VariableName
            Dim table = options.DataTable
            Dim plotOptions = options.PlotOptions
            Dim seriesID = options.SeriesID

            Dim siteAndVariable = siteName + ", " + variableName

            Dim data = table
            If (Not plotOptions.UseCensoredData) Then
                Dim temp As DataTable = table.Copy
                Dim censoredRows() As DataRow = temp.Rows.Cast(Of DataRow).Where(Function(row) DataValue.IsCensored(row("CensorCode"))).ToArray()

                For Each censoredRow As DataRow In censoredRows
                    temp.Rows.Remove(censoredRow)
                Next censoredRow

                data = temp
            End If

            dgvStatSummary.Rows.Add(siteAndVariable, "ID " + seriesID.ToString)
            dgvStatSummary.Rows.Add("# Of Observations", Statistics.Count(data))
            dgvStatSummary.Rows.Add("# Of Censored Obs.", Statistics.CountCensored(data))
            dgvStatSummary.Rows.Add("Arithmetic Mean", Statistics.ArithmeticMean(data))
            dgvStatSummary.Rows.Add("Geometric Mean", Statistics.GeometricMean(data))
            dgvStatSummary.Rows.Add("Maximum", Statistics.Maximum(data))
            dgvStatSummary.Rows.Add("Minimum", Statistics.Minimum(data))
            dgvStatSummary.Rows.Add("Standard Deviation", Statistics.StandardDeviation(data))
            dgvStatSummary.Rows.Add("Coefficient of Variation", Statistics.CoefficientOfVariation(data))
            dgvStatSummary.Rows.Add("Percentiles 10%", Statistics.Percentile(data, 10))
            dgvStatSummary.Rows.Add("Percentiles 25%", Statistics.Percentile(data, 25))
            dgvStatSummary.Rows.Add("Percentiles 50%(median)", Statistics.Percentile(data, 50))
            dgvStatSummary.Rows.Add("Percentiles 75%", Statistics.Percentile(data, 75))
            dgvStatSummary.Rows.Add("Percentiles 90%", Statistics.Percentile(data, 90))
            dgvStatSummary.Rows.Add()
            dgvStatSummary.Columns(0).Width = siteAndVariable.Length * 7
            dgvStatSummary.AutoResizeColumns()
        End Sub

        Private Sub ClearStatTables()
            dgvStatSummary.Rows.Clear()
        End Sub

        Private Sub StatTableStyling()
            Dim count As Integer = 0
            Dim sizecount As Integer = dgvStatSummary.Rows.Count
            For Each i In DirectCast(dgvStatSummary.Rows, IEnumerable)
                dgvStatSummary.Rows(count).Cells(0).Style.BackColor = Drawing.Color.Yellow
                If (count Mod 15 = 0) And Not (count = sizecount) Then
                    dgvStatSummary.Rows(count).Cells(0).Style.BackColor = Drawing.Color.Aqua
                    dgvStatSummary.Rows(count).Cells(1).Style.BackColor = Drawing.Color.Aqua
                End If
                If count Mod 15 = 14 Then
                    dgvStatSummary.Rows(count).Cells(0).Style.BackColor = Drawing.Color.White
                End If
                count += 1
            Next

        End Sub

#End Region

    End Class
End Namespace