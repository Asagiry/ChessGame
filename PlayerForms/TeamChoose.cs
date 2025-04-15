using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Deployment.Application;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessGame.Forms
{
    public partial class TeamChoose : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        int id {  get; set; }
        string name { get; set; }
        public int teamId { get; set; }
        public TeamChoose(int id, string name,string teamName)
        {
            InitializeComponent();
            this.id = id;
            this.name = name;
            label2.Text += id;
            label3.Text += name;
            label4.Text += teamName;
            loadForm();
        }
        public void loadForm()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.MultiSelect = false;
            FillDataGridView();
        }
        private void FillDataGridView()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT 
                    TeamId as [TeamId],
                    TeamName as [Имя команды]
                FROM Команда";

                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                dataAdapter.Fill(dataTable);
                dataGridView1.DataSource = dataTable;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.ReadOnly = true;
                }
                dataGridView1.Columns["TeamId"].Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                // Получаем выбранную ячейку
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                // Получаем строку, к которой относится выбранная ячейка
                DataGridViewRow selectedRow = selectedCell.OwningRow;

                // Получаем PlayerId из выбранной строки
                teamId = (int)selectedRow.Cells["TeamId"].Value;

                DialogResult = DialogResult.OK;

            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для зачисления в команду.");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                // Получаем выбранную ячейку
                DataGridViewCell selectedCell = dataGridView1.SelectedCells[0];

                // Получаем строку, к которой относится выбранная ячейка
                DataGridViewRow selectedRow = selectedCell.OwningRow;

                // Получаем PlayerId из выбранной строки
                teamId = (int)selectedRow.Cells["TeamId"].Value;

                TeamViewForm teamViewForm = new TeamViewForm(teamId);
                teamViewForm.ShowDialog();


            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите ячейку для просмотра команды.");
            }
        }
    }
}
