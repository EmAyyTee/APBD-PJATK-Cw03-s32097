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
}