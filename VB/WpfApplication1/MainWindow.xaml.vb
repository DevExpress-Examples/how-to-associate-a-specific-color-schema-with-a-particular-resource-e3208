Imports Microsoft.VisualBasic
Imports System
Imports System.Data
Imports System.Windows
Imports System.Drawing
Imports System.Data.OleDb
Imports DevExpress.XtraScheduler
Imports System.Collections.Generic
Imports DevExpress.Xpf.Scheduler

Namespace WpfApplication1
	Partial Public Class MainWindow
		Inherits Window

		Private resourceColorSchemas As Dictionary(Of Object, SchedulerColorSchema) = New Dictionary(Of Object, SchedulerColorSchema)()

		Private dataSet As CarsDBDataSet
		Private adapter As CarsDBDataSetTableAdapters.CarSchedulingTableAdapter

		Public Sub New()
			InitializeComponent()

			schedulerControl1.Start = New System.DateTime(2010, 7, 15, 0, 0, 0, 0)

			Me.dataSet = New CarsDBDataSet()

			' Bind the scheduler storage to appointment data.
			Me.schedulerControl1.Storage.AppointmentStorage.DataSource = dataSet.CarScheduling

			' Load data into the 'CarsDBDataSet.CarScheduling' table.
			Me.adapter = New CarsDBDataSetTableAdapters.CarSchedulingTableAdapter()
			Me.adapter.Fill(dataSet.CarScheduling)

			' Bind the scheduler storage to resource data.
			Me.schedulerControl1.Storage.ResourceStorage.DataSource = dataSet.Cars

			' Load data into the 'CarsDBDataSet.Cars' table.
			Dim carsAdapter As New CarsDBDataSetTableAdapters.CarsTableAdapter()
			carsAdapter.Fill(dataSet.Cars)

			AddHandler schedulerControl1.Storage.AppointmentsInserted,
				AddressOf Storage_AppointmentsModified
			AddHandler schedulerControl1.Storage.AppointmentsChanged,
				AddressOf Storage_AppointmentsModified
			AddHandler schedulerControl1.Storage.AppointmentsDeleted,
				AddressOf Storage_AppointmentsModified

			AddHandler adapter.Adapter.RowUpdated, AddressOf adapter_RowUpdated

			' Get copies of color schemas that are currently used to paint resources.
			PrepareResourceColorSchemas()
		End Sub

		Private Sub Storage_AppointmentsModified(ByVal sender As Object, ByVal e As PersistentObjectsEventArgs)
			Me.adapter.Adapter.Update(Me.dataSet)
			Me.dataSet.AcceptChanges()
		End Sub

		Private Sub adapter_RowUpdated(ByVal sender As Object, ByVal e As OleDbRowUpdatedEventArgs)
			If e.Status = UpdateStatus.Continue AndAlso e.StatementType = StatementType.Insert Then
				Dim id As Integer = 0
				Using cmd As New OleDbCommand("SELECT @@IDENTITY", adapter.Connection)
					id = CInt(Fix(cmd.ExecuteScalar()))
				End Using
				e.Row("ID") = id
			End If
		End Sub

		Private Sub PrepareResourceColorSchemas()
			Dim count As Integer = schedulerControl1.Storage.ResourceStorage.Count
			Dim currentColorchemas As SchedulerColorSchemaCollection =
				schedulerControl1.GetResourceColorSchemasCopy()
			Dim schemaCount As Integer = currentColorchemas.Count
			For i As Integer = 0 To count - 1
				Dim resource As Resource = schedulerControl1.Storage.ResourceStorage(i)
				resourceColorSchemas.Add(resource.Id, currentColorchemas(i Mod schemaCount))
			Next i
		End Sub

		Private Sub schedulerControl1_QueryResourceColorSchema(ByVal sender As Object, ByVal e As QueryResourceColorSchemaEventArgs)
			Dim key As Object = e.Resource.Id
			If Me.resourceColorSchemas.ContainsKey(key) Then
				e.ResourceColorSchema = Me.resourceColorSchemas(key)
			End If

			' Specify the custom color schema for the selected resource 
			' if there are more than one visible resources.
			If ShouldHighlightResource(e.Resource) Then
				e.ResourceColorSchema = New SchedulerColorSchema(Colors.LightCyan)
			End If
		End Sub

		Protected Function ShouldHighlightResource(ByVal resource As Resource) As Boolean
			If schedulerControl1.ActiveView.GetResources().Count <= 1 Then
				Return False
			End If

			Return schedulerControl1.SelectedResource.Equals(resource)
		End Function

		Private selectedResource As Resource = ResourceEmpty.Resource
		Private Sub schedulerControl1_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs)
			If (Not selectedResource.Equals(schedulerControl1.SelectedResource)) Then
				selectedResource = schedulerControl1.SelectedResource
				schedulerControl1.ActiveView.LayoutChanged()
			End If
		End Sub
	End Class
End Namespace
