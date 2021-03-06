using Buco.Data;
using BucoDesktop.Providers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BucoDesktop.Views
{
    public partial class MealEntriesView : Form
    {
        private readonly MealEntriesProvider Provider;
        private int CurrentPage = 1;
        private int TotalPages;
        protected int selectedEntryId = 0;
        protected int petId = 0;

        AutoCompleteStringCollection PetCollection = new AutoCompleteStringCollection();

        private readonly string DB_ERROR = "An error has occured with the database! Please try again...";
        private readonly string CREATE_SUCCESS = "New meal entry has successfully been created.";
        private readonly string INVALID_INPUT = "Invalid input!";

        private readonly string UPDATE_SUCCESS = "Meal entry has successfully been updated.";

        private readonly string DELETE_SUCCESS = "Weight entry has successfully been deleted!";

        public MealEntriesView(ApplicationDbContext dataContext)
        {
            Provider = new MealEntriesProvider(dataContext);
            InitializeComponent();
            Icon = new Icon("icon.ico");
            TotalPages = Provider.GetTotalPages();
            SetPagingButtons();
            DataGrid.DataSource = GetGridData(CurrentPage);
            DataGrid.RowHeaderMouseClick += DataGrid_RowHeaderMouseClick;
            SizeGrid();

            PetTextBox.AutoCompleteMode = AutoCompleteMode.Suggest;
            PetTextBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            PetTextBox.AutoCompleteCustomSource = PetCollection;

            PopulatePetCollection();
        }

        private void SetPagingButtons()
        {
            if (CurrentPage == 1)
            {
                PreviousButton.Enabled = false;
            }
            else
            {
                PreviousButton.Enabled = true;
            }

            if (CurrentPage == TotalPages)
            {
                NextButton.Enabled = false;
            }
            else
            {
                NextButton.Enabled = true;
            }
        }

        private DataTable GetGridData(int page)
        {
            var entries = Provider.PopulateGrid(page).ToList();
            DataTable tableData = new DataTable();
            tableData.Columns.Add("ID", typeof(int));
            tableData.Columns[0].ColumnName = "Meal entry ID";
            tableData.Columns.Add("Time", typeof(string));
            tableData.Columns[1].ColumnName = "Meal entry time";
            tableData.Columns.Add("Weight", typeof(int));
            tableData.Columns[2].ColumnName = "Calories";
            tableData.Columns.Add("Pet ID", typeof(int));
            tableData.Columns[3].ColumnName = "Pet ID";

            tableData.Columns[0].ReadOnly = true;
            tableData.Columns[1].ReadOnly = true;
            tableData.Columns[2].ReadOnly = true;
            tableData.Columns[3].ReadOnly = true;

            foreach (var entry in entries)
            {
                tableData.Rows.Add(entry.MealEntryId, entry.MealTime, entry.Calories, entry.PetId);
            }
            return tableData;
        }

        private void SizeGrid()
        {
            DataGridViewElementStates states = DataGridViewElementStates.None;
            DataGrid.ScrollBars = ScrollBars.None;
            var totalHeight = DataGrid.Rows.GetRowsHeight(states) + DataGrid.ColumnHeadersHeight;
            var totalWidth = DataGrid.Columns.GetColumnsWidth(states) + DataGrid.RowHeadersWidth;
            DataGrid.ClientSize = new Size(totalWidth, totalHeight);
        }

        private void PopulatePetCollection()
        {
            IEnumerable<string> petNames = Provider.GetAllPetNames();
            foreach (var name in petNames)
            {
                PetCollection.Add(name);
            }
        }

        private void DataGrid_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            selectedEntryId = Convert.ToInt32(DataGrid.Rows[e.RowIndex].Cells[0].Value.ToString());
            CalorieTextBox.Text = DataGrid.Rows[e.RowIndex].Cells[2].Value.ToString();
            PetTextBox.Text = Provider.GetPetName(Convert.ToInt32(DataGrid.Rows[e.RowIndex].Cells[3].Value.ToString()));
            petId = Provider.GetPet(PetTextBox.Text);
            DeleteButton.Enabled = true;
            UpdateButton.Enabled = true;
        }

        private void ResetDataGrid()
        {
            DataGrid.DataSource = GetGridData(CurrentPage);
            PetTextBox.Text = "";
            CalorieTextBox.Text = "";
            selectedEntryId = 0;
            petId = 0;
            UpdateButton.Enabled = false;
            DeleteButton.Enabled = false;
        }

        private void PetTextBox_TextChanged(object sender, EventArgs e)
        {
            if (PetTextBox.TextLength > 2)
            {
                var petCollection = Provider.GetPetNames(PetTextBox.Text);
                foreach (var name in petCollection)
                {
                    PetCollection.Add(name);
                }
                PetTextBox.AutoCompleteCustomSource = PetCollection;
            }
        }

        private bool ValidateInput()
        {
            if (petId == 0)
            {
                return false;
            }

            if (String.IsNullOrEmpty(CalorieTextBox.Text))
            {
                return false;
            }

            try
            {
                Convert.ToInt32(CalorieTextBox.Text);
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }

        private async void CreateButton_Click(object sender, EventArgs e)
        {
            petId = Provider.GetPet(PetTextBox.Text);

            if (ValidateInput())
            {
                var created = await Provider.CreateMealEnty(Convert.ToInt32(CalorieTextBox.Text), petId);
                if (!created)
                {
                    MessageBox.Show(DB_ERROR);
                }
                else
                {
                    MessageBox.Show(CREATE_SUCCESS);
                }
                ResetDataGrid();
                TotalPages = Provider.GetTotalPages();
                SizeGrid();
                SetPagingButtons();
            }
            else
            {
                MessageBox.Show(INVALID_INPUT);
            }
        }

        private async void UpdateButton_Click(object sender, EventArgs e)
        {
            petId = Provider.GetPet(PetTextBox.Text);

            if (ValidateInput())
            {
                var updated = await Provider.UpdateMealEntry(selectedEntryId, Convert.ToInt32(CalorieTextBox.Text), petId);
                if (!updated)
                {
                    MessageBox.Show(DB_ERROR);
                }
                else
                {
                    MessageBox.Show(UPDATE_SUCCESS);
                }
                ResetDataGrid();
            }
            else
            {
                MessageBox.Show(INVALID_INPUT);
            }
        }

        private async void DeleteButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you want to delete this row ?", "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var deleted = await Provider.DeleteMealEntry(selectedEntryId);
                if (!deleted)
                {
                    MessageBox.Show(DB_ERROR);
                }
                else
                {
                    MessageBox.Show(DELETE_SUCCESS);
                }
                ResetDataGrid();
                TotalPages = Provider.GetTotalPages();
                SizeGrid();
                SetPagingButtons();
            }
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                DataGrid.DataSource = GetGridData(CurrentPage);
                SizeGrid();
            }
            SetPagingButtons();           
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                this.CurrentPage += 1;
                DataGrid.DataSource = GetGridData(CurrentPage);
                SizeGrid();
            }
            SetPagingButtons();
        }
    }
}
