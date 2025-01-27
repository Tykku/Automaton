namespace Automaton.FeaturesSetup.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandHandlerAttribute(string[] commands, string helpMessage, string? configFieldName = null) : Attribute
{
    public string[] Commands { get; } = commands;
    public string HelpMessage { get; } = helpMessage;
    public string? ConfigFieldName { get; } = configFieldName;

    public CommandHandlerAttribute(string command, string helpMessage, string? configFieldName = null) : this([command], helpMessage, configFieldName) { }
}
