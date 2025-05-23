using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Data
{
    /// <summary>
    /// Repository class for managing tags in the database.
    /// </summary>
    public class TagRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagRepository"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TagRepository(ILogger<TagRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the database connection and creates the Tag and ProjectsTags tables if they do not exist.
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
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Tag]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE Tag (
                            ID INT IDENTITY(1,1) PRIMARY KEY,
                            Title NVARCHAR(100) NOT NULL,
                            Color NVARCHAR(50) NOT NULL
                        )
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProjectsTags]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE ProjectsTags (
                            ProjectID INT NOT NULL,
                            TagID INT NOT NULL,
                            PRIMARY KEY(ProjectID, TagID),
                            FOREIGN KEY(ProjectID) REFERENCES Project(ID),
                            FOREIGN KEY(TagID) REFERENCES Tag(ID)
                        )
                    END";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Tag tables");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Retrieves a list of all tags from the database.
        /// </summary>
        /// <returns>A list of <see cref="Tag"/> objects.</returns>
        public async Task<List<Tag>> ListAsync()
        {
            await Init();

            var tags = new List<Tag>();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Tag ORDER BY Title";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(new Tag
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Color = reader.GetString(reader.GetOrdinal("Color"))
                });
            }

            return tags;
        }

        /// <summary>
        /// Retrieves a list of tags associated with a specific project.
        /// </summary>
        /// <param name="projectID">The ID of the project.</param>
        /// <returns>A list of <see cref="Tag"/> objects.</returns>
        public async Task<List<Tag>> ListAsync(int projectID)
        {
            await Init();

            var tags = new List<Tag>();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT t.* 
                FROM Tag t
                INNER JOIN ProjectsTags pt ON t.ID = pt.TagID
                WHERE pt.ProjectID = @projectId
                ORDER BY t.Title";
            command.Parameters.AddWithValue("@projectId", projectID);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tags.Add(new Tag
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Color = reader.GetString(reader.GetOrdinal("Color"))
                });
            }

            return tags;
        }

        /// <summary>
        /// Retrieves a specific tag by its ID.
        /// </summary>
        /// <param name="id">The ID of the tag.</param>
        /// <returns>A <see cref="Tag"/> object if found; otherwise, null.</returns>
        public async Task<Tag?> GetAsync(int id)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Tag WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Tag
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Color = reader.GetString(reader.GetOrdinal("Color"))
                };
            }

            return null;
        }

        /// <summary>
        /// Saves a tag to the database. If the tag ID is 0, a new tag is created; otherwise, the existing tag is updated.
        /// </summary>
        /// <param name="item">The tag to save.</param>
        /// <returns>The ID of the saved tag.</returns>
        public async Task<int> SaveItemAsync(Tag item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            if (item.ID == 0)
            {
                command.CommandText = @"
                    INSERT INTO Tag (Title, Color)
                    VALUES (@title, @color);
                    SELECT SCOPE_IDENTITY();";
            }
            else
            {
                command.CommandText = @"
                    UPDATE Tag 
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
        /// Saves a tag to the database and associates it with a specific project.
        /// </summary>
        /// <param name="item">The tag to save.</param>
        /// <param name="projectID">The ID of the project.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> SaveItemAsync(Tag item, int projectID)
        {
            var tagId = await SaveItemAsync(item);

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                IF NOT EXISTS (SELECT 1 FROM ProjectsTags WHERE ProjectID = @projectId AND TagID = @tagId)
                BEGIN
                    INSERT INTO ProjectsTags (ProjectID, TagID)
                    VALUES (@projectId, @tagId)
                END";
            command.Parameters.AddWithValue("@projectId", projectID);
            command.Parameters.AddWithValue("@tagId", tagId);

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Deletes a tag from the database.
        /// </summary>
        /// <param name="item">The tag to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteItemAsync(Tag item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Tag WHERE ID = @id";
            command.Parameters.AddWithValue("@id", item.ID);

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Deletes a tag from a specific project in the database.
        /// </summary>
        /// <param name="item">The tag to delete.</param>
        /// <param name="projectID">The ID of the project.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteItemAsync(Tag item, int projectID)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ProjectsTags WHERE ProjectID = @projectId AND TagID = @tagId";
            command.Parameters.AddWithValue("@projectId", projectID);
            command.Parameters.AddWithValue("@tagId", item.ID);

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Drops the Tag and ProjectsTags tables from the database.
        /// </summary>
        public async Task DropTableAsync()
        {
            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DROP TABLE IF EXISTS ProjectsTags;
                DROP TABLE IF EXISTS Tag;";
            await command.ExecuteNonQueryAsync();

            _hasBeenInitialized = false;
        }
    }
}