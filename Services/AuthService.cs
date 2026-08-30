using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GestionInterventionApi.Data;
using GestionInterventionApi.DTOs.Auth;
using GestionInterventionApi.DTOs.User;
using GestionInterventionApi.Models;
using GestionInterventionApi.Models.Enums;

namespace GestionInterventionApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IMapper mapper)
    {
        _context = context;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return null;
        }

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto)
    {
        // Vérifier si l'email existe déjà
        var emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == registerDto.Email);

        if (emailExists)
        {
            return null;
        }

        // Créer l'organisation
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = registerDto.OrganizationName,
            SubscriptionPlan = SubscriptionPlan.Free
        };

        // Créer l'utilisateur admin
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password),
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Role = UserRole.Admin,
            OrganizationId = organization.Id,
            Organization = organization
        };

        _context.Organizations.Add(organization);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateAuthResponse(user);
    }

    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var token = GenerateJwtToken(user);
        var expireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 60);

        return new AuthResponseDto(
            Token: token,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expireMinutes),
            User: _mapper.Map<UserDto>(user)
        );
    }

    private string GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"]!;
        var issuer = _configuration["Jwt:Issuer"]!;
        var audience = _configuration["Jwt:Audience"]!;
        var expireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 60);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("OrganizationId", user.OrganizationId.ToString()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations: 100000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32
        );

        var hashBytes = new byte[48];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 32);

        return Convert.ToBase64String(hashBytes);
    }

    // Parametres PBKDF2, nommes une seule fois : HashPassword et VerifyPassword
    // les partageaient auparavant sous forme de litteraux disperses.
    //
    // 100 000 iterations correspondent a la recommandation OWASP de 2021 ; celle
    // de 2023 est de 600 000 pour PBKDF2-HMAC-SHA256. Monter ce nombre invalide
    // les hash existants : la bascule demande une re-derivation a la prochaine
    // connexion reussie, non faite ici (voir README, dette assumee).
    private const int Iterations = 100000;
    private const int TailleSel = 16;
    private const int TailleHash = 32;

    private static bool VerifyPassword(string password, string passwordHash)
    {
        // B-13 (1/2) — un hash absent ou corrompu levait une FormatException
        // remontee en 500 nu. On repond « mot de passe invalide », ce qui est
        // vrai et ne distingue pas un compte casse d'un mot de passe faux.
        byte[] hashBytes;
        try
        {
            hashBytes = Convert.FromBase64String(passwordHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (hashBytes.Length != TailleSel + TailleHash)
        {
            return false;
        }

        var salt = new byte[TailleSel];
        Array.Copy(hashBytes, 0, salt, 0, TailleSel);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: TailleHash
        );

        // B-13 (2/2) — comparaison a temps constant.
        //
        // La boucle precedente sortait au PREMIER octet different : le temps de
        // reponse renseignait sur le nombre d'octets corrects devines, ce qui
        // permet en theorie de reconstruire un hash octet par octet. Le cout de
        // PBKDF2 (100 000 iterations) noie largement cet ecart en pratique, mais
        // une comparaison a temps constant est gratuite — il n'y a aucune raison
        // de laisser la fuite ouverte.
        return CryptographicOperations.FixedTimeEquals(
            hashBytes.AsSpan(TailleSel, TailleHash),
            hash);
    }
}
