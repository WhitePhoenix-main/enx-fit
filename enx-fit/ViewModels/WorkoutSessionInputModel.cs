using System.ComponentModel.DataAnnotations;
using enx_fit.Models;

namespace enx_fit.ViewModels;

public class WorkoutSessionInputModel
{
    [Display(Name = "Пользователь")]
    public string? OwnerId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(160)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public static WorkoutSessionInputModel FromWorkout(WorkoutSession workout) => new()
    {
        OwnerId = workout.UserId,
        Date = workout.Date,
        Title = workout.Title,
        Notes = workout.Notes
    };
}
