using Atlas.Audit.Interfaces;

namespace Atlas.Audit.Services;

public class AuditDataStrategyFactory
{
    private readonly Dictionary<Type, IAuditDataStrategy> _strategies;

    public AuditDataStrategyFactory(IEnumerable<IAuditDataStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.EntityType, s => s);
    }

    public IAuditDataStrategy GetStrategy(Type entityType)
    {
        if (_strategies.TryGetValue(entityType, out var strategy))
        {
            return strategy;
        }

        throw new InvalidOperationException($"No audit strategy found for entity type {entityType.Name}");
    }

    public bool HasStrategy(Type entityType)
    {
        return _strategies.ContainsKey(entityType);
    }
}
