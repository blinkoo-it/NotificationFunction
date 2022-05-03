using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NotificationFunction.Models
{    
    public class InputRequestBody<T>
    {
        public bool IsValid { get; set; }
        public T Value { get; set; }
        public IEnumerable<ValidationResult> ValidationResults { get; set; }
    }
}