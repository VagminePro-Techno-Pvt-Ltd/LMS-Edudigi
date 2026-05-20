using System.ComponentModel.DataAnnotations;
using TMS.Models.Masters;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels
{
    public class RequireOptionTextIfObjectiveAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (validationContext.ObjectInstance is QuizQuestionViewModel question)
            {
                // Only validate if the question type is Objective
                if (question.QuestionType == QuestionType.Objective)
                {
                    if (question.Options == null || question.Options.Count == 0)
                        return new ValidationResult("At least one option is required for objective questions.");

                    foreach (var option in question.Options)
                    {
                        if (string.IsNullOrWhiteSpace(option.OptionText))
                            return new ValidationResult("Option Text is required for all options in objective questions.");
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}
