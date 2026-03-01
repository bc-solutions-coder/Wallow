namespace Foundry.Identity.Infrastructure.Scim;

public enum TokenType
{
    ATTR,
    OP,
    LOGIC,
    LPAREN,
    RPAREN,
    STRING,
    BOOL
}

public sealed record ScimToken(TokenType Type, string Value, int Position);
