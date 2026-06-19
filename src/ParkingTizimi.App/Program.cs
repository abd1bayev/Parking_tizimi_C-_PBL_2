using ParkingTizimi.App;
using ParkingTizimi.Core.Services;
using ParkingTizimi.Infrastructure.Repositories;
using ParkingTizimi.Shared.Time;

var repository = new JsonParkingRepository(Directory.GetCurrentDirectory());
var service = new ParkingSystemService(repository, new SystemClock());
var app = new ConsoleApp(service);

await app.RunAsync();
