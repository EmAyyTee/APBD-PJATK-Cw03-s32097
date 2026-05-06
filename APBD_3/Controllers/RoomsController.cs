using APBD_3.Data;
using APBD_3.Models;
using Microsoft.AspNetCore.Mvc;

namespace APBD_3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Room>> GetRooms(
        [FromQuery] int? minCapacity,
        [FromQuery] bool? hasProjector,
        [FromQuery] bool? activeOnly)
    {
        var rooms = InMemoryDataStore.Rooms.AsEnumerable();

        if (minCapacity.HasValue)
        {
            rooms = rooms.Where(room => room.Capacity >= minCapacity.Value);
        }

        if (hasProjector.HasValue)
        {
            rooms = rooms.Where(room => room.HasProjector == hasProjector.Value);
        }

        if (activeOnly == true)
        {
            rooms = rooms.Where(room => room.IsActive);
        }

        return Ok(rooms);
    }

    [HttpGet("{id:int}")]
    public ActionResult<Room> GetRoomById([FromRoute] int id)
    {
        var room = InMemoryDataStore.Rooms.FirstOrDefault(room => room.Id == id);

        if (room is null)
        {
            return NotFound($"Room with id {id} was not found.");
        }

        return Ok(room);
    }

    [HttpGet("building/{buildingCode}")]
    public ActionResult<IEnumerable<Room>> GetRoomsByBuildingCode([FromRoute] string buildingCode)
    {
        var rooms = InMemoryDataStore.Rooms
            .Where(room => room.BuildingCode.Equals(buildingCode, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Ok(rooms);
    }

    [HttpPost]
    public ActionResult<Room> CreateRoom([FromBody] Room room)
    {
        var newId = InMemoryDataStore.Rooms.Any()
            ? InMemoryDataStore.Rooms.Max(existingRoom => existingRoom.Id) + 1
            : 1;

        room.Id = newId;

        InMemoryDataStore.Rooms.Add(room);

        return CreatedAtAction(
            nameof(GetRoomById),
            new { id = room.Id },
            room
        );
    }

    [HttpPut("{id:int}")]
    public ActionResult<Room> UpdateRoom([FromRoute] int id, [FromBody] Room updatedRoom)
    {
        var room = InMemoryDataStore.Rooms.FirstOrDefault(existingRoom => existingRoom.Id == id);

        if (room is null)
        {
            return NotFound($"Room with id {id} was not found.");
        }

        room.Name = updatedRoom.Name;
        room.BuildingCode = updatedRoom.BuildingCode;
        room.Floor = updatedRoom.Floor;
        room.Capacity = updatedRoom.Capacity;
        room.HasProjector = updatedRoom.HasProjector;
        room.IsActive = updatedRoom.IsActive;

        return Ok(room);
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteRoom([FromRoute] int id)
    {
        var room = InMemoryDataStore.Rooms.FirstOrDefault(existingRoom => existingRoom.Id == id);

        if (room is null)
        {
            return NotFound($"Room with id {id} was not found.");
        }

        var hasReservations = InMemoryDataStore.Reservations
            .Any(reservation => reservation.RoomId == id);

        if (hasReservations)
        {
            return Conflict($"Room with id {id} cannot be deleted because it has reservations.");
        }

        InMemoryDataStore.Rooms.Remove(room);

        return NoContent();
    }
}