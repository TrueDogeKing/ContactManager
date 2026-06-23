using ContactManager.Application.DTOs.Contacts;
using ContactManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Api.Controllers;

/// Contact endpoints. Reads are public; create, update and delete require authentication.
[ApiController]
[Route("api/contacts")]
public class ContactsController : ControllerBase
{
    private readonly IContactService _contactService;

    /// Creates controller with dependencies.
    public ContactsController(IContactService contactService)
    {
        _contactService = contactService;
    }

    /// Returns all contacts. Public endpoint.
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ContactResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var contacts = await _contactService.GetAllAsync(cancellationToken);
        return Ok(contacts);
    }
}
