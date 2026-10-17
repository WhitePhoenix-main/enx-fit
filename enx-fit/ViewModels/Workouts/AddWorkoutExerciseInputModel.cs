using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using enx_fit.ModelBinding;
using enx_fit.Models;
using enx_fit.Serialization;
using enx_fit.Validation;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.ViewModels.Workouts;

public class AddWorkoutExerciseInputModel
{
    [ExerciseIdentifier(ErrorMessage = "Выберите упражнение.")]
    [Display(Name = "Упражнение")]
    [JsonConverter(typeof(ExerciseIdJsonConverter))]
    [ModelBinder(BinderType = typeof(ExerciseIdModelBinder))]
    public Guid ExerciseId { get; set; }
}
