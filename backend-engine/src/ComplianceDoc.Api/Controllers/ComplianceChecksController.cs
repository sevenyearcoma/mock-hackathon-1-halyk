using Microsoft.AspNetCore.Mvc;
using ComplianceDoc.Api.Contracts.Requests;
using ComplianceDoc.Api.Domain.Models;
using ComplianceDoc.Api.Services.Interfaces;
using ComplianceDoc.Api.Services.Implementations;

namespace ComplianceDoc.Api.Controllers;

[ApiController]
[Route("api/compliance-checks")]
public sealed class ComplianceChecksController : ControllerBase
{
    private readonly IComplianceValidationService _validation;
    private readonly IComplianceCheckStore _store;

    public ComplianceChecksController(
        IComplianceValidationService validation,
        IComplianceCheckStore store)
    {
        _validation = validation;
        _store = store;
    }

    /// <summary>Submit one or more compliance cases (structured JSON input).</summary>
    [HttpPost]
    public ActionResult<IReadOnlyList<ComplianceCheck>> Create([FromBody] List<ComplianceCaseInput> inputs)
    {
        if (inputs is null || inputs.Count == 0)
            return BadRequest("At least one case is required.");

        var results = new List<ComplianceCheck>();
        foreach (var input in inputs)
        {
            var check = _validation.CreateCheck(input);
            _store.Save(check);
            results.Add(check);
        }

        return Ok(results);
    }

    /// <summary>List all stored compliance checks.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<ComplianceCheck>> GetAll() => Ok(_store.GetAll());

    /// <summary>Get a single compliance check by its GUID.</summary>
    [HttpGet("{id:guid}")]
    public ActionResult<ComplianceCheck> GetById(Guid id)
    {
        var check = _store.GetById(id);
        return check is null ? NotFound() : Ok(check);
    }

    /// <summary>Record human review of a compliance check. Does NOT approve or reject the transaction.</summary>
    [HttpPost("{id:guid}/review")]
    public ActionResult<ComplianceCheck> Review(Guid id, [FromBody] ReviewComplianceCheckRequest request)
    {
        var check = _store.GetById(id);
        if (check is null) return NotFound();

        check.ReviewedByHuman = request.ApprovedByHuman;
        check.HumanComment = request.HumanComment;
        check.FinalClientMessage = request.EditedClientMessage ?? check.DraftClientMessage;

        _store.Save(check);
        return Ok(check);
    }

    /// <summary>Create a demo compliance check from hardcoded documents (two cases: clean + broken).</summary>
    [HttpPost("demo")]
    public ActionResult<IReadOnlyList<ComplianceCheck>> Demo()
    {
        var inputs = DemoData.GetDemoCases();
        var results = new List<ComplianceCheck>();
        foreach (var input in inputs)
        {
            var check = _validation.CreateCheck(input);
            _store.Save(check);
            results.Add(check);
        }
        return Ok(results);
    }
}
