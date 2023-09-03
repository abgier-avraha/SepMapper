namespace SepMapper;

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using nietras.SeparatedValues;


public interface ISepMapperContext
{
    public SepMapperRulesForType<T> RegisterClass<T>() where T : class;
    public void UnregisterClass<T>() where T : class;
    public string Serialize<T>(T anyObject) where T : class;
    public List<T> Parse<T>(TextReader csv) where T : class;
    public List<T> Parse<T>(Stream csv) where T : class;
    public List<T> Parse<T>(string csv) where T : class;

}

public class SepMapperContext : ISepMapperContext
{
    private readonly Dictionary<Type, dynamic> mappers = new();

    public SepMapperRulesForType<T> RegisterClass<T>() where T : class
    {
        var typeMapper = new SepMapperRulesForType<T>();

        this.mappers.Add(typeof(T), typeMapper);
        return typeMapper;
    }

    public void UnregisterClass<T>() where T : class
    {
        this.mappers.Remove(typeof(T));
    }

    public string Serialize<T>(T anyObject) where T : class
    {
        // TODO: use mapper else default behaviour
        return "";
    }

    public void Write<T>(List<T> instances, Stream text) where T : class
    {
        using var writer = Sep.Writer().To(text);
        this.Write<T>(instances, writer);
    }

    public void Write<T>(List<T> instance, TextWriter text) where T : class
    {
        using var writer = Sep.Writer().To(text);
        this.Write<T>(instance, writer);
    }

    public List<T> Parse<T>(TextReader csv) where T : class
    {
        using var reader = Sep.Reader().From(csv);

        return this.Parse<T>(reader);
    }

    public List<T> Parse<T>(Stream csv) where T : class
    {
        using var reader = Sep.Reader().From(csv);

        return this.Parse<T>(reader);
    }

    public List<T> Parse<T>(string csv) where T : class
    {
        using var reader = Sep.Reader().FromText(csv);

        return this.Parse<T>(reader);
    }

    private void Write<T>(List<T> instances, SepWriter writer) where T : class
    {
        var mapper = this.GetMapper<T>();

        foreach (var instance in instances)
        {
            // TODO: get all rules
            // TODO: get all columns from rules
            // TODO: call the accessor to get the col val
            // TODO: if not call call to CSV to transform
            // TODO: write row 
            // using var writeRow = writer.NewRow(readRow);
        }
    }

    private List<T> Parse<T>(SepReader reader) where T : class
    {
        var model = (T)Activator.CreateInstance(typeof(T), new object[] { })!;

        if (!this.mappers.ContainsKey(typeof(T)))
        {
            throw new Exception($"No mapping class registered for type: {typeof(T)}");
        }

        var mapper = this.GetMapper<T>();

        var list = new List<T>();

        foreach (var readRow in reader)
        {
            foreach (var col in reader.Header.ColNames)
            {
                // TODO: dynamic generic param
                var rule = mapper.GetRule(col);
                if (rule?.Accessor is not null)
                {
                    var setter = GetSetter(rule.Accessor);

                    if (rule.ToProperty != null)
                    {
                        // Parse with ToProperty transformation
                        setter.Invoke(model, rule.ToProperty(readRow[col].ToString()));
                        list.Add(model);
                    }
                    else
                    {
                        // Default parse
                        setter.Invoke(model, readRow[col].ToString());
                        list.Add(model);
                    }
                }
            }
        }

        return list;
    }


    private SepMapperRulesForType<T> GetMapper<T>() where T : class
    {
        return this.mappers[typeof(T)];
    }

    private static Action<T, TProperty> GetSetter<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var memberExpression = (MemberExpression)expression.Body;
        var property = (PropertyInfo)memberExpression.Member;
        var setMethod = property.GetSetMethod();

        var parameterT = Expression.Parameter(typeof(T), "x");
        var parameterTProperty = Expression.Parameter(typeof(TProperty), "y");

        var newExpression =
            Expression.Lambda<Action<T, TProperty>>(
                Expression.Call(parameterT, setMethod!, parameterTProperty),
                parameterT,
                parameterTProperty
            );

        return newExpression.Compile();
    }
}

public class SepMapperRulesForType<T> where T : class
{
    // TODO: some form of type erasure SepMapperTypeRulesForProperty<T, P>
    private readonly Dictionary<string, dynamic> rules = new();

    public SepMapperRulesForType<T> AddRule<P>(string key, Expression<Func<T, P>> accessor, [Optional] Func<string, P> toProperty, [Optional] Func<P, string> toCsv)
    {
        var propertyRules = new SepMapperTypeRulesForProperty<T, P>(accessor)
        {
            ToProperty = toProperty,
            ToCsv = toCsv,
        };
        this.rules.Add(key, propertyRules);
        return this;
    }

    public dynamic? GetRule(string key)
    {
        if (this.rules.ContainsKey(key))
        {
            // TODO: some form of type erasure
            return this.rules[key];
        }
        return null;
    }
}

public class SepMapperTypeRulesForProperty<T, P> where T : class
{
    public SepMapperTypeRulesForProperty(Expression<Func<T, P>> Accessor)
    {
        this.Accessor = Accessor;
    }

    public Expression<Func<T, P>> Accessor;
    public Func<string, P>? ToProperty;
    public Func<P, string>? ToCsv;
}