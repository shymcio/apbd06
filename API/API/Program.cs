using System.Data.SqlClient;
using API.DTOs;
using API.Validators;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<CreateAnimalRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();



app.MapGet("api/animals", (IConfiguration configuration, string orderBy = "name") =>
{
    var animals = new List<GetAllAnimalsResponse>();
    using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("Default")))
    {
        string orderByClause = "";
        var sqlCommand = new SqlCommand();
        
        if ((orderBy.ToLower().Equals("name")) || (orderBy.ToLower().Equals("description")) || (orderBy.ToLower().Equals("area")) || (orderBy.ToLower().Equals("category")))
        {
            orderByClause = $"ORDER BY {orderBy} ASC";
            sqlCommand = new SqlCommand($"SELECT * FROM Animal {orderByClause}", sqlConnection);
        }
        else
        {
            sqlCommand = new SqlCommand($"SELECT * FROM Animal {orderByClause}", sqlConnection);
        }
        
        sqlCommand.Parameters.AddWithValue("@name", orderBy);
        sqlCommand.Connection.Open();
        var reader = sqlCommand.ExecuteReader();

        while (reader.Read())
        {
            animals.Add(new GetAllAnimalsResponse(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)));
        }

        return Results.Ok(animals);
    }
});

app.MapPost("api/animals", (IConfiguration configuration, CreateAnimalRequest request, IValidator<CreateAnimalRequest> validator) =>
{
    var validation = validator.Validate(request);
    if (!validation.IsValid) return Results.ValidationProblem(validation.ToDictionary());
    
    using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("Default")))
    {
        var sqlCommand = new SqlCommand("INSERT INTO Animal (Name, Description, Category, Area) VALUES (@name, @description, @category, @area);", sqlConnection);
        
        sqlCommand.Parameters.AddWithValue("@name", request.Name);
        sqlCommand.Parameters.AddWithValue("@description", request.Description);
        sqlCommand.Parameters.AddWithValue("@category", request.Category);
        sqlCommand.Parameters.AddWithValue("@area", request.Area);
        
        sqlCommand.Connection.Open();

        sqlCommand.ExecuteNonQuery();
        
        return Results.Created();
    }
});

app.Run();