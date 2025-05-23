using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using VehicleServiceCenter.Models;

namespace VehicleServiceCenter.Data;

public class ServiceRepository
{
    private bool _hasBeenInitialized = false;
    private readonly ILogger _logger;

    public ServiceRepository(ILogger<ServiceRepository> logger)
    {
        _logger = logger;
    }

    private async Task Init()
    {
        if (_hasBeenInitialized)
            return;

        using var connection = new SqlConnection(Constants.ConnectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = @"
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ServiceRecords]') AND type in (N'U'))
            BEGIN
                CREATE TABLE ServiceRecords (
                    ID INT IDENTITY(1,1) PRIMARY KEY,
                    ReceiptNumber NVARCHAR(50),
                    Date DATETIME,
                    CustomerName NVARCHAR(100),
                    MobileNumber NVARCHAR(20),
                    Address NVARCHAR(200),
                    VehicleNumber NVARCHAR(50),
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
        command.CommandText = "SELECT * FROM ServiceRecords ORDER BY Date DESC";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var record = new ServiceRecord
            {
                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                ReceiptNumber = reader.GetString(reader.GetOrdinal("ReceiptNumber")),
                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                MobileNumber = reader.GetString(reader.GetOrdinal("MobileNumber")),
                Address = reader.GetString(reader.GetOrdinal("Address")),
                VehicleNumber = reader.GetString(reader.GetOrdinal("VehicleNumber")),
                CurrentReading = reader.GetDouble(reader.GetOrdinal("CurrentReading")),
                NextReading = reader.GetDouble(reader.GetOrdinal("NextReading")),
                Total = (double)reader.GetDecimal(reader.GetOrdinal("Total")),
                MechanicName = reader.GetString(reader.GetOrdinal("MechanicName")),
                MechanicContact = reader.GetString(reader.GetOrdinal("MechanicContact"))
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
                ReceiptNumber = reader.GetString(reader.GetOrdinal("ReceiptNumber")),
                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                MobileNumber = reader.GetString(reader.GetOrdinal("MobileNumber")),
                Address = reader.GetString(reader.GetOrdinal("Address")),
                VehicleNumber = reader.GetString(reader.GetOrdinal("VehicleNumber")),
                CurrentReading = reader.GetDouble(reader.GetOrdinal("CurrentReading")),
                NextReading = reader.GetDouble(reader.GetOrdinal("NextReading")),
                Total = (double)reader.GetDecimal(reader.GetOrdinal("Total")),
                MechanicName = reader.GetString(reader.GetOrdinal("MechanicName")),
                MechanicContact = reader.GetString(reader.GetOrdinal("MechanicContact"))
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
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Grade = reader.GetString(reader.GetOrdinal("Grade")),
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

            if (record.ID == 0)
            {
                command.CommandText = @"
                    INSERT INTO ServiceRecords (ReceiptNumber, Date, CustomerName, MobileNumber, Address, 
                        VehicleNumber, CurrentReading, NextReading, Total, MechanicName, MechanicContact)
                    VALUES (@receiptNumber, @date, @customerName, @mobileNumber, @address,
                        @vehicleNumber, @currentReading, @nextReading, @total, @mechanicName, @mechanicContact);
                    SELECT SCOPE_IDENTITY();";
            }
            else
            {
                command.CommandText = @"
                    UPDATE ServiceRecords 
                    SET ReceiptNumber = @receiptNumber,
                        Date = @date,
                        CustomerName = @customerName,
                        MobileNumber = @mobileNumber,
                        Address = @address,
                        VehicleNumber = @vehicleNumber,
                        CurrentReading = @currentReading,
                        NextReading = @nextReading,
                        Total = @total,
                        MechanicName = @mechanicName,
                        MechanicContact = @mechanicContact
                    WHERE ID = @id;
                    SELECT @id;";
                command.Parameters.AddWithValue("@id", record.ID);
            }

            command.Parameters.AddWithValue("@receiptNumber", record.ReceiptNumber);
            command.Parameters.AddWithValue("@date", record.Date);
            command.Parameters.AddWithValue("@customerName", record.CustomerName);
            command.Parameters.AddWithValue("@mobileNumber", record.MobileNumber);
            command.Parameters.AddWithValue("@address", record.Address);
            command.Parameters.AddWithValue("@vehicleNumber", record.VehicleNumber);
            command.Parameters.AddWithValue("@currentReading", record.CurrentReading);
            command.Parameters.AddWithValue("@nextReading", record.NextReading);
            command.Parameters.AddWithValue("@total", record.Total);
            command.Parameters.AddWithValue("@mechanicName", record.MechanicName);
            command.Parameters.AddWithValue("@mechanicContact", record.MechanicContact);

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
                command.Parameters.AddWithValue("@name", item.Name);
                command.Parameters.AddWithValue("@grade", item.Grade);
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
}