using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Data
{
    /// <summary>
    /// Repository class for managing categories in the database.
    /// </summary>
    public class CategoryRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryRepository"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public CategoryRepository(ILogger<CategoryRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the database connection and creates the Category table if it does not exist.
        /// </summary>
        private async Task Init()
        {
            if (_hasBeenInitialized)
                return;

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            try
            {
                var command = connection.CreateCommand();
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Category]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE Category (
                            ID INT IDENTITY(1,1) PRIMARY KEY,
                            Title NVARCHAR(100) NOT NULL,
                            Color NVARCHAR(50) NOT NULL
                        )
                    END";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Category table");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Retrieves a list of all categories from the database.
        /// </summary>
        /// <returns>A list of <see cref="Category"/> objects.</returns>
        public async Task<List<Category>> ListAsync()
        {
            await Init();

            var categories = new List<Category>();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Category ORDER BY Title";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Color = reader.GetString(reader.GetOrdinal("Color"))
                });
            }

            return categories;
        }

        /// <summary>
        /// Retrieves a specific category by its ID.
        /// </summary>
        /// <param name="id">The ID of the category.</param>
        /// <returns>A <see cref="Category"/> object if found; otherwise, null.</returns>
        public async Task<Category?> GetAsync(int id)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Category WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Category
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Color = reader.GetString(reader.GetOrdinal("Color"))
                };
            }

            return null;
        }

        /// <summary>
        /// Saves a category to the database. If the category ID is 0, a new category is created; otherwise, the existing category is updated.
        /// </summary>
        /// <param name="item">The category to save.</param>
        /// <returns>The ID of the saved category.</returns>
        public async Task<int> SaveItemAsync(Category item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            if (item.ID == 0)
            {
                command.CommandText = @"
                    INSERT INTO Category (Title, Color)
                    VALUES (@title, @color);
                    SELECT SCOPE_IDENTITY();";
            }
            else
            {
                command.CommandText = @"
                    UPDATE Category 
                    SET Title = @title,
                        Color = @color
                    WHERE ID = @id;
                    SELECT @id;";
                command.Parameters.AddWithValue("@id", item.ID);
            }

            command.Parameters.AddWithValue("@title", item.Title);
            command.Parameters.AddWithValue("@color", item.Color);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        /// <summary>
        /// Deletes a category from the database.
        /// </summary>
        /// <param name="item">The category to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteItemAsync(Category item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Category WHERE ID = @id";
            command.Parameters.AddWithValue("@id", item.ID);

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Drops the Category table from the database.
        /// </summary>
        public async Task DropTableAsync()
        {
            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE IF EXISTS Category";
            await command.ExecuteNonQueryAsync();

            _hasBeenInitialized = false;
        }
    }
}