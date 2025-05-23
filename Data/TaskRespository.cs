using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Data
{
    /// <summary>
    /// Repository class for managing tasks in the database.
    /// </summary>
    public class TaskRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRepository"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TaskRepository(ILogger<TaskRepository> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Initializes the database connection and creates the Task table if it does not exist.
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
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Task]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE Task (
                            ID INT IDENTITY(1,1) PRIMARY KEY,
                            Title NVARCHAR(200) NOT NULL,
                            IsCompleted BIT NOT NULL,
                            ProjectID INT NOT NULL
                        )
                    END";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Task table");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Retrieves a list of all tasks from the database.
        /// </summary>
        /// <returns>A list of <see cref="ProjectTask"/> objects.</returns>
        public async Task<List<ProjectTask>> ListAsync()
        {
            await Init();

            var tasks = new List<ProjectTask>();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Task";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(new ProjectTask
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    IsCompleted = reader.GetBoolean(reader.GetOrdinal("IsCompleted")),
                    ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID"))
                });
            }

            return tasks;
        }

        /// <summary>
        /// Retrieves a list of tasks associated with a specific project.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>A list of <see cref="ProjectTask"/> objects.</returns>
        public async Task<List<ProjectTask>> ListAsync(int projectId)
        {
            await Init();

            var tasks = new List<ProjectTask>();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Task WHERE ProjectID = @projectId";
            command.Parameters.AddWithValue("@projectId", projectId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tasks.Add(new ProjectTask
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    IsCompleted = reader.GetBoolean(reader.GetOrdinal("IsCompleted")),
                    ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID"))
                });
            }

            return tasks;
        }

        /// <summary>
        /// Retrieves a specific task by its ID.
        /// </summary>
        /// <param name="id">The ID of the task.</param>
        /// <returns>A <see cref="ProjectTask"/> object if found; otherwise, null.</returns>
        public async Task<ProjectTask?> GetAsync(int id)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Task WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ProjectTask
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    IsCompleted = reader.GetBoolean(reader.GetOrdinal("IsCompleted")),
                    ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID"))
                };
            }

            return null;
        }

        /// <summary>
        /// Saves a task to the database. If the task ID is 0, a new task is created; otherwise, the existing task is updated.
        /// </summary>
        /// <param name="item">The task to save.</param>
        /// <returns>The ID of the saved task.</returns>
        public async Task<int> SaveItemAsync(ProjectTask item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            if (item.ID == 0)
            {
                command.CommandText = @"
                    INSERT INTO Task (Title, IsCompleted, ProjectID)
                    VALUES (@title, @isCompleted, @projectId);
                    SELECT SCOPE_IDENTITY();";
            }
            else
            {
                command.CommandText = @"
                    UPDATE Task 
                    SET Title = @title,
                        IsCompleted = @isCompleted,
                        ProjectID = @projectId
                    WHERE ID = @id;
                    SELECT @id;";
                command.Parameters.AddWithValue("@id", item.ID);
            }

            command.Parameters.AddWithValue("@title", item.Title);
            command.Parameters.AddWithValue("@isCompleted", item.IsCompleted);
            command.Parameters.AddWithValue("@projectId", item.ProjectID);

            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        /// <summary>
        /// Deletes a task from the database.
        /// </summary>
        /// <param name="item">The task to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteItemAsync(ProjectTask item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Task WHERE ID = @id";
            command.Parameters.AddWithValue("@id", item.ID);

            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Drops the Task table from the database.
        /// </summary>
        public async Task DropTableAsync()
        {
            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE IF EXISTS Task";
            await command.ExecuteNonQueryAsync();

            _hasBeenInitialized = false;
        }
    }
}