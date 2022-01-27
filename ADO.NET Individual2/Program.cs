using System;
using System.Data;
using System.Data.SqlClient;

namespace ADO.NET_Individual2
{
    public class Program
    {
        private static int CURRENT_PAGE = 0;
        private static int PAGE_SIZE = 3;
        private static class DbCredentials
        {
            public static string CONNECTION_STRING = "Server=WIN-OBDH18C5VTL;Database=Experimental;Integrated Security=True;";
        }
        enum Options
        {
            Page = 0,
            UpdateRow = 1,
            AddRow = 2,
        }
        static void Main(string[] args)
        {
            Menu();
        }

        private static void Menu(string error = "", string message = "")
        {
            if (!string.IsNullOrEmpty(error))
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(error);
                Console.ResetColor();
                Console.WriteLine();
            }

            if (!string.IsNullOrEmpty(message))
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(message);
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.WriteLine("Please, choose one of the given option: \n0 GetPages\n1 Update Row\n2 AddRow ");
            var selectedOption = (Options)Convert.ToInt16(Console.ReadLine());

            using var connection = new SqlConnection(DbCredentials.CONNECTION_STRING);
            connection.Open();
            using var dbSet = new DataSet();

            switch (selectedOption)
            {
                case Options.Page:
                    {
                        OpenPaginationWindow(dbSet, connection);
                        break;
                    }
                case Options.UpdateRow:
                    {
                        UpdateRow(dbSet, connection);
                        break;
                    }
                case Options.AddRow:
                    {
                        AddRow(dbSet, connection);
                        break;
                    }
            }
        }

        private static void AddRow(DataSet dbSet, SqlConnection connection)
        {
            try
            {
                using var adapter = new SqlDataAdapter("select * from Users;", connection);
                SqlCommandBuilder commandBuilder = new SqlCommandBuilder(adapter);

                adapter.InsertCommand = new SqlCommand("sp_CreateUser", connection);
                adapter.InsertCommand.CommandType = CommandType.StoredProcedure;

                adapter.InsertCommand.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 70, "Name"));
                adapter.InsertCommand.Parameters.Add(new SqlParameter("@birthYear", SqlDbType.Int, 0, "BirthYear"));
                adapter.InsertCommand.Parameters.Add(new SqlParameter("@account", SqlDbType.Float, 0, "Account"));
                SqlParameter parameter = adapter.InsertCommand.Parameters.Add("@Id", SqlDbType.Int, 0, "Id");
                parameter.Direction = ParameterDirection.Output;

                adapter.Fill(dbSet, "Users");
                dbSet.Tables["Users"].PrimaryKey = new DataColumn[] { dbSet.Tables["Users"].Columns["Id"] };

                var newRow = dbSet.Tables["Users"].NewRow();
                newRow["Id"] = 0;
                Console.Write("Enter name: ");
                newRow["name"] = Console.ReadLine();
                Console.WriteLine();
                Console.Write("Enter BirthYear: ");
                newRow["birthYear"] = Console.ReadLine();
                Console.WriteLine();
                Console.Write("Enter Account: ");
                newRow["account"] = Console.ReadLine();
                Console.WriteLine();

                dbSet.Tables["Users"].Rows.Add(newRow);

                adapter.Update(dbSet, "Users");
                dbSet.AcceptChanges();

                var newUser = dbSet.Tables["Users"].Rows.Find(parameter.Value);

                Console.Clear();
                Menu(null, $"New user has been added: {newUser["name"]}, {newUser["birthYear"]}, {newUser["account"]}");
            }
            catch (Exception ex)
            {
                Console.Clear();
                Menu(ex.Message);
            }
        }

        private static void UpdateRow(DataSet dbSet, SqlConnection connection)
        {
            Console.WriteLine("Choose user id which you wanna update: ");
            var usersId = Convert.ToInt32(Console.ReadLine());

            if (usersId <= 0)
            {
                Console.Clear();
                Menu("Id cannot be equal or less than zero");
            }

            using var adapter = new SqlDataAdapter("select * from Users;", connection);
            var builder = new SqlCommandBuilder(adapter);
            adapter.Fill(dbSet, "Users");

            dbSet.Tables["Users"].PrimaryKey = new DataColumn[] { dbSet.Tables["Users"].Columns["Id"] };
            var user = dbSet.Tables["Users"].Rows.Find(usersId);

            if (user == null)
            {
                Console.Clear();
                Menu("There is no user with entered Id");
            }

            Console.WriteLine("{0} {1} {2}", user["name"], user["birthYear"], user["account"]);

            var userId = user["Id"];
            Console.WriteLine("Enter Name: ");
            var newUserName = Console.ReadLine() ?? user["name"];
            Console.WriteLine("Enter BirthYear: ");
            var getNewBirth = Console.ReadLine();
            var newBirthYear = string.IsNullOrEmpty(getNewBirth) ? user["birthYear"] : getNewBirth;
            Console.WriteLine("Enter Account: ");
            var getAccount = Console.ReadLine();
            var newAccount = string.IsNullOrEmpty(getAccount) ? user["account"] : getAccount;

            user["name"] = newUserName;
            user["birthYear"] = newBirthYear;
            user["account"] = newAccount;

            adapter.Update(dbSet, "Users");

            Console.WriteLine("{0} {1} {2}", user["name"], user["birthYear"], user["account"]);

            Console.Clear();
            Menu();
        }

        private static void OpenPaginationWindow(DataSet dbSet, SqlConnection connection)
        {
            using var adapter = new SqlDataAdapter(GetSqlData(connection));

            adapter.Fill(dbSet, "Users");

            Display(dbSet.Tables["Users"]);

            while (true)
            {
                var selectedArrow = Console.ReadKey().Key;

                switch (selectedArrow)
                {
                    case ConsoleKey.LeftArrow:
                        {
                            PreviousPage(dbSet, connection);
                            break;
                        }
                    case ConsoleKey.RightArrow:
                        {
                            NextPage(dbSet, connection);
                            break;
                        }
                }

                if (selectedArrow == ConsoleKey.Escape)
                {
                    Console.Clear();
                    connection.Close();
                    Menu();
                }
            }
        }

        private static SqlCommand GetSqlData(SqlConnection connection)
        {
            var select = @"select * from Users u 
                            order by u.Id 
                            offset (@currentPage * @pageSize) rows fetch next @pageSize rows only;";
            var command = new SqlCommand(select, connection);
            command.Parameters.AddWithValue("@currentPage", CURRENT_PAGE);
            command.Parameters.AddWithValue("@pageSize", PAGE_SIZE);
            return command;
        }

        private static void PreviousPage(DataSet dbSet, SqlConnection connection)
        {
            if (CURRENT_PAGE <= 0) return;

            Console.Clear();

            CURRENT_PAGE = CURRENT_PAGE - 1;
            using var adapter = new SqlDataAdapter(GetSqlData(connection));
            dbSet.Tables["Users"].Clear();
            adapter.Fill(dbSet, "Users");
            Display(dbSet.Tables["Users"]);
        }

        private static void NextPage(DataSet dbSet, SqlConnection connection)
        {
            if (dbSet.Tables["Users"].Rows.Count < PAGE_SIZE) return;

            Console.Clear();

            CURRENT_PAGE = CURRENT_PAGE + 1;
            using var adapter = new SqlDataAdapter(GetSqlData(connection));
            dbSet.Tables["Users"].Clear();
            adapter.Fill(dbSet, "Users");
            Display(dbSet.Tables["Users"]);
        }

        private static void Display(DataTable dataTable)
        {
            Console.Clear();
            foreach (DataColumn column in dataTable.Columns)
                Console.Write("\t{0}", column.ColumnName);
            Console.WriteLine();
            foreach (DataRow row in dataTable.Rows)
            {
                var cells = row.ItemArray;
                foreach (object cell in cells)
                    Console.Write("\t{0}", cell);
                Console.WriteLine();
            }
        }
    }
}
