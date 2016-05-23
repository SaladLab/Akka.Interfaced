namespace Akka.Interfaced
{
    public interface INotificationChannel
    {
        void Notify(NotificationMessage notificationMessage);
    }
}
