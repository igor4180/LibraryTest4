using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.Threading;
using System.Data.SqlClient;

namespace AdoNetSample4
{
    public partial class Form1 : Form
    {
        DbConnection conn = null;
        DbProviderFactory fact = null;
        string connectionString = "";

        public Form1()
        {
            InitializeComponent();
            button1.Enabled = false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            conn = new SqlConnection();
            conn.ConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Овощи и фрукты;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            conn.Open();
            if (conn.State == ConnectionState.Open)
            {
                MessageBox.Show("Успешно");
            }
            else
            {
                MessageBox.Show("Попробуйте снова");
            }

            button1.Enabled = false;
            conn.ConnectionString = connectionString;

             await conn.OpenAsync();

            DbCommand comm = conn.CreateCommand();
            comm.CommandText = "WAITFOR DELAY '00:00:05';";

            comm.CommandText += textBox1.Text.ToString();

            DataTable table = new DataTable();

            using (DbDataReader reader = await comm.ExecuteReaderAsync())
            {
                int line = 0;

                do
                {
                    while (await reader.ReadAsync())
                    {
                        if (line == 0)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                table.Columns.Add(reader.GetName(i));
                            }
                            line++;
                        }
                        DataRow row = table.NewRow();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[i] = await reader.GetFieldValueAsync<Object>(i);
                        }
                        table.Rows.Add(row);
                    }
                } while (reader.NextResult());
            }
            // выводим результаты запроса
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = table;
            button1.Enabled = true;
        }

        /// <summary>
        /// При загрузке окна фабрику для поставщика System.Data.SqlClient 
        /// вызываем метод для получения строки подключения 
        /// из конфигурациооного файла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            fact = DbProviderFactories.GetFactory("System.Data.SqlClient");
            conn = fact.CreateConnection();
            connectionString = GetConnectionStringByProvider("System.Data.SqlClient");
            if(connectionString == null)
            {
                MessageBox.Show("В конфигурационном файле нет требуемой строки подключения");
            }
        }
        /// <summary>
        /// Этот метод по имени поставщика данных
        /// считывает из конфигурационного файла и возвращает
        /// строку подключения, если эта строка есть в 
        /// конфигурационном файле :)
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        static string GetConnectionStringByProvider(string providerName)
        {
            string returnValue = null;

            // читаем все строки подключения из App.config
            ConnectionStringSettingsCollection settings =
                ConfigurationManager.ConnectionStrings;

            // ищем и возвращаем строку подключения для providerName
            if (settings != null)
            {
                foreach (ConnectionStringSettings cs in settings)
                {
                    if (cs.ProviderName == providerName)
                    {
                        returnValue = cs.ConnectionString;
                        break;
                    }
                }
            }
            return returnValue;
        }

        /// <summary>
        /// управление доступностью кнопки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 3)
                button1.Enabled = true;
            else
                button1.Enabled = false;
        }

        static string SlowMethod(string file)
        {
            Thread.Sleep(3000);
            //reading file
            return string.Format("File {0} is read", file);
        }

        static Task<string> SlowMethodAsync(string file)
        {
            return Task.Run<string>(() =>
            {
                return SlowMethod(file);
            });
        }

        private async static void CallMyAsync()
        {
            string result = await SlowMethodAsync("BigFile.txt");
            //сюда можно добавить и другие вызовы нашего метода
            //string result1 = await SlowMethodAsync("BigFile1.txt");
            //string result2 = await SlowMethodAsync("BigFile2.txt");
            Console.WriteLine(result);
        }

        static string SlowMethod1(string file, CancellationToken token)
        {
            Thread.Sleep(3000);
            //reading file
            token.ThrowIfCancellationRequested();
            return string.Format("File {0} is read", file);
        }

        static Task<string> SlowMethodAsync1(string file, CancellationToken token)
        {
            return Task.Run<string>(() =>
            {
                return SlowMethod1(file, token);
            });
        }

        private async static void CallMyAsync1()
        {
            try
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(3));
                Task<string> t1 = SlowMethodAsync1("BigFile.txt", cts.Token);
                string result = await t1;
                Console.WriteLine(result);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
