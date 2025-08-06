namespace MyUserApp.Models
{
    /// <summary>
    /// Defines the possible authors of an annotation. Using an enum is much safer
    /// and clearer than using strings like "Inspector" or "AI", as it prevents
    /// typos and makes the code self-documenting.
    /// </summary>
    public enum AuthorType
    {
        Inspector,
        Verifier,
        AI
    }
}