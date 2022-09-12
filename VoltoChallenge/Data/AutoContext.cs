using System;
using Microsoft.EntityFrameworkCore;
using VoltoChallenge.Models;

namespace VoltoChallenge.Data
{
    public class AutoContext: DbContext
    {
        public AutoContext(DbContextOptions<AutoContext> options) : base(options)
        {
        }

        public DbSet<Auto> Auto => Set<Auto>();
        public DbSet<RegistroEntradas> RegistroEntradas => Set<RegistroEntradas>();
        public DbSet<TipoCliente> TipoCliente => Set<TipoCliente>();
    }
}

