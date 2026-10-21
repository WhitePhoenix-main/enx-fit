namespace enx_fit.ViewModels.Workouts;
public sealed record WorkoutHubCardViewModel(enx_fit.Pages.Training.IndexModel Page,WorkoutHubCard Card,bool Calendar=false,bool Client=false);
