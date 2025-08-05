namespace ImperialBackend.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency
/// </summary>
public sealed class Money : IEquatable<Money>
{
    /// <summary>
    /// Initializes a new instance of the Money class
    /// </summary>
    /// <param name="amount">The monetary amount</param>
    /// <param name="currency">The currency code (ISO 4217)</param>
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code", nameof(currency));

        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Gets the monetary amount
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency code
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Creates a new Money instance with USD currency
    /// </summary>
    /// <param name="amount">The amount in USD</param>
    /// <returns>A new Money instance</returns>
    public static Money Usd(decimal amount) => new(amount, "USD");

    /// <summary>
    /// Creates a new Money instance with EUR currency
    /// </summary>
    /// <param name="amount">The amount in EUR</param>
    /// <returns>A new Money instance</returns>
    public static Money Eur(decimal amount) => new(amount, "EUR");

    /// <summary>
    /// Creates a new Money instance with GBP currency
    /// </summary>
    /// <param name="amount">The amount in GBP</param>
    /// <returns>A new Money instance</returns>
    public static Money Gbp(decimal amount) => new(amount, "GBP");

    /// <summary>
    /// Adds two Money instances of the same currency
    /// </summary>
    /// <param name="left">The first Money instance</param>
    /// <param name="right">The second Money instance</param>
    /// <returns>A new Money instance with the sum</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>
    /// Subtracts two Money instances of the same currency
    /// </summary>
    /// <param name="left">The first Money instance</param>
    /// <param name="right">The second Money instance</param>
    /// <returns>A new Money instance with the difference</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>
    /// Multiplies a Money instance by a scalar value
    /// </summary>
    /// <param name="money">The Money instance</param>
    /// <param name="multiplier">The multiplier</param>
    /// <returns>A new Money instance with the product</returns>
    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    /// <summary>
    /// Divides a Money instance by a scalar value
    /// </summary>
    /// <param name="money">The Money instance</param>
    /// <param name="divisor">The divisor</param>
    /// <returns>A new Money instance with the quotient</returns>
    public static Money operator /(Money money, decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");

        return new Money(money.Amount / divisor, money.Currency);
    }

    /// <summary>
    /// Determines whether two Money instances are equal
    /// </summary>
    /// <param name="left">The first Money instance</param>
    /// <param name="right">The second Money instance</param>
    /// <returns>True if the instances are equal, false otherwise</returns>
    public static bool operator ==(Money? left, Money? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two Money instances are not equal
    /// </summary>
    /// <param name="left">The first Money instance</param>
    /// <param name="right">The second Money instance</param>
    /// <returns>True if the instances are not equal, false otherwise</returns>
    public static bool operator !=(Money? left, Money? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    /// Determines whether this Money instance is equal to another
    /// </summary>
    /// <param name="other">The other Money instance</param>
    /// <returns>True if the instances are equal, false otherwise</returns>
    public bool Equals(Money? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Amount == other.Amount && Currency == other.Currency;
    }

    /// <summary>
    /// Determines whether this Money instance is equal to another object
    /// </summary>
    /// <param name="obj">The other object</param>
    /// <returns>True if the objects are equal, false otherwise</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Money);
    }

    /// <summary>
    /// Gets the hash code for this Money instance
    /// </summary>
    /// <returns>The hash code</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    /// <summary>
    /// Returns a string representation of this Money instance
    /// </summary>
    /// <returns>A string representation</returns>
    public override string ToString()
    {
        return $"{Amount:F2} {Currency}";
    }
}