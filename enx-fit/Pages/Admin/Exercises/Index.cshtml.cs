using enx_fit.Models;
using enx_fit.Security;
using enx_fit.Services;
using enx_fit.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace enx_fit.Pages.Admin.Exercises;

[MinimumRole(UserRole.Administrator)]
public sealed class IndexModel(ExerciseService exercises) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Group { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public IReadOnlyList<Exercise> Items { get; private set; } = [];
    public IReadOnlyList<string> Groups { get; private set; } = [];
    public int Total { get; private set; }
    public int FilteredCount { get; private set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(FilteredCount / 20d));
    public async Task OnGetAsync()
    {
        Response.Headers.CacheControl = "no-cache, no-store";
        Search = Search?.Trim(); if (Search?.Length > 120) Search = Search[..120];
        var all = await exercises.GetAllAsync(null); Total = all.Count;
        Groups = all.Select(e => e.MuscleGroup).Where(g => !string.IsNullOrWhiteSpace(g)).Distinct().Order().ToList();
        if (Group is not null && !Groups.Contains(Group)) Group = null;
        var found = all.Where(e => (string.IsNullOrEmpty(Group) || e.MuscleGroup == Group) &&
            (string.IsNullOrEmpty(Search) || ExercisePresentation.DisplayName(e).Contains(Search, StringComparison.OrdinalIgnoreCase) || e.Equipment.Contains(Search, StringComparison.OrdinalIgnoreCase))).ToList();
        FilteredCount = found.Count; PageNumber = Math.Clamp(PageNumber, 1, PageCount);
        Items = found.Skip((PageNumber - 1) * 20).Take(20).ToList(); ModelState.Clear();
    }
}
