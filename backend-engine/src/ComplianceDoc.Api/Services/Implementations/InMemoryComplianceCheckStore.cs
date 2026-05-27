using System.Collections.Concurrent;
using ComplianceDoc.Api.Domain.Models;
using ComplianceDoc.Api.Services.Interfaces;

namespace ComplianceDoc.Api.Services.Implementations;

public sealed class InMemoryComplianceCheckStore : IComplianceCheckStore
{
    private readonly ConcurrentDictionary<Guid, ComplianceCheck> _store = new();

    public void Save(ComplianceCheck check) => _store[check.Id] = check;

    public ComplianceCheck? GetById(Guid id) =>
        _store.TryGetValue(id, out var check) ? check : null;

    public IReadOnlyList<ComplianceCheck> GetAll() =>
        _store.Values.OrderByDescending(x => x.CreatedAt).ToList();
}
