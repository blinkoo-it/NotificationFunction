using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NotificationFunction.Models;

namespace NotificationFunction.Helpers
{
    public static class ValidateRequestBodyHelper
    {
        // this is a static method that allows to validate the request body
        public static async Task<InputRequestBody<T>> ValidateBodyAsync<T>(HttpRequest request, ILogger log)
        {
            // initialise the response object
            InputRequestBody<T> inputRequestBody = new InputRequestBody<T>();
            // get the string body from the incoming request
            string bodyString = await request.ReadAsStringAsync();
            try {
                // attach the deserialised body object into the value property
                T bodyObject = JsonConvert.DeserializeObject<T>(bodyString);
                // log.LogCritical(bodyObject.ToString());
                inputRequestBody.Value = bodyObject;
                // initalise the validation result list - this will contain any error found during validation
                List<ValidationResult> results = new List<ValidationResult>();
                // we use Microsoft Validator to validate the whole request body
                // we need to provide the deserialised body and the results array as input
                inputRequestBody.IsValid = Validator.TryValidateObject(
                    instance: inputRequestBody.Value, 
                    validationContext: new ValidationContext(
                        instance: inputRequestBody.Value, 
                        serviceProvider: null, 
                        items: null
                    ), 
                    validationResults: results, 
                    validateAllProperties: true
                );
                // finally we attach the results of the validation process to the response object
                inputRequestBody.ValidationResults = results;
            } catch (Exception ex) {
                log.LogWarning(ex.Message);
                // in case body string is empty or it is not a valid json then deserialization and validation will fail
                // for this reason we should catch the exception and handle it manually
                ValidationResult validationResult = new ValidationResult(errorMessage: "request body is not a valid JSON");
                inputRequestBody.IsValid = false;
                inputRequestBody.ValidationResults = new List<ValidationResult>() { validationResult };
            }
            // return the response object - main functions will check .IsValid
            return inputRequestBody;
        }
    }
}

