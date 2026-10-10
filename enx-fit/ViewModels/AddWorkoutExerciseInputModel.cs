using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using enx_fit.Models;
using Microsoft.AspNetCore.Mvc;

namespace enx_fit.ViewModels;

public class AddWorkoutExerciseInputModel
{
    [ExerciseIdentifier(ErrorMessage = "Выберите упражнение.")]
    [Display(Name = "Упражнение")]
    [JsonConverter(typeof(ExerciseIdJsonConverter))]
    [ModelBinder(BinderType = typeof(ExerciseIdModelBinder))]
    public Guid ExerciseId { get; set; }
}
