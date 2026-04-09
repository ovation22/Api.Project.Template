using RabbitMQ.Client;

namespace Api.Project.Template.Infrastructure.Messaging.RabbitMq;

/// <summary>
/// Parses RabbitMQ connection strings into a <see cref="ConnectionFactory"/>.
/// Supports both URI format (amqp://user:pass@host:port/vhost) and
/// key-value format (Host=localhost;Username=guest;Password=guest).
/// </summary>
internal static class RabbitMqConnectionStringParser
{
    public static ConnectionFactory Parse(string connectionString)
    {
        var factory = new ConnectionFactory();

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "amqp" || uri.Scheme == "amqps"))
        {
            factory.Uri = uri;
        }
        else
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToLowerInvariant();
                var value = kv[1].Trim();

                switch (key)
                {
                    case "host":
                    case "hostname":
                        factory.HostName = value;
                        break;
                    case "username":
                    case "user":
                        factory.UserName = value;
                        break;
                    case "password":
                    case "pwd":
                        factory.Password = value;
                        break;
                    case "virtualhost":
                    case "vhost":
                        factory.VirtualHost = value;
                        break;
                    case "port":
                        if (int.TryParse(value, out var port))
                            factory.Port = port;
                        break;
                }
            }
        }

        return factory;
    }
}
