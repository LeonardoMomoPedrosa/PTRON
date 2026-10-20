using Microsoft.AspNetCore.Identity;
using PTRON.Localization;

namespace PTRON.Identity;

public sealed class PortugueseIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
        => Error(nameof(DefaultError), StaticText.Get("Não foi possível concluir a operação."));

    public override IdentityError ConcurrencyFailure()
        => Error(nameof(ConcurrencyFailure), StaticText.Get("Os dados foram alterados por outra operação. Tente novamente."));

    public override IdentityError PasswordMismatch()
        => Error(nameof(PasswordMismatch), StaticText.Get("Senha incorreta."));

    public override IdentityError InvalidToken()
        => Error(nameof(InvalidToken), StaticText.Get("O link é inválido ou expirou."));

    public override IdentityError LoginAlreadyAssociated()
        => Error(nameof(LoginAlreadyAssociated), StaticText.Get("Este login já está associado a uma conta."));

    public override IdentityError InvalidUserName(string? userName)
        => Error(nameof(InvalidUserName), StaticText.Get("O e-mail informado é inválido."));

    public override IdentityError InvalidEmail(string? email)
        => Error(nameof(InvalidEmail), StaticText.Get("O e-mail informado é inválido."));

    public override IdentityError DuplicateUserName(string userName)
        => Error(nameof(DuplicateUserName), StaticText.Get("Este e-mail não está disponível."));

    public override IdentityError DuplicateEmail(string email)
        => Error(nameof(DuplicateEmail), StaticText.Get("Este e-mail não está disponível."));

    public override IdentityError UserAlreadyHasPassword()
        => Error(nameof(UserAlreadyHasPassword), StaticText.Get("A conta já possui uma senha."));

    public override IdentityError UserLockoutNotEnabled()
        => Error(nameof(UserLockoutNotEnabled), StaticText.Get("O bloqueio de conta não está habilitado para este usuário."));

    public override IdentityError PasswordTooShort(int length)
        => Error(nameof(PasswordTooShort), StaticText.Format("A senha deve ter pelo menos {0} caracteres.", length));

    public override IdentityError PasswordRequiresNonAlphanumeric()
        => Error(nameof(PasswordRequiresNonAlphanumeric), StaticText.Get("A senha deve ter pelo menos um caractere especial."));

    public override IdentityError PasswordRequiresDigit()
        => Error(nameof(PasswordRequiresDigit), StaticText.Get("A senha deve ter pelo menos um número."));

    public override IdentityError PasswordRequiresLower()
        => Error(nameof(PasswordRequiresLower), StaticText.Get("A senha deve ter pelo menos uma letra minúscula."));

    public override IdentityError PasswordRequiresUpper()
        => Error(nameof(PasswordRequiresUpper), StaticText.Get("A senha deve ter pelo menos uma letra maiúscula."));

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        => Error(nameof(PasswordRequiresUniqueChars), StaticText.Format("A senha deve ter pelo menos {0} caracteres diferentes.", uniqueChars));

    private static IdentityError Error(string code, string description)
        => new() { Code = code, Description = description };
}
