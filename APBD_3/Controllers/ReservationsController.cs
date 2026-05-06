using APBD_3.Data;
using APBD_3.Models;
using Microsoft.AspNetCore.Mvc;

namespace APBD_3.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Reservation>> GetReservations(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        [FromQuery] int? roomId)
    {
        var reservations = InMemoryDataStore.Reservations.AsEnumerable();

        if (date.HasValue)
        {
            reservations = reservations.Where(reservation => reservation.Date == date.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            reservations = reservations.Where(reservation =>
                reservation.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (roomId.HasValue)
        {
            reservations = reservations.Where(reservation => reservation.RoomId == roomId.Value);
        }

        return Ok(reservations);
    }

    [HttpGet("{id:int}")]
    public ActionResult<Reservation> GetReservationById([FromRoute] int id)
    {
        var reservation = InMemoryDataStore.Reservations
            .FirstOrDefault(reservation => reservation.Id == id);

        if (reservation is null)
        {
            return NotFound($"Reservation with id {id} was not found.");
        }

        return Ok(reservation);
    }

    [HttpPost]
    public ActionResult<Reservation> CreateReservation([FromBody] Reservation reservation)
    {
        var room = InMemoryDataStore.Rooms
            .FirstOrDefault(room => room.Id == reservation.RoomId);

        if (room is null)
        {
            return BadRequest($"Room with id {reservation.RoomId} does not exist.");
        }

        if (!room.IsActive)
        {
            return BadRequest($"Room with id {reservation.RoomId} is not active.");
        }

        var hasTimeConflict = InMemoryDataStore.Reservations.Any(existingReservation =>
            existingReservation.RoomId == reservation.RoomId &&
            existingReservation.Date == reservation.Date &&
            !existingReservation.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) &&
            ReservationTimesOverlap(
                reservation.StartTime,
                reservation.EndTime,
                existingReservation.StartTime,
                existingReservation.EndTime
            )
        );

        if (hasTimeConflict)
        {
            return Conflict("Reservation conflicts with an existing reservation for the same room.");
        }

        var newId = InMemoryDataStore.Reservations.Any()
            ? InMemoryDataStore.Reservations.Max(existingReservation => existingReservation.Id) + 1
            : 1;

        reservation.Id = newId;

        InMemoryDataStore.Reservations.Add(reservation);

        return CreatedAtAction(
            nameof(GetReservationById),
            new { id = reservation.Id },
            reservation
        );
    }

    [HttpPut("{id:int}")]
    public ActionResult<Reservation> UpdateReservation(
        [FromRoute] int id,
        [FromBody] Reservation updatedReservation)
    {
        var reservation = InMemoryDataStore.Reservations
            .FirstOrDefault(existingReservation => existingReservation.Id == id);

        if (reservation is null)
        {
            return NotFound($"Reservation with id {id} was not found.");
        }

        var room = InMemoryDataStore.Rooms
            .FirstOrDefault(room => room.Id == updatedReservation.RoomId);

        if (room is null)
        {
            return BadRequest($"Room with id {updatedReservation.RoomId} does not exist.");
        }

        if (!room.IsActive)
        {
            return BadRequest($"Room with id {updatedReservation.RoomId} is not active.");
        }

        var hasTimeConflict = InMemoryDataStore.Reservations.Any(existingReservation =>
            existingReservation.Id != id &&
            existingReservation.RoomId == updatedReservation.RoomId &&
            existingReservation.Date == updatedReservation.Date &&
            !existingReservation.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) &&
            ReservationTimesOverlap(
                updatedReservation.StartTime,
                updatedReservation.EndTime,
                existingReservation.StartTime,
                existingReservation.EndTime
            )
        );

        if (hasTimeConflict)
        {
            return Conflict("Reservation conflicts with an existing reservation for the same room.");
        }

        reservation.RoomId = updatedReservation.RoomId;
        reservation.OrganizerName = updatedReservation.OrganizerName;
        reservation.Topic = updatedReservation.Topic;
        reservation.Date = updatedReservation.Date;
        reservation.StartTime = updatedReservation.StartTime;
        reservation.EndTime = updatedReservation.EndTime;
        reservation.Status = updatedReservation.Status;

        return Ok(reservation);
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteReservation([FromRoute] int id)
    {
        var reservation = InMemoryDataStore.Reservations
            .FirstOrDefault(existingReservation => existingReservation.Id == id);

        if (reservation is null)
        {
            return NotFound($"Reservation with id {id} was not found.");
        }

        InMemoryDataStore.Reservations.Remove(reservation);

        return NoContent();
    }

    private static bool ReservationTimesOverlap(
        TimeOnly newStartTime,
        TimeOnly newEndTime,
        TimeOnly existingStartTime,
        TimeOnly existingEndTime)
    {
        return newStartTime < existingEndTime && newEndTime > existingStartTime;
    }
}