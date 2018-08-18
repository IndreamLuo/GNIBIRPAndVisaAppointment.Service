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

    var today = DateTime.UtcNow.AddHours(1).Date.AddDays(-1);
    var yesterday = today.AddDays(-1);
    var tomorrow = today.AddDays(1);
    var partitionKey = $"{today.ToString("yyyyMMdd-24")}h";

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

        var hasRecord = false;

        foreach (var appointment in currentAppointments.ToArray())
        {
            if (appointment.Appointed < currentTime)
            {
                currentAppointments.Remove(appointment);
            }
            else
            {
                IncreaseValidAppointments(currentTime, nextPeriod, appointment, ref currentStatistic);
                hasRecord = true;
            }
        }

        while (todayAppointments.Any() && todayAppointments.Peek().Published < nextPeriod)
        {
            if (todayAppointments.Peek().Published >= currentTime)
            {
                IncreasePublishAppointments(todayAppointments.Peek(), ref currentStatistic);
                hasRecord = true;
            }

            if (todayAppointments.Peek().Appointed >= currentTime)
            {
                IncreaseValidAppointments(currentTime, nextPeriod, todayAppointments.Peek(), ref currentStatistic);
                hasRecord = true;
                currentAppointments.Add(todayAppointments.Peek());
            }

            todayAppointments.Dequeue();
        }

        if (hasRecord)
        {
            statistics[currentTime] = currentStatistic;
        }
    }

    foreach (var statistic in statistics.Values)
    {
        log.Info($"{statistic.PartitionKey} - {statistic.RowKey}");
        appointmentStatisticsTable.Execute(TableOperation.Insert(statistic));
    }
}

public static void IncreaseValidAppointments(DateTime currentTime, DateTime nextPeriod, Appointment appointment, ref AppointmentStatistics statistic)
{
    var continuousTime = ((appointment.Appointed == null || appointment.Appointed > nextPeriod
            ? nextPeriod
            : appointment.Appointed.Value)
        - (appointment.Published < currentTime ? currentTime : appointment.Published))
        .TotalSeconds;

    switch (appointment.Category)
    {
        case Categories.Work:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.ValidIRPWorkNew++;
                statistic.TotalContinuousIRPWorkNew += continuousTime;
            }
            else
            {
                statistic.ValidIRPWorkRenew++;
                statistic.TotalContinuousIRPWorkRenew += continuousTime;
            }
            break;
        case Categories.Study:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.ValidIRPStudyNew++;
                statistic.TotalContinuousIRPStudyNew += continuousTime;
            }
            else
            {
                statistic.ValidIRPStudyRenew++;
                statistic.TotalContinuousIRPStudyRenew += continuousTime;
            }
            break;
        case Categories.Other:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.ValidIRPOtherNew++;
                statistic.TotalContinuousIRPOtherNew += continuousTime;
            }
            else
            {
                statistic.ValidIRPOtherRenew++;
                statistic.TotalContinuousIRPOtherRenew += continuousTime;
            }
            break;
        case Categories.Family:
            statistic.ValidVisaFamily++;
            statistic.TotalContinuousVisaFamily += continuousTime;
            break;
        case Categories.Individual:
            statistic.ValidVisaIndividual++;
            statistic.TotalContinuousVisaIndividual += continuousTime;
            break;
        case Categories.Emergency:
            statistic.ValidVisaEmergency++;
            statistic.TotalContinuousVisaEmergency += continuousTime;
            break;
    }
}

public static void IncreasePublishAppointments(Appointment appointment, ref AppointmentStatistics statistic)
{
    switch (appointment.Category)
    {
        case Categories.Work:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.PublishIRPWorkNew++;
            }
            else
            {
                statistic.PublishIRPWorkRenew++;
            }
            break;
        case Categories.Study:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.PublishIRPStudyNew++;
            }
            else
            {
                statistic.PublishIRPStudyRenew++;
            }
            break;
        case Categories.Other:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.PublishIRPOtherNew++;
            }
            else
            {
                statistic.PublishIRPOtherRenew++;
            }
            break;
        case Categories.Family:
            statistic.PublishVisaFamily++;
            break;
        case Categories.Individual:
            statistic.PublishVisaIndividual++;
            break;
        case Categories.Emergency:
            statistic.PublishVisaEmergency++;
            break;
    }
}