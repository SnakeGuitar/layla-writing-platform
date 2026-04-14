namespace Layla.Desktop.Models
{
    /// <summary>
    /// A reference from a chapter to a wiki entity, detected by the
    /// worldbuilding service when chapter content is saved.
    /// </summary>
    public class Mention
    {
        /// <summary>UUID of the referenced wiki entry.</summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Cached entity name at detection time.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Entity type (Character, Location, Event, Object).</summary>
        public string EntityType { get; set; } = string.Empty;
    }
}
