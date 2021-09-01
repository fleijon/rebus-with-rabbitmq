using System;

namespace Common.Messages
{
    public class DateTimeMessage
    {
        public DateTime DateTime { get; }

        public DateTimeMessage(DateTime dateTime)
        {
            DateTime = dateTime;
        }
    }
}
