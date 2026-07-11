using System.ComponentModel.DataAnnotations;

namespace SmartCondoApi.Attributes
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        private string PropertyName { get; set; }
        private object[] DesiredValues { get; set; }

        public RequiredIfAttribute(string propertyName, params object[] desiredValues)
        {
            PropertyName = propertyName;
            DesiredValues = desiredValues;
            ErrorMessage = "The {0} field is required"; // Mensagem padrão
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();

            // Obtém o valor da propriedade que estamos verificando
            var propertyValue = type.GetProperty(PropertyName)?.GetValue(instance, null);

            // Verifica se o propertyValue está entre os valores desejados
            var isRequired = false;
            foreach (var desiredValue in DesiredValues)
            {
                if (desiredValue.Equals(propertyValue))
                {
                    isRequired = true;
                    break;
                }
            }

            if (isRequired && (value == null || (value is string str && string.IsNullOrWhiteSpace(str))))
            {
                return new ValidationResult(FormatErrorMessage(context.DisplayName));
            }

            return ValidationResult.Success;
        }
    }
}
