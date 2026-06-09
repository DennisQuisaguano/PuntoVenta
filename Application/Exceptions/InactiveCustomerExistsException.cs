namespace Application.Exceptions;

public class InactiveCustomerExistsException : Exception
{
    public int CustomerId { get; }
    public string IDCard { get; }

    public InactiveCustomerExistsException(int customerId, string idCard) 
        : base($"El cliente con cédula {idCard} ya existe pero está inactivo.")
    {
        CustomerId = customerId;
        IDCard = idCard;
    }
}
