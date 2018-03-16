#r "Microsoft.Azure.Documents.Client"
using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;

public static async Task Run(IReadOnlyList<Document> documents, TraceWriter log, IEnumerable<dynamic> lastTwoAppointments)
{
    log.Info("Start");
    var newAppointments = lastTwoAppointments.First();
    var lastAppointments = lastTwoAppointments.Skip(1).FirstOrDefault();

    if (documents != null && documents.Count > 0 && documents[0].Id == newAppointments.Id)
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

        var newOptions = newAppointmentList
            .SelectMany(appointment =>
            {
                var index = -1;
                return (appointment
                    .Times as IEnumerable<object>)
                    .Cast<dynamic>()
                    .Select(time =>
                    {
                        index++;
                        return new AppointmentOption
                        {
                            Type = appointment.Type,
                            Category = appointment.Category,
                            SubCategory = appointment.SubCategory,
                            Time = DateTime.Parse(time),
                            Expiration = appointment.Expirations[index] == null ? null : DateTime.Parse(appointment.Expirations[index])
                        };
                    });
            });

        var lastOptions = lastAppointmentsList?
            .SelectMany(appointment =>
            {
                var index = -1;
                return (appointment
                    .Times as IEnumerable<object>)
                    .Cast<dynamic>()
                    .Select(time =>
                    {
                        index++;
                        return new AppointmentOption
                        {
                            Type = appointment.Type,
                            Category = appointment.Category,
                            SubCategory = appointment.SubCategory,
                            Time = DateTime.Parse(time),
                            Expiration = appointment.Expirations[index] == null ? null : DateTime.Parse(appointment.Expirations[index])
                        };
                    });
            });

        var updatedOptions = lastAppointmentsList == null ? newOptions : newOptions.Except(lastOptions);

        foreach (var option in updatedOptions)
        {
            log.Info($"{option.Type}{option.Category}{option.SubCategory}{option.Time}{option.Expiration}");
        }
    }
}

public class AppointmentOption
{
    public string Type { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public DateTime Time { get; set; }
    public DateTime? Expiration { get; set; }
}

public class AppointmentOptionComparer : IEqualityComparer<AppointmentOption>
{
    public bool Equals(AppointmentOption left, AppointmentOption right)
    {
        return left.GetHashCode() == right.GetHashCode();
    }

    public int GetHashCode(AppointmentOption obj)
    {
        return $"New Appointment: {obj.Type}/{obj.Category}/{obj.SubCategory} {obj.Time}-{obj.Expiration}".GetHashCode();
    }
}