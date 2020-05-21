using Microsoft.WindowsAzure.Storage.Table;

namespace models_and_validators
{
    public class AdminConfiguration : TableEntity
    {
        public string ConfigId { get; set; }
        public string FlagName { get; set; }
        public string FlagValue { get; set; }
        public string FlagDescription { get; set; }
    }

    public class AllowExtenedUser : TableEntity
    {
        public string UserId { get; set; }
    }
}
