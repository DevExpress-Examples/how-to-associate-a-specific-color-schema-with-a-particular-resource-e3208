using System;
using System.Data;
using System.Windows;
using System.Drawing;
using System.Data.OleDb;
using DevExpress.XtraScheduler;
using System.Collections.Generic;
using DevExpress.Xpf.Scheduler;
using System.Windows.Media;

namespace WpfApplication1 {
    public partial class MainWindow : Window {

        Dictionary<object, SchedulerColorSchema> resourceColorSchemas = 
            new Dictionary<object, SchedulerColorSchema>();

        CarsDBDataSet dataSet;
        CarsDBDataSetTableAdapters.CarSchedulingTableAdapter adapter;

        public MainWindow() {
            InitializeComponent();

            schedulerControl1.Start = new System.DateTime(2010, 7, 15, 0, 0, 0, 0);

            this.dataSet = new CarsDBDataSet();

            // Bind the scheduler storage to appointment data.
            this.schedulerControl1.Storage.AppointmentStorage.DataSource = dataSet.CarScheduling;

            // Load data into the 'CarsDBDataSet.CarScheduling' table.
            this.adapter = new CarsDBDataSetTableAdapters.CarSchedulingTableAdapter();
            this.adapter.Fill(dataSet.CarScheduling);

            // Bind the scheduler storage to resource data.
            this.schedulerControl1.Storage.ResourceStorage.DataSource = dataSet.Cars;

            // Load data into the 'CarsDBDataSet.Cars' table.
            CarsDBDataSetTableAdapters.CarsTableAdapter carsAdapter = 
                new CarsDBDataSetTableAdapters.CarsTableAdapter();
            carsAdapter.Fill(dataSet.Cars);

            this.schedulerControl1.Storage.AppointmentsInserted += 
                new PersistentObjectsEventHandler(Storage_AppointmentsModified);
            this.schedulerControl1.Storage.AppointmentsChanged += 
                new PersistentObjectsEventHandler(Storage_AppointmentsModified);
            this.schedulerControl1.Storage.AppointmentsDeleted += 
                new PersistentObjectsEventHandler(Storage_AppointmentsModified);

            this.adapter.Adapter.RowUpdated += 
                new System.Data.OleDb.OleDbRowUpdatedEventHandler(adapter_RowUpdated);

            // Get copies of color schemas that are currently used to paint resources.
            PrepareResourceColorSchemas();
        }

        void Storage_AppointmentsModified(object sender, PersistentObjectsEventArgs e) {
            this.adapter.Adapter.Update(this.dataSet);
            this.dataSet.AcceptChanges();
        }

        private void adapter_RowUpdated(object sender, OleDbRowUpdatedEventArgs e) {
            if (e.Status == UpdateStatus.Continue && e.StatementType == StatementType.Insert) {
                int id = 0;
                using (OleDbCommand cmd = new OleDbCommand("SELECT @@IDENTITY", adapter.Connection)) {
                    id = (int)cmd.ExecuteScalar();
                }
                e.Row["ID"] = id;
            }
        }

        private void PrepareResourceColorSchemas() {
            int count = schedulerControl1.Storage.ResourceStorage.Count;
            SchedulerColorSchemaCollection currentColorchemas = 
                schedulerControl1.GetResourceColorSchemasCopy();
            int schemaCount = currentColorchemas.Count;
            for (int i = 0; i < count; i++) {
                Resource resource = schedulerControl1.Storage.ResourceStorage[i];
                resourceColorSchemas.Add(resource.Id, currentColorchemas[i % schemaCount]);
            }
        }

        private void schedulerControl1_QueryResourceColorSchema(object sender, QueryResourceColorSchemaEventArgs e) {
            object key = e.Resource.Id;
            if (this.resourceColorSchemas.ContainsKey(key))
                e.ResourceColorSchema = this.resourceColorSchemas[key];
            
            // Specify the custom color schema for the selected resource 
            // if there are more than one visible resources.
            if (ShouldHighlightResource(e.Resource)) {
                e.ResourceColorSchema = new SchedulerColorSchema(Colors.LightCyan);
            }
        }

        protected bool ShouldHighlightResource(Resource resource) { 
            if (schedulerControl1.ActiveView.GetResources().Count <= 1)
                return false;

            return schedulerControl1.SelectedResource.Equals(resource);
        }

        Resource selectedResource = ResourceEmpty.Resource;
        private void schedulerControl1_SelectionChanged(object sender, EventArgs e) {
            if (!selectedResource.Equals(schedulerControl1.SelectedResource)) {
                selectedResource = schedulerControl1.SelectedResource;
                schedulerControl1.ActiveView.LayoutChanged();
            }
        }
    }
}
