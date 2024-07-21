
namespace SessionModManagerCore.Classes
{
    public class CatalogSubscription
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }

        public CatalogSubscription()
        {
            IsActive = true;
        }
    }
}
