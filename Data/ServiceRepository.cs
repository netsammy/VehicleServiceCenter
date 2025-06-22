using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Data;

public class ServiceRepository : IServiceRepository
{    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;
    private readonly IPreferences _preferences;

    public ServiceRepository(ILogger<ServiceRepository> logger, IPreferences preferences)
    {
        _logger = logger;
        _preferences = preferences;
    }

    private async Task Init()
    {
        if (_hasBeenInitialized)
            return;

        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();        command.CommandText = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceRecords]') AND type in (N'U'))
            BEGIN
                CREATE TABLE ServiceRecords (
                    ID INT IDENTITY(1,1) PRIMARY KEY,
                    ReceiptNumber NVARCHAR(50),
                    ReceiptDate DATETIME,
                    CustomerName NVARCHAR(100),
                    MobileNumber NVARCHAR(20),
                    Address NVARCHAR(200),
                    VehicleNumber NVARCHAR(50),
                    VehicleMake NVARCHAR(50),
                    VehicleModel NVARCHAR(50),
                    CurrentReading FLOAT,
                    NextReading FLOAT,
                    Total DECIMAL(18,2),
                    MechanicName NVARCHAR(100),
                    MechanicContact NVARCHAR(20)
                )
            END

            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceItems]') AND type in (N'U'))
            BEGIN
                CREATE TABLE ServiceItems (
                    ID INT IDENTITY(1,1) PRIMARY KEY,
                    ServiceRecordId INT,
                    Name NVARCHAR(100),
                    Grade NVARCHAR(50),
                    Quantity FLOAT,
                    Rate DECIMAL(18,2),
                    Amount DECIMAL(18,2),
                    FOREIGN KEY(ServiceRecordId) REFERENCES ServiceRecords(ID)
                )
            END";

        await command.ExecuteNonQueryAsync();
        _hasBeenInitialized = true;
    }

    public async Task<List<ServiceRecord>> ListAsync()
    {
        await Init();
        var serviceRecords = new List<ServiceRecord>();
        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ServiceRecords ORDER BY ReceiptDate DESC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var record = new ServiceRecord
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                ReceiptNumber = reader.IsDBNull(reader.GetOrdinal("ReceiptNumber")) ? "" : reader.GetString(reader.GetOrdinal("ReceiptNumber")),
                ReceiptDate = reader.GetDateTime(reader.GetOrdinal("ReceiptDate")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? "" : reader.GetString(reader.GetOrdinal("CustomerName")),
                MobileNumber = reader.IsDBNull(reader.GetOrdinal("MobileNumber")) ? "" : reader.GetString(reader.GetOrdinal("MobileNumber")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? "" : reader.GetString(reader.GetOrdinal("Address")),
                VehicleNumber = reader.IsDBNull(reader.GetOrdinal("VehicleNumber")) ? "" : reader.GetString(reader.GetOrdinal("VehicleNumber")),
                VehicleMake = reader.IsDBNull(reader.GetOrdinal("VehicleMake")) ? "" : reader.GetString(reader.GetOrdinal("VehicleMake")),
                VehicleModel = reader.IsDBNull(reader.GetOrdinal("VehicleModel")) ? "" : reader.GetString(reader.GetOrdinal("VehicleModel")),
                CurrentReading = reader.GetDouble(reader.GetOrdinal("CurrentReading")),
                NextReading = reader.GetDouble(reader.GetOrdinal("NextReading")),
                Total = (double)reader.GetDecimal(reader.GetOrdinal("Total")),
                MechanicName = reader.IsDBNull(reader.GetOrdinal("MechanicName")) ? "" : reader.GetString(reader.GetOrdinal("MechanicName")),
                MechanicContact = reader.IsDBNull(reader.GetOrdinal("MechanicContact")) ? "" : reader.GetString(reader.GetOrdinal("MechanicContact"))
            };

            record.Items = await ListItemsAsync(record.ID);
            serviceRecords.Add(record);
        }

        return serviceRecords;
    }

    public async Task<ServiceRecord?> GetAsync(int id)
    {
        await Init();

        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ServiceRecords WHERE ID = @id";
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var record = new ServiceRecord
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                ReceiptNumber = reader.IsDBNull(reader.GetOrdinal("ReceiptNumber")) ? "" : reader.GetString(reader.GetOrdinal("ReceiptNumber")),
                ReceiptDate = reader.GetDateTime(reader.GetOrdinal("ReceiptDate")),
                CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? "" : reader.GetString(reader.GetOrdinal("CustomerName")),
                MobileNumber = reader.IsDBNull(reader.GetOrdinal("MobileNumber")) ? "" : reader.GetString(reader.GetOrdinal("MobileNumber")),
                Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? "" : reader.GetString(reader.GetOrdinal("Address")),
                VehicleNumber = reader.IsDBNull(reader.GetOrdinal("VehicleNumber")) ? "" : reader.GetString(reader.GetOrdinal("VehicleNumber")),
                VehicleMake = reader.IsDBNull(reader.GetOrdinal("VehicleMake")) ? "" : reader.GetString(reader.GetOrdinal("VehicleMake")),
                VehicleModel = reader.IsDBNull(reader.GetOrdinal("VehicleModel")) ? "" : reader.GetString(reader.GetOrdinal("VehicleModel")),
                CurrentReading = reader.GetDouble(reader.GetOrdinal("CurrentReading")),
                NextReading = reader.GetDouble(reader.GetOrdinal("NextReading")),
                Total = (double)reader.GetDecimal(reader.GetOrdinal("Total")),
                MechanicName = reader.IsDBNull(reader.GetOrdinal("MechanicName")) ? "" : reader.GetString(reader.GetOrdinal("MechanicName")),
                MechanicContact = reader.IsDBNull(reader.GetOrdinal("MechanicContact")) ? "" : reader.GetString(reader.GetOrdinal("MechanicContact"))
            };

            record.Items = await ListItemsAsync(record.ID);
            return record;
        }

        return null;
    }

    public async Task<List<ServiceItem>> ListItemsAsync(int serviceRecordId)
    {
        await Init();

        var items = new List<ServiceItem>();

        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM ServiceItems WHERE ServiceRecordId = @serviceRecordId";
        command.Parameters.AddWithValue("@serviceRecordId", serviceRecordId);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new ServiceItem
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                ServiceRecordId = reader.GetInt32(reader.GetOrdinal("ServiceRecordId")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? "" : reader.GetString(reader.GetOrdinal("Name")),
                Grade = reader.IsDBNull(reader.GetOrdinal("Grade")) ? "" : reader.GetString(reader.GetOrdinal("Grade")),
                Quantity = reader.GetDouble(reader.GetOrdinal("Quantity")),
                Rate = (double)reader.GetDecimal(reader.GetOrdinal("Rate")),
                Amount = (double)reader.GetDecimal(reader.GetOrdinal("Amount"))
            });
        }

        return items;
    }

    public async Task<int> SaveAsync(ServiceRecord record)
    {
        await Init();

        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            // Save or update service record
            if (record.ID == 0)
            {
                command.CommandText = @"
                    INSERT INTO ServiceRecords (
                        ReceiptNumber, ReceiptDate, CustomerName, MobileNumber, 
                        Address, VehicleNumber, VehicleMake, VehicleModel,
                        CurrentReading, NextReading, Total, MechanicName, MechanicContact
                    ) 
                    VALUES (
                        @receiptNumber, @receiptDate, @customerName, @mobileNumber,
                        @address, @vehicleNumber, @vehicleMake, @vehicleModel,
                        @currentReading, @nextReading, @total, @mechanicName, @mechanicContact
                    );
                    SELECT SCOPE_IDENTITY();";
            }
            else
            {
                command.CommandText = @"
                    UPDATE ServiceRecords SET 
                        ReceiptNumber = @receiptNumber,
                        ReceiptDate = @receiptDate,
                        CustomerName = @customerName,
                        MobileNumber = @mobileNumber,
                        Address = @address,
                        VehicleNumber = @vehicleNumber,
                        VehicleMake = @vehicleMake,
                        VehicleModel = @vehicleModel,
                        CurrentReading = @currentReading,
                        NextReading = @nextReading,
                        Total = @total,
                        MechanicName = @mechanicName,
                        MechanicContact = @mechanicContact
                    WHERE ID = @id;
                    SELECT @id;";
                command.Parameters.AddWithValue("@id", record.ID);
            }

            command.Parameters.AddWithValue("@receiptNumber", (object?)record.ReceiptNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@receiptDate", record.ReceiptDate);
            command.Parameters.AddWithValue("@customerName", (object?)record.CustomerName ?? DBNull.Value);
            command.Parameters.AddWithValue("@mobileNumber", (object?)record.MobileNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@address", (object?)record.Address ?? DBNull.Value);
            command.Parameters.AddWithValue("@vehicleNumber", (object?)record.VehicleNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@vehicleMake", (object?)record.VehicleMake ?? DBNull.Value);
            command.Parameters.AddWithValue("@vehicleModel", (object?)record.VehicleModel ?? DBNull.Value);
            command.Parameters.AddWithValue("@currentReading", record.CurrentReading);
            command.Parameters.AddWithValue("@nextReading", record.NextReading);
            command.Parameters.AddWithValue("@total", record.Total);
            command.Parameters.AddWithValue("@mechanicName", (object?)record.MechanicName ?? DBNull.Value);
            command.Parameters.AddWithValue("@mechanicContact", (object?)record.MechanicContact ?? DBNull.Value);

            record.ID = Convert.ToInt32(await command.ExecuteScalarAsync());

            // Delete existing items
            command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM ServiceItems WHERE ServiceRecordId = @serviceRecordId";
            command.Parameters.AddWithValue("@serviceRecordId", record.ID);
            await command.ExecuteNonQueryAsync();

            // Insert new items
            foreach (var item in record.Items)
            {
                command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO ServiceItems (ServiceRecordId, Name, Grade, Quantity, Rate, Amount)
                    VALUES (@serviceRecordId, @name, @grade, @quantity, @rate, @amount)";
                command.Parameters.AddWithValue("@serviceRecordId", record.ID);
                command.Parameters.AddWithValue("@name", (object?)item.Name ?? DBNull.Value);
                command.Parameters.AddWithValue("@grade", (object?)item.Grade ?? DBNull.Value);
                command.Parameters.AddWithValue("@quantity", item.Quantity);
                command.Parameters.AddWithValue("@rate", item.Rate);
                command.Parameters.AddWithValue("@amount", item.Amount);
                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
            return record.ID;
        }
        catch (Exception)
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> DeleteAsync(ServiceRecord record)
    {
        await Init();

        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM ServiceItems WHERE ServiceRecordId = @id";
            command.Parameters.AddWithValue("@id", record.ID);
            await command.ExecuteNonQueryAsync();

            command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM ServiceRecords WHERE ID = @id";
            command.Parameters.AddWithValue("@id", record.ID);
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

    public async Task DropTableAsync()
    {
        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            DROP TABLE IF EXISTS ServiceItems;
            DROP TABLE IF EXISTS ServiceRecords;";
        await command.ExecuteNonQueryAsync();

        _hasBeenInitialized = false;
    }

    public async Task<string> GenerateReceiptNumberAsync()
    {
        await Init();

        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();        // Get the receipt prefix from preferences
        var prefix = _preferences.Get("ReceiptPrefix", "VSC");

        // Get the max receipt number for today
        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT TOP 1 ReceiptNumber 
            FROM ServiceRecords 
            WHERE CAST(ReceiptDate AS DATE) = CAST(GETDATE() AS DATE) 
                AND ReceiptNumber LIKE @prefix + '%'
            ORDER BY ID DESC";
        command.Parameters.AddWithValue("@prefix", prefix);

        var lastNumber = await command.ExecuteScalarAsync() as string;

        int sequence = 1;
        if (lastNumber != null)
        {
            // Extract the numeric part from the receipt number
            var numericPart = lastNumber.Substring(prefix.Length);
            if (int.TryParse(numericPart, out int lastSequence))
            {
                sequence = lastSequence + 1;
            }
        }

        return $"{prefix}{sequence:D3}";
    }
}