using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Data
{
    /// <summary>
    /// Repository class for managing projects in the database.
    /// </summary>
    public class ProjectRepository
    {
        private bool _hasBeenInitialized = false;
        private readonly ILogger _logger;
        private readonly TaskRepository _taskRepository;
        private readonly TagRepository _tagRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectRepository"/> class.
        /// </summary>
        /// <param name="taskRepository">The task repository instance.</param>
        /// <param name="tagRepository">The tag repository instance.</param>
        /// <param name="logger">The logger instance.</param>
        public ProjectRepository(TaskRepository taskRepository, TagRepository tagRepository, ILogger<ProjectRepository> logger)
        {
            _taskRepository = taskRepository;
            _tagRepository = tagRepository;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the database connection and creates the Project table if it does not exist.
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
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Project]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE Project (
                            ID INT IDENTITY(1,1) PRIMARY KEY,
                            Name NVARCHAR(100) NOT NULL,
                            Description NVARCHAR(MAX) NOT NULL,
                            Icon NVARCHAR(50) NOT NULL,
                            CategoryID INT NOT NULL
                        )
                    END";
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Project table");
                throw;
            }

            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Retrieves a list of all projects from the database.
        /// </summary>
        /// <returns>A list of <see cref="Project"/> objects.</returns>
        public async Task<List<Project>> ListAsync()
        {
            await Init();

            var projects = new List<Project>();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Project";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var project = new Project
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Icon = reader.GetString(reader.GetOrdinal("Icon")),
                    CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID"))
                };

                project.Tasks = await _taskRepository.ListAsync(project.ID);
                project.Tags = await _tagRepository.ListAsync(project.ID);

                projects.Add(project);
            }

            return projects;
        }

        /// <summary>
        /// Retrieves a specific project by its ID.
        /// </summary>
        /// <param name="id">The ID of the project.</param>
        /// <returns>A <see cref="Project"/> object if found; otherwise, null.</returns>
        public async Task<Project?> GetAsync(int id)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Project WHERE ID = @id";
            command.Parameters.AddWithValue("@id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var project = new Project
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Icon = reader.GetString(reader.GetOrdinal("Icon")),
                    CategoryID = reader.GetInt32(reader.GetOrdinal("CategoryID"))
                };

                project.Tasks = await _taskRepository.ListAsync(project.ID);
                project.Tags = await _tagRepository.ListAsync(project.ID);

                return project;
            }

            return null;
        }

        /// <summary>
        /// Saves a project to the database. If the project ID is 0, a new project is created; otherwise, the existing project is updated.
        /// </summary>
        /// <param name="item">The project to save.</param>
        /// <returns>The ID of the saved project.</returns>
        public async Task<int> SaveItemAsync(Project item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;

                if (item.ID == 0)
                {
                    command.CommandText = @"
                        INSERT INTO Project (Name, Description, Icon, CategoryID)
                        VALUES (@name, @description, @icon, @categoryId);
                        SELECT SCOPE_IDENTITY();";
                }
                else
                {
                    command.CommandText = @"
                        UPDATE Project 
                        SET Name = @name,
                            Description = @description,
                            Icon = @icon,
                            CategoryID = @categoryId
                        WHERE ID = @id;
                        SELECT @id;";
                    command.Parameters.AddWithValue("@id", item.ID);
                }

                command.Parameters.AddWithValue("@name", item.Name);
                command.Parameters.AddWithValue("@description", item.Description);
                command.Parameters.AddWithValue("@icon", item.Icon);
                command.Parameters.AddWithValue("@categoryId", item.CategoryID);

                item.ID = Convert.ToInt32(await command.ExecuteScalarAsync());

                foreach (var task in item.Tasks)
                {
                    task.ProjectID = item.ID;
                    await _taskRepository.SaveItemAsync(task);
                }

                foreach (var tag in item.Tags)
                {
                    await _tagRepository.SaveItemAsync(tag, item.ID);
                }

                transaction.Commit();
                return item.ID;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Deletes a project from the database.
        /// </summary>
        /// <param name="item">The project to delete.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> DeleteItemAsync(Project item)
        {
            await Init();

            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var task in item.Tasks)
                {
                    await _taskRepository.DeleteItemAsync(task);
                }

                foreach (var tag in item.Tags)
                {
                    await _tagRepository.DeleteItemAsync(tag, item.ID);
                }

                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM Project WHERE ID = @id";
                command.Parameters.AddWithValue("@id", item.ID);
                var result = await command.ExecuteNonQueryAsync();

                transaction.Commit();
                return result;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Drops the Project table from the database.
        /// </summary>
        public async Task DropTableAsync()
        {
            using var connection = new SqlConnection(Constants.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE IF EXISTS Project";
            await command.ExecuteNonQueryAsync();

            _hasBeenInitialized = false;
        }
    }
}