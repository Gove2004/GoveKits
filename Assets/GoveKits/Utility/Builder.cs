using System;
using System.Collections.Generic;
using System.Linq;


public class Builder<T> where T : new()
{
    protected Dictionary<string, object> attributes = new();
    private Dictionary<string, Func<object, bool>> validators = new();
    private Dictionary<string, object> defaults = new();
    protected T target = new();

    public Builder<T> WithAttr(string name, object value)
    {
        attributes[name] = value;
        return this;
    }

    public Builder<T> WithValidator(string name, Func<object, bool> validator)
    {
        validators[name] = validator;
        return this;
    }

    public Builder<T> WithDefault(string name, object defaultValue)
    {
        defaults[name] = defaultValue;
        return this;
    }

    public T Build()
    {
        // Apply defaults
        foreach (var pair in defaults)
        {
            if (!attributes.ContainsKey(pair.Key))
            {
                attributes[pair.Key] = pair.Value;
            }
        }
        // Validate
        foreach (var pair in validators)
        {
            if (attributes.ContainsKey(pair.Key) && !pair.Value(attributes[pair.Key]))
            {
                throw new ArgumentException($"Validation failed for attribute {pair.Key}");
            }
        }
        // Set properties using reflection
        var properties = typeof(T).GetProperties();
        foreach (var pair in attributes)
        {
            var property = properties.FirstOrDefault(p => p.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, pair.Value);
            }
        }
        return target;
    }
}


// // Example usage
// public class UserProfile
// {
//     public string Name { get; set; }
//     public int Age { get; set; }
//     public string Email { get; set; }
//     public bool IsAdmin { get; set; }

//     public override string ToString()
//     {
//         return $"UserProfile(Name={Name}, Age={Age}, Email={Email}, IsAdmin={IsAdmin})";
//     }
// }

// class Program
// {
//     static void Main(string[] args)
//     {
//         try
//         {
//             // Basic builder
//             Console.WriteLine("=== Basic Builder ===");
//             var user1 = new GenericBuilder<UserProfile>()
//                 .WithAttr("Name", "Alice")
//                 .WithAttr("Age", 30)
//                 .WithAttr("Email", "alice@example.com")
//                 .Build();
//             Console.WriteLine(user1);

//             // Advanced builder
//             Console.WriteLine("\n=== Advanced Builder ===");
//             var user2 = new AdvancedBuilder<UserProfile>()
//                 .WithDefault("IsAdmin", false)
//                 .WithValidator("Age", val => (int)val >= 18)
//                 .WithValidator("Email", val => ((string)val).Contains("@"))
//                 .WithAttr("Name", "Bob")
//                 .WithAttr("Age", 25)
//                 .WithAttr("Email", "bob@example.com")
//                 .Build();
//             Console.WriteLine(user2);

//             // Test validation failure
//             Console.WriteLine("\n=== Testing Validation ===");
//             try
//             {
//                 var user3 = new AdvancedBuilder<UserProfile>()
//                     .WithAttr("Age", 15)  // Under 18
//                     .WithAttr("Email", "invalid-email")  // No @ symbol
//                     .Build();
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Error: {ex.Message}");
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Unexpected error: {ex.Message}");
//         }
//     }
// }
