using Microsoft.AspNetCore.Identity;

namespace PTRON.Identity;

public sealed class PortugueseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => Error(nameof(DefaultError), "Não foi possível concluir a operação.");

    public override IdentityError ConcurrencyFailure()
        => Error(nameof(ConcurrencyFailure), "Os dados foram alterados por outra operação. Tente novamente.");

    public override IdentityError PasswordMismatch()
        => Error(nameof(PasswordMismatch), "Senha incorreta.");

    public override IdentityError InvalidToken()
        => Error(nameof(InvalidToken), "O link é inválido ou expirou.");

    public override IdentityError LoginAlreadyAssociated()
        => Error(nameof(LoginAlreadyAssociated), "Este login já está associado a uma conta.");

    public override IdentityError InvalidUserName(string? userName)
        => Error(nameof(InvalidUserName), "O e-mail informado é inválido.");

    public override IdentityError InvalidEmail(string? email)
        => Error(nameof(InvalidEmail), "O e-mail informado é inválido.");

    public override IdentityError DuplicateUserName(string userName)
        => Error(nameof(DuplicateUserName), "Este e-mail não está disponível.");

    public override IdentityError DuplicateEmail(string email)
        => Error(nameof(DuplicateEmail), "Este e-mail não está disponível.");

    public override IdentityError UserAlreadyHasPassword()
        => Error(nameof(UserAlreadyHasPassword), "A conta já possui uma senha.");

    public override IdentityError UserLockoutNotEnabled()
        => Error(nameof(UserLockoutNotEnabled), "O bloqueio de conta não está habilitado para este usuário.");

    public override IdentityError PasswordTooShort(int length)
        => Error(nameof(PasswordTooShort), $"A senha deve ter pelo menos {length} caracteres.");

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => Error(nameof(PasswordRequiresNonAlphanumeric), "A senha deve ter pelo menos um caractere especial.");

    public override IdentityError PasswordRequiresDigit()
        => Error(nameof(PasswordRequiresDigit), "A senha deve ter pelo menos um número.");

    public override IdentityError PasswordRequiresLower()
        => Error(nameof(PasswordRequiresLower), "A senha deve ter pelo menos uma letra minúscula.");

    public override IdentityError PasswordRequiresUpper()
        => Error(nameof(PasswordRequiresUpper), "A senha deve ter pelo menos uma letra maiúscula.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => Error(nameof(PasswordRequiresUniqueChars), $"A senha deve ter pelo menos {uniqueChars} caracteres diferentes.");

    private static IdentityError Error(string code, string description)
        => new() { Code = code, Description = description };
}
