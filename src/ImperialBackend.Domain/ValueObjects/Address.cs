namespace ImperialBackend.Domain.ValueObjects;

/// <summary>
/// Represents an address value object
/// </summary>
public sealed class Address : IEquatable<Address>
{
    /// <summary>
    /// Initializes a new instance of the Address class
    /// </summary>
    /// <param name="street">The street address</param>
    /// <param name="city">The city</param>
    /// <param name="state">The state or province</param>
    /// <param name="postalCode">The postal code</param>
    /// <param name="country">The country</param>
    public Address(string street, string city, string state, string postalCode, string country)
    {
        Street = ValidateAndTrimString(street, nameof(street), 200);
        City = ValidateAndTrimString(city, nameof(city), 100);
        State = ValidateAndTrimString(state, nameof(state), 100);
        PostalCode = ValidateAndTrimString(postalCode, nameof(postalCode), 20);
        Country = ValidateAndTrimString(country, nameof(country), 100);
    }

    /// <summary>
    /// Gets the street address
    /// </summary>
    public string Street { get; }

    /// <summary>
    /// Gets the city
    /// </summary>
    public string City { get; }

    /// <summary>
    /// Gets the state or province
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the postal code
    /// </summary>
    public string PostalCode { get; }

    /// <summary>
    /// Gets the country
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Gets the full address as a formatted string
    /// </summary>
    public string FullAddress => $"{Street}, {City}, {State} {PostalCode}, {Country}";

    /// <summary>
    /// Determines whether two Address instances are equal
    /// </summary>
    /// <param name="left">The first Address instance</param>
    /// <param name="right">The second Address instance</param>
    /// <returns>True if the instances are equal, false otherwise</returns>
    public static bool operator ==(Address? left, Address? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two Address instances are not equal
    /// </summary>
    /// <param name="left">The first Address instance</param>
    /// <param name="right">The second Address instance</param>
    /// <returns>True if the instances are not equal, false otherwise</returns>
    public static bool operator !=(Address? left, Address? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Determines whether this Address instance is equal to another
    /// </summary>
    /// <param name="other">The other Address instance</param>
    /// <returns>True if the instances are equal, false otherwise</returns>
    public bool Equals(Address? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Street == other.Street &&
               City == other.City &&
               State == other.State &&
               PostalCode == other.PostalCode &&
               Country == other.Country;
    }

    /// <summary>
    /// Determines whether this Address instance is equal to another object
    /// </summary>
    /// <param name="obj">The other object</param>
    /// <returns>True if the objects are equal, false otherwise</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Address);
    }

    /// <summary>
    /// Gets the hash code for this Address instance
    /// </summary>
    /// <returns>The hash code</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Street, City, State, PostalCode, Country);
    }

    /// <summary>
    /// Returns a string representation of this Address instance
    /// </summary>
    /// <returns>A string representation</returns>
    public override string ToString()
    {
        return FullAddress;
    }

    private static string ValidateAndTrimString(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be empty", paramName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{paramName} cannot exceed {maxLength} characters", paramName);

        return trimmed;
    }
}