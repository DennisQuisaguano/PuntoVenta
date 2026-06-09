namespace Domain.ValueObjects;

public record class Price
{
    public decimal Worth { get; init; }

    public Price(decimal worth)
    {

        if (worth < 0)
        {
            throw new ArgumentException("El precio no puede ser negativo");
        }

        Worth = worth;

    }

    public static Price operator *(Price price, int quantity) => new(price.Worth * quantity);
    public static implicit operator decimal(Price price) => price.Worth;
}
