using System.Text.Json.Serialization;

namespace Sonorize.Core.Settings;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SidebarPosition
{
    Left,
    Right
}