namespace Righthand.RetroDbgDataProvider.KickAssembler.Services.CompletionOptionCollectors;

public static class ArrayContentExtractor
{
    /// <summary>
    /// Extracts last value from text array, such as from "value1, value2, value3" it would extract value3.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    internal static string FindItemWithinTextArray(ReadOnlySpan<char> line)
    {
        int lastComma = line.LastIndexOf(',');
        if (lastComma >= 0)
        {
            return line[lastComma..].Trim().ToString();
        }

        return line.Trim().ToString();
    }
    /// <summary>
    /// Extract key-value pairs from incomplete <param name="text" /> with some tolerance.
    /// </summary>
    /// <param name="text">A key-value pairs as text, optionally ends with ]</param>
    /// <returns></returns>
    internal static ImmutableArray<KeyValuePair<string, string>> Extract(ReadOnlySpan<char> text)
    {
        var state = State.PreName;
        var result = new List<KeyValuePair<string, string>>();
        int start = -1;
        string? name = null;
        int i;
        for (i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (state)
            {
                case State.PreName:
                    if (char.IsLetterOrDigit(c))
                    {
                        state = State.Name;
                        start = i;
                    }
                    else if (c is not (' ' or '\t'))
                    {
                        return Finish(text);
                    }

                    break;
                case State.Name:
                    if (c == '=')
                    {
                        name = text[start..i].ToString();
                        state = State.PreValue;
                        start = -1;
                    }
                    else if (c == ',')
                    {
                        name = text[start..i].ToString();
                        result.Add(new (name!, string.Empty));
                        state = State.PreName;
                        start = -1;
                    }
                    else if (c is ']')
                    {
                        return Finish(text);
                    }
                    else if (c is (' ' or '\t'))
                    {
                        name = text[start..i].ToString();
                        state = State.PreEqual;
                        start = -1;
                    }

                    break;
                case State.PreEqual:
                    if (c == '=')
                    {
                        state = State.PreValue;
                    }
                    else if (c is ',')
                    {
                        name = text[start..i].ToString();
                        result.Add(new (name!, string.Empty));
                        state = State.PreName;
                    }
                    else
                    {
                        return Finish(text);
                    }
                    break;
                case State.PreValue:
                    if (c is ']')
                    {
                        return [..result];
                    }
                    else if (c == ',')
                    {
                        state = State.PreName;
                        result.Add(new(name!, string.Empty));
                    }
                    else if (c == '"')
                    {
                        start = i;
                        state = State.StringValue;
                    }
                    else if (c is not (' ' or '\t'))
                    {
                        start = i;
                        state = State.Value;
                    }
                    break;
                case State.Value:
                    if (c is (' ' or '\t'))
                    {
                        state = State.Comma;
                        string value = text[start..i].ToString();
                        result.Add(new (name!, value));
                    }
                    else if (c == ',')
                    {
                        state = State.PreName;
                        string value = text[start..i].ToString();
                        result.Add(new (name!, value));
                    }
                    else if (c == ']')
                    {
                        string value = text[start..i].ToString();
                        result.Add(new (name!, value));
                        return [..result];
                    }
                    break;
                case State.StringValue:
                    if (c == '"')
                    {
                        state = State.Comma;
                        string value = text[start..i].ToString();
                        result.Add(new (name!, value));
                        start = -1;
                    }
                    break;
            }
        }
        return Finish(text);
        
        // ReSharper disable once VariableHidesOuterVariable
        ImmutableArray<KeyValuePair<string, string>> Finish(ReadOnlySpan<char> text)
        {
            switch (state)
            {
                case State.StringValue:
                case State.Value:
                    string value = text[start..i].ToString();
                    result.Add(new (name!, value));
                    break;
                case State.Name:
                    result.Add(new (text[start..i].ToString(), string.Empty));
                    break;
                case State.PreValue:
                    result.Add(new (name!, string.Empty));
                    break;
            }
            return [..result];
        }

    }

    private enum State
    {
        PreName,
        Name,
        PreEqual,
        PreValue,
        Value,
        StringValue,
        Comma
    }
}