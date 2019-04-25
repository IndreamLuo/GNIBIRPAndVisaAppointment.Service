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

    log.Info($"Date{today:yyyyMMdd}");

    var pickedAppointments = appointmentTable
        .Where(appointment =>
            appointment.Published > yesterday
            && appointment.Published < tomorrow)
        .ToList()
        .OrderBy(appointment => appointment.Published)
        .ToList();

    log.Info($"{pickedAppointments.Count()} appointments picked from table.");

    foreach (var appointment in pickedAppointments)
    {
        log.Info(string.Format("{0} - {1} - {2}", appointment.Published, appointment.Appointed, appointment.Time));
    }

    var todayAppointments = new Queue<Appointment>(pickedAppointments);

    var statistics = new Dictionary<DateTime, AppointmentStatistics>();
    var currentAppointments = new Queue<Appointment>();

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
        var currentTimePeriods = new Dictionary<string, Queue<Tuple<DateTime, DateTime?>>>();

        var lastAppointments = currentAppointments;
        currentAppointments = new Queue<Appointment>();

        while (lastAppointments.Any())
        {
            if (lastAppointments.Peek().Appointed >= currentTime)
            {
                IncreaseValidAppointments(lastAppointments.Peek(), ref currentStatistic);
                PutCurrentPeriodInToQueue(lastAppointments.Peek(), ref currentTimePeriods);
                currentAppointments.Enqueue(lastAppointments.Peek());
            }

            lastAppointments.Dequeue();
        }

        while (todayAppointments.Any() && todayAppointments.Peek().Published < nextPeriod)
        {
            if (todayAppointments.Peek().Published >= currentTime)
            {
                IncreasePublishAppointments(todayAppointments.Peek(), ref currentStatistic);
            }

            if (todayAppointments.Peek().Appointed >= currentTime)
            {
                IncreaseValidAppointments(todayAppointments.Peek(), ref currentStatistic);
                PutCurrentPeriodInToQueue(todayAppointments.Peek(), ref currentTimePeriods);
                currentAppointments.Enqueue(todayAppointments.Peek());
            }

            todayAppointments.Dequeue();
        }

        if (currentTimePeriods.Any())
        {
            CalculateTotalContinuous(currentStatistic, currentTimePeriods);
            statistics[currentTime] = currentStatistic;
        }
    }

    foreach (var statistic in statistics.Values)
    {
        log.Info($"{statistic.PartitionKey} - {statistic.RowKey}");
        appointmentStatisticsTable.Execute(TableOperation.Insert(statistic));
    }
}


public static void PutCurrentPeriodInToQueue(Appointment appointment, ref Dictionary<string, Queue<Tuple<DateTime, DateTime?>>> currentTimePeriods)
{
    var currentPeriod = new Tuple<DateTime, DateTime?>(appointment.Published, appointment.Appointed);
    var key = $"{appointment.Type}{appointment.Category}{appointment.SubCategory}";
    Queue<Tuple<DateTime, DateTime?>> queue;

    if (!currentTimePeriods.TryGetValue(key, out queue))
    {
        queue = new Queue<Tuple<DateTime, DateTime?>>();
        currentTimePeriods.Add(key, queue);
    }

    queue.Enqueue(currentPeriod);
}

public static void CalculateTotalContinuous(AppointmentStatistics statistics, Dictionary<string, Queue<Tuple<DateTime, DateTime?>>> currentTimePeriods)
{
    statistics.TotalContinuousIRPAllNew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.All}{SubCategories.New}", statistics);
    statistics.TotalContinuousIRPAllRenew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.All}{SubCategories.Renewal}", statistics);
    
    statistics.TotalContinuousIRPWorkNew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.Work}{SubCategories.New}", statistics);
    statistics.TotalContinuousIRPWorkRenew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.Work}{SubCategories.Renewal}", statistics);
    statistics.TotalContinuousIRPStudyNew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.Study}{SubCategories.New}", statistics);
    statistics.TotalContinuousIRPStudyRenew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.Study}{SubCategories.Renewal}", statistics);
    statistics.TotalContinuousIRPOtherNew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.Other}{SubCategories.New}", statistics);
    statistics.TotalContinuousIRPOtherRenew = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.IRP}{Categories.Other}{SubCategories.Renewal}", statistics);
    
    statistics.TotalContinuousVisaIndividual = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.Visa}{Categories.Individual}", statistics);
    statistics.TotalContinuousVisaFamily = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.Visa}{Categories.Family}", statistics);
    statistics.TotalContinuousVisaEmergency = GetCalculateTotalContinuous(currentTimePeriods, $"{AppointmentTypes.Visa}{Categories.Emergency}", statistics);
}

public static double GetCalculateTotalContinuous(Dictionary<string, Queue<Tuple<DateTime, DateTime?>>> currentTimePeriods, string key, AppointmentStatistics statistics)
{
    var start = statistics.StartTime;
    var end = start;
    var totalContinuous = 0d;
    Queue<Tuple<DateTime, DateTime?>> queue;
    
    if (currentTimePeriods.TryGetValue(key, out queue))
    {
        while(queue.Any())
        {
            var period = queue.Dequeue();

            if (period.Item1 > end)
            {
                totalContinuous += (end - start).TotalSeconds;
                start = period.Item1;
            }

            var periodEnd = period.Item2 == null || period.Item2 > statistics.EndTime
                ? statistics.EndTime
                : period.Item2.Value;

            if (periodEnd > end)
            {
                end = periodEnd;
            }
        }
        
        totalContinuous += (end - start).TotalSeconds;
    }
    
    return totalContinuous;
}

public static void IncreaseValidAppointments(Appointment appointment, ref AppointmentStatistics statistic)
{
    switch (appointment.Category)
    {
        case Categories.Work:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.ValidIRPWorkNew++;
            }
            else
            {
                statistic.ValidIRPWorkRenew++;
            }
            break;
        case Categories.All:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.ValidIRPAllNew++;
            }
            else
            {
                statistic.ValidIRPAllRenew++;
            }
            break;
        case Categories.Study:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.ValidIRPStudyNew++;
            }
            else
            {
                statistic.ValidIRPStudyRenew++;
            }
            break;
        case Categories.Other:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.ValidIRPOtherNew++;
            }
            else
            {
                statistic.ValidIRPOtherRenew++;
            }
            break;
        case Categories.Family:
            statistic.ValidVisaFamily++;
            break;
        case Categories.Individual:
            statistic.ValidVisaIndividual++;
            break;
        case Categories.Emergency:
            statistic.ValidVisaEmergency++;
            break;
    }
}

public static void IncreasePublishAppointments(Appointment appointment, ref AppointmentStatistics statistic)
{
    switch (appointment.Category)
    {
        case Categories.All:
            if (appointment.SubCategory == SubCategories.New)
            {
                statistic.PublishIRPAllNew++;
            }
            else
            {
                statistic.PublishIRPAllRenew++;
            }
            break;
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