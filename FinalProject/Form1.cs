using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FinalProject
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // **************************************************************************************
        // ** naming convention (database : snake_case, class : PascalCase, local : camelCase) **
        // **************************************************************************************


        public class BookResult
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public int Year { get; set; }
        }

        public class OpenLibraryResponse
        {
            public List<Doc> Docs { get; set; }
        }

        public class Doc
        {
            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("author_name")]
            public List<string> AuthorName { get; set; }

            [JsonProperty("first_publish_year")]
            public int? FirstPublishYear { get; set; }
        }

        // new content ========================================================================

        private void SetSelectedTextFromRow(int rowIndex)
        {
            if (rowIndex >= 0)
            {
                selectedVarText.Text = bookDataGrid.Rows[rowIndex].Cells[0].Value?.ToString();
            }
        }
        private void bookDataGrid_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            SetSelectedTextFromRow(e.RowIndex);

            string selectedTitle = bookDataGrid.Rows[e.RowIndex].Cells["Title"].Value.ToString();

            // Optional: Get author too for better uniqueness
            string selectedAuthor = bookDataGrid.Rows[e.RowIndex].Cells["Author"].Value.ToString();

            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\OnlineBooks.mdf;Integrated Security=True;";
            string query = "SELECT notes FROM JournalEntries WHERE book_title = @title AND book_author = @author";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", selectedTitle);
                    cmd.Parameters.AddWithValue("@author", selectedAuthor);

                    conn.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        noteRichTextBox.Text = result.ToString(); // make sure this is your control name
                    }
                    else
                    {
                        noteRichTextBox.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load saved notes: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        private void saveButton_Click_1(object sender, EventArgs e)
        {
            if (bookDataGrid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a book to save", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\OnlineBooks.mdf;Integrated Security=True;";

            string query = "INSERT INTO JournalEntries (book_title, book_author, book_year, notes) " +
                           "VALUES (@title, @author, @year, @notes)";

            // identify user selected row
            var selectedRow = bookDataGrid.SelectedRows[0];

            // assign variables to each column in selected row and convert each value into string
            string title = selectedRow.Cells["Title"].Value.ToString();
            string author = selectedRow.Cells["Author"].Value.ToString();
            string year = selectedRow.Cells["Year"].Value.ToString();
            string notes = noteRichTextBox.Text;


            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@author", author);
                    cmd.Parameters.AddWithValue("@year", year);
                    cmd.Parameters.AddWithValue("@notes", notes);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Journal entry saved", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save entry: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }

        private async void searchButton_Click_1(object sender, EventArgs e)
        {
            string query = searchBar.Text.ToLower();

            if (string.IsNullOrEmpty(query))
            {
                throw new Exception(string.Empty);
            }

            // api link
            string url = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}";

            try
            {
                using (HttpClient client = new HttpClient())
                {

                    // send request to Open Library API url
                    string response = await client.GetStringAsync(url);

                    // converts JSON text into OpenLibraryResponse object so (Title : List<string> AuthorName : PublishYear)
                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenLibraryResponse>(response);

                    var books = result.Docs
                    .Select(book => new BookResult
                    {
                        Title = book.Title,
                        Author = book.AuthorName?.FirstOrDefault() ?? "Unknown",
                        Year = book.FirstPublishYear ?? 0
                    })
                    .ToList();

                    bookDataGrid.DataSource = null; // clear existing bindings
                    bookDataGrid.AutoGenerateColumns = true;
                    bookDataGrid.DataSource = books;
                    bookDataGrid.Columns["Title"].Width = 200;
                    bookDataGrid.Columns["Author"].Width = 200;
                    bookDataGrid.Columns["Year"].Width = 60;

                    MessageBox.Show($"Amount of books found: {books.Count}");

                    bookDataGrid.Update();
                    bookDataGrid.Refresh();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

