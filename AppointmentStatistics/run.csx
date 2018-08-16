#load "..\shared.csx"

#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;

using System;
using System.Collections.Generic;

public static void Run(
    TimerInfo Timer,
    TraceWriter log,
    IQueryable<Appointment> appointmentTable,
    CloudTable appointmentStatisticsTable)
{
    log.Info($"Timer triggers function.");

    var today = DateTime.UtcNow.Date.AddDays(-1);
    var yesterday = today.AddDays(-1);
    var tomorrow = today.AddDays(1);
    var partitionKey = today.ToString("yyyyMMDD-24h");

    var pickedAppointments = appointmentTable
        .Where(appointment =>
            appointment.Timestamp > yesterday
            && appointment.Timestamp < tomorrow)
        .ToList()
        .OrderBy(appointment => appointment.Published)
        .ToList();

    foreach (var appointment in pickedAppointments)
    {
        log.Info(string.Format("{0} - {1} - {2}", appointment.Published, appointment.Appointed, appointment.Time));
    }

    var todayAppointments = new Queue<Appointment>(pickedAppointments);

    var statistics = new Dictionary<DateTime, AppointmentStatistics>();
    var currentAppointments = new HashSet<Appointment>();

    DateTime nextPeriod;
    for (var currentTime = today; currentTime < tomorrow; currentTime = nextPeriod)
    {
        nextPeriod = currentTime.AddHours(0.5);

        var currentStatistic = new AppointmentStatistics
        {
            PartitionKey = partitionKey,
            RowKey = currentTime.ToString("HH:mm"),
            StartTime = currentTime,
            EndTime = nextPeriod
        };

        foreach (var appointment in currentAppointments.ToArray())
        {
            if (appointment.Appointed < currentTime)
            {
                currentAppointments.Remove(appointment);
            }
            else
            {
                currentStatistic.ValidAppointments++;
            }
        }

        while (todayAppointments.Any() && todayAppointments.Peek().Published < nextPeriod)
        {
            if (todayAppointments.Peek().Published >= currentTime)
            {
                currentStatistic.PublishAppointments++;
            }

            if (todayAppointments.Peek().Appointed >= currentTime)
            {
                currentStatistic.ValidAppointments++;
                currentAppointments.Add(todayAppointments.Peek());
            }

            todayAppointments.Dequeue();
        }

        if (currentStatistic.PublishAppointments + currentStatistic.ValidAppointments > 0)
        {
            statistics[currentTime] = currentStatistic;
        }
    }

    foreach (var statistic in statistics.Values)
    {
        log.Info(string.Format("{0} - {1} - {2} - {3}", statistic.PartitionKey, statistic.RowKey, statistic.PublishAppointments, statistic.ValidAppointments));
        appointmentStatisticsTable.Execute(TableOperation.Insert(statistic));
    }
}
