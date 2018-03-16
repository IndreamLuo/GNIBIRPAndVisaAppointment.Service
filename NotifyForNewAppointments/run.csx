#r "Microsoft.Azure.Documents.Client"
using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;

public static async Task Run(IReadOnlyList<Document> documents, TraceWriter log, IEnumerable<dynamic> lastTwoAppointments)
{
    log.Info("Start");
    var newAppointments = lastTwoAppointments.First();
    var lastAppointments = lastTwoAppointments.Skip(1).FirstOrDefault();
    
    if (documents != null && documents.Count > 0 && documents[0].Id == newAppointments.id)
    {
        log.Info("Last appointments update is triggered.");

        var newAppointmentList = (newAppointments
            .Appointments as IEnumerable<object>)
            .Cast<dynamic>()
            .OrderBy(appointment => appointment.Type)
            .ThenBy(appointment => appointment.Category)
            .ThenBy(appointment => appointment.SubCategory);
        var lastAppointmentsList = (lastAppointments?
            .Appointments as IEnumerable<object>)?
            .Cast<dynamic>()
            .OrderBy(appointment => appointment.Type)
            .ThenBy(appointment => appointment.Category)
            .ThenBy(appointment => appointment.SubCategory);

        log.Info("Listed apointments.");
        log.Info($"New appointments({newAppointmentList.Count()}), last appointments({lastAppointmentsList?.Count() ?? -1}).");

        var newOptions = newAppointmentList.SelectMany(appointment =>
            {
                var index = -1;
                return (appointment
                    .TimeRanges as IEnumerable<object>)
                    .Cast<dynamic>()
                    .Select(timeRange =>
                    {
                        index++;
                        return new AppointmentOption
                        {
                            Type = appointment.Type,
                            Category = appointment.Category,
                            SubCategory = appointment.SubCategory,
                            TimeRange = new TimeRange
                            {
                                Time = DateTime.Parse(timeRange.Time.ToString()),
                                Expiration = timeRange.Expirations == null ? null : DateTime.Parse(timeRange.Expirations.ToString())
                            }
                        };
                    });
            });

        log.Info("New appointments flatted.");

        var lastOptions = lastAppointmentsList?.SelectMany(appointment =>
            {
                var index = -1;
                return (appointment
                    .TimeRanges as IEnumerable<object>)
                    .Cast<dynamic>()
                    .Select(timeRange =>
                    {
                        index++;
                        return new AppointmentOption
                        {
                            Type = appointment.Type,
                            Category = appointment.Category,
                            SubCategory = appointment.SubCategory,
                            TimeRange = new TimeRange
                            {
                                Time = DateTime.Parse(timeRange.ToString()),
                                Expiration = timeRange.Expirations == null ? null : DateTime.Parse(timeRange.Expirations.ToString())
                            }
                        };
                    });
            });

        log.Info("Last appointments flatted.");
        log.Info($"Last appointments is null({lastAppointmentsList == null})");
        log.Info($"New options({newOptions.Count()}), last options({(lastOptions?.Count() ?? -1)}).");

        var updatedOptions = lastAppointmentsList == null ? newOptions : newOptions.Except(lastOptions, new AppointmentOptionComparer()).ToArray();

        log.Info("Filterred replicated options.");
        if (updatedOptions != null)
        {
            foreach (var option in updatedOptions)
            {
                log.Info($"Option-Updated: {option.Type}/{option.Category}/{option.SubCategory} {option.TimeRange.Time}-{option.TimeRange.Expiration}");
            }
        }
        else
        {
            log.Info("No update.");
        }
    }
    else
    {
        log.Info("No updates.");
        log.Info($"documents != null({documents != null}), documents.Count > 0({documents.Count > 0}), documents[0].Id({documents[0].Id}) == newAppointments.Id({newAppointments.id})({documents[0].Id == newAppointments.Id})");
    }
}

public class AppointmentOption
{
    public string Type { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public TimeRange TimeRange { get; set; }
}

public class TimeRange
{
    public DateTime Time { get; set; }
    public DateTime? Expiration { get; set; }
}

public class AppointmentOptionComparer : IEqualityComparer<AppointmentOption>
{
    public bool Equals(AppointmentOption left, AppointmentOption right)
    {
        return left.Type == right.Type
        && left.Category == right.Category
        && left.SubCategory == right.SubCategory
        && left.TimeRange.Time == right.TimeRange.Time
        && left.TimeRange.Expiration == right.TimeRange.Expiration;
    }

    public int GetHashCode(AppointmentOption obj)
    {
        return $"New Appointment: {obj.Type}/{obj.Category}/{obj.SubCategory} {obj.TimeRange.Time}-{obj.TimeRange.Expiration}".GetHashCode();
    }
}