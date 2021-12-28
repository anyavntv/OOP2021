using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab1

{
    public partial class Tabledit : Form
    {
        private const int _maxCols = 10;
        private const int _maxRows = 10;
        private const int _rowHeaderWidth = 75;
        private const float _defaultFontSize = 19F;
        private const string _defaultFontName = "Microsoft Sans Serif";

        private const string CAPTION_ERR = "Помилка";
        private const string CAPTION_WARNING = "Попередження";
        private const string CAPTION_DELETE_ROW = "Видалення рядка";
        private const string CAPTION_DELETE_COL = "Видалення стовпчика";
        


        private const string WARNING_ERROR = "Виправте помилку в таблиці перед збереженням";
        private const string WARNING_SAVE = "Зберегти поточну таблицю?";

        private const string ASK_DELETE_ROW = "Ви точно хочете видалити поточний рядок?";
        private const string ASK_DELETE_COL = "Ви точно хочете видалити поточний стовпчик?";

        private const string ERROR_LAST_ROW = "Ви не можете видалити останній рядок";
        private const string ERROR_LAST_COL = "Ви не можете видалити останній стовпчик";
        private const string ERROR_DEPENDENCIES_ROW = "У таблиці наявні посилання на комірки рядку, який ви намагаєтеся видалити. Будь ласка, видаліть їх та спробуйте ще раз";
        private const string ERROR_DEPENDENCIES_COL = "У таблиці наявні посилання на комірки стовпчика, який ви намагаєтеся видалити. Будь ласка, видаліть їх та спробуйте ще раз";

        

        private string _currentFilePath = "";
        private bool _formulaView = false;
        private bool _errorOccured = false;

        
        public Tabledit()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;

            InitializeDataGridView();
            InitializeAllCells();
            CellManager.Instance.DataGridView = dataGridView;
        }

        // Sets up the initial state of a data grid view.
        private void InitializeDataGridView()
        {
            dataGridView.AllowUserToAddRows = false;
            dataGridView.ColumnCount = _maxCols;
            dataGridView.RowCount = _maxRows;

            FillHeaders();

            dataGridView.AutoResizeRows();
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView.RowHeadersWidth = _rowHeaderWidth;
        }

        // Sets up the data grid view's headers with corresponding indices.
        private void FillHeaders()
        {
            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                col.HeaderText = "C" + (col.Index + 1);
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                row.HeaderCell.Value = "R" + (row.Index + 1);
            }
        }

        // Names each cell in the table according to its index
        // and attaches the 'Cell' - formula, value and name holder - to it.
        private void InitializeAllCells()
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                row.DefaultCellStyle.Font = new Font(_defaultFontName, _defaultFontSize, GraphicsUnit.Point);

                foreach (DataGridViewCell cell in row.Cells)
                {
                    InitializeSingleCell(row, cell);
                }
            }
        }

        // Names a single cell in the table according to its index
        // and attaches the 'Cell' - formula, value and name holder - to it.
        private void InitializeSingleCell(DataGridViewRow row, DataGridViewCell cell)
        {
            string cellName = "R" + (row.Index + 1).ToString() + "C" + (cell.ColumnIndex + 1).ToString();
            cell.Tag = new Cell(cell, cellName, "0");
            cell.Value = "0";
        }

        // Refreshes values of all cells.
        private void UpdateCellValues()
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell dgvCell in row.Cells)
                {
                    UpdateSingleCellValue(dgvCell);
                }
            }
        }

        // Refreshes values of all cells except of the one
        // that's just been edited.
        private void UpdateCellValues(DataGridViewCell invoker)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell dgvCell in row.Cells)
                {
                    if (invoker != dgvCell)
                    {
                        UpdateSingleCellValue(dgvCell);
                    }
                }
            }
        }

        // Updates a cell value depending on whether or not it holds formula.
        private void UpdateSingleCellValue(DataGridViewCell dgvCell)
        {
            Cell cell = (Cell)dgvCell.Tag;

            if (!_formulaView)
            {
                if (cell.Formula.Equals("") || Regex.IsMatch(cell.Formula,@"^\d+$"))
                {
                    dgvCell.Value = cell.Value;
                }
                else
                {
                    dgvCell.Value = cell.Evaluate();
                }
            }
            else
            {
                dgvCell.Value = cell.Formula;
            }
        }

        // Adds a new row under the data grid view's last one.
        private void AddRow()
        {
            dataGridView.Rows.Add(new DataGridViewRow());
            FillHeaders();

            DataGridViewRow addedRow = dataGridView.Rows[dataGridView.RowCount - 1];
            addedRow.DefaultCellStyle.Font = new Font(_defaultFontName, _defaultFontSize, GraphicsUnit.Point);

            foreach (DataGridViewCell cell in addedRow.Cells)
            {
                InitializeSingleCell(addedRow, cell);
            }
        }

        // Deletes the data grid view's last row.
        private void DeleteRow()
        {
            DialogResult result = MessageBox.Show(ASK_DELETE_ROW, CAPTION_DELETE_ROW, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                if (dataGridView.RowCount == 1)
                {
                    MessageBox.Show(ERROR_LAST_ROW, CAPTION_ERR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (DeletedRowHasDependencies())
                {
                    MessageBox.Show(ERROR_DEPENDENCIES_ROW, CAPTION_ERR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int lastRowInd = dataGridView.RowCount - 1;
                dataGridView.Rows.RemoveAt(lastRowInd);
            }
        }

        // Indicates whether or not the cells of the row being deleted
        // are referred to in the remaining cells.
        private bool DeletedRowHasDependencies()
        {
            List<string> deletedNames = new List<string>();
            int lastInd = dataGridView.RowCount - 1;

            foreach (DataGridViewCell dgvCell in dataGridView.Rows[lastInd].Cells)
            {
                Cell cell = (Cell)dgvCell.Tag;
                deletedNames.Add(cell.Name);
            }

            return FindDeletedRowDependenciesInTable(deletedNames, lastInd);
        }

        // Loops the row being deleted to find dependencies mentioned thereover,
        // and returns the indication of whether or not they are present.
        private bool FindDeletedRowDependenciesInTable(List<string> deletedNames, int lastInd)
        {
            for (int i = 0; i < lastInd; i++)
            {
                foreach (DataGridViewCell dgvCell in dataGridView.Rows[i].Cells)
                {
                    Cell cell = (Cell)dgvCell.Tag;
                    List<Cell> refs = cell.CellReferences;

                    for (int j = refs.Count - 1; j >= 0; j--)
                    {
                        if (deletedNames.Contains(refs[j].Name))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Adds a new column to the right of the data grid view's last one.
        private void AddColumn()
        {
            dataGridView.Columns.Add(new DataGridViewColumn(dataGridView.Rows[0].Cells[0]));
            FillHeaders();

            foreach (DataGridViewRow dgvRow in dataGridView.Rows)
            {
                InitializeSingleCell(dgvRow, dgvRow.Cells[dataGridView.ColumnCount - 1]);
            }
        }

        // Deletes the data grid view's last column.
        private void DeleteColumn()
        {
            DialogResult result = MessageBox.Show(ASK_DELETE_COL, CAPTION_DELETE_COL, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                if (dataGridView.ColumnCount == 1)
                {
                    MessageBox.Show(ERROR_LAST_COL, CAPTION_ERR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (DeletedColumnHasDependencies())
                {
                    MessageBox.Show(ERROR_DEPENDENCIES_COL, CAPTION_ERR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int lastColInd = dataGridView.ColumnCount - 1;
                dataGridView.Columns.RemoveAt(lastColInd);
            }
        }

        // Indicates whether or not the cells of the column being deleted
        // are referred to in the remaining cells.
        private bool DeletedColumnHasDependencies()
        {
            List<string> deletedNames = new List<string>();
            int lastInd = dataGridView.ColumnCount - 1;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                Cell cell = (Cell)row.Cells[lastInd].Tag;
                deletedNames.Add(cell.Name);
            }

            return FindDeletedColumnDependenciesInTable(deletedNames, lastInd);
        }

        // Loops the column being deleted to find dependencies mentioned thereover,
        // and returns the indication of whether or not they are present.
        private bool FindDeletedColumnDependenciesInTable(List<string> deletedNames, int lastInd)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                for (int i = 0; i < lastInd; i++)
                {
                    Cell cell = (Cell)row.Cells[i].Tag;
                    List<Cell> refs = cell.CellReferences;

                    for (int j = refs.Count - 1; j >= 0; j--)
                    {
                        if (deletedNames.Contains(refs[j].Name))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Invoked when user clicks the cell;
        // causes the editing process to start.
        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 || e.ColumnIndex == -1)
            {
                return;
            }

            Cell cell = (Cell)dataGridView[e.ColumnIndex, e.RowIndex].Tag;
            DataGridViewCell dgvCell = cell.Parent;

            if (!dgvCell.ReadOnly)
            {
                dataGridView.BeginEdit(true);
            }
        }

        // Invoked when user starts to edit a cell;
        // prepares the cell for further work on it.
        private void DataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            Cell cell = (Cell)dataGridView[e.ColumnIndex, e.RowIndex].Tag;

            CellManager.Instance.CurrentCell = cell;
            DataGridViewCell dgvCell = cell.Parent;
            dgvCell.Value = cell.Formula;
        }

        // Invoked when user leaves the current cell;
        // performs the work on updating cells' and data grid view's state.
        private void DataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Cell cell = (Cell)dataGridView[e.ColumnIndex, e.RowIndex].Tag;
            DataGridViewCell dgvCell = cell.Parent;
            
            if (dgvCell.Value == null)
            { 
                cell.Formula = "0";
                cell.Value = "0";
                dgvCell.Value = "0";
            }

            ClearRemovedReferences(cell);
            ResolveCellFormula(cell, dgvCell);
        }

        // If the cell's formula had contained several references and then user deleted some,
        // removes them from the cell's reference list.
        private void ClearRemovedReferences(Cell cell)
        {
            List<Cell> removedCells = new List<Cell>();

            foreach (Cell refCell in cell.CellReferences)
            {
                if (!cell.Formula.Contains(refCell.Name))
                {
                    removedCells.Add(refCell);
                }
            }

            foreach (Cell refCell in removedCells)
            {
                cell.CellReferences.Remove(refCell);
            }
        }

        // Performs the work on the current cell's formula,
        // potentionally affecting the rest of the cells' values.
        private void ResolveCellFormula(Cell cell, DataGridViewCell dgvCell)
        {
            cell.Formula = dgvCell.Value.ToString();
            string cellValue = cell.Evaluate();

            if (!cell.Error.Equals(""))
            {
                _errorOccured = true;
                MessageBox.Show(cell.Error, CAPTION_ERR, MessageBoxButtons.OK, MessageBoxIcon.Error);
                cell.Error = "";
                DisableCellsButCurrent(dgvCell);
            }
            else
            {
                _errorOccured = false;
                dgvCell.Value = _formulaView ? cell.Formula : cellValue;
                EnableCells();
                UpdateCellValues(dgvCell);
            }
        }

        // Disables all cells but the one having caused the error
        // for user to be able to correct it.
        private void DisableCellsButCurrent(DataGridViewCell current)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.ReadOnly = true;
                    cell.Style.BackColor = Color.LightGray;
                    cell.Style.ForeColor = Color.DarkGray;
                }
            }

            current.ReadOnly = false;
            current.Style.BackColor = current.OwningColumn.DefaultCellStyle.BackColor;
            current.Style.ForeColor = current.OwningColumn.DefaultCellStyle.ForeColor;
        } 

        // Enables all the cells after the mistake was corrected.
        private void EnableCells()
        {
            if (!_errorOccured)
            {
                return;
            }

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.ReadOnly = false;
                    cell.Style.BackColor = cell.OwningColumn.DefaultCellStyle.BackColor;
                    cell.Style.ForeColor = cell.OwningColumn.DefaultCellStyle.ForeColor;
                }
            }
        }

        // Declines user's attempts to start editing the disabled cell.
        private void DataGridView_CellStateChanged(object sender, DataGridViewCellStateChangedEventArgs e)
        {
            if (e.Cell.ReadOnly)
            {
                e.Cell.Selected = false;
            }
        }

        // Saves the data grid view to the specified path as the .XML file.
        private void SaveDataGridView(string filePath)
        {
            _currentFilePath = filePath;
            dataGridView.EndEdit();

            DataTable table = new DataTable("data");
            ForgeDataTable(table);
            table.WriteXml(filePath);
        }

        // Saves the data grid view depending on whether or not the specified file path
        // has already been specified;
        // if not, urges user to specify using file save dialog.
        private bool SaveDataGridView(string filePath, string dummy)
        {
            if (!filePath.Equals(""))
            {
                SaveDataGridView(filePath);
                return true;
            }
            else if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveDataGridView(saveFileDialog.FileName);
                return true;
            }
            return false;
        }

        // Forges the data table to hereinafter be stored inside of the .XML file.
        private void ForgeDataTable(DataTable table)
        {
            foreach (DataGridViewColumn dgvColumn in dataGridView.Columns)
            {
                table.Columns.Add(dgvColumn.Index.ToString());
            }

            foreach (DataGridViewRow dgvRow in dataGridView.Rows)
            {
                DataRow dataRow = table.NewRow();

                foreach (DataColumn col in table.Columns)
                {
                    Cell cell = (Cell)dgvRow.Cells[Int32.Parse(col.ColumnName)].Tag;
                    dataRow[col.ColumnName] = cell.Formula;
                }

                table.Rows.Add(dataRow);
            }
        }

        // Loads data grid view from the specified file path.
        private void LoadDataGridView(string filePath)
        {
            _currentFilePath = filePath;
            DataSet dataSet = new DataSet();
            dataSet.ReadXml(filePath);
            DataTable table = dataSet.Tables[0];

            dataGridView.ColumnCount = table.Columns.Count;
            dataGridView.RowCount = table.Rows.Count;

            foreach (DataGridViewRow dgvRow in dataGridView.Rows)
            {
                foreach (DataGridViewCell dgvCell in dgvRow.Cells)
                {
                    string cellName = "R" + (dgvRow.Index + 1).ToString() + "C" + (dgvCell.ColumnIndex + 1).ToString();
                    string formula = table.Rows[dgvCell.RowIndex][dgvCell.ColumnIndex].ToString();
                    dgvCell.Tag = new Cell(dgvCell, cellName, formula);
                }
            }

            UpdateCellValues();
        }

        // Invoked when 'Open' button is clicked,
        // causing the loading of the data grid view to start.
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                CellManager.Instance.CurrentCell = new Cell();
                LoadDataGridView(openFileDialog.FileName);
            }
        }

        // Invoked when 'Save' button is clicked,
        // causing the saving of the data grid view to start.
        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_errorOccured)
            {
                MessageBox.Show(WARNING_ERROR, CAPTION_WARNING, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveDataGridView(_currentFilePath, "");
        }

        // Invoked when 'Add Row' button is clicked,
        // causing the row adding to start.
        private void AddRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddRow();
        }

        // Invoked when 'Add Column' button is clicked,
        // causing the column adding to start.
        private void AddColumnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddColumn();
        }

        // Invoked when 'Delete Row' button is clicked,
        // causing the row deleting to start.
        private void DeleteRowToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DeleteRow();
        }

        // Invoked when 'Delete Column' button is clicked,
        // causing the column deleting to start.
        private void DeleteColumnToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DeleteColumn();
        }

        // Invoked when 'Close Window' button is clicked,
        // asking user to save changes to the data grid view.
        // If some cell is erronous, user is urged to correct the error before saving.
        private void Tabledit_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show(WARNING_SAVE, CAPTION_WARNING, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                if (_errorOccured)
                {
                    MessageBox.Show(WARNING_ERROR, CAPTION_WARNING, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }

                if (!SaveDataGridView(_currentFilePath, ""))
                {
                    e.Cancel = true;
                }
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        // Invoked when 'View values' button is clicked,
        // causing the data grid view to display values instead of formulas.
        private void ValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            valuesToolStripMenuItem.Checked = true;
            valuesToolStripMenuItem.Enabled = false;
            formulasToolStripMenuItem.Checked = false;
            formulasToolStripMenuItem.Enabled = true;
            ToggleFormulaView();
        }

        // Invoked when 'View formulas' button is clicked,
        // causing the data grid view to display formulas instead of values.
        private void FormulasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            formulasToolStripMenuItem.Checked = true;
            formulasToolStripMenuItem.Enabled = false;
            valuesToolStripMenuItem.Checked = false;
            valuesToolStripMenuItem.Enabled = true;
            ToggleFormulaView();
        }

        // Toggles the formula/value display style in the data grid view's cells.
        private void ToggleFormulaView()
        {
            _formulaView = !_formulaView;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell dgvCell in row.Cells)
                {
                    Cell cell = CellManager.Instance.GetCell(dgvCell);

                    if (_formulaView)
                    {
                        dgvCell.Value = cell.Formula;
                    }
                    else
                    {
                        dgvCell.Value = cell.Value;
                    }
                }
            }
        }

        
    }
}
