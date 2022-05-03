namespace NotificationFunction.Models
{
    public class ServiceResponse<T>
    {
        // Data is the actual data - in this case charaters or list of characters
        public T Data { get; set; }
        // then we can define additionalstuff to send
        // a success that tells if the operation went well
        public bool Success { get; set; } = true;

        public int Status { get; set; } = 200;

        // a message in case something went wrong will display the message from the exception
        public string Message { get; set; } = null;

        public ServiceResponse() {}
        public ServiceResponse(T Data, bool Success, int Status, string Message)
        {
            this.Data = Data;
            this.Success = Success;
            this.Status = Status;
            this.Message = Message;
        }
    }
}