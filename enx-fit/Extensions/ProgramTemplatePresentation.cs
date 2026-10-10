using enx_fit.Models;

namespace enx_fit.Extensions;

public static class ProgramTemplatePresentation
{
    public static bool IsStarter(TrainingProgram program) => program.IsTemplate && program.Id is -6 or -7;
    public static string StarterEquipment(TrainingProgram program) => program.Id == -6 ? "Без оборудования" : "Пара гантелей";
}
