using PMQ.Domain;

namespace PMQ.Domain.Tests;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public Money(decimal amount, string currency)
        {
            if (amount < 0)
                AddNotification(nameof(Amount), "Amount cannot be negative.");

            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }

        public string Currency { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class Weight : ValueObject
    {
        public Weight(decimal amount, string unit)
        {
            Amount = amount;
            Unit = unit;
        }

        public decimal Amount { get; }

        public string Unit { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Unit;
        }
    }

    [Fact]
    public void Equals_WithSameComponents_ShouldBeEqual()
    {
        var first = new Money(10.50m, "BRL");
        var second = new Money(10.50m, "BRL");

        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentComponents_ShouldNotBeEqual()
    {
        new Money(10.50m, "BRL").ShouldNotBe(new Money(10.50m, "USD"));
        new Money(10.50m, "BRL").ShouldNotBe(new Money(99.00m, "BRL"));
    }

    [Fact]
    public void Equals_WithSameComponentsButDifferentType_ShouldNotBeEqual()
    {
        // Money(10, "kg") e Weight(10, "kg") têm componentes idênticos mas não são o mesmo conceito.
        new Money(10m, "kg").Equals(new Weight(10m, "kg")).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldNotBeEqual()
    {
        var money = new Money(10m, "BRL");

        money.Equals(null).ShouldBeFalse();
        (money == null).ShouldBeFalse();
        (money != null).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_WithBothNull_ShouldBeEqual()
    {
        Money? left = null;
        Money? right = null;

        (left == right).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_ShouldDependOnComponentOrder()
    {
        // Componentes trocados descrevem outro valor e não podem colidir por construção.
        new Money(1m, "2").GetHashCode().ShouldNotBe(new Money(2m, "1").GetHashCode());
    }

    [Fact]
    public void ValueObject_ShouldBeValidatable()
    {
        var invalid = new Money(-1m, "BRL");

        invalid.IsInvalid.ShouldBeTrue();
        invalid.ValidationResult.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe("Amount");
    }

    [Fact]
    public void Equals_ShouldIgnoreValidationState()
    {
        // A validação é estado de diagnóstico, não parte da identidade do valor.
        var invalid = new Money(-1m, "BRL");
        var alsoInvalid = new Money(-1m, "BRL");

        invalid.ShouldBe(alsoInvalid);
    }
}
