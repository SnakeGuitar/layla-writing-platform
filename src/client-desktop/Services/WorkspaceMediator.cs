using System;

namespace Layla.Desktop.Services
{
    /// <summary>
    /// Lightweight event bus that enables cross-tab navigation within the workspace.
    /// Child views raise requests; <see cref="Views.WorkspaceView"/> subscribes and
    /// switches the active tab + passes the target context.
    /// </summary>
    public static class WorkspaceMediator
    {
        /// <summary>
        /// Raised when any view requests a switch to the Wiki tab and selection
        /// of a specific entity by its ID.
        /// </summary>
        public static event Action<string>? NavigateToWikiEntry;

        /// <summary>
        /// Raised when any view requests a switch to the Manuscript Editor tab
        /// and selection of a specific chapter in a specific manuscript.
        /// </summary>
        public static event Action<string, string>? NavigateToChapter; // manuscriptId, chapterId

        /// <summary>
        /// Raised when any view requests a switch to the Graph tab and optionally
        /// highlights a specific entity node.
        /// </summary>
        public static event Action<string?>? NavigateToGraph;

        public static void RequestNavigateToWikiEntry(string entityId)
            => NavigateToWikiEntry?.Invoke(entityId);

        public static void RequestNavigateToChapter(string manuscriptId, string chapterId)
            => NavigateToChapter?.Invoke(manuscriptId, chapterId);

        public static void RequestNavigateToGraph(string? entityId = null)
            => NavigateToGraph?.Invoke(entityId);
    }
}
