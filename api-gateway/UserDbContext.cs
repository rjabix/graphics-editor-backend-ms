using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api_gateway;

public class UserDbContext(DbContextOptions<UserDbContext> options) : IdentityDbContext<IdentityUser>(options);