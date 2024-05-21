using System;

namespace Scripts.Utils
{
    public static class EventUtilities
    {
        public static void ConditionalSubscribeOrUnsubscribe(
            bool subscribe,
            Action<EventHandler> subscribeHandler,
            Action<EventHandler> unsubscribeHandler)
        {
            if (subscribe)
            {
                subscribeHandler.Invoke(null);
            }
            else
            {
                unsubscribeHandler.Invoke(null);
            }
        }
    }
}
