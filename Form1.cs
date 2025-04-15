using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChessGame.Forms;
using ChessGame.GameForms;
using ChessGame.StatisticForms;
using ChessGame.TeamForms;

namespace ChessGame
{
    public partial class Form1 : Form
    {
        string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=v_epishin_17R;Integrated Security=True;";
        public bool isTournament = false;
        public Form1()
        {
            InitializeComponent();
            string query = "SELECT COUNT(*) FROM Партия";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    int count = (int)command.ExecuteScalar(); // Получаем количество записей в таблице Партия

                    if (count > 0)
                    {
                        isTournament = true; // Устанавливаем флаг в true, если количество партий больше 0
                        button4.Enabled = false;
                    }
                    else
                    {
                        isTournament = false; // В противном случае флаг остается false
                        button5.Enabled = false;
                    }
                }
            }
            button6.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e) ///ШАХМАТИСТЫ
        {
            button1.Text = "Таблица шахматистов открыта";
            Form playerForm = new PlayerForm(this);
            playerForm.Show();
            if (isTournament)
            {
                button5.Enabled = false;
            }
            else
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
            }    
        }

        private void button2_Click(object sender, EventArgs e) ///КОМАНДЫ
        {
            button2.Text = "Таблица команд открыта";
            TeamForm teamForm = new TeamForm(this);
            teamForm.Show();
            if (isTournament)
            {
                button5.Enabled = false;
            }
            else
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e) ///ТАБЛИЦА ПАРТИЙ
        {
            button3.Text = "Таблица партий открыта";
            GameForm gameForm = new GameForm(this);
            gameForm.Show(); 
            if (isTournament)
            {
                button5.Enabled = false;
            }
            else
            {
                button4.Enabled = false;
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e) // Турнир старт
        {
            button6.Enabled = false;
            string checkQuery = @"
            SELECT 
                CASE 
                    WHEN EXISTS (SELECT 1 FROM Шахматист WHERE Команда_TeamId = 0) THEN CAST(1 AS BIT)
                    ELSE CAST(0 AS BIT)
                END AS Result;
            ";

            string deleteQuery = "DELETE FROM Партия;";
            string resetPlayerStatsQuery = "UPDATE Шахматист SET PlayerGamePlayed = 0, PlayerPoints = 0;"; // Запрос для обнуления

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(checkQuery, connection))
                {
                    bool hasPlayerWithTeamIdZero = (bool)command.ExecuteScalar();
                    if (hasPlayerWithTeamIdZero)
                    {
                        MessageBox.Show("В таблице шахматистов есть участники без команды.\nНевозможно начать турнир.");
                        return;
                    }
                }

                using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                {
                    deleteCommand.ExecuteNonQuery();
                }
                using (SqlCommand resetCommand = new SqlCommand(resetPlayerStatsQuery, connection))
                {
                    resetCommand.ExecuteNonQuery();
                }

                isTournament = true;
                button5.Enabled = true;  
                button4.Enabled = false; 
                MessageBox.Show("Турнир начался!");
            }
        }

        private void button5_Click(object sender, EventArgs e) //Турнир конец
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
            SELECT 
                PlayerName,
                PlayerGamePlayed, 
                (SELECT COUNT(*) 
                 FROM Шахматист p 
                 WHERE p.Команда_TeamId != s.Команда_TeamId) AS [RequiredGames]
            FROM Шахматист s";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        string playerName = reader.GetString(0);
                        int playerGamePlayed = reader.GetInt32(1);
                        int requiredGames = reader.GetInt32(2);

                        if (playerGamePlayed != requiredGames)
                        {
                            MessageBox.Show($"Игрок {playerName} должен сыграть {requiredGames} игр.\nНо сыграл {playerGamePlayed}.");
                            return; 
                        }
                    }
                }
            }
            isTournament = false;
            button5.Enabled = false;
            button4.Enabled = true;
            MessageBox.Show("Турнир завершен");
            button6.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            StatisticForm statisticForm = new StatisticForm();
            statisticForm.ShowDialog();
        }
    }
}
