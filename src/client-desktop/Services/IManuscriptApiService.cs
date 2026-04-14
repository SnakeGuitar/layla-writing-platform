using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Layla.Desktop.Models;

namespace Layla.Desktop.Services
{
    /// <summary>
    /// Contract for all HTTP communication with the worldbuilding service's manuscript endpoints.
    /// </summary>
    public interface IManuscriptApiService
    {
        /// <summary>
        /// Returns all manuscripts for <paramref name="projectId"/> as index objects
        /// (chapter metadata without content), or <c>null</c> on failure.
        /// </summary>
        Task<List<Manuscript>?> GetManuscriptsByProjectAsync(Guid projectId);

        /// <summary>
        /// Returns a single manuscript with its chapter index, or <c>null</c> when not found.
        /// </summary>
        Task<Manuscript?> GetManuscriptAsync(Guid projectId, string manuscriptId);

        /// <summary>
        /// Creates a new manuscript with the given <paramref name="title"/> and display <paramref name="order"/>.
        /// Returns the created manuscript, or <c>null</c> on failure.
        /// </summary>
        Task<Manuscript?> CreateManuscriptAsync(Guid projectId, string title, int order);

        /// <summary>
        /// Updates the <paramref name="title"/> and/or <paramref name="order"/> of the specified manuscript.
        /// Pass <c>null</c> for any field that should remain unchanged.
        /// Returns the updated manuscript, or <c>null</c> on failure.
        /// </summary>
        Task<Manuscript?> UpdateManuscriptAsync(Guid projectId, string manuscriptId, string? title, int? order);

        /// <summary>
        /// Permanently deletes the manuscript and all its chapters.
        /// Returns <c>true</c> on success, <c>false</c> on failure.
        /// </summary>
        Task<bool> DeleteManuscriptAsync(Guid projectId, string manuscriptId);

        /// <summary>
        /// Returns a single chapter with full RTF content, or <c>null</c> when not found.
        /// </summary>
        Task<Chapter?> GetChapterAsync(Guid projectId, string manuscriptId, Guid chapterId);

        /// <summary>
        /// Creates a new chapter inside the specified manuscript and returns it,
        /// or <c>null</c> on failure.
        /// </summary>
        Task<Chapter?> CreateChapterAsync(Guid projectId, string manuscriptId, string title, string content, int order);

        /// <summary>
        /// Updates a chapter's fields using Last-Write-Wins semantics.
        /// Supply <paramref name="clientTimestamp"/> to enable server-side conflict detection.
        /// Returns the updated chapter, or <c>null</c> on conflict or failure.
        /// </summary>
        Task<Chapter?> UpdateChapterAsync(Guid projectId, string manuscriptId, Guid chapterId, string title, string content, int order, DateTime? clientTimestamp = null);

        /// <summary>
        /// Removes a chapter from its manuscript.
        /// Returns <c>true</c> on success, <c>false</c> on failure.
        /// </summary>
        Task<bool> DeleteChapterAsync(Guid projectId, string manuscriptId, Guid chapterId);
    }
}
